using System.Text.Json;
using System.Text.Json.Serialization;
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
        const string context = "DictionaryExtraction";
        var userPrompt = PromptTemplates.DictionaryExtractionUser + script.Text;
        LogLlmRequest(context, userPrompt.Length, script.Text.Length);

        try
        {
            var result = await _chatClient.GetChatCompletionAsync(
                PromptTemplates.DictionaryExtractionSystem,
                userPrompt,
                cancellationToken,
                requireJson: true);
            LogTokenUsage(result, context);
            LogLlmResponse(context, result);
            var candidates = ParseCandidates(result.Content, context);
            _logger.LogInformation("LLM {Context} returned {Count} candidates", context, candidates.Count);
            return Deduplicate(candidates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM extraction failed for {Context}; script length: {Length}", context, script.Text.Length);
            throw;
        }
    }

    public async Task<string> RewriteScriptAsync(Script script, CancellationToken cancellationToken)
    {
        const string context = "ScriptRewrite";
        var rewritePrompt = PromptTemplates.ScriptRewriteUser + "\n" + script.Text;
        LogLlmRequest(context, rewritePrompt.Length, script.Text.Length);

        try
        {
            var result = await _chatClient.GetChatCompletionAsync(
                PromptTemplates.ScriptRewriteSystem,
                rewritePrompt,
                cancellationToken);
            LogTokenUsage(result, context);
            LogLlmResponse(context, result);
            return result.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM rewrite failed for {Context}; script length: {Length}", context, script.Text.Length);
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

    private IReadOnlyList<DictionaryCandidate> ParseCandidates(string json, string context)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogError("LLM parsing failed for {Context}. Response was empty.", context);
            throw new LlmResponseParseException("LLMのレスポンスが空です。");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var wrapperResult = TryParseWrapper(json, options, context);
            if (wrapperResult != null)
            {
                return wrapperResult;
            }

            var arrayResult = TryParseTopLevelArray(json, options, context);
            if (arrayResult != null)
            {
                return arrayResult;
            }

            _logger.LogError("LLM parsing failed for {Context}. Response format does not match expected JSON structure.", context);
            throw new LlmResponseParseException(
                "LLMのレスポンスがJSON形式ではありません。期待される形式: { \"candidates\": [...] }",
                json,
                new InvalidOperationException("Unexpected JSON format."));
        }
        catch (LlmResponseParseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during LLM parsing for {Context}", context);
            throw new LlmResponseParseException("LLMのレスポンス解析に失敗しました。", json, ex);
        }
    }

    private void LogTokenUsage(ChatCompletionResult result, string context)
    {
        var promptCost = (result.PromptTokens / 1000m) * _settings.PromptCostPer1kTokens;
        var completionCost = (result.CompletionTokens / 1000m) * _settings.CompletionCostPer1kTokens;
        var totalCost = promptCost + completionCost;
        _logger.LogInformation(
            "LLM {Context} token usage prompt={Prompt} completion={Completion} total={Total} cost={Cost}",
            context,
            result.PromptTokens,
            result.CompletionTokens,
            result.TotalTokens,
            totalCost);
    }

    private void LogLlmRequest(string context, int promptLength, int scriptLength)
    {
        _logger.LogInformation(
            "LLM {Context} request start promptLength={PromptLength} scriptLength={ScriptLength}",
            context,
            promptLength,
            scriptLength);
    }

    private void LogLlmResponse(string context, ChatCompletionResult result)
    {
        _logger.LogInformation(
            "LLM {Context} response tokens prompt={PromptTokens} completion={CompletionTokens} total={TotalTokens} length={Length}",
            context,
            result.PromptTokens,
            result.CompletionTokens,
            result.TotalTokens,
            result.Content.Length);
    }

    private IReadOnlyList<DictionaryCandidate>? TryParseWrapper(
        string json,
        JsonSerializerOptions options,
        string context)
    {
        try
        {
            var wrapper = JsonSerializer.Deserialize<ExtractionResponse>(json, options);
            if (wrapper?.Candidates == null)
            {
                return null;
            }

            if (wrapper.Candidates.Count == 0)
            {
                _logger.LogError("LLM parsing failed for {Context}. Candidates array was empty.", context);
                throw new LlmResponseParseException("LLMのレスポンスに候補が含まれていません。");
            }

            return ConvertToDictionaryCandidates(wrapper.Candidates);
        }
        catch (JsonException ex)
        {
            _logger.LogTrace(ex, "LLM wrapper parse failed for {Context}", context);
            return null;
        }
    }

    private IReadOnlyList<DictionaryCandidate>? TryParseTopLevelArray(
        string json,
        JsonSerializerOptions options,
        string context)
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<CandidateDto>>(json, options);
            if (items == null)
            {
                return null;
            }

            if (items.Count == 0)
            {
                _logger.LogError("LLM parsing failed for {Context}. Candidates array was empty.", context);
                throw new LlmResponseParseException("LLMのレスポンスに候補が含まれていません。");
            }

            _logger.LogWarning(
                "LLM returned top-level array format; this compatibility will be removed in 2026-Q2. Use {ExpectedFormat}.",
                "{\"candidates\": [...]}"
                );
            return ConvertToDictionaryCandidates(items);
        }
        catch (JsonException ex)
        {
            _logger.LogTrace(ex, "LLM top-level array parse failed for {Context}", context);
            return null;
        }
    }

    private static IReadOnlyList<DictionaryCandidate> ConvertToDictionaryCandidates(List<CandidateDto> dtos)
    {
        return dtos.Select(dto => new DictionaryCandidate
        {
            Surface = dto.Surface,
            Pronunciation = dto.Pronunciation,
            AccentType = dto.AccentType,
            ConfidenceLevel = ParseConfidence(dto.Confidence)
        }).ToList();
    }

    private static ConfidenceLevel ParseConfidence(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ConfidenceLevel.Medium;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "high" => ConfidenceLevel.High,
            "low" => ConfidenceLevel.Low,
            "medium" => ConfidenceLevel.Medium,
            _ => ConfidenceLevel.Medium
        };
    }

    private sealed class ExtractionResponse
    {
        [JsonPropertyName("candidates")]
        public List<CandidateDto>? Candidates { get; set; }
    }

    private sealed class CandidateDto
    {
        [JsonPropertyName("surface")]
        public string Surface { get; set; } = string.Empty;

        [JsonPropertyName("pronunciation")]
        public string Pronunciation { get; set; } = string.Empty;

        [JsonPropertyName("accent_type")]
        public int AccentType { get; set; }

        [JsonPropertyName("confidence")]
        public string? Confidence { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }
}
