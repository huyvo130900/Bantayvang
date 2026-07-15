using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanTayVang.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationAndColumnRenames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamPapers_ExamCampaigns_KyThiId",
                table: "ExamPapers");

            migrationBuilder.RenameColumn(
                name: "MaNhanVien",
                table: "Users",
                newName: "EmployeeCode");

            migrationBuilder.RenameColumn(
                name: "ChucDanh",
                table: "Users",
                newName: "JobTitle");

            migrationBuilder.RenameColumn(
                name: "ThoiGianTraLoi",
                table: "SubmissionDetails",
                newName: "AnswerTime");

            migrationBuilder.RenameColumn(
                name: "DaLuu",
                table: "SubmissionDetails",
                newName: "IsSaved");

            migrationBuilder.RenameColumn(
                name: "CauTraLoiTuLuan",
                table: "SubmissionDetails",
                newName: "EssayAnswer");

            migrationBuilder.RenameColumn(
                name: "DaXoa",
                table: "Questions",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "ThoiGianTao",
                table: "Phiendangnhaps",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "ThoiGianHetHan",
                table: "Phiendangnhaps",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiGianCongBoRieng",
                table: "ExamSubmissions",
                newName: "IndividualPublishedAt");

            migrationBuilder.RenameColumn(
                name: "DonViCongTac",
                table: "ExamRegistrations",
                newName: "WorkUnit");

            migrationBuilder.RenameColumn(
                name: "ThoiGianCongBo",
                table: "ExamPapers",
                newName: "PublishedAt");

            migrationBuilder.RenameColumn(
                name: "KyThiId",
                table: "ExamPapers",
                newName: "ExamCampaignId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamPapers_KyThiId",
                table: "ExamPapers",
                newName: "IX_ExamPapers_ExamCampaignId");

            migrationBuilder.RenameColumn(
                name: "MaKhoa",
                table: "Departments",
                newName: "DeptCode");

            migrationBuilder.RenameColumn(
                name: "LoaiCanhBao",
                table: "CheatWarnings",
                newName: "WarningType");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamPapers_ExamCampaigns_ExamCampaignId",
                table: "ExamPapers",
                column: "ExamCampaignId",
                principalTable: "ExamCampaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamPapers_ExamCampaigns_ExamCampaignId",
                table: "ExamPapers");

            migrationBuilder.RenameColumn(
                name: "JobTitle",
                table: "Users",
                newName: "ChucDanh");

            migrationBuilder.RenameColumn(
                name: "EmployeeCode",
                table: "Users",
                newName: "MaNhanVien");

            migrationBuilder.RenameColumn(
                name: "IsSaved",
                table: "SubmissionDetails",
                newName: "DaLuu");

            migrationBuilder.RenameColumn(
                name: "EssayAnswer",
                table: "SubmissionDetails",
                newName: "CauTraLoiTuLuan");

            migrationBuilder.RenameColumn(
                name: "AnswerTime",
                table: "SubmissionDetails",
                newName: "ThoiGianTraLoi");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Questions",
                newName: "DaXoa");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "Phiendangnhaps",
                newName: "ThoiGianTao");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Phiendangnhaps",
                newName: "ThoiGianHetHan");

            migrationBuilder.RenameColumn(
                name: "IndividualPublishedAt",
                table: "ExamSubmissions",
                newName: "ThoiGianCongBoRieng");

            migrationBuilder.RenameColumn(
                name: "WorkUnit",
                table: "ExamRegistrations",
                newName: "DonViCongTac");

            migrationBuilder.RenameColumn(
                name: "PublishedAt",
                table: "ExamPapers",
                newName: "ThoiGianCongBo");

            migrationBuilder.RenameColumn(
                name: "ExamCampaignId",
                table: "ExamPapers",
                newName: "KyThiId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamPapers_ExamCampaignId",
                table: "ExamPapers",
                newName: "IX_ExamPapers_KyThiId");

            migrationBuilder.RenameColumn(
                name: "DeptCode",
                table: "Departments",
                newName: "MaKhoa");

            migrationBuilder.RenameColumn(
                name: "WarningType",
                table: "CheatWarnings",
                newName: "LoaiCanhBao");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamPapers_ExamCampaigns_KyThiId",
                table: "ExamPapers",
                column: "KyThiId",
                principalTable: "ExamCampaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
