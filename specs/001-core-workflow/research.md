# Research: Core Workflow Implementation

**Feature**: `001-core-workflow`  
**Date**: 2026-01-27

## 目的

feature specで要求される3つのコア機能（辞書候補抽出、API登録、台本リライト）を実現するための技術選定と設計方針を決定する。

---

## 1. WPF + Fluent テーマ

### Decision

.NET 10 WPF + **FluentWPF** または **WPF-UI** ライブラリを採用する。

### Rationale

- **FluentWPF**: 軽量で既存のWPFアプリに統合しやすい。Fluent Design Systemのアクリル効果やリベルエフェクトを提供。
- **WPF-UI**: より包括的なコントロールセットを提供。Modern UI要素（NavView、ProgressRing等）を含む。

どちらもMITライセンスで商用利用可能。本プロジェクトでは**WPF-UI**を推奨（より包括的で、ウィザード形式のナビゲーションに適している）。

### Alternatives Considered

- **ModernWpfUI**: 開発が停滞気味。
- **MahApps.Metro**: Fluent Designではなく、Metro風デザイン。

---

## 2. Generic Host + CommunityToolkit MVVM

### Decision

**Microsoft.Extensions.Hosting**を使用したGeneric Hostパターンを採用し、MVVMには**CommunityToolkit.Mvvm**（旧 MVVM Toolkit）を使用する。

### Rationale

- **Generic Host**:
  - 依存性注入（DI）コンテナを標準で提供
  - 設定（IConfiguration）、ログ（ILogger）、ライフサイクル管理を統一的に扱える
  - バックグラウンドサービス（IHostedService）でLLM/API呼び出しの非同期処理を管理可能
- **CommunityToolkit.Mvvm**:
  - Source Generatorベースで高速かつ簡潔
  - `[ObservableProperty]`, `[RelayCommand]`でボイラープレートを削減
  - .NET標準のMVVMフレームワークとして推奨されている

### Alternatives Considered

- **Prism**: 高機能だが学習コストが高く、本プロジェクトの規模には過剰。
- **Caliburn.Micro**: 規約ベースだが、Source Generator時代には冗長。

---

## 3. ナビゲーション方式

### Decision

**Page/Frame**ベースのナビゲーションを採用する。各ウィザードステップを`Page`として実装し、`Frame`でホストする。

### Rationale

- WPF標準のナビゲーション機構を利用可能
- 各ステップを独立したViewModelで管理でき、テスタビリティが向上
- WPF-UIの`NavigationView`コントロールと自然に統合できる

### Alternatives Considered

- **UserControl切り替え**: シンプルだが、履歴管理やパラメータ渡しが煩雑。
- **Window切り替え**: モーダルダイアログの連鎖になり、UXが劣る。

---

## 4. Azure OpenAI統合

### Decision

**Azure.AI.OpenAI** NuGetパッケージを使用し、構造化出力（JSON mode）を前提とする。

### Rationale

- 公式SDKで、認証（APIキー、Entra ID）を標準サポート
- ストリーミング、トークンカウント、リトライポリシーを統合管理
- JSON mode（`response_format: {"type": "json_object"}`）でLLM出力を型安全にパース可能

### プロンプト方針

#### A) 辞書候補抽出

```text
システム: あなたは音声合成用の辞書作成の専門家です。与えられた日本語台本から、固有名詞・専門用語・特殊な読みが必要な単語を抽出し、読み（カタカナ）とアクセント核位置を提案してください。出力はJSON配列形式で、以下のスキーマに従ってください。

[
  {
    "surface": "単語の表層形",
    "pronunciation": "カタカナ読み",
    "accent_type": 整数（アクセント核位置、0=平板、1以上=核の位置）,
    "confidence": "high|medium|low",
    "note": "補足情報（任意）"
  }
]

ユーザー: {台本テキスト（個人情報マスキング済み）}
```

#### B) 台本リライト

```text
システム: あなたは音声合成用の台本リライトの専門家です。与えられた台本を、意味を保持しつつ、音声合成エンジンで読み上げやすい表現に変換してください。以下の点に注意してください：
- 長文は適切に分割
- 読点を適切に配置
- 難解な漢字は平易な表現に
- 数字は「〇〇年」「〇〇円」など単位を明示
出力は変換後の台本のみを返してください。

ユーザー: {元の台本}
```

### フォールバック方針

- **JSON parseエラー**: LLMに再試行を促す（最大2回）。失敗時はエラー終了し、ログに生レスポンスを記録。
- **トークン超過**: 台本を10,000文字で切断し、エラー終了（ユーザーに分割を促す）。

### トークン最適化

- **1回の呼び出しにまとめる**: 辞書候補抽出では、単語ごとに呼び出すのではなく、台本全体を1回で処理。
- **プロンプトキャッシュ**: Azure OpenAIではシステムプロンプトの一部がキャッシュされる可能性があるが、明示的な制御はSDK側で行わない（コスト最適化は今後の課題）。

