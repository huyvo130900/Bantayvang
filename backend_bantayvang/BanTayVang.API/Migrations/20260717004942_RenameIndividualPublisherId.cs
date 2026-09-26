using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanTayVang.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameIndividualPublisherId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NguoiIsIndividualResultPublished",
                table: "ExamSubmissions",
                newName: "IndividualPublisherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IndividualPublisherId",
                table: "ExamSubmissions",
                newName: "NguoiIsIndividualResultPublished");
        }
    }
}
