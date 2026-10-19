using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Services;

public class ProfessionalProfileService(ApplicationDbContext db, IFileUploadService files)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static ProfessionalSnapshot Read(string json) => JsonSerializer.Deserialize<ProfessionalSnapshot>(json, Json)!;
    private static string Write(ProfessionalSnapshot snapshot) => JsonSerializer.Serialize(snapshot, Json);
    private static ProfessionalSnapshot Snapshot(DoctorDetail d) => new(d.Degree, d.PracticeStartYear,
        d.ExperienceYears, d.SpecialtyId, d.Specialty.SpecialtyName, d.CertificateUrl, d.Price, d.Bio);
    private static void RequireAdmin(User actor)
    {
        if (actor.Role != RoleStatus.ADMIN) throw new UnauthorizedAccessException("Chỉ quản trị viên được thực hiện thao tác này.");
    }
    private async Task<DoctorDetail> Load(int doctorId) => await db.DoctorDetails.Include(d => d.User)
        .Include(d => d.Specialty).FirstOrDefaultAsync(d => d.DoctorId == doctorId)
        ?? throw new InvalidOperationException("Không tìm thấy hồ sơ bác sĩ.");
    private async Task Lock(DoctorDetail doctor)
    {
        // Same account lock as booking/blocking; no booking may race a specialty/price change.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({-doctor.UserId})");
        await db.Entry(doctor).ReloadAsync();
        doctor.Specialty = await db.Specialties.SingleAsync(s => s.SpecialtyId == doctor.SpecialtyId);
    }
    public async Task<int> OwnDoctorId(User actor) => await db.DoctorDetails.Where(d => d.UserId == actor.UserId)
        .Select(d => (int?)d.DoctorId).SingleOrDefaultAsync()
        ?? throw new InvalidOperationException("Bạn chưa có hồ sơ bác sĩ.");
    public async Task<object> Current(User actor, int doctorId)
    {
        var d = await Load(doctorId);
        if (actor.Role != RoleStatus.ADMIN && d.UserId != actor.UserId) throw new UnauthorizedAccessException();
        return new { d.DoctorId, doctorName = d.User.FullName, currentProfile = Snapshot(d),
            version = d.ProfessionalVersion, verificationStatus = d.VerificationStatus.ToString(), d.IdCardUrl,
            activeAppointments = await ActiveAppointments(doctorId).CountAsync(),
            hasPending = await db.ProfessionalUpdates.AnyAsync(r => r.DoctorId == doctorId && r.Status == "PENDING") };
    }
    private IQueryable<Appointment> ActiveAppointments(int doctorId) => db.Appointments.Where(a => a.Schedule.DoctorId == doctorId &&
        (a.BookingStatus == BookingStatus.PENDING || a.BookingStatus == BookingStatus.CONFIRMED));
    public async Task<PagedResult<object>> List(User actor, int? doctorId, string? status, int page, int size)
    {
        page = Math.Clamp(page, 0, 1_000_000); size = Math.Clamp(size, 1, 50);
        var query = db.ProfessionalUpdates.AsNoTracking().AsQueryable();
        if (actor.Role != RoleStatus.ADMIN) query = query.Where(r => r.Doctor.UserId == actor.UserId);
        if (doctorId.HasValue) query = query.Where(r => r.DoctorId == doctorId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status is not ("PENDING" or "APPROVED" or "REJECTED")) throw new InvalidOperationException("Trạng thái không hợp lệ.");
            query = query.Where(r => r.Status == status);
        }
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(r => r.Id).Skip(page * size).Take(size)
            .Select(r => new { r.Id, r.DoctorId, doctorName = r.Doctor.User.FullName, r.Status, r.AppliedDirectly,
                r.BeforeJson, r.ProposedJson, r.AppliedBeforeJson, r.EvidenceUrl, r.Reason, r.DecisionReason,
                r.CreatedAt, r.ReviewedAt, r.SubmittedByUserId, submittedBy = r.SubmittedByUser.FullName,
                r.ReviewedByUserId, reviewedBy = r.ReviewedByUser == null ? null : r.ReviewedByUser.FullName }).ToListAsync();
        return PagedResult<object>.Create(rows.Select(r => (object)new { r.Id, r.DoctorId, r.doctorName, r.Status,
            r.AppliedDirectly, before = Read(r.BeforeJson), proposed = Read(r.ProposedJson),
            appliedBefore = r.AppliedBeforeJson == null ? null : Read(r.AppliedBeforeJson), r.EvidenceUrl, r.Reason,
            r.DecisionReason, r.CreatedAt, r.ReviewedAt, r.SubmittedByUserId, r.submittedBy, r.ReviewedByUserId, r.reviewedBy }).ToList(), page, size, total);
    }
    public async Task<long> Submit(User actor, int doctorId, ProfessionalUpdateRequest req, bool direct)
    {
        if (direct) RequireAdmin(actor);
        var d = await Load(doctorId);
        if (!direct && (d.UserId != actor.UserId || actor.Role != RoleStatus.DOCTOR)) throw new UnauthorizedAccessException();
        if (string.IsNullOrWhiteSpace(req.Reason) || req.Reason.Trim().Length < 10 || req.Reason.Length > 1000)
            throw new InvalidOperationException("Vui lòng nhập lý do từ 10 đến 1000 ký tự.");
        if (!direct && (req.Price != null || req.Bio != null))
            throw new InvalidOperationException("Giá khám và giới thiệu được cập nhật riêng tại hồ sơ công khai.");
        await using var tx = await db.Database.BeginTransactionAsync();
        await Lock(d);
        if (!direct && d.VerificationStatus != VerificationStatus.APPROVED)
            throw new InvalidOperationException("Chỉ hồ sơ đã được duyệt mới gửi yêu cầu cập nhật chuyên môn.");
        if (req.ExpectedVersion != null && req.ExpectedVersion != d.ProfessionalVersion)
            throw new InvalidOperationException("Hồ sơ đã thay đổi. Vui lòng tải lại và kiểm tra thông tin mới.");
        if (!direct && await db.ProfessionalUpdates.AnyAsync(r => r.DoctorId == doctorId && r.Status == "PENDING"))
            throw new InvalidOperationException("Đã có yêu cầu đang chờ duyệt. Vui lòng chờ kết quả trước khi gửi tiếp.");
        var before = Snapshot(d);
        var degree = req.Degree?.Trim() ?? before.Degree;
        if (degree != before.Degree && !DoctorDegree.IsValid(degree)) throw new InvalidOperationException("Học vị/bằng cấp không hợp lệ.");
        var year = req.PracticeStartYear ?? before.PracticeStartYear;
        if (req.PracticeStartYear is int start && (start < 1900 || start > VietnamTime.Now.Year))
            throw new InvalidOperationException("Năm bắt đầu hành nghề phải từ 1900 đến năm hiện tại.");
        var specialtyId = req.SpecialtyId ?? before.SpecialtyId;
        var specialty = await db.Specialties.FindAsync(specialtyId)
            ?? throw new InvalidOperationException("Chuyên khoa không tồn tại.");
        if (specialtyId != before.SpecialtyId && specialty.IsActive != true)
            throw new InvalidOperationException("Chuyên khoa đã ngừng hoạt động.");
        if (direct && req.Price.HasValue && (!double.IsFinite(req.Price.Value) || req.Price <= 0 || req.Price > 1_000_000_000))
            throw new InvalidOperationException("Giá khám phải từ 1 đến 1.000.000.000 đồng.");
        string? evidence = null, certificate = before.CertificateUrl;
        if (req.EvidenceFile != null) evidence = await Upload(req.EvidenceFile, "Minh chứng");
        if (req.CertificateFile != null) certificate = await Upload(req.CertificateFile, "Chứng chỉ hành nghề");
        if (req.UseExistingCertificate && !string.IsNullOrWhiteSpace(before.CertificateUrl)) evidence ??= before.CertificateUrl;
        evidence ??= req.CertificateFile == null ? null : certificate;
        if (!direct && evidence == null) throw new InvalidOperationException("Vui lòng đính kèm minh chứng hoặc chọn chứng chỉ hiện có.");
        var proposed = new ProfessionalSnapshot(degree, year, before.LegacyExperienceYears, specialtyId,
            specialty.SpecialtyName, certificate, req.Price ?? before.Price, req.Bio ?? before.Bio);
        if (proposed == before) throw new InvalidOperationException("Chưa có thông tin nào thay đổi.");
        var update = new ProfessionalUpdate { DoctorId = doctorId, SubmittedByUserId = actor.UserId,
            BaseVersion = d.ProfessionalVersion, BeforeJson = Write(before), ProposedJson = Write(proposed),
            EvidenceUrl = evidence, Reason = req.Reason.Trim(), AppliedDirectly = direct };
        if (direct)
        {
            await Apply(d, before, proposed);
            update.Status = "APPROVED"; update.ReviewedByUserId = actor.UserId; update.ReviewedAt = DateTime.UtcNow;
            update.DecisionReason = req.Reason.Trim();
            update.AppliedBeforeJson = Write(before);
            Notify(d.UserId, "Thông tin chuyên môn đã được quản trị viên cập nhật", req.Reason.Trim(), "/Doctor/ProfessionalUpdates");
        }
        else
        {
            foreach (var adminId in await db.Users.Where(u => u.Role == RoleStatus.ADMIN && u.IsActive == true).Select(u => u.UserId).ToListAsync())
                Notify(adminId, "Yêu cầu cập nhật chuyên môn", d.User.FullName + " đã gửi yêu cầu cập nhật.", "/Admin/ProfessionalUpdates");
        }
        db.ProfessionalUpdates.Add(update);
        await db.SaveChangesAsync(); await tx.CommitAsync(); return update.Id;
    }
    public async Task Decide(User actor, long id, ProfessionalDecision decision)
    {
        RequireAdmin(actor);
        if (!decision.Approve && (string.IsNullOrWhiteSpace(decision.Reason) || decision.Reason.Trim().Length < 3))
            throw new InvalidOperationException("Từ chối phải có lý do ít nhất 3 ký tự.");
        if (decision.Reason?.Length > 1000) throw new InvalidOperationException("Lý do không được vượt quá 1000 ký tự.");
        var update = await db.ProfessionalUpdates.FindAsync(id) ?? throw new InvalidOperationException("Không tìm thấy yêu cầu.");
        var d = await Load(update.DoctorId);
        await using var tx = await db.Database.BeginTransactionAsync();
        await Lock(d); await db.Entry(update).ReloadAsync();
        if (update.Status != "PENDING") throw new InvalidOperationException("Yêu cầu đã được xử lý.");
        if (decision.Approve)
        {
            if (d.VerificationStatus != VerificationStatus.APPROVED) throw new InvalidOperationException("Hồ sơ chưa được xác minh.");
            if (d.ProfessionalVersion != update.BaseVersion)
                throw new InvalidOperationException("Hồ sơ đã thay đổi sau khi gửi yêu cầu. Hãy từ chối và yêu cầu bác sĩ gửi lại để tránh ghi đè.");
            update.AppliedBeforeJson = Write(Snapshot(d));
            await Apply(d, Read(update.BeforeJson), Read(update.ProposedJson));
        }
        update.Status = decision.Approve ? "APPROVED" : "REJECTED";
        update.DecisionReason = decision.Reason?.Trim(); update.ReviewedByUserId = actor.UserId; update.ReviewedAt = DateTime.UtcNow;
        Notify(d.UserId, decision.Approve ? "Yêu cầu cập nhật chuyên môn đã được duyệt" : "Yêu cầu cập nhật chuyên môn bị từ chối",
            decision.Reason?.Trim() ?? "Thông tin đã được cập nhật; xem lịch sử để biết chi tiết.", "/Doctor/ProfessionalUpdates");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    private async Task Apply(DoctorDetail d, ProfessionalSnapshot before, ProfessionalSnapshot proposed)
    {
        if (before.SpecialtyId != proposed.SpecialtyId)
        {
            if (!await db.Specialties.AnyAsync(s => s.SpecialtyId == proposed.SpecialtyId && s.IsActive == true))
                throw new InvalidOperationException("Chuyên khoa mới đã ngừng hoạt động.");
            if (await ActiveAppointments(d.DoctorId).AnyAsync())
                throw new InvalidOperationException("Bác sĩ còn lịch chờ xác nhận hoặc đã xác nhận. Cần khám xong hoặc hủy các lịch đó trước khi chuyển chuyên khoa.");
            d.SpecialtyId = proposed.SpecialtyId;
        }
        if (before.Degree != proposed.Degree) d.Degree = proposed.Degree;
        if (before.PracticeStartYear != proposed.PracticeStartYear) d.PracticeStartYear = proposed.PracticeStartYear;
        if (before.CertificateUrl != proposed.CertificateUrl) d.CertificateUrl = proposed.CertificateUrl;
        if (before.Price != proposed.Price) d.Price = proposed.Price;
        if (before.Bio != proposed.Bio) d.Bio = proposed.Bio;
        d.ProfessionalVersion++;
    }
    private async Task<string> Upload(IFormFile file, string label)
    {
        files.ValidateFile(file, label, "application/pdf", "image/jpeg", "image/png");
        var url = await files.UploadFileAsync(file);
        if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Không thể tải lên " + label.ToLower() + ". Vui lòng thử lại.");
        return url;
    }
    private void Notify(int userId, string title, string message, string url) => db.Notifications.Add(new Notification
        { UserId = userId, Title = title, Message = message, Url = url });
}