### 機密配慮

- ログには台本の文字数と抽出件数のみを記録（本文は記録しない）。
- マスキング後の台本をLLMに送信（後述）。

---

## 5. VOICEVOX辞書API仕様

### Decision

VOICEVOX Engine API v0.25.1の仕様に準拠した実装を行う。

### API Endpoints

#### GET /user_dict

- **用途**: 既存辞書の取得（冪等性チェック用）
- **返り値**: `{ "uuid1": UserDictWord, "uuid2": UserDictWord, ... }`

#### POST /user_dict_word

- **必須パラメータ**:
  - `surface` (string): 単語の表層形
  - `pronunciation` (string): カタカナ読み
  - `accent_type` (integer): アクセント核位置
- **任意パラメータ**:
  - `word_type`: PROPER_NOUN | COMMON_NOUN | VERB | ADJECTIVE | SUFFIX
  - `priority` (integer, 0-10): デフォルトは5が推奨
- **返り値**: UUID（文字列）

#### PUT /user_dict_word/{word_uuid}

- **用途**: 既存単語の更新
- **パラメータ**: POSTと同じ
- **返り値**: 204 No Content

#### DELETE /user_dict_word/{word_uuid}

- **用途**: 単語削除
- **返り値**: 204 No Content

### 冪等性の実現

- **既定動作**: 同一`surface`が存在する場合、既存のUUIDを取得して`PUT`で更新。
- **スキップモード**: appsettings.jsonで`UpdateExisting=false`を指定可能（既存単語をスキップ）。

### 部分成功時のレポート

- 100件中50件が成功した場合、成功リスト（UUID）と失敗リスト（surface, エラーメッセージ）を別々に記録。
- UIには成功率（50/100）と失敗詳細を表示。

---

## 6. CSV/JSON辞書案ファイル設計

### Decision

**CSVを基準フォーマット**とし、JSONは互換フォーマットとして併用可能とする。

### CSV Schema

```csv
surface,pronunciation,accent_type,word_type,priority,note
東京,トウキョウ,0,PROPER_NOUN,5,地名
VOICEVOX,ボイスボックス,3,PROPER_NOUN,7,製品名
```

- **区切り文字**: カンマ（`,`）
- **エンコーディング**: UTF-8 with BOM（Excel互換）
- **エスケープ**: RFC 4180準拠（ダブルクォート、改行をサポート）

### JSON Schema

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

### フィールド定義

| Field | Type | Required | Description | VOICEVOX API対応 |
|-------|------|----------|-------------|------------------|
| surface | string | Yes | 単語の表層形 | 必須 |
| pronunciation | string | Yes | カタカナ読み | 必須 |
| accent_type | integer | Yes | アクセント核位置（0=平板） | 必須 |
| word_type | enum | No | PROPER_NOUN等 | 任意 |
| priority | integer(0-10) | No | 優先度（デフォルト5） | 任意 |
| note | string | No | 備考（LLMの自信度等） | API未送信 |
| uuid | string | No | 更新時に使用 | 更新時に必要 |

### バリデーション

- **accent_type**: 0以上の整数（上限はVOICEVOXが自動判定するため事前チェック不要）
- **pronunciation**: カタカナのみ（全角、長音・促音・拗音を含む）
- **CSV行数制限**: 1万行（メモリ保護）

---

## 7. 個人情報マスキング

### Decision

**正規表現ベースの自動マスキング**を実装する。

### Patterns

| 対象 | 正規表現例 | 置換後 |
|------|-----------|--------|
| メールアドレス | `[\w\.-]+@[\w\.-]+` | `[EMAIL]` |
| 電話番号 | `0\d{1,4}-\d{1,4}-\d{4}` | `[PHONE]` |
| 住所（都道府県） | `(東京都|大阪府|北海道|...)(市|区|町|村)?` | `[ADDRESS]` |
| 個人名（推測） | `(様|さん|氏|殿)$` を含む単語 | `[NAME]` |

### 制約

- **完全な検出は保証しない**: あくまで「ベストエフォート」として実装。
- **ログへの記録**: マスキング前後の文字数と置換件数をログに記録（監査用）。

---

## 8. HTTP Client + Retry

### Decision

**System.Net.Http.HttpClient** + **Polly** を使用する。

### Rationale

- HttpClientは.NET標準で、DIコンテナからIHttpClientFactoryで注入可能。
- Pollyはリトライ、サーキットブレーカー、タイムアウトを宣言的に記述可能。

### Retry Policy

