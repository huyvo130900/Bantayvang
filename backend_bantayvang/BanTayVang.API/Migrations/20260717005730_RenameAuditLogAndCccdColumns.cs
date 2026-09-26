using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanTayVang.API.Migrations
{
    public partial class RenameAuditLogAndCccdColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("EXEC sp_rename '[ExamRegistrations].[Cccd]', 'IdCardNumber', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename '[AuditLogs].[PhuongThuc]', 'HttpMethod', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename '[AuditLogs].[DuongDan]', 'ApiPath', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename '[AuditLogs].[LoaiThaoTac]', 'ActionType', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename '[AuditLogs].[DiaChi_IP]', 'IpAddress', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename '[AuditLogs].[MaHttp]', 'HttpStatusCode', 'COLUMN';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("EXEC sp_rename '[ExamRegistrations].[IdCardNumber]', 'Cccd', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename '[AuditLogs].[HttpMethod]', 'PhuongThuc', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename '[AuditLogs].[ApiPath]', 'DuongDan', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename '[AuditLogs].[ActionType]', 'LoaiThaoTac', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename '[AuditLogs].[IpAddress]', 'DiaChi_IP', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename '[AuditLogs].[HttpStatusCode]', 'MaHttp', 'COLUMN';");
        }
    }
}
