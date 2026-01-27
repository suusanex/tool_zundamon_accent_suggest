# Azure OpenAI Prompt Templates

**Feature**: `001-core-workflow`  
**Date**: 2026-01-27

本ファイルはAzure OpenAIへのプロンプトテンプレートとレスポンス契約を定義する。

---

## 1. Dictionary Extraction（辞書候補抽出）

### System Prompt

```text
あなたは音声合成用の辞書作成の専門家です。与えられた日本語台本から、固有名詞・専門用語・特殊な読みが必要な単語を抽出し、読み（カタカナ）とアクセント核位置を提案してください。

アクセント核位置について:
- 0 = 平板型（下がり目なし）
- 1以上 = 下がり目の位置（モーラ数で指定）
例: 
  - 「東京」（トウキョウ）→ accent_type=0（平板）
  - 「日本」（ニホン）→ accent_type=1（二ホン↓）
  - 「VOICEVOX」（ボイスボックス）→ accent_type=3（ボイスボ↓ックス）

出力は以下のJSONスキーマに厳密に従ってください。JSON以外のテキストは一切出力しないでください。

{
  "candidates": [
    {
      "surface": "単語の表層形（元のテキストから抽出）",
      "pronunciation": "カタカナ読み（長音記号を含む）",
      "accent_type": 整数（0以上）,
      "confidence": "high|medium|low",
      "note": "補足情報（任意、抽出理由や注意点）"
    }
  ]
}

制約:
- surfaceは台本から抽出された単語そのものを使用（変更しない）
- pronunciationはカタカナと長音記号（ー）のみを使用
- accent_typeは0以上の整数
- confidenceは抽出の自信度（high: 確実、medium: やや不確実、low: ユーザー確認推奨）
- 重複した単語は1つのみ抽出（最初の出現を優先）
- 辞書登録が不要な一般的な単語は抽出しない
```

### User Prompt Template

```text
以下の台本から、音声合成用の辞書登録が必要な単語を抽出してください。

台本:
{script_text}
```

### Request Parameters

```csharp
var request = new ChatCompletionsOptions
{
    DeploymentName = _settings.DeploymentName,
    Messages =
    {
        new ChatRequestSystemMessage(systemPrompt),
        new ChatRequestUserMessage(userPrompt)
    },
    ResponseFormat = ChatCompletionsResponseFormat.JsonObject, // JSON mode
    Temperature = 0.7f,
    MaxTokens = _settings.MaxTokens,
    FrequencyPenalty = 0,
    PresencePenalty = 0
};
```

### Expected Response

```json
{
  "candidates": [
    {
      "surface": "東京",
      "pronunciation": "トウキョウ",
      "accent_type": 0,
      "confidence": "high",
      "note": "地名、平板型"
    },
    {
      "surface": "VOICEVOX",
      "pronunciation": "ボイスボックス",
      "accent_type": 3,
      "confidence": "high",
      "note": "製品名"
    }
  ]
}
```

### Response Parsing

```csharp
var content = response.Choices[0].Message.Content;
var result = JsonSerializer.Deserialize<ExtractionResult>(content);

// result.candidatesをDictionaryCandidateリストに変換
var candidates = result.Candidates.Select(c => new DictionaryCandidate
{
    Surface = c.Surface,
    Pronunciation = c.Pronunciation,
    AccentType = c.AccentType,
    Confidence = ParseConfidence(c.Confidence),
    Note = c.Note
}).ToList();
```

### Error Handling

#### JSON Parse Error

```csharp
try
{
    var result = JsonSerializer.Deserialize<ExtractionResult>(content);
}
catch (JsonException ex)
{
  _logger.LogWarning("LLM応答のJSONパースに失敗: {Exception}", ex.ToString());

  // Fail-fast（原則リトライしない）
  _logger.LogError("LLM応答のパースに失敗しました（生レスポンスはログに出力しない）。");
  throw new LlmResponseParseException("LLMの応答がJSON形式ではありませんでした。");
}
```

#### Token Limit Exceeded

```csharp
if (script.Length > _settings.MaxScriptLength)
{
    throw new ScriptTooLongException(
        $"台本が長すぎます（{script.Length}文字）。{_settings.MaxScriptLength}文字以内に短縮してください。");
}
```

---

## 2. Script Rewrite（台本リライト）

### System Prompt

```text
あなたは音声合成用の台本リライトの専門家です。与えられた台本を、意味を保持しつつ、音声合成エンジンで読み上げやすい表現に変換してください。

リライト方針:
1. 長文は適切に分割し、読点（、）を追加
2. 複雑な漢字熟語は平易な表現に置き換え（意味は保持）
3. 数字は単位を明示（例: 「100」→「100個」「2024」→「2024年」）
4. 読みづらい外来語はカタカナで明記
5. 句読点のない長文には適切な区切りを追加
6. 同音異義語の誤読を防ぐため、文脈を明確化

制約:
- 元の意味を変えない
- 台詞の口調・文体は保持
- 追加情報は最小限に留める
- 出力はリライト後の台本のみ（JSON不要、説明文不要）
```

### User Prompt Template

```text
以下の台本を、音声合成で読み上げやすい表現にリライトしてください。

元の台本:
{script_text}
```

### Request Parameters

```csharp
var request = new ChatCompletionsOptions
{
    DeploymentName = _settings.DeploymentName,
    Messages =
    {
        new ChatRequestSystemMessage(systemPrompt),
        new ChatRequestUserMessage(userPrompt)
    },
    Temperature = 0.5f, // 創造性を抑え、忠実性を優先
    MaxTokens = _settings.MaxTokens,
    FrequencyPenalty = 0,
    PresencePenalty = 0
};
```

