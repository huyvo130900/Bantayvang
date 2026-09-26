using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanTayVang.API.Migrations
{
    /// <inheritdoc />
    public partial class AddExamCampaignDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: Users.FailedLoginAttempts/LockoutEnd were dropped from this migration - the
            // scaffolder picked them up as model drift (columns already exist in the live DB from
            // an earlier session that added them outside of an EF migration), but they were never
            // recorded in __EFMigrationsHistory. Re-adding them here fails with "column already
            // exists". This migration is scoped to ExamCampaignDepartments only.

            migrationBuilder.CreateTable(
                name: "ExamCampaignDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamCampaignId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamCampaignDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamCampaignDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamCampaignDepartments_ExamCampaigns_ExamCampaignId",
                        column: x => x.ExamCampaignId,
                        principalTable: "ExamCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamCampaignDepartments_DepartmentId",
                table: "ExamCampaignDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamCampaignDepartments_ExamCampaignId_DepartmentId",
                table: "ExamCampaignDepartments",
                columns: new[] { "ExamCampaignId", "DepartmentId" },
                unique: true);

            // Backfill: mỗi kỳ thi cũ đang gán 1 khoa (ExamCampaigns.DepartmentId) giờ trở thành 1
            // dòng trong bảng nối. Cột DepartmentId cũ được GIỮ LẠI (đánh dấu Obsolete trong code,
            // không xóa) để không mất dữ liệu nếu cần đối chiếu/rollback sau này.
            migrationBuilder.Sql(@"
                INSERT INTO ExamCampaignDepartments (ExamCampaignId, DepartmentId)
                SELECT Id, DepartmentId FROM ExamCampaigns WHERE DepartmentId IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamCampaignDepartments");
        }
    }
}
