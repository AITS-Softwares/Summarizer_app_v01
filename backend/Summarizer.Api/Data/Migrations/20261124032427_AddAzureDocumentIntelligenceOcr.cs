using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Summarizer.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAzureDocumentIntelligenceOcr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AzureDocumentIntelligenceEndpoint",
                table: "ProviderSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AzureDocumentIntelligenceKeyEncrypted",
                table: "ProviderSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AzureDocumentIntelligenceModel",
                table: "ProviderSettings",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AzureUseAiFallback",
                table: "ProviderSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OcrProviderMode",
                table: "ProviderSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "ProviderSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AzureDocumentIntelligenceEndpoint", "AzureDocumentIntelligenceKeyEncrypted", "AzureDocumentIntelligenceModel", "AzureUseAiFallback", "OcrProviderMode" },
                values: new object[] { "", null, "prebuilt-layout", true, "ai" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AzureDocumentIntelligenceEndpoint",
                table: "ProviderSettings");

            migrationBuilder.DropColumn(
                name: "AzureDocumentIntelligenceKeyEncrypted",
                table: "ProviderSettings");

            migrationBuilder.DropColumn(
                name: "AzureDocumentIntelligenceModel",
                table: "ProviderSettings");

            migrationBuilder.DropColumn(
                name: "AzureUseAiFallback",
                table: "ProviderSettings");

            migrationBuilder.DropColumn(
                name: "OcrProviderMode",
                table: "ProviderSettings");
        }
    }
}
