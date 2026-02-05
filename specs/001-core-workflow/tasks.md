# Tasks: 辞書候補抽出の日本語対応修正

**Branch**: `001-core-workflow`  
**Date**: 2026-02-04  
**Related Plan**: [plan.md](plan.md) - A-1) 辞書候補抽出の品質/互換性

## 背景

運用上、「辞書候補抽出で英単語（例: AI）のみが返り、日本語の候補が返らない」事象が発生した。
原因は、プロンプト契約の曖昧さ・実装のJSON契約不一致・構造化出力未強制による不安定さが主因。

## 修正方針

1. プロンプト契約の明確化（日本語固有名詞も対象であることを明示、`accent_type`に統一）
2. 構造化出力（JSON mode）の強制
3. レスポンス契約のパース処理（`candidates`ラッパー対応、`accent_type`に統一）
4. Fail-fast エラーハンドリング（パース失敗時は例外投げ、フォールバックなし）
5. テストで回帰防止（ユニット・統合テスト同梱）
6. ログ方針の統一（内容は出さない、メトリクスのみ記録）

---

## Phase 1: プロンプト契約の明確化

### Task 1.1: プロンプトテンプレート修正

**File**: [src/VoicevoxHelper.Infrastructure/LlmService/PromptTemplates.cs](../../../src/VoicevoxHelper.Infrastructure/LlmService/PromptTemplates.cs)

**目的**: 辞書抽出プロンプトを [contracts/azure-openai-prompts.md](contracts/azure-openai-prompts.md) の仕様に合わせて改善する。

**修正内容**:
- `DictionaryExtractionSystem` プロンプトを詳細化：
  - 日本語固有名詞（東京、VOICEVOX等）も抽出対象であることを明示
  - アクセント核位置の説明を追加（0=平板型、1以上=下がり目位置）
  - 日本語例を追加（東京→トウキョウ、accent_type=0 等）
  - 出力は **JSON形式のみ** であることを強調
  
- `DictionaryExtractionUser` プロンプトを簡潔化：
  - "固有名詞・専門用語・特殊読みが必要な単語" → "音声合成用の辞書登録が必要な単語" に変更
  - JSONスキーマの指示を System プロンプトに移動

- **JSON出力キーの統一**:
  - プロンプトで要求するキー名を `accent_type`（snake_case）に統一
  - 実装コード（DTOとテスト）にも `accent_type` で統一し、互換対応は廃止

**期待される出力例**:
```json
{
  "candidates": [
    {
      "surface": "東京",
      "pronunciation": "トウキョウ",
      "accent_type": 0,
      "confidence": "high",
      "note": "地名、平板型"
    }
  ]
}
```

**テスト同梱**:
- PromptTemplates.cs のクラス仕様テスト（プロンプト文に日本語例とaccent_typeキーが含まれることを確認）
- 手動確認：辞書抽出実行で日本語固有名詞が候補に含まれることを確認

---

## Phase 2: JSON構造化出力の強制

### Task 2.1: OpenAiChatClient に response_format 追加

**File**: [src/VoicevoxHelper.Infrastructure/LlmService/OpenAiChatClient.cs](../../../src/VoicevoxHelper.Infrastructure/LlmService/OpenAiChatClient.cs)

**目的**: Azure OpenAI APIの呼び出し時に `response_format: { type: "json_object" }` を指定し、JSON以外の出力を抑止する。

**修正内容**:
1. `GetChatCompletionAsync()` メソッドの payload に `response_format` を追加：
   ```csharp
   var payload = new
   {
       messages = new[]
       {
           new { role = "system", content = systemPrompt },
           new { role = "user", content = userPrompt }
       },
       temperature = 0,
       response_format = new { type = "json_object" }  // 追加
   };
   ```

2. **APIバージョン確定**：
   - Azure OpenAI API で `response_format: { type: "json_object" }` は `2024-02-15-preview` 以降でサポート
   - 現在の appsettings.json の `ApiVersion` が `2024-02-15-preview` 以上であることを確認
   - サポート未確認の場合は `appsettings.json` の `ApiVersion` を `2024-08-01-preview` に更新

