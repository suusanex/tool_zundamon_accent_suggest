using System.Text.Json;
using Microsoft.Extensions.Logging;
using VoicevoxHelper.Core.Exceptions;
using VoicevoxHelper.Core.Interfaces;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Infrastructure.LlmService;

/// <summary>
/// Azure OpenAI連携サービス。
/// </summary>
public sealed class AzureOpenAIService : ILlmService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly LlmSettings _settings;

    public AzureOpenAIService(
        IChatClient chatClient,
        ILogger<AzureOpenAIService> logger,
        Microsoft.Extensions.Options.IOptions<LlmSettings> options)
    {
        _chatClient = chatClient;
        _logger = logger;
        _settings = options.Value;
    }

    public AzureOpenAIService(
        IChatClient chatClient,
        ILogger<AzureOpenAIService> logger,
        LlmSettings settings)
    {
        _chatClient = chatClient;
        _logger = logger;
        _settings = settings;
    }

    public async Task<IReadOnlyList<DictionaryCandidate>> ExtractDictionaryCandidatesAsync(
        Script script,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _chatClient.GetChatCompletionAsync(
                PromptTemplates.DictionaryExtractionSystem,
                PromptTemplates.DictionaryExtractionUser + "\n" + script.Text,
                cancellationToken);
            LogTokenUsage(result);
            var candidates = ParseCandidates(result.Content);
            return Deduplicate(candidates);
        }
        catch (Exception ex)
        {
            _logger.LogError("LLM extraction failed: {Exception}", ex.ToString());
            throw;
        }
    }

    public async Task<string> RewriteScriptAsync(Script script, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _chatClient.GetChatCompletionAsync(
                PromptTemplates.ScriptRewriteSystem,
                PromptTemplates.ScriptRewriteUser + "\n" + script.Text,
                cancellationToken);
            LogTokenUsage(result);
            return result.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError("LLM rewrite failed: {Exception}", ex.ToString());
            throw;
        }
    }

    private static IReadOnlyList<DictionaryCandidate> Deduplicate(IReadOnlyList<DictionaryCandidate> candidates)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<DictionaryCandidate>();

        foreach (var candidate in candidates)
        {
            if (seen.Add(candidate.Surface))
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    private static IReadOnlyList<DictionaryCandidate> ParseCandidates(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var items = JsonSerializer.Deserialize<List<DictionaryCandidate>>(json, options);
            return items ?? new List<DictionaryCandidate>();
        }
        catch (Exception ex)
        {
            throw new LlmResponseParseException("LLMのレスポンス解析に失敗しました。", ex);
        }
    }

    private void LogTokenUsage(ChatCompletionResult result)
    {
        var promptCost = (result.PromptTokens / 1000m) * _settings.PromptCostPer1kTokens;
        var completionCost = (result.CompletionTokens / 1000m) * _settings.CompletionCostPer1kTokens;
        var totalCost = promptCost + completionCost;
        _logger.LogInformation(
            "TokenUsage prompt={Prompt} completion={Completion} total={Total} cost={Cost}",
            result.PromptTokens,
            result.CompletionTokens,
            result.TotalTokens,
            totalCost);
    }
}
