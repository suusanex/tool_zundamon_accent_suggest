using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using VoicevoxHelper.Core.Exceptions;
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

                public Task<ChatCompletionResult> GetChatCompletionAsync(
                        string systemPrompt,
                        string userPrompt,
                        CancellationToken cancellationToken,
                        bool requireJson = false)
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
        public async Task ExtractDictionaryCandidatesAsync_WhenWrapperFormat_ParsesCorrectly()
        {
                var json = """
                {
                    "candidates": [
                        {
                            "surface": "東京",
                            "pronunciation": "トウキョウ",
                            "accent_type": 0
                        }
                    ]
                }
                """;
                var service = new AzureOpenAIService(new FakeChatClient(json), NullLogger<AzureOpenAIService>.Instance, new LlmSettings());

                var result = await service.ExtractDictionaryCandidatesAsync(new Script { Text = "test" }, CancellationToken.None);

                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result[0].Surface, Is.EqualTo("東京"));
                Assert.That(result[0].Pronunciation, Is.EqualTo("トウキョウ"));
                Assert.That(result[0].AccentType, Is.EqualTo(0));
        }

        [Test]
        public async Task ExtractDictionaryCandidatesAsync_WhenTopLevelArray_ParsesCorrectly()
        {
                var json = """
                [
                    {
                        "surface": "VOICEVOX",
                        "pronunciation": "ボイスボックス",
                        "accent_type": 3
                    }
                ]
                """;
                var service = new AzureOpenAIService(new FakeChatClient(json), NullLogger<AzureOpenAIService>.Instance, new LlmSettings());

                var result = await service.ExtractDictionaryCandidatesAsync(new Script { Text = "test" }, CancellationToken.None);

                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result[0].Surface, Is.EqualTo("VOICEVOX"));
                Assert.That(result[0].AccentType, Is.EqualTo(3));
        }

        [Test]
        public async Task ExtractDictionaryCandidatesAsync_WhenTopLevelArray_EmitsWarningLog()
        {
                var json = """
                [
                    {
                        "surface": "日本",
                        "pronunciation": "ニホン",
                        "accent_type": 1
                    }
                ]
                """;
                var mockLogger = new Mock<ILogger<AzureOpenAIService>>();
                var service = new AzureOpenAIService(new FakeChatClient(json), mockLogger.Object, new LlmSettings());

                var result = await service.ExtractDictionaryCandidatesAsync(new Script { Text = "test" }, CancellationToken.None);

                Assert.That(result, Has.Count.EqualTo(1));
                mockLogger.Verify(
                        x => x.Log(
                                LogLevel.Warning,
                                It.IsAny<EventId>(),
                                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("top-level array")),
                                It.IsAny<Exception>(),
                                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                        Times.Once);
        }

        [Test]
        public void ExtractDictionaryCandidatesAsync_WhenInvalidJson_ThrowsException()
        {
                var invalidJson = "<not json>";
                var service = new AzureOpenAIService(new FakeChatClient(invalidJson), NullLogger<AzureOpenAIService>.Instance, new LlmSettings());

                var ex = Assert.ThrowsAsync<LlmResponseParseException>(
                        () => service.ExtractDictionaryCandidatesAsync(new Script { Text = "test" }, CancellationToken.None));

                Assert.That(ex!.Message, Does.Contain("JSON形式"));
        }

        [Test]
        public void ExtractDictionaryCandidatesAsync_WhenWrapperEmpty_ThrowsException()
        {
                var json = """
                { "candidates": [] }
                """;
                var service = new AzureOpenAIService(new FakeChatClient(json), NullLogger<AzureOpenAIService>.Instance, new LlmSettings());

                var ex = Assert.ThrowsAsync<LlmResponseParseException>(
                        () => service.ExtractDictionaryCandidatesAsync(new Script { Text = "test" }, CancellationToken.None));

                Assert.That(ex!.Message, Does.Contain("候補"));
        }

        [Test]
        public async Task ExtractDictionaryCandidatesAsync_WhenJapaneseSurface_NotDropped()
        {
                var json = """
                {
                    "candidates": [
                        {
                            "surface": "東京",
                            "pronunciation": "トウキョウ",
                            "accent_type": 0
                        },
                        {
                            "surface": "VOICEVOX",
                            "pronunciation": "ボイスボックス",
                            "accent_type": 3
                        }
                    ]
                }
                """;
                var service = new AzureOpenAIService(new FakeChatClient(json), NullLogger<AzureOpenAIService>.Instance, new LlmSettings());

                var result = await service.ExtractDictionaryCandidatesAsync(new Script { Text = "test" }, CancellationToken.None);

                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That(result.Any(c => c.Surface == "東京"), Is.True);
                Assert.That(result.Any(c => c.Surface == "VOICEVOX"), Is.True);
        }

        [Test]
        public async Task ExtractDictionaryCandidatesAsync_WhenValidJson_ReturnsDeduplicated()
        {
                var json = """
                {
                    "candidates": [
                        { "surface": "VOICEVOX", "pronunciation": "ボイスボックス", "accent_type": 1 },
                        { "surface": "VOICEVOX", "pronunciation": "ボイスボックス", "accent_type": 1 }
                    ]
                }
                """;
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
