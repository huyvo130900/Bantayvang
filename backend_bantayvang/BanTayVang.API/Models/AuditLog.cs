using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models;

public partial class AuditLog
{
    public int Id { get; set; }

    /// <summary>FK → USER - người thực hiện thao tác</summary>
    public int? UserId { get; set; }

    /// <summary>Username cached tại thời điểm log (không join khi đọc)</summary>
    public string? Username { get; set; }

    public int? ExamSubmissionId { get; set; }

    /// <summary>HTTP Method: GET, POST, PUT, DELETE, PATCH</summary>
    public string? HttpMethod { get; set; }

    /// <summary>API Path: /api/Question/123</summary>
    public string? ApiPath { get; set; }

    /// <summary>Loại thao tác: POST_CAUHOI, DELETE_USER, etc.</summary>
    public string? ActionType { get; set; }

    /// <summary>Chi tiết thao tác (ghi chú)</summary>
    public string? Detail { get; set; }

    public DateTime? ActionTime { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    /// <summary>HTTP status code: 200, 400, 500...</summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>Khoa/Phòng của người thực hiện</summary>
    public string? Department { get; set; }

    public virtual ExamSubmission? ExamSubmission { get; set; }
    public virtual User? User { get; set; }
}
