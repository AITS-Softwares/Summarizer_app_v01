using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Summarizer.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyProcessingWorkflowAndAuditRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_OccurredAtUtc_EventType",
                table: "AuditEvents");

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
                name: "MakerUserIdsJson",
                table: "ScreeningRuns");

            migrationBuilder.DropColumn(
                name: "CaseId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CaseId",
                table: "Conversations");

            migrationBuilder.AddColumn<string>(
                name: "ActorRoles",
                table: "AuditEvents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorUserName",
                table: "AuditEvents",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "AuditEvents",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AuditRetentionPolicies",
                columns: table => new
                {
                    Category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    KeepForever = table.Column<bool>(type: "bit", nullable: false),
                    RetentionDays = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditRetentionPolicies", x => x.Category);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingSourceFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScreeningRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceFileIndex = table.Column<int>(type: "int", nullable: false),
                    OriginalName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExtractionStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingSourceFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingSourceFiles_ScreeningRuns_ScreeningRunId",
                        column: x => x.ScreeningRunId,
                        principalTable: "ScreeningRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AuditRetentionPolicies",
                columns: new[] { "Category", "KeepForever", "RetentionDays", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { "audit-access", false, 180, new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { "authentication", true, null, new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { "configuration", true, null, new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { "identity", true, null, new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { "processing", false, 2555, new DateTime(2026, 11, 19, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Category_OccurredAtUtc",
                table: "AuditEvents",
                columns: new[] { "Category", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingSourceFiles_ScreeningRunId_SourceFileIndex",
                table: "ProcessingSourceFiles",
                columns: new[] { "ScreeningRunId", "SourceFileIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditRetentionPolicies");

            migrationBuilder.DropTable(
                name: "ProcessingSourceFiles");

            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_Category_OccurredAtUtc",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "ActorRoles",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "ActorUserName",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "AuditEvents");

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
                name: "MakerUserIdsJson",
                table: "ScreeningRuns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<Guid>(
                name: "CaseId",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CaseId",
                table: "Conversations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ReferenceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
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
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    GrantedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GrantedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Permissions = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
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
                name: "IX_AuditEvents_OccurredAtUtc_EventType",
                table: "AuditEvents",
                columns: new[] { "OccurredAtUtc", "EventType" });

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
    }
}