### Expected Response

```text
こんにちは。東京へようこそ。
この街には、たくさんの魅力があります。美味しい食べ物、美しい景色、そして温かい人々。
ぜひ、ゆっくりと散策してみてください。
```

- **Plain Text形式**（JSON不要）
- レスポンスをそのまま`RewrittenScript`として使用

### Response Processing

```csharp
var rewrittenText = response.Choices[0].Message.Content.Trim();

// サニタイズ（余計なメタ情報があれば除去）
if (rewrittenText.StartsWith("リライト結果:") || rewrittenText.StartsWith("変換後:"))
{
    // LLMが余計な前置きを追加した場合
    rewrittenText = rewrittenText.Split('\n', 2).Last().Trim();
}

return rewrittenText;
```

---

## 3. Common Settings

### Timeout

```csharp
var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(2) // LLM呼び出しは長時間かかる可能性がある
};
```

### Retry Policy

- 本ツールは原因特定を優先するため、原則として自動リトライは行わない（失敗したらエラー終了）。

---

## 4. Token Usage Tracking

### Request Logging

```csharp
_logger.LogDebug("Azure OpenAI呼び出し: DeploymentName={Deployment}, PromptLength={Length}", 
    deploymentName, scriptText.Length);
```

### Response Logging

```csharp
var usage = response.Usage;
_logger.LogInformation(
    "Azure OpenAI応答: PromptTokens={PromptTokens}, CompletionTokens={CompletionTokens}, TotalTokens={TotalTokens}",
    usage.PromptTokens, usage.CompletionTokens, usage.TotalTokens);

// コスト推定（GPT-4 Turboの場合: $0.01/1K prompt tokens, $0.03/1K completion tokens）
var estimatedCost = (usage.PromptTokens * 0.01m / 1000) + (usage.CompletionTokens * 0.03m / 1000);
_logger.LogInformation("推定コスト: ${Cost:F4}", estimatedCost);
```

---

## 5. Security & Privacy

### API Key Management

- **secrets.json**に保存（環境変数でも可）
- ログには出力しない

```csharp
_logger.LogDebug("Azure OpenAI認証: Endpoint={Endpoint}, ApiKeyLength={Length}", 
    endpoint, apiKey.Length); // APIキーの長さのみ記録
```

### Script Masking

LLM送信前に個人情報をマスキング：

```csharp
var maskedScript = _maskingService.MaskPersonalInfo(script.Text);
var userPrompt = promptTemplate.Replace("{script_text}", maskedScript);
```

### Logging Policy

- **台本の本文**: ログに出力しない（文字数のみ）
- **LLM応答（辞書候補）**: 公開情報のため記録可
- **LLM応答（リライト後台本）**: 機密の可能性があるため記録しない

```csharp
// OK
_logger.LogInformation("辞書候補を抽出しました: {Count}件", candidates.Count);

// NG
_logger.LogInformation("台本: {Script}", script.Text); // 機密情報が含まれる可能性
```

---

## 6. Testing Strategy

### Unit Tests

- **T-LLM-1**: プロンプトテンプレートが正しく生成される
- **T-LLM-2**: JSON応答が正しくパースされる
- **T-LLM-3**: Token使用量が正しく記録される

### Integration Tests

- Integration Testは外部APIに接続せず、スタブ/モック（固定レスポンス）でフローを検証する。

### Mock Strategy

- **Unit Test**: Azure SDKのレスポンスをMock（固定JSON）
- **Integration Test**: HttpMessageHandler等を差し替え、固定レスポンスで実行

---

## 7. Performance Optimization

### Caching (Future Enhancement)

- 同一台本に対する複数回の呼び出しをキャッシュ（メモリまたはRedis）
- Cache Key: `SHA256(systemPrompt + userPrompt)`

### Batch Processing (Future Enhancement)

- 複数の台本を一度に処理（現状は1台本=1リクエスト）

---

## 8. Example Data Flows

### Flow 1: 辞書候補抽出

```
[Input]
Script.Text = "こんにちは、東京へようこそ。VOICEVOXで音声合成してみましょう。"

[LLM Request]
System: {辞書抽出プロンプト}
User: "以下の台本から、音声合成用の辞書登録が必要な単語を抽出してください。\n\n台本:\nこんにちは、東京へようこそ。VOICEVOXで音声合成してみましょう。"

[LLM Response]
{
  "candidates": [
    { "surface": "東京", "pronunciation": "トウキョウ", "accent_type": 0, "confidence": "high", "note": "地名" },
    { "surface": "VOICEVOX", "pronunciation": "ボイスボックス", "accent_type": 3, "confidence": "high", "note": "製品名" }
  ]
}

[Output]
List<DictionaryCandidate>: 2件
```

### Flow 2: 台本リライト

```
[Input]
Script.Text = "2024年、我々は新しい時代を迎えた。AIの進化は目覚ましく、100を超えるアプリケーションが登場した。"

[LLM Request]
System: {リライトプロンプト}
User: "以下の台本を、音声合成で読み上げやすい表現にリライトしてください。\n\n元の台本:\n2024年、我々は新しい時代を迎えた。AIの進化は目覚ましく、100を超えるアプリケーションが登場した。"

[LLM Response]
"2024年、私たちは新しい時代を迎えました。エーアイの進化は目覚ましく、100個を超えるアプリケーションが登場しました。"

[Output]
RewrittenScript = "2024年、私たちは新しい時代を迎えました。エーアイの進化は目覚ましく、100個を超えるアプリケーションが登場しました。"
```
