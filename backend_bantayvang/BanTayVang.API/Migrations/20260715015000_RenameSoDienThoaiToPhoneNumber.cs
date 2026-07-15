using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanTayVang.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameSoDienThoaiToPhoneNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Phiendangnhaps");

            migrationBuilder.RenameColumn(
                name: "SoDienThoai",
                table: "Users",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "SoDienThoai",
                table: "ExamRegistrations",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "ChiTiet",
                table: "AuditLogs",
                newName: "Detail");

            migrationBuilder.CreateTable(
                name: "LoginSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    IP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ThietBiUserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PHIENDAN__3214EC0786D0DC9F", x => x.Id);
                    table.ForeignKey(
                        name: "FK__PHIENDANG__IdTai__5DCAEF64",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginSessions_UserId",
                table: "LoginSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginSessions");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "Users",
                newName: "SoDienThoai");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "ExamRegistrations",
                newName: "SoDienThoai");

            migrationBuilder.RenameColumn(
                name: "Detail",
                table: "AuditLogs",
                newName: "ChiTiet");

            migrationBuilder.CreateTable(
                name: "Phiendangnhaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    IP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ThietBiUserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PHIENDAN__3214EC0786D0DC9F", x => x.Id);
                    table.ForeignKey(
                        name: "FK__PHIENDANG__IdTai__5DCAEF64",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Phiendangnhaps_UserId",
                table: "Phiendangnhaps",
                column: "UserId");
        }
    }
}
