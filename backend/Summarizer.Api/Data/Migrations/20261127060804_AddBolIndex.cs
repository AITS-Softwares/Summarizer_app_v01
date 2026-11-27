using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Summarizer.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBolIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScreeningRows_ScreeningRunId_SourceFileIndex_TemplateRowNumber",
                table: "ScreeningRows");

            migrationBuilder.AddColumn<int>(
                name: "BolIndex",
                table: "ScreeningRows",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningRows_ScreeningRunId_SourceFileIndex_BolIndex_TemplateRowNumber",
                table: "ScreeningRows",
                columns: new[] { "ScreeningRunId", "SourceFileIndex", "BolIndex", "TemplateRowNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScreeningRows_ScreeningRunId_SourceFileIndex_BolIndex_TemplateRowNumber",
                table: "ScreeningRows");

            migrationBuilder.DropColumn(
                name: "BolIndex",
                table: "ScreeningRows");

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningRows_ScreeningRunId_SourceFileIndex_TemplateRowNumber",
                table: "ScreeningRows",
                columns: new[] { "ScreeningRunId", "SourceFileIndex", "TemplateRowNumber" },
                unique: true);
        }
    }
}
