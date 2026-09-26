using System.Collections.Generic;
using System.Linq;
using BanTayVang.API.Models;

namespace BanTayVang.API.Helpers
{
    /// <summary>
    /// Tên khoa dùng cho ExamPaper.Department. Mọi kiểm tra quyền Quản lý khoa trong toàn bộ
    /// ứng dụng (ExamValidationService, ExamService, GradingController, ExamController...) so
    /// sánh field này bằng == với ĐÚNG 1 chuỗi tên khoa - không có nơi nào hiểu "nhiều khoa".
    /// Nối nhiều tên khoa thành 1 chuỗi sẽ không bao giờ khớp == với tên khoa thật của bất kỳ
    /// Quản lý khoa nào. Chỉ trả về tên khoa khi kỳ thi gán ĐÚNG 1 khoa (khớp chính xác);
    /// 0 hoặc 2+ khoa trả về null - giữ đúng quy ước "kỳ thi dùng chung/không gán khoa thì
    /// Quản lý khoa không tự quản lý đề thủ công được" (Admin vẫn quản lý được).
    /// (Trước đây copy-paste độc lập ở ExamManagementService.cs và ExamCampaignService.cs.)
    /// </summary>
    public static class DepartmentLabelHelper
    {
        public static string? BuildDepartmentLabel(ExamCampaign examCampaign)
        {
            return BuildDepartmentLabel(examCampaign.ExamCampaignDepartments?.Select(kd => kd.Department?.DepartmentName));
        }

        public static string? BuildDepartmentLabel(IEnumerable<string?>? departmentNames)
        {
            var names = (departmentNames ?? Enumerable.Empty<string?>())
                .Where(n => !string.IsNullOrEmpty(n))
                .Select(n => n!)
                .ToList();
            return names.Count == 1 ? names[0] : null;
        }
    }
}
