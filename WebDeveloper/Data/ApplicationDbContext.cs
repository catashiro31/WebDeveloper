using Microsoft.EntityFrameworkCore;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<DoctorDetail> DoctorDetails => Set<DoctorDetail>();
        public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
        public DbSet<DoctorTransferRequest> DoctorTransferRequests => Set<DoctorTransferRequest>();
        public DbSet<Facility> Facilities => Set<Facility>();
        public DbSet<MedicalResult> MedicalResults => Set<MedicalResult>();
        public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Specialty> Specialties => Set<Specialty>();
        public DbSet<TokenBlacklist> TokenBlacklists => Set<TokenBlacklist>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Store enums as strings (matching Java @Enumerated(EnumType.STRING))
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Appointment>()
                .Property(a => a.BookingStatus)
                .HasConversion<string>();

            modelBuilder.Entity<DoctorDetail>()
                .Property(d => d.VerificationStatus)
                .HasConversion<string>();

            modelBuilder.Entity<DoctorSchedule>()
                .Property(s => s.TimeSlot)
                .HasConversion<string>();

            modelBuilder.Entity<DoctorSchedule>()
                .Property(s => s.SlotStatus)
                .HasConversion<string>();

            modelBuilder.Entity<DoctorTransferRequest>()
                .Property(t => t.Status)
                .HasConversion<string>();

            modelBuilder.Entity<PatientProfile>()
                .Property(p => p.Gender)
                .HasConversion<string>();

            // === Indexes (matching Java @Index annotations) ===

            // Appointment indexes
            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.PatientId)
                .HasDatabaseName("idx_appointment_patient");

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.ScheduleId)
                .HasDatabaseName("idx_appointment_schedule");

            // DoctorDetail index
            modelBuilder.Entity<DoctorDetail>()
                .HasIndex(d => new { d.SpecialtyId, d.FacilityId })
                .HasDatabaseName("idx_doctor_specialty_facility");

            // DoctorDetail: unique user_id
            modelBuilder.Entity<DoctorDetail>()
                .HasIndex(d => d.UserId)
                .IsUnique();

            // Facility index
            modelBuilder.Entity<Facility>()
                .HasIndex(f => f.IsActive)
                .HasDatabaseName("idx_facility_is_active");

            // Specialty index
            modelBuilder.Entity<Specialty>()
                .HasIndex(s => s.IsActive)
                .HasDatabaseName("idx_specialty_is_active");

            // TokenBlacklist unique token
            modelBuilder.Entity<TokenBlacklist>()
                .HasIndex(t => t.Token)
                .IsUnique();

            // MedicalResult: unique appointment_id
            modelBuilder.Entity<MedicalResult>()
                .HasIndex(m => m.AppointmentId)
                .IsUnique();

            // Review: unique appointment_id
            modelBuilder.Entity<Review>()
                .HasIndex(r => r.AppointmentId)
                .IsUnique();

            // === Relationships ===

            // User -> PatientProfiles (1:N)
            modelBuilder.Entity<User>()
                .HasMany(u => u.PatientProfiles)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // DoctorDetail -> DoctorSchedules (1:N)
            modelBuilder.Entity<DoctorDetail>()
                .HasMany(d => d.DoctorSchedules)
                .WithOne(s => s.Doctor)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // DoctorSchedule -> Appointments (1:N)
            modelBuilder.Entity<DoctorSchedule>()
                .HasMany(s => s.Appointments)
                .WithOne(a => a.Schedule)
                .HasForeignKey(a => a.ScheduleId)
                .OnDelete(DeleteBehavior.NoAction);

            // PatientProfile -> Appointments (1:N)
            modelBuilder.Entity<PatientProfile>()
                .HasMany(p => p.Appointments)
                .WithOne(a => a.Patient)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            // Facility -> DoctorDetails (1:N)
            modelBuilder.Entity<Facility>()
                .HasMany(f => f.DoctorDetails)
                .WithOne(d => d.Facility)
                .HasForeignKey(d => d.FacilityId)
                .OnDelete(DeleteBehavior.NoAction);

            // Specialty -> DoctorDetails (1:N)
            modelBuilder.Entity<Specialty>()
                .HasMany(s => s.DoctorDetails)
                .WithOne(d => d.Specialty)
                .HasForeignKey(d => d.SpecialtyId)
                .OnDelete(DeleteBehavior.NoAction);

            // DoctorTransferRequest relationships
            modelBuilder.Entity<DoctorTransferRequest>()
                .HasOne(t => t.Doctor)
                .WithMany()
                .HasForeignKey(t => t.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DoctorTransferRequest>()
                .HasOne(t => t.TargetFacility)
                .WithMany()
                .HasForeignKey(t => t.TargetFacilityId)
                .OnDelete(DeleteBehavior.NoAction);

            // 13. Xử lý Múi giờ (Timezone) trong Khám chữa bệnh Từ xa (Telehealth)
            // Bắt buộc toàn bộ DateTime đọc/ghi xuống DB đều phải quy chuẩn về UTC
            var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                v => !v.HasValue ? v : (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()),
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Tự động set CreatedAt/UpdatedAt (tương đương @CreationTimestamp / @UpdateTimestamp trong Hibernate)
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                var now = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    var createdAt = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                    if (createdAt != null && createdAt.CurrentValue == null)
                        createdAt.CurrentValue = now;
                }

                var updatedAt = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAt != null)
                    updatedAt.CurrentValue = now;
            }
        }
    }
}
