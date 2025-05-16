using Microsoft.AspNetCore.Mvc;
using CoverLetterApp.Models;
using CoverLetterApp.Services;

namespace CoverLetterApp.Controllers
{
    public class CoverLetterController : Controller
    {
        private readonly IOpenAiService _openAi;
        public CoverLetterController(IOpenAiService openAi)
        {
            _openAi = openAi;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Generate(CoverLetterRequest request)
        {
            if (!ModelState.IsValid)
                return View("Index");

            var result = await _openAi.GenerateCoverLetterAsync(request.JobDescription, request.CVText);
            var vm = new CoverLetterResponse { CoverLetter = result };
            return View("Result", vm);
        }
    }
}
