using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoverLetterApp.Services;
using Xunit;

namespace CoverLetterGenerator.Tests.Services
{
    public class OpenAiServiceTests
    {
        private class TestHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task> _assert;
            private readonly HttpResponseMessage _response;

            public TestHandler(Func<HttpRequestMessage, Task> assert, HttpResponseMessage response)
            {
                _assert = assert;
                _response = response;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (_assert != null)
                {
                    await _assert(request);
                }
                return _response;
            }
        }

        [Fact]
        public async Task GenerateCoverLetterAsync_SendsProperRequestAndParsesResponse()
        {
            var expectedReply = "Generated letter";
            var handler = new TestHandler(async req =>
            {
                Assert.Equal(HttpMethod.Post, req.Method);
                Assert.Equal("https://api.openai.com/v1/chat/completions", req.RequestUri!.ToString());
                Assert.Equal("Bearer test-key", req.Headers.Authorization!.ToString());
                var body = await req.Content!.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                Assert.Equal("gpt-4", doc.RootElement.GetProperty("model").GetString());
            }, new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"choices\":[{{\"message\":{{\"content\":\"{expectedReply}\"}}}}]}}")
            });

            var httpClient = new HttpClient(handler);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");
            var service = new OpenAiService(httpClient);

            var result = await service.GenerateCoverLetterAsync("job", "cv");

            Assert.Equal(expectedReply, result);
        }
    }
}