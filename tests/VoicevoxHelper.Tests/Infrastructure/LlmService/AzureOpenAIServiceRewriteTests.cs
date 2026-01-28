using NUnit.Framework;
using Microsoft.Extensions.Logging.Abstractions;
using VoicevoxHelper.Core.Models;
using VoicevoxHelper.Infrastructure.LlmService;

namespace VoicevoxHelper.Tests.Infrastructure.LlmService;

[TestFixture]
public class AzureOpenAIServiceRewriteTests
{
    private sealed class FakeChatClient : IChatClient
    {
        public Task<ChatCompletionResult> GetChatCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ChatCompletionResult
            {
                Content = "リライト結果",
                PromptTokens = 10,
                CompletionTokens = 5,
                TotalTokens = 15
            });
        }
    }

    [Test]
    public async Task RewriteScriptAsync_ReturnsContent()
    {
        var service = new AzureOpenAIService(new FakeChatClient(), NullLogger<AzureOpenAIService>.Instance, new LlmSettings());
        var result = await service.RewriteScriptAsync(new Script { Text = "元の台本" }, CancellationToken.None);

        Assert.That(result, Is.EqualTo("リライト結果"));
    }
}
