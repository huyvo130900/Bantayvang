using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanTayVang.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEnglishColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamCampaigns_Departments_KhoaPhongId",
                table: "ExamCampaigns");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamRegistrations_Departments_KhoaPhongId",
                table: "ExamRegistrations");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_IdKhoaQuanLy",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "IdKhoaQuanLy",
                table: "Users",
                newName: "DeptManagerDeptId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_IdKhoaQuanLy",
                table: "Users",
                newName: "IX_Users_DeptManagerDeptId");

            migrationBuilder.RenameColumn(
                name: "IdLuaChonDaChon",
                table: "SubmissionDetails",
                newName: "SelectedOptionId");

            migrationBuilder.RenameIndex(
                name: "IX_SubmissionDetails_IdLuaChonDaChon",
                table: "SubmissionDetails",
                newName: "IX_SubmissionDetails_SelectedOptionId");

            migrationBuilder.RenameColumn(
                name: "TenVaiTro",
                table: "Roles",
                newName: "RoleName");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "Roles",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DoKho",
                table: "Questions",
                newName: "Difficulty");

            migrationBuilder.RenameColumn(
                name: "ThuTu",
                table: "QuestionOptions",
                newName: "OrderIndex");

            migrationBuilder.RenameColumn(
                name: "LaDapAnDung",
                table: "QuestionOptions",
                newName: "IsCorrect");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "QuestionCategories",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "TongSoCau",
                table: "ExamSubmissions",
                newName: "TotalQuestions");

            migrationBuilder.RenameColumn(
                name: "ThoiGianBatDau",
                table: "ExamSubmissions",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "KhoaPhongId",
                table: "ExamRegistrations",
                newName: "DepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamRegistrations_KhoaPhongId",
                table: "ExamRegistrations",
                newName: "IX_ExamRegistrations_DepartmentId");

            migrationBuilder.RenameColumn(
                name: "ThoiGianLamBai",
                table: "ExamPapers",
                newName: "MinPassQuestions");

            migrationBuilder.RenameColumn(
                name: "ThoiGianBatDau",
                table: "ExamPapers",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "SoCauDungToiThieu",
                table: "ExamPapers",
                newName: "DurationMinutes");

            migrationBuilder.RenameColumn(
                name: "CongBoKetQua",
                table: "ExamPapers",
                newName: "IsResultPublished");

            migrationBuilder.RenameColumn(
                name: "TongSoCauHoi",
                table: "ExamCampaigns",
                newName: "TotalQuestions");

            migrationBuilder.RenameColumn(
                name: "ThoiGianLamBai",
                table: "ExamCampaigns",
                newName: "MinPassQuestions");

            migrationBuilder.RenameColumn(
                name: "ThoiGianKetThuc",
                table: "ExamCampaigns",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "ThoiGianBatDau",
                table: "ExamCampaigns",
                newName: "EndTime");

            migrationBuilder.RenameColumn(
                name: "SoCauDungToiThieu",
                table: "ExamCampaigns",
                newName: "DurationMinutes");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "ExamCampaigns",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "KhoaPhongId",
                table: "ExamCampaigns",
                newName: "DepartmentId");

            migrationBuilder.RenameColumn(
                name: "DonViToChuc",
                table: "ExamCampaigns",
                newName: "OrganizedBy");

            migrationBuilder.RenameIndex(
                name: "IX_ExamCampaigns_KhoaPhongId",
                table: "ExamCampaigns",
                newName: "IX_ExamCampaigns_DepartmentId");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "Departments",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "ThoiGian",
                table: "CheatWarnings",
                newName: "ActionTime");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "CheatWarnings",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "ThoiGian",
                table: "AuditLogs",
                newName: "ActionTime");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamCampaigns_Departments_DepartmentId",
                table: "ExamCampaigns",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamRegistrations_Departments_DepartmentId",
                table: "ExamRegistrations",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DeptManagerDeptId",
                table: "Users",
                column: "DeptManagerDeptId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamCampaigns_Departments_DepartmentId",
                table: "ExamCampaigns");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamRegistrations_Departments_DepartmentId",
                table: "ExamRegistrations");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DeptManagerDeptId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "DeptManagerDeptId",
                table: "Users",
                newName: "IdKhoaQuanLy");

            migrationBuilder.RenameIndex(
                name: "IX_Users_DeptManagerDeptId",
                table: "Users",
                newName: "IX_Users_IdKhoaQuanLy");

            migrationBuilder.RenameColumn(
                name: "SelectedOptionId",
                table: "SubmissionDetails",
                newName: "IdLuaChonDaChon");

            migrationBuilder.RenameIndex(
                name: "IX_SubmissionDetails_SelectedOptionId",
                table: "SubmissionDetails",
                newName: "IX_SubmissionDetails_IdLuaChonDaChon");

            migrationBuilder.RenameColumn(
                name: "RoleName",
                table: "Roles",
                newName: "TenVaiTro");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Roles",
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "Difficulty",
                table: "Questions",
                newName: "DoKho");

            migrationBuilder.RenameColumn(
                name: "OrderIndex",
                table: "QuestionOptions",
                newName: "ThuTu");

            migrationBuilder.RenameColumn(
                name: "IsCorrect",
                table: "QuestionOptions",
                newName: "LaDapAnDung");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "QuestionCategories",
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "TotalQuestions",
                table: "ExamSubmissions",
                newName: "TongSoCau");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "ExamSubmissions",
                newName: "ThoiGianBatDau");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "ExamRegistrations",
                newName: "KhoaPhongId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamRegistrations_DepartmentId",
                table: "ExamRegistrations",
                newName: "IX_ExamRegistrations_KhoaPhongId");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "ExamPapers",
                newName: "ThoiGianBatDau");

            migrationBuilder.RenameColumn(
                name: "MinPassQuestions",
                table: "ExamPapers",
                newName: "ThoiGianLamBai");

            migrationBuilder.RenameColumn(
                name: "IsResultPublished",
                table: "ExamPapers",
                newName: "CongBoKetQua");

            migrationBuilder.RenameColumn(
                name: "DurationMinutes",
                table: "ExamPapers",
                newName: "SoCauDungToiThieu");

            migrationBuilder.RenameColumn(
                name: "TotalQuestions",
                table: "ExamCampaigns",
                newName: "TongSoCauHoi");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "ExamCampaigns",
                newName: "ThoiGianKetThuc");

            migrationBuilder.RenameColumn(
                name: "OrganizedBy",
                table: "ExamCampaigns",
                newName: "DonViToChuc");

            migrationBuilder.RenameColumn(
                name: "MinPassQuestions",
                table: "ExamCampaigns",
                newName: "ThoiGianLamBai");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "ExamCampaigns",
                newName: "ThoiGianBatDau");

            migrationBuilder.RenameColumn(
                name: "DurationMinutes",
                table: "ExamCampaigns",
                newName: "SoCauDungToiThieu");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ExamCampaigns",
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "ExamCampaigns",
                newName: "KhoaPhongId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamCampaigns_DepartmentId",
                table: "ExamCampaigns",
                newName: "IX_ExamCampaigns_KhoaPhongId");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Departments",
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "CheatWarnings",
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "ActionTime",
                table: "CheatWarnings",
                newName: "ThoiGian");

            migrationBuilder.RenameColumn(
                name: "ActionTime",
                table: "AuditLogs",
                newName: "ThoiGian");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamCampaigns_Departments_KhoaPhongId",
                table: "ExamCampaigns",
                column: "KhoaPhongId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamRegistrations_Departments_KhoaPhongId",
                table: "ExamRegistrations",
                column: "KhoaPhongId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_IdKhoaQuanLy",
                table: "Users",
                column: "IdKhoaQuanLy",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
