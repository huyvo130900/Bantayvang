using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models
{
    /// <summary>
    /// Bảng nối kỳ thi - khoa: 1 kỳ thi có thể gán cho 1-n khoa (thay cho ExamCampaign.DepartmentId
    /// đơn trước đây). Quyết định eligibility của học viên/thí sinh và quyền quản lý của Quản lý
    /// khoa đối với kỳ thi.
    /// </summary>
    public class ExamCampaignDepartment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExamCampaignId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [ForeignKey("ExamCampaignId")]
        public virtual ExamCampaign? ExamCampaign { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
    }
}
