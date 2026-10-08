using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4Workflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignmentExpiresAt",
                table: "ServiceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RequestRejections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: false),
                    TechnicianProfileId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestRejections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestRejections_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestRejections_TechnicianProfiles_TechnicianProfileId",
                        column: x => x.TechnicianProfileId,
                        principalTable: "TechnicianProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestRejections_ServiceRequestId_TechnicianProfileId",
                table: "RequestRejections",
                columns: new[] { "ServiceRequestId", "TechnicianProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestRejections_TechnicianProfileId",
                table: "RequestRejections",
                column: "TechnicianProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestRejections");

            migrationBuilder.DropColumn(
                name: "AssignmentExpiresAt",
                table: "ServiceRequests");
        }
    }
}
