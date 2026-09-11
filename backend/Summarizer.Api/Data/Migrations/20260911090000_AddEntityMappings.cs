using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Summarizer.Api.Data;

#nullable disable

namespace Summarizer.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260911090000_AddEntityMappings")]
public partial class AddEntityMappings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EntityMappings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Heading = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                EntityName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                EntityTypeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Priority = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                Version = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_EntityMappings", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_EntityMappings_IsActive_Heading_EntityName",
            table: "EntityMappings",
            columns: new[] { "IsActive", "Heading", "EntityName" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EntityMappings");
    }
}
