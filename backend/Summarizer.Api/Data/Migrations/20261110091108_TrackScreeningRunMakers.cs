using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Summarizer.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TrackScreeningRunMakers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MakerUserIdsJson",
                table: "ScreeningRuns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            // Existing approvals cannot establish whether a later row editor also approved.
            migrationBuilder.Sql("UPDATE [ScreeningRuns] SET [ApprovedByUserId] = NULL, [ApprovedAtUtc] = NULL WHERE [Status] <> 'exported' AND [ApprovedAtUtc] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MakerUserIdsJson",
                table: "ScreeningRuns");
        }
    }
}
