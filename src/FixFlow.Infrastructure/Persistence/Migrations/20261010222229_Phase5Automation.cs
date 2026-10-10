using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase5Automation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EscalatedAt",
                table: "ServiceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceRequestId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EscalatedAt",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "ServiceRequestId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Notifications");
        }
    }
}
