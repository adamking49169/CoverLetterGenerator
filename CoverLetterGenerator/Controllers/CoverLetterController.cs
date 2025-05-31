using System.IO;
using System.Threading.Tasks;
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
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Generate(CoverLetterRequest request)
        {
            // If model validation fails (e.g. no file was uploaded), re-show the form.
            if (!ModelState.IsValid)
            {
                return View("Index", request);
            }

            // Read Job Description file into a string
            string jobDescriptionText;
            using (var reader = new StreamReader(request.JobDescriptionFile.OpenReadStream()))
            {
                jobDescriptionText = await reader.ReadToEndAsync();
            }

            // Read CV file into a string
            string cvText;
            using (var reader = new StreamReader(request.CVFile.OpenReadStream()))
            {
                cvText = await reader.ReadToEndAsync();
            }

            // Call your existing service (which expects two plain-text strings)
            var generatedCoverLetter = await _openAi.GenerateCoverLetterAsync(jobDescriptionText, cvText);

            // Package into a view model for the Result view
            var vm = new CoverLetterResponse
            {
                CoverLetter = generatedCoverLetter
            };

            return View("Result", vm);
        }
    }
}
