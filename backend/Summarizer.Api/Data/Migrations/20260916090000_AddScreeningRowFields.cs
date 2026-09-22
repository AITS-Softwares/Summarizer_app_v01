using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Summarizer.Api.Data;

namespace Summarizer.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260916090000_AddScreeningRowFields")]
public partial class AddScreeningRowFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(name: "ExtractedFieldsJson", table: "ScreeningRows", type: "nvarchar(max)", nullable: false, defaultValue: "{}");
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "ExtractedFieldsJson", table: "ScreeningRows");
}
