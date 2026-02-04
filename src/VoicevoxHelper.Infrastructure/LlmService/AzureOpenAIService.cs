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
        const string context = "DictionaryExtraction";
        var userPrompt = PromptTemplates.DictionaryExtractionUser + "\n" + script.Text;
        LogLlmRequest(context, userPrompt, script.Text);

        try
        {
            var result = await _chatClient.GetChatCompletionAsync(
                PromptTemplates.DictionaryExtractionSystem,
                userPrompt,
                cancellationToken);
            LogTokenUsage(result, context);
            LogLlmResponse(context, result);
            var candidates = ParseCandidates(result.Content, context);
            _logger.LogInformation("LLM {Context} returned {Count} candidates", context, candidates.Count);
            return Deduplicate(candidates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM extraction failed for {Context}; script snippet: {Snippet}", context, Summarize(script.Text));
            throw;
        }
    }

    public async Task<string> RewriteScriptAsync(Script script, CancellationToken cancellationToken)
    {
        const string context = "ScriptRewrite";
        var rewritePrompt = PromptTemplates.ScriptRewriteUser + "\n" + script.Text;
        LogLlmRequest(context, rewritePrompt, script.Text);

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
            _logger.LogError(ex, "LLM rewrite failed for {Context}; script snippet: {Snippet}", context, Summarize(script.Text));
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
            _logger.LogError(ex, "LLM parsing failed for {Context}; raw response snippet: {Raw}", context, Summarize(json, 400));
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

    private void LogLlmRequest(string context, string prompt, string scriptText)
    {
        _logger.LogInformation(
            "LLM {Context} request start length={Length} preview={Preview}",
            context,
            prompt.Length,
            Summarize(prompt));
        _logger.LogDebug("LLM {Context} script snippet={Snippet}", context, Summarize(scriptText, 200));
    }

    private void LogLlmResponse(string context, ChatCompletionResult result)
    {
        _logger.LogInformation(
            "LLM {Context} response tokens prompt={PromptTokens} completion={CompletionTokens} total={TotalTokens} length={Length} preview={Preview}",
            context,
            result.PromptTokens,
            result.CompletionTokens,
            result.TotalTokens,
            result.Content.Length,
            Summarize(result.Content));
    }

    private static string Summarize(string text, int maxLength = 256)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength] + "...";
    }
}
