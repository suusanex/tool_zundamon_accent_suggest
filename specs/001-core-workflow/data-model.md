# Data Model: Core Workflow

**Feature**: `001-core-workflow`  
**Date**: 2026-01-27

本ドキュメントはアプリケーション内で扱うデータモデルを定義する。

---

## 1. Script（台本）

### 説明

音声合成用のテキスト。ユーザーが入力し、LLMで処理される。

### Properties

| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| Text | string | Yes | MaxLength: 10000 | 台本の本文 |
| Length | int | Computed | - | 文字数（Text.Length） |
| MaskedText | string | Computed | - | 個人情報マスキング後のテキスト |
| MaskingSummary | MaskingResult | Computed | - | マスキング処理の結果サマリ |

### Validation Rules

- **VR-S1**: `Text`は1文字以上10,000文字以下であること。
- **VR-S2**: `Text`が空白のみで構成されていないこと（`string.IsNullOrWhiteSpace`でNG）。

### State Transitions

1. **Input** → **Validated** → **Masked** → **Processed**
   - **Input**: ユーザー入力直後
   - **Validated**: バリデーション通過
   - **Masked**: 個人情報マスキング完了
   - **Processed**: LLM処理完了（辞書候補抽出 or リライト）

---

## 2. MaskingResult（マスキング結果）

### 説明

台本テキストの個人情報マスキング処理の結果。

### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| OriginalLength | int | Yes | マスキング前の文字数 |
| MaskedLength | int | Yes | マスキング後の文字数 |
| ReplacementCount | int | Yes | 置換箇所数 |
| Replacements | List&lt;MaskingReplacement&gt; | Yes | 置換詳細のリスト |

### MaskingReplacement

| Property | Type | Description |
|----------|------|-------------|
| Type | MaskingType (enum) | EMAIL / PHONE / ADDRESS / NAME |
| OriginalText | string | 置換前の文字列（ログには記録しない） |
| Placeholder | string | 置換後のプレースホルダー（例: `[EMAIL]`） |
| Position | int | 元テキスト内の位置 |

---

## 3. DictionaryCandidate（辞書候補）

### 説明

台本から抽出された、またはユーザーが編集した辞書登録候補。

### Properties

| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| Surface | string | Yes | MaxLength: 100, Pattern: `^[ぁ-ゖァ-ヺー一-龥々〆〤a-zA-Z0-9]+$` | 単語の表層形 |
| Pronunciation | string | Yes | MaxLength: 100, Pattern: `^[ァ-ヺー]+$` | カタカナ読み |
| AccentType | int | Yes | Min: 0 | アクセント核位置（0=平板） |
| WordType | WordType? | No | - | 品詞（任意） |
| Priority | int? | No | Range: 0-10 | 優先度（デフォルト5） |
| Note | string? | No | MaxLength: 500 | 備考（LLM自信度等） |
| Confidence | ConfidenceLevel? | No | - | LLM抽出時の自信度 |
| SourceContext | string? | No | MaxLength: 200 | 抽出元の台本行（部分文字列） |
| Uuid | string? | No | GUID形式 | VOICEVOX登録後に取得されるUUID |

### Enums

#### WordType

```csharp
public enum WordType
{
    ProperNoun,      // 固有名詞
    CommonNoun,      // 普通名詞
    Verb,            // 動詞
    Adjective,       // 形容詞
    Suffix           // 語尾
}
```

#### ConfidenceLevel

```csharp
public enum ConfidenceLevel
{
    High,    // 高い自信度
    Medium,  // 中程度
    Low      // 低い（ユーザー確認推奨）
}
```

### Validation Rules

- **VR-DC1**: `Surface`は日本語文字、アルファベット、数字のみを含む。
- **VR-DC2**: `Pronunciation`はカタカナと長音記号（ー）のみを含む。
- **VR-DC3**: `AccentType`は0以上の整数（上限はVOICEVOX側で検証）。
- **VR-DC4**: `Priority`が指定される場合、0〜10の範囲内であること。
- **VR-DC5**: 同一`Surface`を持つ候補が重複しない（リスト内でユニーク）。

### State Transitions

1. **Extracted** → **Edited** → **Validated** → **Registered**
   - **Extracted**: LLMから抽出直後
   - **Edited**: ユーザーがCSV/JSONで編集後
   - **Validated**: バリデーション通過
   - **Registered**: VOICEVOX APIへ登録完了（Uuid取得）

---

## 4. DictionaryEntry（VOICEVOX辞書エントリ）

### 説明

