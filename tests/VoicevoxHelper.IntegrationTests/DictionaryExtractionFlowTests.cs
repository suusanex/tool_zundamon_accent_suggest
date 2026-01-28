using NUnit.Framework;
using Microsoft.Extensions.Logging.Abstractions;
using VoicevoxHelper.Core.Models;
using VoicevoxHelper.Infrastructure.FileIO;
using VoicevoxHelper.Infrastructure.LlmService;
using VoicevoxHelper.Infrastructure.Masking;

namespace VoicevoxHelper.IntegrationTests;

[TestFixture]
public class DictionaryExtractionFlowTests
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
    public async Task ExtractionFlow_WritesCsv()
    {
        var masking = new PersonalInfoMaskingService();
        var llm = new AzureOpenAIService(
            new FakeChatClient("[{\"surface\":\"VOICEVOX\",\"pronunciation\":\"ボイスボックス\",\"accentType\":1}]"),
            NullLogger<AzureOpenAIService>.Instance,
            new LlmSettings());
        var csv = new CsvDictionaryParser();

        var masked = masking.Mask("test@example.com");
        var candidates = await llm.ExtractDictionaryCandidatesAsync(new Script { Text = masked.MaskedText }, CancellationToken.None);
        var output = csv.Write(candidates);

        Assert.That(output, Does.Contain("VOICEVOX"));
    }
}
