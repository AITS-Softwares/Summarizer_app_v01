using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Summarizer.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SupportMultipleSourceFilesPerRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScreeningRows_ScreeningRunId_TemplateRowNumber",
                table: "ScreeningRows");

            migrationBuilder.AddColumn<int>(
                name: "SourceFileIndex",
                table: "ScreeningRows",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningRows_ScreeningRunId_SourceFileIndex_TemplateRowNumber",
                table: "ScreeningRows",
                columns: new[] { "ScreeningRunId", "SourceFileIndex", "TemplateRowNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScreeningRows_ScreeningRunId_SourceFileIndex_TemplateRowNumber",
                table: "ScreeningRows");

            migrationBuilder.DropColumn(
                name: "SourceFileIndex",
                table: "ScreeningRows");

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningRows_ScreeningRunId_TemplateRowNumber",
                table: "ScreeningRows",
                columns: new[] { "ScreeningRunId", "TemplateRowNumber" },
                unique: true);
        }
    }
}
