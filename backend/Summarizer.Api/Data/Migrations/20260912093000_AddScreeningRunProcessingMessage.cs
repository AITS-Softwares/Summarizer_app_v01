using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Summarizer.Api.Data;

#nullable disable

namespace Summarizer.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260912093000_AddScreeningRunProcessingMessage")]
public partial class AddScreeningRunProcessingMessage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ProcessingMessage",
            table: "ScreeningRuns",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ProcessingMessage", table: "ScreeningRuns");
    }
}
