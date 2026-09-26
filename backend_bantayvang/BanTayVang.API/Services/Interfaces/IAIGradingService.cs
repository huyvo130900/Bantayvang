namespace BanTayVang.API.Services.Interfaces
{
    /// <summary>
    /// Service giao tiep voi AI provider de cham diem cau tu luan
    /// </summary>
    public interface IAIGradingService
    {
        /// <summary>
        /// Goi AI de cham 1 cau tu luan.
        /// Tra ve (score, comment) - score chi duoc la 0, 0.5 hoac 1 - CHI khi cham thanh cong
        /// (bao gom ca truong hop hop le "thi sinh bo trong cau tra loi" -> (0, "...")).
        ///
        /// NEM EXCEPTION khi co loi he thong that su (sai cau hinh, goi AI that bai sau khi da
        /// retry, response khong parse duoc...). KHONG duoc nuot loi va tra ve (0, "loi...") vi
        /// caller (AiGradingWorker) se hieu nham day la ket qua cham hop le va luu 0 diem lam
        /// diem chinh thuc cua thi sinh.
        /// </summary>
        Task<(double Score, string Comment)> GradeEssayAsync(
            string questionContent,
            string? suggestedAnswer,
            string? essayAnswer,
            string? essayImageUrl,
            CancellationToken ct = default);
    }
}
