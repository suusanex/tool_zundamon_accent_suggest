using System.Net;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using VoicevoxHelper.Core.Models;
using VoicevoxHelper.Infrastructure.LlmService;

namespace VoicevoxHelper.Tests.Infrastructure.LlmService;

[TestFixture]
public class OpenAiChatClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }
        public Uri? RequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestBody = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            var responseJson = """
            {
              "choices": [
                {
                  "message": { "content": "{}" }
                }
              ],
              "usage": {
                "prompt_tokens": 1,
                "completion_tokens": 1,
                "total_tokens": 2
              }
            }
            """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }

    [Test]
    public async Task GetChatCompletionAsync_WhenRequireJson_IncludesResponseFormat()
    {
        var handler = new StubHandler();
        var httpClient = new HttpClient(handler);
        var settings = new LlmSettings
        {
            Endpoint = "https://example.openai.azure.com/",
            ApiKey = "test-key",
            Deployment = "test-deployment",
            ApiVersion = "2024-08-01-preview",
            TimeoutSeconds = 30
        };
        var client = new OpenAiChatClient(httpClient, settings);

        await client.GetChatCompletionAsync("system", "user", CancellationToken.None, requireJson: true);

        Assert.That(handler.RequestBody, Is.Not.Null);
        using var document = JsonDocument.Parse(handler.RequestBody!);
        var responseFormat = document.RootElement.GetProperty("response_format");
        Assert.That(responseFormat.GetProperty("type").GetString(), Is.EqualTo("json_object"));
    }
}
