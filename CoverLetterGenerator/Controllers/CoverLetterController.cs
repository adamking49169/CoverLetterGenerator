using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using CoverLetterApp.Models;
using CoverLetterApp.Services;

// Open XML SDK namespaces:
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

// If you are still using PdfPig + OpenXml in ExtractTextFromFile:
using UglyToad.PdfPig;
using DocumentFormat.OpenXml;

namespace CoverLetterApp.Controllers
{
    public class CoverLetterController : Controller
    {
        private readonly IWebHostEnvironment _env;

        private readonly IOpenAiService _openAi;

        public CoverLetterController(IOpenAiService openAi, IWebHostEnvironment env)
        {
            _openAi = openAi;
            _env = env;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(CoverLetterRequest request)
        {
            if (!ModelState.IsValid)
                return View("Index", request);

            // 1) Read job description text
            string jobDescriptionText = await ExtractTextFromFile(request.JobDescriptionFile);

            // 2) Read CV text (full body)
            string cvText = await ExtractTextFromFile(request.CVFile);

            // 3) Parse key fields from CV text
            //    We’ll assume:
            //      - Line 0: Name
            //      - Line 1: Title (e.g. “Business Analyst”)
            //      - Line 2: Address
            //      - Line 3: City, State, Zip
            //    Then use regex for email & phone anywhere.

            var lines = cvText
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            string name = lines.Length > 0 ? lines[0] : "";
            string title = lines.Length > 1 ? lines[1] : "";
            string addressLine = lines.Length > 2 ? lines[2] : "";
            string cityStateZip = lines.Length > 3 ? lines[3] : "";
            string fullAddress = addressLine + (string.IsNullOrWhiteSpace(cityStateZip) ? "" : ", " + cityStateZip);

            // 3a) Extract first matching email
            var emailMatch = Regex.Match(cvText,
                @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,6}");
            string email = emailMatch.Success ? emailMatch.Value : "";

            // 3b) Extract first matching phone number (loose North American style)
            var phoneMatch = Regex.Match(cvText,
                @"(\+?\d{1,3}[\s-]?)?            # optional country code
                  (\(?\d{3}\)?[\s-]?)           # area code
                  (\d{3}[\s-]?\d{4})            # first 3 + last 4 digits
                 ",
                RegexOptions.IgnorePatternWhitespace);
            string phone = phoneMatch.Success ? phoneMatch.Value.Trim() : "";

            // 4) Generate the “templated” cover letter with placeholders
            //    (Ensure your AI prompt explicitly uses those placeholders:
            //       [Your Name], [Your Address], [City, State, Zip], [Email], [Phone Number] )
            var generatedTemplatedLetter = await _openAi.GenerateCoverLetterAsync(jobDescriptionText, cvText);
            string noBrackets = Regex.Replace(generatedTemplatedLetter, @"\[[^\]]*\]", string.Empty);
            var filteredLines = noBrackets
       .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
       .Where(line => !string.IsNullOrWhiteSpace(line))
       .ToArray();

            string actualLetterBody = string.Join("\n\n", filteredLines);
            // 5) Replace placeholders in the templated letter
            //    So we end up with the “body” portion (with “Dear …” through the signature).
            //string actualLetterBody = generatedTemplatedLetter;


            // 6) Populate ViewModel
            var vm = new CoverLetterResponse
            {
                Name = "Name Test",
                Title = title,
                Email = email,
                Address = fullAddress,
                Phone = phone,
                CoverLetterBody = actualLetterBody
            };

            return View("Result", vm);
        }

        /// <summary>
        /// Downloads a DOCX version of the cover letter using our template.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DownloadWord(
             string name,
             string title,
             string email,
             string address,
             string phone,
             string coverLetterBody)
        {
            // 1) Validate inputs
            if (string.IsNullOrWhiteSpace(coverLetterBody) ||
                string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction("Index");
            }

            // 2) Find template path
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "CoverLetterTemplate.docx"
            );
            if (!System.IO.File.Exists(templatePath))
                return Content("ERROR: Template not found.");

            // 3) Copy template into a MemoryStream
            using var mem = new MemoryStream();
            using (var templateStream = System.IO.File.OpenRead(templatePath))
            {
                templateStream.CopyTo(mem);
            }
            mem.Position = 0;

