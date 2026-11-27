using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Summarizer.Api.Data;

#nullable disable

namespace Summarizer.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261127073000_AddScreeningProviderSettings")]
public sealed class AddScreeningProviderSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ScreeningProviderMode", table: "ProviderSettings", type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "api");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ScreeningProviderMode", table: "ProviderSettings");
    }
}
