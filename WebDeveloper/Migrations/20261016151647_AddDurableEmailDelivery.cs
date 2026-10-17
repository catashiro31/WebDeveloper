using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebDeveloper.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableEmailDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Các bản triển khai cũ có thể đã tạo hai cột này bằng SQL lúc khởi động.
            migrationBuilder.Sql("ALTER TABLE appointments ADD COLUMN IF NOT EXISTS cancellation_reason text;");
            migrationBuilder.Sql("ALTER TABLE appointments ADD COLUMN IF NOT EXISTS cancelled_by text;");

            migrationBuilder.CreateTable(
                name: "email_outbox",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    DeliveryKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Approved = table.Column<bool>(type: "boolean", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DiscardedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_outbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_outbox_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_outbox_DeliveryKey",
                table: "email_outbox",
                column: "DeliveryKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_outbox_SentAt_NextAttemptAt",
                table: "email_outbox",
                columns: new[] { "SentAt", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_email_outbox_UserId",
                table: "email_outbox",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_outbox");

            // Giữ dữ liệu lý do hủy do phiên bản trước đã dùng các cột này.

        }
    }
}
