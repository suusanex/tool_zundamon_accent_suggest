using NUnit.Framework;
using Microsoft.Extensions.Logging.Abstractions;
using VoicevoxHelper.Core.Models;
using VoicevoxHelper.Infrastructure.LlmService;

namespace VoicevoxHelper.Tests.Infrastructure.LlmService;

[TestFixture]
public class AzureOpenAIServiceTests
{
    private sealed class FakeChatClient : IChatClient
    {
        private readonly string _response;

        public FakeChatClient(string response)
        {
            _response = response;
        }

        public Task<ChatCompletionResult> GetChatCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ChatCompletionResult
            {
                Content = _response,
                PromptTokens = 10,
                CompletionTokens = 5,
                TotalTokens = 15
            });
        }
    }

    [Test]
    public async Task ExtractDictionaryCandidatesAsync_WhenValidJson_ReturnsDeduplicated()
    {
        var json = "[{\"surface\":\"VOICEVOX\",\"pronunciation\":\"ボイスボックス\",\"accentType\":1},{\"surface\":\"VOICEVOX\",\"pronunciation\":\"ボイスボックス\",\"accentType\":1}]";
        var service = new AzureOpenAIService(new FakeChatClient(json), NullLogger<AzureOpenAIService>.Instance, new LlmSettings());

        var result = await service.ExtractDictionaryCandidatesAsync(new Script { Text = "test" }, CancellationToken.None);

        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RewriteScriptAsync_ReturnsResponse()
    {
        var service = new AzureOpenAIService(new FakeChatClient("リライト"), NullLogger<AzureOpenAIService>.Instance, new LlmSettings());

        var result = await service.RewriteScriptAsync(new Script { Text = "test" }, CancellationToken.None);

        Assert.That(result, Is.EqualTo("リライト"));
    }
}
