using WebDeveloper.Moderation;
using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services;

var cases = new (string Text, string? Expected)[] {
    ("địt mẹ mày đồ khốn", "Thô tục"),
    ("Mua ngay sản phẩm giảm giá freeship liên hệ shop", "Quảng cáo"),
    ("Nhấn link nhận quà trúng thưởng https://example.test", "Spam"),
    ("spam spam spam spam spam spam", "Spam"),
    ("Bác sĩ khám kỹ giải thích rõ ràng", null),
    ("Tôi chờ lâu rất thất vọng giá khám quá cao", null),
    ("", null),
    ("zzunknownword", null)
};
foreach (var (text, expected) in cases) {
    var scores = ReviewClassifier.Instance.Classify(text);
    var actual = ReviewClassifier.Suggestion(scores);
    if (actual != expected) throw new Exception($"Expected {expected ?? "normal"}, got {actual ?? "normal"}: {string.Join(", ", scores)}");
    if (Math.Abs(scores.Values.Sum() - 100) > .05) throw new Exception("Scores are not normalized");
}
Console.WriteLine($"PASS: {cases.Length} moderation cases, neutral/negative feedback retained, normalized scores.");
if (Environment.GetEnvironmentVariable("MODERATION_TEST_CONNECTION") is { } connection)
{
    await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options);
    await using var transaction = await db.Database.BeginTransactionAsync();
    var key = Guid.NewGuid().ToString("N");
    var user = new User { Email = key + "@example.test", FullName = key, Role = RoleStatus.DOCTOR, IsActive = true };
    var doctor = new DoctorDetail { User = user, Specialty = new Specialty { SpecialtyName = key, IsActive = true },
        Facility = new Facility { FacilityName = key, Address = "Test", IsActive = true }, VerificationStatus = VerificationStatus.APPROVED };
    var schedule = new DoctorSchedule { Doctor = doctor, DateWorking = DateOnly.FromDateTime(DateTime.UtcNow), TimeSlot = TimeSlot.SLOT_09_00 };
    var appointment = new Appointment { Patient = new PatientProfile { User = user, FullName = key }, Schedule = schedule, BookingStatus = BookingStatus.COMPLETED };
    var review = new Review { Appointment = appointment, Rating = 1, Comment = cases[2].Text, IsVisible = true };
    db.Reviews.Add(review);
    await db.SaveChangesAsync();
    var admin = new AdminService(db, null!, null!); // Read path does not upload files or send email.
    var queue = await admin.GetAllReviews(0, 1, "spam", key);
    if (queue.TotalElements != 1 || queue.Content.Single().ReviewId != review.ReviewId) throw new Exception("Filtered pagination lost matching review");
    if ((await admin.GetAllReviews(0, 1, "advertising", key)).TotalElements != 0) throw new Exception("Wrong queue label");
    if (review.IsVisible != true || review.Rating != 1) throw new Exception("Classification mutated review");
    await transaction.RollbackAsync();
    Console.WriteLine("PASS: PostgreSQL admin queue filters before paging and preserves visibility/stars; fixtures rolled back.");
}
