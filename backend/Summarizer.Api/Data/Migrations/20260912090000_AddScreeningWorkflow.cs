using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Summarizer.Api.Data;

#nullable disable

namespace Summarizer.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260912090000_AddScreeningWorkflow")]
public partial class AddScreeningWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ScreeningTemplates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                OriginalName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                StoragePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                WorksheetName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                HeaderRowNumber = table.Column<int>(type: "int", nullable: false),
                RowsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ScreeningTemplates", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ScreeningRuns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                SourceFilesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                OutputPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ScreeningRuns", x => x.Id);
                table.ForeignKey("FK_ScreeningRuns_ScreeningTemplates_TemplateId", x => x.TemplateId, "ScreeningTemplates", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ScreeningRows",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ScreeningRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TemplateRowNumber = table.Column<int>(type: "int", nullable: false),
                Heading = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                EntityName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                EntityTypeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                SourceFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                SourcePageNumber = table.Column<int>(type: "int", nullable: true),
                Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ScreeningRows", x => x.Id);
                table.ForeignKey("FK_ScreeningRows_ScreeningRuns_ScreeningRunId", x => x.ScreeningRunId, "ScreeningRuns", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_ScreeningTemplates_IsActive", table: "ScreeningTemplates", column: "IsActive");
        migrationBuilder.CreateIndex(name: "IX_ScreeningRuns_TemplateId", table: "ScreeningRuns", column: "TemplateId");
        migrationBuilder.CreateIndex(name: "IX_ScreeningRows_ScreeningRunId_TemplateRowNumber", table: "ScreeningRows", columns: new[] { "ScreeningRunId", "TemplateRowNumber" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ScreeningRows");
        migrationBuilder.DropTable(name: "ScreeningRuns");
        migrationBuilder.DropTable(name: "ScreeningTemplates");
    }
}