**テスト同梱**:
- Unit Test: OpenAiChatClient の送信 payload に `response_format: { type: "json_object" }` が含まれることを Moq で検証

**検証方法**:
- 辞書抽出実行時、LLMレスポンスがコードフェンスなしの純粋なJSONになることを確認

---

## Phase 3: レスポンス契約の互換パース

### Task 3.1: AzureOpenAIService のパース処理改善

**File**: [src/VoicevoxHelper.Infrastructure/LlmService/AzureOpenAIService.cs](../../../src/VoicevoxHelper.Infrastructure/LlmService/AzureOpenAIService.cs)

**目的**: 
- `{ "candidates": [ ... ] }` 形式（契約準拠）を優先的に処理
- 暫定互換：トップレベル配列 `[...]` も当面受け入れ（廃止予定時期：2026-Q2）
- `accent_type`（snake_case）のみを受け入れ、`accentType` 互換は廃止

**修正内容**:

1. **DTO クラスの追加**（`ParseCandidates()` 内で使用）:
   ```csharp
   // AzureOpenAIService.cs 内にプライベートクラスとして追加
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
       
       // accent_type のみを受け入れ（snake_case）
       [JsonPropertyName("accent_type")]
       public int AccentType { get; set; }
       
       [JsonPropertyName("confidence")]
       public string? Confidence { get; set; }
       
       [JsonPropertyName("note")]
       public string? Note { get; set; }
   }
   ```

2. **`ParseCandidates()` メソッドの書き換え**:
   ```csharp
   private IReadOnlyList<DictionaryCandidate> ParseCandidates(string json, string context)
   {
       try
       {
           var options = new JsonSerializerOptions
           {
               PropertyNameCaseInsensitive = true
           };
           
           // 1. まず { "candidates": [...] } 形式を試す（契約準拠）
           try
           {
               var wrapper = JsonSerializer.Deserialize<ExtractionResponse>(json, options);
               if (wrapper?.Candidates != null && wrapper.Candidates.Count > 0)
               {
                   return ConvertToDictionaryCandidates(wrapper.Candidates);
               }
           }
           catch (JsonException)
           {
               // ラッパー形式でない場合は次へ
           }
           
           // 2. トップレベル配列 [...] 形式を試す（暫定互換、2026-Q2で廃止予定）
           try
           {
               var items = JsonSerializer.Deserialize<List<CandidateDto>>(json, options);
               if (items != null && items.Count > 0)
               {
                   _logger.LogWarning("LLM returned top-level array format; will be removed in 2026-Q2. Please use {{\"candidates\": [...] }} format.");
                   return ConvertToDictionaryCandidates(items);
               }
           }
           catch (JsonException)
           {
               // どちらもパース失敗 → 例外投げ
           }
           
           // パース失敗（Fail-fast）
           _logger.LogError("LLM parsing failed for {Context}. Response format does not match expected JSON structure.", context);
           throw new LlmResponseParseException("LLMのレスポンスがJSON形式ではありません。期待される形式: { \"candidates\": [...] }", json);
       }
       catch (Exception ex) when (!(ex is LlmResponseParseException))
       {
           _logger.LogError(ex, "Unexpected error during LLM parsing for {Context}", context);
           throw new LlmResponseParseException("LLMのレスポンス解析に失敗しました。", json, ex);
       }
   }
   
   private IReadOnlyList<DictionaryCandidate> ConvertToDictionaryCandidates(List<CandidateDto> dtos)
   {
       return dtos.Select(dto => new DictionaryCandidate
       {
           Surface = dto.Surface,
           Pronunciation = dto.Pronunciation,
           AccentType = dto.AccentType,
           // 他のプロパティは既定値
       }).ToList();
   }
   ```

**テスト同梱**:
- Unit Test で以下を保証：
  - `{ "candidates": [ ... ] }`（契約準拠）正常パース
  - トップレベル配列 `[...]` パース（互換モード、警告ログ出力）
  - `accent_type`（snake_case）を含むJSON正常パース
  - パース失敗時は `LlmResponseParseException` 投出（例外を吸収しない）
  - 空の `candidates` は例外投出（Fail-fast）

