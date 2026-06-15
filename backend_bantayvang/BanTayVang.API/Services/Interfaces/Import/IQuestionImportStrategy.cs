using ClosedXML.Excel;
using BanTayVang.API.Models;

namespace BanTayVang.API.Services.Interfaces.Import
{
    public interface IQuestionImportStrategy
    {
        string QuestionTypeName { get; }
        
        Task<List<Cauhoi>> ParseAndValidateAsync(
            IXLWorksheet worksheet, 
            int nguoiTao, 
            string khoaPhong, 
            List<string> errors,
            bool isExamImport = false);
            
        void GenerateTemplate(IXLWorksheet worksheet, bool isExamImport = false);
    }
}