VOICEVOX APIに登録されている辞書データ。冪等性チェックで使用。

### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Uuid | string | Yes | VOICEVOX側で生成されたUUID |
| Surface | string | Yes | 単語の表層形 |
| Pronunciation | string | Yes | カタカナ読み |
| AccentType | int | Yes | アクセント核位置 |
| WordType | WordType? | No | 品詞 |
| Priority | int? | No | 優先度 |

### Relationships

- `DictionaryCandidate.Uuid` == `DictionaryEntry.Uuid`の場合、更新（PUT）対象。
- `DictionaryCandidate.Surface` == `DictionaryEntry.Surface`の場合、重複判定（設定により更新 or スキップ）。

---

## 5. ExecutionReport（処理結果レポート）

### 説明

各モードの実行結果を記録するレポート。UIに表示され、ログに記録される。

### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Mode | ExecutionMode | Yes | 実行されたモード |
| StartTime | DateTime | Yes | 処理開始時刻 |
| EndTime | DateTime | Yes | 処理終了時刻 |
| Duration | TimeSpan | Computed | `EndTime - StartTime` |
| Status | ExecutionStatus | Yes | 成功 / 部分成功 / 失敗 |
| TotalCount | int | Yes | 総処理件数 |
| SuccessCount | int | Yes | 成功件数 |
| FailureCount | int | Computed | `TotalCount - SuccessCount` |
| FailureDetails | List&lt;FailureDetail&gt; | Yes | 失敗詳細のリスト |
| TokenUsage | TokenUsage? | No | LLM使用時のトークン消費量 |
| OutputFilePath | string? | No | 出力ファイルパス（辞書候補抽出時） |

### Enums

#### ExecutionMode

```csharp
public enum ExecutionMode
{
    DictionaryExtraction,  // 辞書候補抽出
    DictionaryRegistration, // API登録
    ScriptRewrite          // 台本リライト
}
```

#### ExecutionStatus

```csharp
public enum ExecutionStatus
{
    Success,        // 全て成功
    PartialSuccess, // 一部成功
    Failure         // 全て失敗
}
```

### FailureDetail

| Property | Type | Description |
|----------|------|-------------|
| ItemIdentifier | string | 失敗したアイテムの識別子（Surface等） |
| ErrorCode | string | エラーコード（例: "AUTH_ERROR", "TIMEOUT"） |
| ErrorMessage | string | エラーメッセージ |
| Timestamp | DateTime | エラー発生時刻 |

### TokenUsage

| Property | Type | Description |
|----------|------|-------------|
| PromptTokens | int | プロンプトトークン数 |
| CompletionTokens | int | 完了トークン数 |
| TotalTokens | int | 合計トークン数 |
| EstimatedCost | decimal? | 推定コスト（円） |

---

## 6. ApplicationSettings（アプリケーション設定）

### 説明

appsettings.json と secrets.json から読み込まれる設定。

### AzureOpenAISettings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| Endpoint | string | - | Azure OpenAIエンドポイント |
| DeploymentName | string | - | デプロイメント名 |
| ApiVersion | string | "2024-02-15-preview" | APIバージョン |
| ApiKey | string | - | APIキー（secrets.json） |
| MaxTokens | int | 4096 | 最大トークン数 |
| Temperature | double | 0.7 | 温度パラメータ |
| Timeout | TimeSpan | 2分 | タイムアウト時間 |

### VoiceVoxSettings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| BaseUrl | string | "http://127.0.0.1:50021" | VOICEVOX APIベースURL |
| Timeout | TimeSpan | 30秒 | タイムアウト時間 |
| UpdateExisting | bool | true | 既存単語を更新するか |

### DictionarySettings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| DefaultFormat | FileFormat | CSV | 既定のファイル形式 |
| DefaultEncoding | string | "UTF-8" | 既定のエンコーディング |
| MaxFileSize | long | 10MB | ファイルサイズ上限 |
| OutputDirectory | string | "./output" | 出力ディレクトリ |

### ScriptSettings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| MaxLength | int | 10000 | 台本の最大文字数 |
| EnableMasking | bool | true | 個人情報マスキングを有効化 |

---

## 7. File Formats（ファイル形式）

### CSV Format

#### Schema

```csv
surface,pronunciation,accent_type,word_type,priority,note
東京,トウキョウ,0,PROPER_NOUN,5,地名
VOICEVOX,ボイスボックス,3,PROPER_NOUN,7,製品名
```

#### Encoding

