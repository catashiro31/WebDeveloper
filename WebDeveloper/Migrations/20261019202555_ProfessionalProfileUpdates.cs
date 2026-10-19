using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebDeveloper.Migrations
{
    /// <inheritdoc />
    public partial class ProfessionalProfileUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "degree",
                table: "doctor_details",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PracticeStartYear",
                table: "doctor_details",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfessionalVersion",
                table: "doctor_details",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BookedFacilityAddress",
                table: "appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookedFacilityId",
                table: "appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookedFacilityName",
                table: "appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BookedPrice",
                table: "appointments",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookedSpecialtyId",
                table: "appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookedSpecialtyName",
                table: "appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLegacySnapshot",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Historical prices were never stored: preserve the currently known values
            // explicitly as legacy estimates, rather than claiming an original booking price.
            migrationBuilder.Sql("""
                UPDATE appointments a
                SET "BookedPrice" = d.price,
                    "BookedSpecialtyId" = d.specialty_id,
                    "BookedSpecialtyName" = sp.specialty_name,
                    "BookedFacilityId" = COALESCE(s.facility_id, d.facility_id),
                    "BookedFacilityName" = f.facility_name,
                    "BookedFacilityAddress" = f.address,
                    "IsLegacySnapshot" = TRUE
                FROM doctor_schedules s
                JOIN doctor_details d ON d.doctor_id = s.doctor_id
                JOIN specialties sp ON sp.specialty_id = d.specialty_id
                LEFT JOIN facilities f ON f.facility_id = COALESCE(s.facility_id, d.facility_id)
                WHERE a.schedule_id = s.schedule_id;
                """);

            migrationBuilder.CreateTable(
                name: "professional_updates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<int>(type: "integer", nullable: false),
                    SubmittedByUserId = table.Column<int>(type: "integer", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AppliedDirectly = table.Column<bool>(type: "boolean", nullable: false),
                    BaseVersion = table.Column<int>(type: "integer", nullable: false),
                    BeforeJson = table.Column<string>(type: "text", nullable: false),
                    ProposedJson = table.Column<string>(type: "text", nullable: false),
                    AppliedBeforeJson = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DecisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EvidenceUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professional_updates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_professional_updates_doctor_details_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "doctor_details",
                        principalColumn: "doctor_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_professional_updates_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_professional_updates_users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_professional_updates_DoctorId",
                table: "professional_updates",
                column: "DoctorId",
                unique: true,
                filter: "\"Status\" = 'PENDING'");

            migrationBuilder.CreateIndex(
                name: "IX_professional_updates_ReviewedByUserId",
                table: "professional_updates",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_professional_updates_Status_CreatedAt",
                table: "professional_updates",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_professional_updates_SubmittedByUserId",
                table: "professional_updates",
                column: "SubmittedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "professional_updates");

            migrationBuilder.DropColumn(
                name: "PracticeStartYear",
                table: "doctor_details");

            migrationBuilder.DropColumn(
                name: "ProfessionalVersion",
                table: "doctor_details");

            migrationBuilder.DropColumn(
                name: "BookedFacilityAddress",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "BookedFacilityId",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "BookedFacilityName",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "BookedPrice",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "BookedSpecialtyId",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "BookedSpecialtyName",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "IsLegacySnapshot",
                table: "appointments");

            migrationBuilder.AlterColumn<string>(
                name: "degree",
                table: "doctor_details",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
