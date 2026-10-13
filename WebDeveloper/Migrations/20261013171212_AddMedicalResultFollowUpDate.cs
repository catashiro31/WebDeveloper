using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDeveloper.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalResultFollowUpDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "follow_up_date",
                table: "medical_results",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "follow_up_date",
                table: "medical_results");
        }
    }
}
