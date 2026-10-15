using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebDeveloper.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountAppealsAndTransferOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OldFacilityName",
                table: "doctor_transfer_requests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE doctor_transfer_requests t SET "OldFacilityName" = f.facility_name
                FROM doctor_details d JOIN facilities f ON f.facility_id = d.facility_id
                WHERE t.doctor_id = d.doctor_id AND t.status = 'PENDING';
                """);

            migrationBuilder.CreateTable(
                name: "account_appeals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AdminReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcessedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_appeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_account_appeals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_appeals_UserId_Status",
                table: "account_appeals",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_appeals");

            migrationBuilder.DropColumn(
                name: "OldFacilityName",
                table: "doctor_transfer_requests");
        }
    }
}