- **Character Set**: UTF-8 with BOM
- **Delimiter**: `,` (comma)
- **Quote**: `"` (double quote, RFC 4180)
- **Line Ending**: CRLF (Windows)

#### Validation

- ヘッダー行が必須（`surface`, `pronunciation`, `accent_type`は必須カラム）
- パースエラー（形式不正）が1件でもあれば処理中断（Fail-fast）

### JSON Format

#### Schema

```json
[
  {
    "surface": "東京",
    "pronunciation": "トウキョウ",
    "accent_type": 0,
    "word_type": "PROPER_NOUN",
    "priority": 5,
    "note": "地名"
  }
]
```

#### Encoding

- **Character Set**: UTF-8（BOMなし）
- **Format**: Pretty-printed（インデント2スペース）

#### Validation

- JSON配列形式であること
- 各要素が`surface`, `pronunciation`, `accent_type`を含むこと

---

## 8. Domain Rules（ドメインルール）

### DR-1: 辞書候補の重複排除

- 同一`Surface`を持つ候補が抽出された場合、最初の出現を優先し、2つ目以降は自動的に除外する。
- ユーザーがCSV/JSONで複数の候補を手動追加する場合は許容（登録時に順次処理）。

### DR-2: 冪等性の保証

- `UpdateExisting=true`の場合、既存の`Surface`に対してはGET /user_dictで既存UUIDを取得し、PUT /user_dict_word/{uuid}で更新。
- `UpdateExisting=false`の場合、既存の`Surface`をスキップし、ログに記録。

### DR-3: 部分成功時の継続

- 100件中1件が失敗した場合でも、残りの99件は処理を継続する。
- 失敗した項目は`FailureDetails`に記録し、最後にレポート表示。

### DR-4: トークン制限の厳格化

- LLMに送信前に台本の文字数をチェック。10,000文字を超える場合はエラー終了（処理開始前）。
- LLMレスポンスが`max_tokens`を超えた場合（不完全なJSON）は、パースエラーとして扱い、エラー終了（原則再試行しない）。

---

## 9. Integration Points（統合ポイント）

### VOICEVOX API Mapping

| DictionaryCandidate Property | VOICEVOX API Parameter | Notes |
|------------------------------|------------------------|-------|
| Surface | `surface` | 必須、そのまま送信 |
| Pronunciation | `pronunciation` | 必須、そのまま送信 |
| AccentType | `accent_type` | 必須、そのまま送信 |
| WordType | `word_type` | 任意、enum→stringに変換 |
| Priority | `priority` | 任意、nullなら送信しない |
| Uuid | `{word_uuid}` | 更新時のパスパラメータ |

### LLM Output Mapping

LLMからのJSON出力を`DictionaryCandidate`にマッピング：

```json
{
  "surface": "東京" → DictionaryCandidate.Surface,
  "pronunciation": "トウキョウ" → DictionaryCandidate.Pronunciation,
  "accent_type": 0 → DictionaryCandidate.AccentType,
  "confidence": "high" → DictionaryCandidate.Confidence,
  "note": "地名" → DictionaryCandidate.Note
}
```

---

## 10. Example Data Flows

### Flow 1: 辞書候補抽出

```
[User Input]
  ↓
Script(Text="こんにちは、東京へようこそ")
  ↓ Validation
Script(Text="こんにちは、東京へようこそ", Length=15)
  ↓ Masking
Script(MaskedText="こんにちは、東京へようこそ", MaskingSummary={ReplacementCount=0})
  ↓ LLM Processing
List<DictionaryCandidate>:
  - {Surface="東京", Pronunciation="トウキョウ", AccentType=0, Confidence=High}
  ↓ Save to CSV
output/dictionary_2026-01-27_123456.csv
```

### Flow 2: 辞書API登録

```
[CSV File]
  ↓ Parse
List<DictionaryCandidate>: 100件
  ↓ Validation
List<DictionaryCandidate>: 100件（全て有効）
  ↓ GET /user_dict（既存辞書取得）
List<DictionaryEntry>: 50件
  ↓ Matching（冪等性チェック）
  50件: 新規登録（POST）
  30件: 既存更新（PUT）
  20件: スキップ（UpdateExisting=false）
  ↓ API Registration
ExecutionReport:
  - SuccessCount=80
  - FailureCount=0
  - Status=Success
```

---

## Next Steps

- **Phase 2**: 実装タスクの詳細化（contracts/, quickstart.md, tasks.md）
- **Testing Strategy**: 各エンティティのUnit Testシナリオを定義
- **UI Design**: ウィザード各画面での入力/出力フィールドとデータモデルのバインディング設計