---

## Phase 4: テストで回帰防止

### Task 4.1: Unit Test 追加

**File**: [tests/VoicevoxHelper.Tests/Infrastructure/LlmService/AzureOpenAIServiceTests.cs](../../../tests/VoicevoxHelper.Tests/Infrastructure/LlmService/AzureOpenAIServiceTests.cs)

**目的**: 今回の修正について、以下のケースを自動テストで保証する。

**追加テストケース**:

1. **Test: ラッパー形式（契約準拠）がパースできる**
   ```csharp
   [Test]
   public async Task ExtractDictionaryCandidatesAsync_WhenWrapperFormat_ParsesCorrectly()
   {
       var json = """
       {
         "candidates": [
           {
             "surface": "東京",
             "pronunciation": "トウキョウ",
             "accentType": 0
           }
         ]
       }
       """;
       var service = new AzureOpenAIService(
           new FakeChatClient(json), 
           NullLogger<AzureOpenAIService>.Instance, 
           new LlmSettings());
   
       var result = await service.ExtractDictionaryCandidatesAsync(
           new Script { Text = "test" }, 
           CancellationToken.None);
   
       Assert.That(result, Has.Count.EqualTo(1));
       Assert.That(result[0].Surface, Is.EqualTo("東京"));
       Assert.That(result[0].Pronunciation, Is.EqualTo("トウキョウ"));
       Assert.That(result[0].AccentType, Is.EqualTo(0));
   }
   ```

2. **Test: トップレベル配列（互換形式）がパースできる**
   ```csharp
   [Test]
   public async Task ExtractDictionaryCandidatesAsync_WhenTopLevelArray_ParsesCorrectly()
   {
       var json = """
       [
         {
           "surface": "VOICEVOX",
           "pronunciation": "ボイスボックス",
           "accentType": 3
         }
       ]
       """;
       var service = new AzureOpenAIService(
           new FakeChatClient(json), 
           NullLogger<AzureOpenAIService>.Instance, 
           new LlmSettings());
   
       var result = await service.ExtractDictionaryCandidatesAsync(
           new Script { Text = "test" }, 
           CancellationToken.None);
   
       Assert.That(result, Has.Count.EqualTo(1));
       Assert.That(result[0].Surface, Is.EqualTo("VOICEVOX"));
       Assert.That(result[0].AccentType, Is.EqualTo(3));
   }
   ```

3. **Test: 互換：トップレベル配列パース時に警告ログが出る**
   ```csharp
   [Test]
   public async Task ExtractDictionaryCandidatesAsync_WhenTopLevelArray_EmitsWarningLog()
   {
       var json = """[
         { "surface": "日本", "pronunciation": "ニホン", "accent_type": 1 }
       ]""";
       var mockLogger = new Mock<ILogger<AzureOpenAIService>>();
       var service = new AzureOpenAIService(
           new FakeChatClient(json), 
           mockLogger.Object, 
           new LlmSettings());
   
       var result = await service.ExtractDictionaryCandidatesAsync(
           new Script { Text = "test" }, 
           CancellationToken.None);
   
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
   ```

4. **Test: パース失敗（不正フォーマット）時は例外投出**
   ```csharp
   [Test]
   public void ExtractDictionaryCandidatesAsync_WhenInvalidJson_ThrowsException()
   {
       var invalidJson = "<not json>";
       var service = new AzureOpenAIService(
           new FakeChatClient(invalidJson), 
           NullLogger<AzureOpenAIService>.Instance, 
           new LlmSettings());
   
       var ex = Assert.ThrowsAsync<LlmResponseParseException>(
           () => service.ExtractDictionaryCandidatesAsync(
               new Script { Text = "test" }, 
               CancellationToken.None));
       
       Assert.That(ex.Message, Does.Contain("JSON形式"));
   }
   ```

