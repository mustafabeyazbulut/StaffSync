using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StaffSync.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class v8_ContactCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactCorrections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContactId = table.Column<int>(type: "int", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactCorrections_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContactCorrectionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContactCorrectionId = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NewValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactCorrectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactCorrectionItems_ContactCorrections_ContactCorrectionId",
                        column: x => x.ContactCorrectionId,
                        principalTable: "ContactCorrections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactCorrectionItems_ContactCorrectionId",
                table: "ContactCorrectionItems",
                column: "ContactCorrectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactCorrections_ContactId",
                table: "ContactCorrections",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactCorrections_ReportedByUserId_CreatedDate",
                table: "ContactCorrections",
                columns: new[] { "ReportedByUserId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactCorrections_Status",
                table: "ContactCorrections",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactCorrectionItems");

            migrationBuilder.DropTable(
                name: "ContactCorrections");
        }
    }
}