            // 4) Open with Open XML SDK
            using (var wordDoc = WordprocessingDocument.Open(mem, true))
            {
                var mainPart = wordDoc.MainDocumentPart;
                var doc = mainPart.Document;

                // Helper to replace simple placeholders inside a single <w:t> run
                void SimpleReplace(OpenXmlElement root, string placeholder, string value)
                {
                    foreach (var text in root.Descendants<Text>()
                                             .Where(t => t.Text.Contains(placeholder)))
                    {
                        text.Text = text.Text.Replace(placeholder, value);
                    }
                }

                // 4a) Replace header placeholders (in main body, headers, footers)
                SimpleReplace(mainPart.Document, "{{Name}}", name);
                SimpleReplace(mainPart.Document, "{{Title}}", title);
                SimpleReplace(mainPart.Document, "{{Email}}", email);
                SimpleReplace(mainPart.Document, "{{Address}}", address);
                SimpleReplace(mainPart.Document, "{{Phone}}", phone);

                // Also replace in header parts (if your placeholders live there)
                foreach (var headerPart in mainPart.HeaderParts)
                {
                    SimpleReplace(headerPart.Header, "{{Name}}", name);
                    SimpleReplace(headerPart.Header, "{{Title}}", title);
                    SimpleReplace(headerPart.Header, "{{Email}}", email);
                    SimpleReplace(headerPart.Header, "{{Address}}", address);
                    SimpleReplace(headerPart.Header, "{{Phone}}", phone);
                    headerPart.Header.Save();
                }

                // (If you have footer placeholders, do the same for footerPart.Footer)

                // 4b) Handle the {{Body}} placeholder by splitting into real Word paragraphs
                // First: find the PARAGRAPH (<w:p>) that contains any <w:t> whose Text includes "{{Body}}"
                var bodyParagraph = doc.Descendants<Paragraph>()
                    .FirstOrDefault(p => p.Descendants<Text>().Any(t => t.Text.Contains("{{Body}}")));

                if (bodyParagraph != null)
                {
                    // Remove all runs in that paragraph that contain "{{Body}}"
                    foreach (var run in bodyParagraph.Descendants<Run>()
                                            .Where(r => r.Descendants<Text>().Any(t => t.Text.Contains("{{Body}}")))
                                            .ToList())
                    {
                        run.Remove();
                    }

                    // Now split coverLetterBody into paragraph blocks. 
                    // We consider blank lines (two newlines) as paragraph separators.
                    var paragraphBlocks = coverLetterBody
                        .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.None);

                    // Insert a new Paragraph for each block of text, immediately after the original placeholder paragraph
                    var parent = bodyParagraph.Parent; // typically a Body element
                    OpenXmlElement insertAfter = bodyParagraph;

                    foreach (var block in paragraphBlocks)
                    {
                        // Create a new <w:p>
                        var newPara = new Paragraph();
                        var newRun = new Run();

                        var runProps = new RunProperties();
                        runProps.Append(new FontSize() { Val = "16" });
                        newRun.PrependChild(runProps);

                        // Split the block by single newlines (soft line breaks). 
                        // For each line within the block, append a <w:t> and a <w:br> (except last line)
                        var lines = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            var textNode = new Text(lines[i])
                            {
                                Space = SpaceProcessingModeValues.Preserve
                            };
                            newRun.Append(textNode);

                            // If not the last line, insert a break
                            if (i < lines.Length - 1)
                                newRun.Append(new Break());
                        }

                        newPara.Append(newRun);

                        // Insert the new paragraph after the placeholder paragraph (or previous insertion)
                        parent.InsertAfter(newPara, insertAfter);
                        insertAfter = newPara;
                    }

                    // Finally, remove the original (now-empty) placeholder paragraph so it doesn't leave a gap
                    bodyParagraph.Remove();
                }

                mainPart.Document.Save();
            }

            // 5) Return the MemoryStream as a FileResult
            mem.Position = 0;
            return File(
                mem.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "CoverLetter.docx"
            );
        }

        /// <summary>
        /// Reads an IFormFile and extracts its text. Supports .pdf and .docx via Open XML,
        /// otherwise treats as plain text.
        /// </summary>
        private async Task<string> ExtractTextFromFile(Microsoft.AspNetCore.Http.IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension == ".pdf")
            {
                using var stream = file.OpenReadStream();
                using var pdfDoc = PdfDocument.Open(stream);
                var sb = new StringBuilder();
                foreach (var page in pdfDoc.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
                return sb.ToString();
            }
            else if (extension == ".docx")
            {
                using var stream = file.OpenReadStream();
                using var wordDoc = WordprocessingDocument.Open(stream, false);
                var body = wordDoc.MainDocumentPart.Document.Body;
                var allText = body
                    .Descendants<Text>()
                    .Select(t => t.Text)
                    .Where(t => !string.IsNullOrWhiteSpace(t));
                return string.Join(Environment.NewLine, allText);
            }
            else
            {
                using var reader = new StreamReader(file.OpenReadStream());
                return await reader.ReadToEndAsync();
            }
        }
    }
}