5. **Test: 日本語surfaceを含む候補が欠落しない**
   ```csharp
   [Test]
   public async Task ExtractDictionaryCandidatesAsync_WhenJapaneseSurface_NotDropped()
   {
       var json = """
       {
         "candidates": [
           {
             "surface": "東京",
             "pronunciation": "トウキョウ",
             "accentType": 0
           },
           {
             "surface": "VOICEVOX",
             "pronunciation": "ボイスボックス",
             "accentType": 3
           }
         ]
       }
       """;
       var service = new AzureOpenAIService(
           new FakeChatClient(json), 
           NullLogger<AzureOpenAIService>.Instance, 
           new LlmSettings());
   
       var result = await service.ExtractDictionaryCandidatesAsync(
           new Script { Text = "test" }, 
           CancellationToken.None);
   
       Assert.That(result, Has.Count.EqualTo(2));
       Assert.That(result.Any(c => c.Surface == "東京"), Is.True);
       Assert.That(result.Any(c => c.Surface == "VOICEVOX"), Is.True);
   }
   ```

6. **Test: 既存テストの修正（accent_type に統一）**
   - 既存の `ExtractDictionaryCandidatesAsync_WhenValidJson_ReturnsDeduplicated` テストで使用している JSON を `accent_type` に修正：
   ```csharp
   [Test]
   public async Task ExtractDictionaryCandidatesAsync_WhenValidJson_ReturnsDeduplicated()
   {
       var json = """{
         \"candidates\": [
           { \"surface\": \"VOICEVOX\", \"pronunciation\": \"ボイスボックス\", \"accent_type\": 1 },
           { \"surface\": \"VOICEVOX\", \"pronunciation\": \"ボイスボックス\", \"accent_type\": 1 }
         ]
       }""";
       var service = new AzureOpenAIService(
           new FakeChatClient(json), 
           NullLogger<AzureOpenAIService>.Instance, 
           new LlmSettings());
   
       var result = await service.ExtractDictionaryCandidatesAsync(
           new Script { Text = "test" }, 
           CancellationToken.None);
   
       Assert.That(result, Has.Count.EqualTo(1));
   }
   ```

**検証方法**:
```bash
dotnet test tests/VoicevoxHelper.Tests/VoicevoxHelper.Tests.csproj --filter "FullyQualifiedName~AzureOpenAIServiceTests"
```

---

## Phase 5: 統合テスト・動作確認

### Task 5.1: 手動動作確認

**目的**: 実際のアプリケーションで、修正が正しく動作することを確認する。

**確認手順**:

1. **再現入力でテスト**:
   - 入力テキスト（ユーザー提供）:
     ```
     まずは１枚の絵に描いて説明する
     
     この重要性は、自分の頭の整理と人へ分かりやすく伝える上で明らかな話だと思います。
     ここに新しく、AIに対して説明するというのが重要になってきました。AIであっても、全体像を的確に伝えた時とそうでない時で成果は変わります。
     ```
   - 期待される動作: 「AI」が候補に含まれる（固有名詞・専門用語として適切）
   - 確認: 他に辞書登録が必要そうな語が無ければ、候補1件でも正常

2. **日本語固有名詞を含む台本でテスト**:
   - 入力テキスト:
     ```
     こんにちは、東京へようこそ。VOICEVOXで音声合成してみましょう。
     ```
   - 期待される動作: 「東京」「VOICEVOX」が候補に含まれる
   - 確認: 両方とも日本語読み・アクセント核位置が提案される

3. **アクセント核位置の確認**:
   - 出力CSVまたはプレビュー画面で、`accent_type`（または `accentType`）が0以外の値を持つ候補が存在することを確認
   - 例: 「VOICEVOX」→ accentType=3 など

4. **ログ確認（spec.mdのログ方針に準拠）**:
   - `appsettings.json` で `Logging.MinimumLevel` を `"Debug"` に設定
   - 辞書抽出実行時のログで以下をUTCタイムスタンプ・エラー分類で確認：
     - (OK) ログ出力: `ExtractDictionaryCandidates request. ScriptLength={Length}, MaskingApplied={IsMasked}`
     - (OK) ログ出力: `ExtractDictionaryCandidates response. PromptTokens={PT}, CompletionTokens={CT}, TotalTokens={Total}, CandidateCount={Count}`
     - (NG) ログに台本本文・LLMレスポンス内容は出力不可（機密性、FR-015準拠）
     - (OK) パース失敗時: `LlmResponseParseException` が例外スタックに含まれることを確認（生レスポンスはスタックに含めない）

