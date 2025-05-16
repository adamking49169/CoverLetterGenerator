using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoverLetterApp.Services
{
    public class OpenAiService : IOpenAiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public OpenAiService(HttpClient http)
        {
            
            _http = http;
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                      ?? throw new InvalidOperationException("OPENAI_API_KEY is not set");
        }

        public async Task<string> GenerateCoverLetterAsync(string jobDescription, string cvText)
        {
            var prompt = $"Write a professional cover letter for the following job description:\n{jobDescription}\n\nUse details from this CV to highlight qualifications:\n{cvText}";

            var payload = new
            {
                model = "gpt-4",
                messages = new[]
                {
            new { role = "system",  content = "You are a helpful assistant that writes cover letters." },
            new { role = "user",    content = prompt }
        },
                max_tokens = 500,
                temperature = 0.7
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.openai.com/v1/chat/completions"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            // Pull out the assistant's reply from choices[0].message.content
            var content = doc.RootElement
                             .GetProperty("choices")[0]
                             .GetProperty("message")
                             .GetProperty("content")
                             .GetString();

            return content.Trim();
        }

    }
}
