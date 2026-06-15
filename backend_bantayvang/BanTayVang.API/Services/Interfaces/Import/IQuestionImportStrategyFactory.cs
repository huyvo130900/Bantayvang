namespace BanTayVang.API.Services.Interfaces.Import
{
    public interface IQuestionImportStrategyFactory
    {
        IQuestionImportStrategy GetStrategy(string questionTypeName);
        IQuestionImportStrategy GetStrategyById(int questionTypeId);
    }
}
