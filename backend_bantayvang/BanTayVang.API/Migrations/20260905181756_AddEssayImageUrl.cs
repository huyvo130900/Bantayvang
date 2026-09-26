using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanTayVang.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEssayImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Columns AiComment, AiGradingStatus, AiScore were already added manually to DB

            migrationBuilder.AddColumn<string>(
                name: "EssayImageUrl",
                table: "SubmissionDetails",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Columns AiComment, AiGradingStatus, AiScore were already added manually to DB

            migrationBuilder.DropColumn(
                name: "EssayImageUrl",
                table: "SubmissionDetails");
        }
    }
}
