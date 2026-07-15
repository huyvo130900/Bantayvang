using ClosedXML.Excel;
using BanTayVang.API.Models;

namespace BanTayVang.API.Services.Interfaces.Import
{
    public interface IQuestionImportStrategy
    {
        string QuestionTypeName { get; }
        
        Task<List<Question>> ParseAndValidateAsync(
            IXLWorksheet worksheet, 
            int createdBy, 
            string department, 
            List<string> errors,
            bool isExamImport = false);
            
        void GenerateTemplate(IXLWorksheet worksheet, bool isExamImport = false);
    }
}
