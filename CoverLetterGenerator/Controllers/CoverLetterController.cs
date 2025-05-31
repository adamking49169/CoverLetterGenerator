using System.IO;
using System.Text;
using System.Threading.Tasks;
using CoverLetterApp.Models;
using CoverLetterApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;          

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

            // Extract text from each uploaded file (handles .txt or .pdf)
            string jobDescriptionText = await ExtractTextFromFile(request.JobDescriptionFile);
            string cvText = await ExtractTextFromFile(request.CVFile);

            // Call your existing service (which expects two plain-text strings)
            var generatedCoverLetter = await _openAi.GenerateCoverLetterAsync(jobDescriptionText, cvText);

            // Package into a view model for the Result view
            var vm = new CoverLetterResponse
            {
                CoverLetter = generatedCoverLetter
            };

            return View("Result", vm);
        }

        /// <summary>
        /// Reads an IFormFile. If it’s a PDF (.pdf extension), uses PdfPig to extract text.
        /// Otherwise, reads it as plain text via StreamReader.
        /// </summary>
        private async Task<string> ExtractTextFromFile(IFormFile file)
        {
            // Determine the file extension in lowercase
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension == ".pdf")
            {
                // Use PdfPig to extract text from PDF
                using (var stream = file.OpenReadStream())
                using (PdfDocument document = PdfDocument.Open(stream))
                {
                    var sb = new StringBuilder();

                    foreach (var page in document.GetPages())
                    {
                        // Append each page’s text
                        sb.AppendLine(page.Text);
                    }

                    return sb.ToString();
                }
            }
            else
            {
                // Fallback: treat as plain text
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
    }
}
