using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanTayVang.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameLanDangNhapCuoiToLastLoginAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LanDangNhapCuoi",
                table: "Users",
                newName: "LastLoginAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastLoginAt",
                table: "Users",
                newName: "LanDangNhapCuoi");
        }
    }
}
