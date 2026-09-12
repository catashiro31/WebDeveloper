namespace WebDeveloper.Models.DTOs.Portal
{
    public class DoctorCardResponse
    {
        public int DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? SpecialtyName { get; set; }
        public string? DoctorEmail { get; set; }
        public string? DoctorPhone { get; set; }
        public string? AvatarUrl { get; set; }
        public double? RatingAverage { get; set; }
        public int? TotalReviews { get; set; }
        public double? Price { get; set; }
        public int? Experience { get; set; }
    }

    public class DoctorDetailPublicResponse
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? SpecialtyName { get; set; }
        public int? SpecialtyId { get; set; }
        public string? Degree { get; set; }
        public int? ExperienceYears { get; set; }
        public double? Price { get; set; }
        public string? Bio { get; set; }
        public string? FacilityName { get; set; }
        public string? FacilityAddress { get; set; }
        public string? FacilityProvince { get; set; }
        public string? FacilityMapUrl { get; set; }
        public bool? FacilityVerified { get; set; }
        public string? DoctorEmail { get; set; }
        public string? DoctorPhone { get; set; }
        public string? AvatarUrl { get; set; }
        public double? RatingAverage { get; set; }
        public int? TotalReviews { get; set; }
    }

    public class DoctorSlotResponse
    {
        public int ScheduleId { get; set; }
        public string? TimeSlot { get; set; }
        public string? SlotStatus { get; set; }
    }

    public class FacilityResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? LicenseUrl { get; set; }
        public string? MapUrl { get; set; }
        public string? Province { get; set; }
        public bool Verified { get; set; }
    }

    public class PortalStatsResponse
    {
        public long TotalDoctors { get; set; }
        public long TotalAppointments { get; set; }
        public double AverageRating { get; set; }
    }

    public class SpecialtyResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