```csharp
services.AddHttpClient("VOICEVOX")
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

- **対象エラー**: 5xx、タイムアウト、接続エラー
- **リトライ回数**: 最大3回
- **待機時間**: 指数バックオフ（2秒、4秒、8秒）

---

## 9. ログ/監査設計

### Decision

**Microsoft.Extensions.Logging** + **Serilog** シンクを使用する。

### Log Levels

| Level | 用途 |
|-------|------|
| Trace | LLMリクエスト/レスポンスの詳細（トークン数、所要時間） |
| Debug | 辞書登録の進捗（N件中M件完了） |
| Information | モード開始/終了、成否サマリ |
| Warning | 部分失敗（50/100件成功） |
| Error | API認証エラー、LLMタイムアウト |
| Critical | 予期しない例外（全処理中断） |

### 機密情報の扱い

- **台本本文**: ログに出力しない（文字数のみ記録）
- **APIキー**: ログに出力しない（認証エラー時も伏せる）
- **辞書候補**: surface/pronunciation/accent_typeは記録可（公開情報のため）

### ログ出力先

- **開発時**: Console + Debug出力
- **本番**: `logs/voicevox-helper-{Date}.log`（日次ローテーション、最大7世代）

---

## 10. 設定設計

### appsettings.json

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "gpt-4",
    "ApiVersion": "2024-02-15-preview",
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "Timeout": "00:02:00"
  },
  "VoiceVox": {
    "BaseUrl": "http://127.0.0.1:50021",
    "Timeout": "00:00:30",
    "RetryCount": 3,
    "UpdateExisting": true
  },
  "Dictionary": {
    "DefaultFormat": "CSV",
    "DefaultEncoding": "UTF-8",
    "MaxFileSize": 10485760,
    "OutputDirectory": "./output"
  },
  "Script": {
    "MaxLength": 10000,
    "EnableMasking": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### secrets.json

```json
{
  "AzureOpenAI": {
    "ApiKey": "your-api-key-here"
  }
}
```

- **配置場所**: `%APPDATA%/VoicevoxHelper/secrets.json`（Gitリポジトリ外）
- **読み込み順序**: appsettings.json → secrets.json（後者が上書き）

---

## 11. エラーハンドリング方針

### Decision

**Fail-fast**原則を採用する。フォールバックは行わず、エラー原因を明確にして終了。

### 例

- **API認証失敗**: 「Azure OpenAIの認証に失敗しました。secrets.jsonのApiKeyを確認してください。」
- **LLMタイムアウト**: 「LLMの応答がタイムアウトしました。台本が長すぎる可能性があります。」
- **辞書登録APIエラー**: 「VOICEVOX APIに接続できませんでした。VOICEVOXが起動しているか確認してください。」

### ユーザー向けエラー文言

- **再試行可能性**: 「ネットワーク接続を確認して再試行してください」
- **対処手順**: 「設定ファイルの修正方法: [ドキュメントへのリンク]」

---

## 12. 実装技術サマリ

| 技術領域 | 選定 | ライセンス | 採用理由 |
|---------|------|-----------|---------|
| UI Framework | WPF (.NET 10) | MS-PL | Windows標準 |
| Theme | WPF-UI | MIT | Modern Fluent Design |
| Host | Microsoft.Extensions.Hosting | MIT | DI/設定/ログ統合 |
| MVVM | CommunityToolkit.Mvvm | MIT | Source Generator対応 |
| Navigation | Page/Frame | - | WPF標準 |
| LLM SDK | Azure.AI.OpenAI | MIT | 公式SDK |
| HTTP Client | HttpClient + Polly | BSD-3 | リトライ/タイムアウト制御 |
| Logging | Microsoft.Extensions.Logging + Serilog | Apache 2.0 | 構造化ログ |
| CSV Parser | CsvHelper | MS-PL / Apache 2.0 | 高速かつRFC 4180準拠 |
| JSON Parser | System.Text.Json | MIT | .NET標準 |
| Testing | NUnit + Moq | MIT | 既存方針に準拠 |

---

## 13. 残課題

以下は実装フェーズで詳細化する：

1. **アクセント核位置の妥当性検証**: VOICEVOXが受け入れる範囲の調査
2. **LLMプロンプトの精度向上**: Few-shot例の追加（実装後に評価）
3. **個人情報マスキングの精度**: 誤検出/漏れ検出の評価（実装後にテスト）
4. **辞書登録の並列化**: 現状は順次実行だが、並列化でパフォーマンス向上の余地あり

---

## References

- [WPF-UI GitHub](https://github.com/lepoco/wpfui)
- [CommunityToolkit.Mvvm Docs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Azure OpenAI .NET SDK](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.openai-readme)
- [VOICEVOX Engine API Docs](https://voicevox.github.io/voicevox_engine/api/)
- [Polly Documentation](https://www.pollydocs.org/)
- [CsvHelper Documentation](https://joshclose.github.io/CsvHelper/)
