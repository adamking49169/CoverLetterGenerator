using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CoverLetterApp.Controllers;
using CoverLetterApp.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CoverLetterGenerator.Tests.Controllers
{
    public class CoverLetterControllerTests
    {
        private class DummyService : IOpenAiService
        {
            public Task<string> GenerateCoverLetterAsync(string a, string b) => Task.FromResult(string.Empty);
        }

        private static Task<string> InvokeExtract(CoverLetterController controller, IFormFile file)
        {
            var method = typeof(CoverLetterController).GetMethod("ExtractTextFromFile", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Task<string>)method.Invoke(controller, new object[] { file });
        }

        private static IFormFile CreateDocx(string text)
        {
            var mem = new MemoryStream();
            using (var wordDoc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text(text)))));
            }
            mem.Position = 0;
            return new FormFile(mem, 0, mem.Length, "Data", "test.docx");
        }

        private static IFormFile CreateText(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var mem = new MemoryStream(bytes);
            return new FormFile(mem, 0, bytes.Length, "Data", "test.txt");
        }

        [Fact]
        public async Task ExtractTextFromFile_ReadsDocx()
        {
            var controller = new CoverLetterController(new DummyService(), null);
            var file = CreateDocx("Hello world");

            var result = await InvokeExtract(controller, file);

            Assert.Contains("Hello world", result);
        }

        [Fact]
        public async Task ExtractTextFromFile_ReadsPlainText()
        {
            var controller = new CoverLetterController(new DummyService(), null);
            var file = CreateText("Plain text file");

            var result = await InvokeExtract(controller, file);

            Assert.Equal("Plain text file", result);
        }
    }
}