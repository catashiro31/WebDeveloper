using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebDeveloper.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_Postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "facilities",
                columns: table => new
                {
                    facility_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    facility_name = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    license_url = table.Column<string>(type: "text", nullable: true),
                    map_url = table.Column<string>(type: "text", nullable: true),
                    province = table.Column<string>(type: "text", nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facilities", x => x.facility_id);
                });

            migrationBuilder.CreateTable(
                name: "specialties",
                columns: table => new
                {
                    specialty_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    specialty_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specialties", x => x.specialty_id);
                });

            migrationBuilder.CreateTable(
                name: "token_blacklist",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_token_blacklist", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    role = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verification_code = table.Column<string>(type: "text", nullable: true),
                    code_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason_banned = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "doctor_details",
                columns: table => new
                {
                    doctor_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    specialty_id = table.Column<int>(type: "integer", nullable: false),
                    facility_id = table.Column<int>(type: "integer", nullable: false),
                    bio = table.Column<string>(type: "text", nullable: true),
                    degree = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    experience_years = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<double>(type: "double precision", nullable: true),
                    id_card_url = table.Column<string>(type: "text", nullable: true),
                    certificate_url = table.Column<string>(type: "text", nullable: true),
                    verification_status = table.Column<string>(type: "text", nullable: false),
                    rating_average = table.Column<double>(type: "double precision", nullable: true),
                    review_count = table.Column<int>(type: "integer", nullable: true),
                    reason_reject = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctor_details", x => x.doctor_id);
                    table.ForeignKey(
                        name: "FK_doctor_details_facilities_facility_id",
                        column: x => x.facility_id,
                        principalTable: "facilities",
                        principalColumn: "facility_id");
                    table.ForeignKey(
                        name: "FK_doctor_details_specialties_specialty_id",
                        column: x => x.specialty_id,
                        principalTable: "specialties",
                        principalColumn: "specialty_id");
                    table.ForeignKey(
                        name: "FK_doctor_details_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "patient_profiles",
                columns: table => new
                {
                    patient_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    relationship = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_profiles", x => x.patient_id);
                    table.ForeignKey(
                        name: "FK_patient_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "doctor_schedules",
                columns: table => new
                {
                    schedule_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    doctor_id = table.Column<int>(type: "integer", nullable: false),
                    facility_id = table.Column<int>(type: "integer", nullable: true),
                    date_working = table.Column<DateOnly>(type: "date", nullable: false),
                    time_slot = table.Column<string>(type: "text", nullable: false),
                    slot_status = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctor_schedules", x => x.schedule_id);
                    table.ForeignKey(
                        name: "FK_doctor_schedules_doctor_details_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctor_details",
                        principalColumn: "doctor_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_doctor_schedules_facilities_facility_id",
                        column: x => x.facility_id,
                        principalTable: "facilities",
                        principalColumn: "facility_id");
                });

            migrationBuilder.CreateTable(
                name: "doctor_transfer_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    doctor_id = table.Column<int>(type: "integer", nullable: false),
                    target_facility_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    admin_note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctor_transfer_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_doctor_transfer_requests_doctor_details_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctor_details",
                        principalColumn: "doctor_id");
                    table.ForeignKey(
                        name: "FK_doctor_transfer_requests_facilities_target_facility_id",
                        column: x => x.target_facility_id,
                        principalTable: "facilities",
                        principalColumn: "facility_id");
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    appointment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<int>(type: "integer", nullable: false),
                    schedule_id = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    booking_status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.appointment_id);
                    table.ForeignKey(
                        name: "FK_appointments_doctor_schedules_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "doctor_schedules",
                        principalColumn: "schedule_id");
                    table.ForeignKey(
                        name: "FK_appointments_patient_profiles_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patient_profiles",
                        principalColumn: "patient_id");
                });

            migrationBuilder.CreateTable(
                name: "medical_results",
                columns: table => new
                {
                    result_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    appointment_id = table.Column<int>(type: "integer", nullable: false),
                    diagnosis = table.Column<string>(type: "text", nullable: true),
                    prescription_url = table.Column<string>(type: "text", nullable: true),
                    doctor_notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medical_results", x => x.result_id);
                    table.ForeignKey(
                        name: "FK_medical_results_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "appointment_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review",
                columns: table => new
                {
                    review_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    appointment_id = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_visible = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review", x => x.review_id);
                    table.ForeignKey(
                        name: "FK_review_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "appointment_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_appointment_patient",
                table: "appointments",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "idx_appointment_schedule",
                table: "appointments",
                column: "schedule_id");

            migrationBuilder.CreateIndex(
                name: "idx_doctor_specialty_facility",
                table: "doctor_details",
                columns: new[] { "specialty_id", "facility_id" });

            migrationBuilder.CreateIndex(
                name: "IX_doctor_details_facility_id",
                table: "doctor_details",
                column: "facility_id");

            migrationBuilder.CreateIndex(
                name: "IX_doctor_details_user_id",
                table: "doctor_details",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_doctor_schedules_doctor_id",
                table: "doctor_schedules",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_doctor_schedules_facility_id",
                table: "doctor_schedules",
                column: "facility_id");

            migrationBuilder.CreateIndex(
                name: "IX_doctor_transfer_requests_doctor_id",
                table: "doctor_transfer_requests",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_doctor_transfer_requests_target_facility_id",
                table: "doctor_transfer_requests",
                column: "target_facility_id");

            migrationBuilder.CreateIndex(
                name: "idx_facility_is_active",
                table: "facilities",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_medical_results_appointment_id",
                table: "medical_results",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_patient_profiles_user_id",
                table: "patient_profiles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_review_appointment_id",
                table: "review",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_specialty_is_active",
                table: "specialties",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_token_blacklist_token",
                table: "token_blacklist",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "doctor_transfer_requests");

            migrationBuilder.DropTable(
                name: "medical_results");

            migrationBuilder.DropTable(
                name: "review");

            migrationBuilder.DropTable(
                name: "token_blacklist");

            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "doctor_schedules");

            migrationBuilder.DropTable(
                name: "patient_profiles");

            migrationBuilder.DropTable(
                name: "doctor_details");

            migrationBuilder.DropTable(
                name: "facilities");

            migrationBuilder.DropTable(
                name: "specialties");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
