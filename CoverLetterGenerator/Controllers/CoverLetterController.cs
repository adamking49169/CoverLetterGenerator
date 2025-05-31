using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;                       // PdfPig for .pdf
using DocumentFormat.OpenXml.Packaging;      // Open XML SDK
using DocumentFormat.OpenXml.Wordprocessing; // Wordprocessing elements

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
            if (!ModelState.IsValid)
            {
                return View("Index", request);
            }

            // Extract text from Job Description (txt/pdf/docx)
            string jobDescriptionText = await ExtractTextFromFile(request.JobDescriptionFile);

            // Extract text from CV (txt/pdf/docx)
            string cvText = await ExtractTextFromFile(request.CVFile);

            // Now call your service
            var generatedCoverLetter = await _openAi.GenerateCoverLetterAsync(jobDescriptionText, cvText);

            var vm = new CoverLetterResponse
            {
                CoverLetter = generatedCoverLetter
            };

            return View("Result", vm);
        }

        /// <summary>
        /// Reads an IFormFile. If it’s .pdf, uses PdfPig. If it’s .docx, uses Open XML SDK.
        /// Otherwise, treats as plain text.
        /// </summary>
        private async Task<string> ExtractTextFromFile(IFormFile file)
        {
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // 1) PDF (.pdf) using PdfPig
            if (extension == ".pdf")
            {
                using (var stream = file.OpenReadStream())
                using (var pdfDocument = PdfDocument.Open(stream))
                {
                    var sb = new StringBuilder();
                    foreach (var page in pdfDocument.GetPages())
                    {
                        sb.AppendLine(page.Text);
                    }
                    return sb.ToString();
                }
            }
            // 2) DOCX (.docx) using Open XML SDK
            else if (extension == ".docx")
            {
                using (var stream = file.OpenReadStream())
                using (var wordDoc = WordprocessingDocument.Open(stream, false))
                {
                    var body = wordDoc.MainDocumentPart.Document.Body;

                    // Extract all the TEXT() nodes from paragraphs, tables, headers, footers, etc.
                    // A common quick way is to grab every <w:t> element under the main document body:
                    var allText = body
                        .Descendants<Text>()
                        .Select(t => t.Text)
                        .Where(txt => !string.IsNullOrWhiteSpace(txt));

                    // Join with spaces or newlines as you prefer
                    return string.Join(" ", allText);
                }
            }
            // 3) Fallback: treat as plain text
            else
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
    }
}
