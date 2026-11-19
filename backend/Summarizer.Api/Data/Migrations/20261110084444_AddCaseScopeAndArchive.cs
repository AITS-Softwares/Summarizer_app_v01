using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Summarizer.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseScopeAndArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "ScreeningRuns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserId",
                table: "ScreeningRuns",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CaseId",
                table: "ScreeningRuns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "ScreeningRuns",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                table: "Documents",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CaseId",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Documents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CaseId",
                table: "Conversations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Conversations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ReferenceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cases_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseAccessGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Permissions = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GrantedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GrantedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseAccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseAccessGrants_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningRuns_CaseId",
                table: "ScreeningRuns",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CaseId",
                table: "Documents",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_CaseId",
                table: "Conversations",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseAccessGrants_CaseId_UserId",
                table: "CaseAccessGrants",
                columns: new[] { "CaseId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_ClientId_Status",
                table: "Cases",
                columns: new[] { "ClientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ReferenceCode",
                table: "Clients",
                column: "ReferenceCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Cases_CaseId",
                table: "Conversations",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Cases_CaseId",
                table: "Documents",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScreeningRuns_Cases_CaseId",
                table: "ScreeningRuns",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Cases_CaseId",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Cases_CaseId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_ScreeningRuns_Cases_CaseId",
                table: "ScreeningRuns");

            migrationBuilder.DropTable(
                name: "CaseAccessGrants");

            migrationBuilder.DropTable(
                name: "Cases");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_ScreeningRuns_CaseId",
                table: "ScreeningRuns");

            migrationBuilder.DropIndex(
                name: "IX_Documents_CaseId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_CaseId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "ScreeningRuns");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "ScreeningRuns");

            migrationBuilder.DropColumn(
                name: "CaseId",
                table: "ScreeningRuns");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ScreeningRuns");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CaseId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CaseId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Conversations");
        }
    }
}