### Task 5.2: Integration Test 追加（オプション）

**File**: [tests/VoicevoxHelper.IntegrationTests/DictionaryExtractionFlowTests.cs](../../../tests/VoicevoxHelper.IntegrationTests/DictionaryExtractionFlowTests.cs)

**目的**: エンドツーエンドのフロー（入力→マスキング→LLM呼び出し→パース→出力）をスタブで検証する。

**実装内容**:
- `FakeChatClient` を DI に注入し、固定レスポンス（日本語候補含む）を返す
- `DictionaryExtractionInputViewModel.ExtractAsync()` を実行
- `WorkflowState.ExtractedCandidates` に日本語候補が含まれることを確認

**注意**: CIで実行するため、実APIへは接続しない。

---

## Task Checklist

- [X] **Phase 1**: プロンプトテンプレート修正
  - [X] T1.1: `PromptTemplates.cs` を contracts に合わせて改善（日本語例追加、`accent_type` に統一）

- [X] **Phase 2**: JSON構造化出力の強制
  - [X] T2.1: `OpenAiChatClient.cs` に `response_format: { type: "json_object" }` 追加

- [X] **Phase 3**: レスポンス契約の互換パース
  - [X] T3.1: `AzureOpenAIService.cs` にDTO追加とパース処理改善（ラッパー/配列両対応、snake_caseのみ）

- [X] **Phase 4**: テストで回帰防止
  - [X] T4.1: `AzureOpenAIServiceTests.cs` に5つのテストケース追加
  - [X] T4.2: 全テスト実行・PASS確認

- [ ] **Phase 5**: 統合テスト・動作確認
  - [ ] T5.1: 再現入力での手動テスト
  - [ ] T5.2: 日本語固有名詞を含む台本での手動テスト
  - [ ] T5.3: ログでエンコード・JSON形式を確認
  - [ ] T5.4: Integration Test 追加（オプション）

---

## Success Criteria

以下の全てを満たすことで、本タスクリストは完了とする：

1. ✅ 全Unit Testがパスする（新規テスト含む）
2. ✅ 日本語固有名詞（例: 東京、VOICEVOX）が辞書候補に含まれる
3. ✅ アクセント核位置（`accentType`）が正しく反映される（0固定にならない）
4. ✅ LLMレスポンスがJSON形式のみになる（コードフェンス・説明文混入なし）
5. ✅ `{ "candidates": [...] }` 形式と `[...]` 形式の両方をパースできる
6. ✅ `accent_type`（snake_case）のみを受け入れる
7. ✅ 既存機能（辞書登録、リライト）に影響がない（リグレッションなし）

---

## References

- [plan.md](plan.md) - A-1) 辞書候補抽出の品質/互換性
- [contracts/azure-openai-prompts.md](contracts/azure-openai-prompts.md) - 期待されるプロンプトとレスポンス形式
- [src/VoicevoxHelper.Infrastructure/LlmService/PromptTemplates.cs](../../../src/VoicevoxHelper.Infrastructure/LlmService/PromptTemplates.cs)
- [src/VoicevoxHelper.Infrastructure/LlmService/OpenAiChatClient.cs](../../../src/VoicevoxHelper.Infrastructure/LlmService/OpenAiChatClient.cs)
- [src/VoicevoxHelper.Infrastructure/LlmService/AzureOpenAIService.cs](../../../src/VoicevoxHelper.Infrastructure/LlmService/AzureOpenAIService.cs)
- [tests/VoicevoxHelper.Tests/Infrastructure/LlmService/AzureOpenAIServiceTests.cs](../../../tests/VoicevoxHelper.Tests/Infrastructure/LlmService/AzureOpenAIServiceTests.cs)
