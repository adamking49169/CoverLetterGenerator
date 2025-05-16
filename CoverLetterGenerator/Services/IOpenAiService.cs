using System.Threading.Tasks;

namespace CoverLetterApp.Services
{
    public interface IOpenAiService
    {
        Task<string> GenerateCoverLetterAsync(string jobDescription, string cvText);
    }
}