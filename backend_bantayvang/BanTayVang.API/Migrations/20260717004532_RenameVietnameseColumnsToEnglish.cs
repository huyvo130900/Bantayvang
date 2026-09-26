using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanTayVang.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameVietnameseColumnsToEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaVaiTro",
                table: "Roles",
                newName: "RoleCode");

            migrationBuilder.RenameColumn(
                name: "ThietBiUserAgent",
                table: "LoginSessions",
                newName: "UserAgent");

            migrationBuilder.RenameColumn(
                name: "TongWarningCount",
                table: "ExamSubmissions",
                newName: "WarningCount");

            migrationBuilder.RenameColumn(
                name: "NguoiCongBoRieng",
                table: "ExamSubmissions",
                newName: "NguoiIsIndividualResultPublished");

            migrationBuilder.RenameColumn(
                name: "DanhGiaKhoa",
                table: "ExamSubmissions",
                newName: "DepartmentEvaluation");

            migrationBuilder.RenameColumn(
                name: "CongBoRieng",
                table: "ExamSubmissions",
                newName: "IsIndividualResultPublished");

            migrationBuilder.RenameColumn(
                name: "NguoiDuyetId",
                table: "ExamRegistrations",
                newName: "ApproverId");

            migrationBuilder.RenameColumn(
                name: "NgayDuyet",
                table: "ExamRegistrations",
                newName: "ApprovalDate");

            migrationBuilder.RenameColumn(
                name: "NgayDangKy",
                table: "ExamRegistrations",
                newName: "RegistrationDate");

            migrationBuilder.RenameColumn(
                name: "MucDichThi",
                table: "ExamRegistrations",
                newName: "Major");

            migrationBuilder.RenameColumn(
                name: "MatKhauHash",
                table: "ExamRegistrations",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "GhiChu",
                table: "ExamRegistrations",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ChuyenNganh",
                table: "ExamRegistrations",
                newName: "ExamPurpose");

            migrationBuilder.RenameColumn(
                name: "TrongSo",
                table: "ExamPaperQuestions",
                newName: "Weight");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RoleCode",
                table: "Roles",
                newName: "MaVaiTro");

            migrationBuilder.RenameColumn(
                name: "UserAgent",
                table: "LoginSessions",
                newName: "ThietBiUserAgent");

            migrationBuilder.RenameColumn(
                name: "WarningCount",
                table: "ExamSubmissions",
                newName: "TongWarningCount");

            migrationBuilder.RenameColumn(
                name: "NguoiIsIndividualResultPublished",
                table: "ExamSubmissions",
                newName: "NguoiCongBoRieng");

            migrationBuilder.RenameColumn(
                name: "IsIndividualResultPublished",
                table: "ExamSubmissions",
                newName: "CongBoRieng");

            migrationBuilder.RenameColumn(
                name: "DepartmentEvaluation",
                table: "ExamSubmissions",
                newName: "DanhGiaKhoa");

            migrationBuilder.RenameColumn(
                name: "RegistrationDate",
                table: "ExamRegistrations",
                newName: "NgayDangKy");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "ExamRegistrations",
                newName: "MatKhauHash");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "ExamRegistrations",
                newName: "GhiChu");

            migrationBuilder.RenameColumn(
                name: "Major",
                table: "ExamRegistrations",
                newName: "MucDichThi");

            migrationBuilder.RenameColumn(
                name: "ExamPurpose",
                table: "ExamRegistrations",
                newName: "ChuyenNganh");

            migrationBuilder.RenameColumn(
                name: "ApproverId",
                table: "ExamRegistrations",
                newName: "NguoiDuyetId");

            migrationBuilder.RenameColumn(
                name: "ApprovalDate",
                table: "ExamRegistrations",
                newName: "NgayDuyet");

            migrationBuilder.RenameColumn(
                name: "Weight",
                table: "ExamPaperQuestions",
                newName: "TrongSo");
        }
    }
}
