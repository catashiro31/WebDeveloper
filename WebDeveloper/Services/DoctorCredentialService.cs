using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;
namespace WebDeveloper.Services;
public partial class DoctorService
{
    public async Task<string> CreateCredentialRequest(User user, CredentialChangeRequest req)
    {
        var doctor = await _db.DoctorDetails.SingleOrDefaultAsync(d => d.UserId == user.UserId)
            ?? throw new InvalidOperationException("Không tìm thấy hồ sơ bác sĩ");
        if (doctor.VerificationStatus != VerificationStatus.APPROVED)
            throw new InvalidOperationException("Vui lòng hoàn thiện hồ sơ và chờ duyệt trước khi gửi thay đổi");
        if (req.Kind is not ("DEGREE" or "DIPLOMA" or "CERTIFICATE"))
            throw new InvalidOperationException("Loại yêu cầu không hợp lệ");
        if (await _db.DoctorCredentialRequests.AnyAsync(r => r.DoctorId == doctor.DoctorId && r.Status == TransferStatus.PENDING))
            throw new InvalidOperationException("Bạn đang có yêu cầu thay đổi chuyên môn chờ duyệt");
        var degree = req.Kind == "DEGREE" ? req.Degree?.Trim() : doctor.Degree;
        if (req.Kind != "CERTIFICATE" && !DoctorDegree.IsValid(degree))
            throw new InvalidOperationException("Vui lòng chọn học vị hợp lệ");
        if (req.Document == null || req.Document.Length == 0) throw new InvalidOperationException("Vui lòng đính kèm minh chứng");
        _fileUpload.ValidateFile(req.Document, "Minh chứng", "application/pdf");
        var url = await _fileUpload.UploadFileAsync(req.Document);
        if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Tải minh chứng thất bại");
        _db.DoctorCredentialRequests.Add(new DoctorCredentialRequest {
            DoctorId = doctor.DoctorId, Kind = req.Kind, OldDegree = doctor.Degree, NewDegree = degree,
            OldDocumentUrl = req.Kind == "CERTIFICATE" ? doctor.CertificateUrl : doctor.DiplomaUrl,
            NewDocumentUrl = url, Status = TransferStatus.PENDING, CreatedAt = DateTime.UtcNow
        });
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        { throw new InvalidOperationException("Bạn đang có yêu cầu chờ duyệt"); }
        return "Đã gửi yêu cầu. Hồ sơ đã duyệt vẫn hoạt động bình thường.";
    }

    public async Task<PagedResult<object>> GetCredentialRequests(TransferStatus status, int page, int size)
    {
        page = Math.Max(0, page); size = Math.Clamp(size, 1, 100);
        var query = _db.DoctorCredentialRequests.AsNoTracking().Where(r => r.Status == status);
        var count = await query.CountAsync();
        var rows = await query.OrderByDescending(r => r.Id).Skip(page * size).Take(size).Select(r => new {
            r.Id, DoctorName = r.Doctor.User.FullName, r.Kind, r.OldDegree, r.NewDegree,
            r.OldDocumentUrl, r.NewDocumentUrl, r.CreatedAt, r.ProcessedAt, r.AdminNote
        }).ToListAsync();
        return PagedResult<object>.Create(rows.Cast<object>().ToList(), page, size, count);
    }

    public async Task ProcessCredentialRequest(int id, bool approve, string? adminNote)
    {
        if (!approve && string.IsNullOrWhiteSpace(adminNote))
            throw new InvalidOperationException("Vui lòng nhập lý do từ chối");
        var request = await _db.DoctorCredentialRequests.Include(r => r.Doctor).SingleOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("Không tìm thấy yêu cầu");
        if (request.Status != TransferStatus.PENDING) throw new InvalidOperationException("Yêu cầu đã được xử lý");
        if (approve)
        {
            var doctor = request.Doctor;
            if (doctor.VerificationStatus != VerificationStatus.APPROVED) throw new InvalidOperationException("Hồ sơ bác sĩ chưa được duyệt");
            if (doctor.Degree != request.OldDegree || (request.Kind == "CERTIFICATE" ? doctor.CertificateUrl : doctor.DiplomaUrl) != request.OldDocumentUrl)
                throw new InvalidOperationException("Hồ sơ đã thay đổi. Vui lòng từ chối và yêu cầu gửi lại");
            if (request.Kind == "CERTIFICATE") doctor.CertificateUrl = request.NewDocumentUrl;
            else { doctor.Degree = request.NewDegree; doctor.DiplomaUrl = request.NewDocumentUrl; }
        }
        request.Status = approve ? TransferStatus.APPROVED : TransferStatus.REJECTED;
        request.AdminNote = adminNote?.Trim();
        request.ProcessedAt = DateTime.UtcNow;
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("Yêu cầu đã được quản trị viên khác xử lý. Vui lòng tải lại"); }
    }
}
