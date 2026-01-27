# Implementation Plan: Core Workflow

**Branch**: `001-core-workflow` | **Date**: 2026-01-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-core-workflow/spec.md`

**Note**: This plan describes the complete architecture, design, and technical approach for implementing the VOICEVOX dictionary creation helper application.

## Summary

VOICEVOX台本整形・辞書作成支援ツールは、.NET 10 WPF デスクトップアプリケーションとして実装される。Azure OpenAI（LLM）を使用した辞書候補抽出と台本リライト、VOICEVOX API経由の辞書登録、ウィザード形式のUIを提供する。Generic Host + WPF-UI + CommunityToolkit MVVM を採用し、設定はappsettings.json/secrets.jsonで管理、CSV/JSON形式の辞書ファイルをサポートする。

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: WPF, Generic Host (Microsoft.Extensions.Hosting), WPF-UI (Fluent Design), CommunityToolkit.Mvvm, Azure.AI.OpenAI, CsvHelper  
**Storage**: ファイルシステム（appsettings.json, secrets.json, CSV/JSON辞書案ファイル）  
**Testing**: NUnit, Moq（Assertは`Assert.That`形式で統一）  
**Target Platform**: Windows 10/11 (x64)  
**Project Type**: デスクトップアプリケーション（WPF）  
**Performance Goals**: 
- 辞書候補抽出: 500文字台本を3分以内に処理
- API登録: 100件の辞書候補を5分以内に登録
- リライト: 500文字台本を2分以内に処理

**Constraints**:
- LLM呼び出しコスト: 1回の辞書抽出あたり平均100円以内
- メモリ使用量: 1GB以下（通常動作時）
- 台本サイズ上限: 10,000文字（LLMトークン制限）
- ログにユーザーの機密情報（台本本文、APIキー）を記録しない

**Scale/Scope**:
- ユーザー数: シングルユーザー（ローカル実行）
- 画面数: 約10画面（ウィザードステップ）
- 辞書候補: 1回の抽出で最大500件想定

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Gate 1: Verifiable Specification

**Status**: ✅ PASS

- spec.mdには**User Scenarios & Testing**が含まれており、各User Storyに**Independent Test**と**Acceptance Scenarios**が記載されている。
- 自動テスト方針: 全Acceptance Scenarioは以下のように裏付ける：
  - **US1（辞書候補抽出）**: Unit Test（LLM応答のモック）+ Integration Test（スタブ/モック。実APIへ接続しない）
  - **US2（API登録）**: Integration Test（スタブ/モック。実APIへ接続しない）
  - **US3（リライト）**: Unit Test（LLM応答のモック）
- 例外: LLMの出力品質評価（再現率80%）は手動レビュー（ドメイン知識を持つユーザーによる）を前提とする。これは自動化が困難なため、spec.mdの**Success Criteria**に明記済み。

### Gate 2: Test Coverage

**Status**: ✅ PASS (Planned)

- 全レイヤー（Core, Infrastructure, App）に対してUnit Testを作成する。
- 外部依存（VOICEVOX API, Azure OpenAI）は`IVoicevoxApiClient`, `ILlmService`インターフェースで抽象化し、テスト時はMoqでスタブ化する。
- CI環境では実OS変更（レジストリ、デバイス等）は行わない。
- 目標: Line Coverage 80%以上（計測はCoverlet使用）

### Gate 3: No Secrets in Repository

**Status**: ✅ PASS

- secrets.jsonはユーザーディレクトリ（`%APPDATA%/VoicevoxHelper/secrets.json`）に配置し、.gitignoreに明記。
- Azure OpenAI APIキー、エンドポイントURLはsecrets.jsonに分離。
- appsettings.jsonにはパブリックなデフォルト値のみ記載（例: `"BaseUrl": "http://127.0.0.1:50021"`）。

### Gate 4: Fail-fast Error Handling

**Status**: ✅ PASS

- 原則として処理失敗時のフォールバックは行わず、エラー原因を明確にしてエラー終了する。
- 例: 
  - LLM JSON parseエラー → 失敗ならエラー終了（再試行しない。部分結果は保存しない）
  - 辞書登録APIエラー → 失敗した項目を`FailureDetails`に記録し、最後にレポート表示（継続可能ならスキップして次へ進む）
- 全ての例外は`Exception.ToString()`の内容をログに出力する。

### Summary

全てのゲートはPASSまたはPASS(Planned)。Phase 2（実装）開始可能。

## Project Structure

### Documentation (this feature)

```text
specs/001-core-workflow/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output - technology decisions
├── data-model.md        # Phase 1 output - domain entities
├── quickstart.md        # Phase 1 output - development setup
└── contracts/           # Phase 1 output - API contracts
    ├── voicevox-api.md
    └── azure-openai-prompts.md
```

### Source Code (repository root)

```text
src/
├── VoicevoxHelper.Core/              # ドメインモデル・ビジネスロジック
│   ├── Models/                       # エンティティ（DictionaryCandidate, Script, etc.）
│   │   ├── DictionaryCandidate.cs
│   │   ├── Script.cs
│   │   ├── MaskingResult.cs
│   │   ├── ExecutionReport.cs
│   │   └── ApplicationSettings.cs
│   ├── Interfaces/                   # サービスインターフェース
│   │   ├── IVoicevoxApiClient.cs
│   │   ├── ILlmService.cs
│   │   ├── ICsvParser.cs
│   │   ├── IJsonParser.cs
│   │   └── IMaskingService.cs
│   ├── Validators/                   # バリデーションロジック
│   │   ├── DictionaryCandidateValidator.cs
│   │   └── ScriptValidator.cs
│   └── Exceptions/                   # カスタム例外
│       ├── LlmResponseParseException.cs
│       ├── ScriptTooLongException.cs
│       └── VoicevoxApiException.cs
│
├── VoicevoxHelper.Infrastructure/    # 外部API・I/O実装
│   ├── LlmService/                   # Azure OpenAI統合
│   │   ├── AzureOpenAIService.cs
│   │   └── PromptTemplates.cs
│   ├── VoicevoxApi/                  # VOICEVOX API統合
│   │   ├── VoicevoxApiClient.cs
│   │   └── Models/                   # API専用DTOs
│   │       └── VoicevoxDictionaryEntry.cs
│   ├── FileIO/                       # CSV/JSONパーサー
│   │   ├── CsvDictionaryParser.cs
│   │   └── JsonDictionaryParser.cs
│   └── Masking/                      # 個人情報マスキング
│       └── PersonalInfoMaskingService.cs
│
├── VoicevoxHelper.App/               # WPFアプリケーション
│   ├── ViewModels/                   # MVVM ViewModels
│   │   ├── MainViewModel.cs
│   │   ├── ModeSelectionViewModel.cs
│   │   ├── DictionaryExtraction/
│   │   │   ├── InputViewModel.cs
│   │   │   ├── PreviewViewModel.cs
│   │   │   └── OutputViewModel.cs
│   │   ├── DictionaryRegistration/
│   │   │   ├── FileSelectionViewModel.cs
│   │   │   ├── ValidationViewModel.cs
│   │   │   └── RegistrationViewModel.cs
│   │   └── ScriptRewrite/
│   │       ├── InputViewModel.cs
│   │       └── ResultViewModel.cs
│   ├── Views/                        # XAML Views（Pages）
│   │   ├── MainWindow.xaml
│   │   ├── ModeSelectionPage.xaml
│   │   ├── DictionaryExtraction/
│   │   │   ├── InputPage.xaml
│   │   │   ├── PreviewPage.xaml
│   │   │   └── OutputPage.xaml
│   │   ├── DictionaryRegistration/
│   │   │   ├── FileSelectionPage.xaml
│   │   │   ├── ValidationPage.xaml
│   │   │   └── RegistrationPage.xaml
│   │   └── ScriptRewrite/
│   │       ├── InputPage.xaml
│   │       └── ResultPage.xaml
│   ├── Services/                     # UI層サービス（ナビゲーション等）
│   │   └── NavigationService.cs
│   ├── App.xaml                      # Generic Host設定
│   ├── App.xaml.cs
│   └── appsettings.json              # デフォルト設定
│
└── tests/
    ├── VoicevoxHelper.Tests/         # Unit Tests
    │   ├── Core/
    │   │   ├── Models/
    │   │   │   ├── DictionaryCandidateTests.cs
    │   │   │   └── ScriptTests.cs
    │   │   └── Validators/
    │   │       └── DictionaryCandidateValidatorTests.cs
    │   ├── Infrastructure/
    │   │   ├── LlmService/
    │   │   │   └── AzureOpenAIServiceTests.cs
    │   │   ├── VoicevoxApi/
    │   │   │   └── VoicevoxApiClientTests.cs
    │   │   └── FileIO/
    │   │       ├── CsvDictionaryParserTests.cs
    │   │       └── JsonDictionaryParserTests.cs
    │   └── App/
    │       └── ViewModels/
    │           └── ModeSelectionViewModelTests.cs
    │
    └── VoicevoxHelper.IntegrationTests/ # Integration Tests
        ├── VoicevoxApiIntegrationTests.cs
        └── LlmServiceIntegrationTests.cs
```

**Structure Decision**: 

- **Single Solution + 3 Projects**: コアロジック（Core）、インフラ実装（Infrastructure）、UI（App）を分離。それぞれ独立してテスト可能。
- **MVVM Pattern**: ViewはXAML、ViewModelはC#、ModelはCoreに配置。CommunityToolkit.Mvvmで実装。
- **Generic Host**: App.xaml.csでIHostを構成し、DI（Dependency Injection）で全サービスを管理。
- **Page/Frame Navigation**: MainWindowに`Frame`を配置し、各ウィザードステップを`Page`として実装。

**Rationale**: 
- WPF標準の構造を維持しつつ、現代的なDI/ログ/設定管理を統合。
- 外部依存をインターフェースで抽象化し、テスタビリティを最大化。
- 各モード（辞書抽出、API登録、リライト）を独立したViewModelで管理し、並行開発を可能にする。

## Complexity Tracking

> **No violations detected**

本プランはConstitutionの全てのGateを満たしており、不要な複雑性はない。プロジェクトは単一ソリューション（3プロジェクト構成）で、リポジトリパターン等の過剰な抽象化は採用していない。

---

## Architecture Details

### 1. 全体アーキテクチャ

```
┌─────────────────────────────────────────────────────────┐
│                     WPF App (UI Layer)                  │
│  ┌─────────────┐  ┌─────────────┐  ┌────────────────┐  │
│  │ Mode Select │  │ Dict Extract│  │ Dict Register  │  │
│  │ ViewModel   │  │ ViewModels  │  │ ViewModels     │  │
│  │             │  │             │  │                │  │
│  │ ┌─────────┐ │  │ ┌─────────┐ │  │ ┌────────────┐ │  │
│  │ │ Views   │ │  │ │ Pages   │ │  │ │ Pages      │ │  │
│  │ └─────────┘ │  │ └─────────┘ │  │ └────────────┘ │  │
│  └─────────────┘  └─────────────┘  └────────────────┘  │
└─────────────────────────────────────────────────────────┘
              ↓ DI (Generic Host)
┌─────────────────────────────────────────────────────────┐
│              Core (Domain & Business Logic)             │
│  ┌─────────────────┐  ┌──────────────────────────────┐ │
│  │ Models          │  │ Interfaces                    │ │
│  │ - Dictionary    │  │ - IVoicevoxApiClient         │ │
│  │   Candidate     │  │ - ILlmService                │ │
│  │ - Script        │  │ - ICsvParser / IJsonParser   │ │
│  │ - ExecutionReport│  │ - IMaskingService             │ │
│  └─────────────────┘  └──────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
              ↓ Implementation
┌─────────────────────────────────────────────────────────┐
│          Infrastructure (External Integration)          │
│  ┌───────────────┐  ┌──────────────┐  ┌─────────────┐ │
│  │ LlmService    │  │ VoicevoxApi  │  │ FileIO      │ │
│  │ (Azure OpenAI)│  │ (HTTP Client)│  │ (CSV/JSON)  │ │
│  └───────────────┘  └──────────────┘  └─────────────┘ │
│  ┌───────────────────────────────────────────────────┐ │
│  │ Masking Service (Personal Info Detection)        │ │
│  └───────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### 2. 層/責務

| Layer | Responsibility | Dependencies |
|-------|----------------|--------------|
| **App (UI)** | ユーザー入力、画面遷移、レポート表示 | Core, Infrastructure |
| **Core (Domain)** | ドメインモデル、バリデーション、インターフェース定義 | なし（Pure Domain） |
| **Infrastructure** | 外部API呼び出し、ファイルI/O、個人情報マスキング | Core |

### 3. 依存関係の方向

```
App (UI) ──┐
           ├──→ Core (Domain) ←── Infrastructure
           └──→ Infrastructure
```

- **App**は**Core**と**Infrastructure**に依存
- **Infrastructure**は**Core**のインターフェースを実装（依存性逆転）
- **Core**は外部に依存しない（Pure Domain）

### 4. 画面/遷移設計（ウィザード）

#### モード選択画面

```
┌────────────────────────────────────────────┐
│  VOICEVOX Helper - モード選択               │
├────────────────────────────────────────────┤
│  実行したい処理を選択してください：          │
│                                            │
│  ○ 辞書候補抽出（台本から辞書候補を作成）     │
│  ○ 辞書API登録（CSVをVOICEVOXに登録）        │
│  ○ 台本リライト（読み上げやすく変換）        │
│                                            │
│  [次へ] [キャンセル]                        │
└────────────────────────────────────────────┘
```

#### モードA: 辞書候補抽出

```
Step 1: 入力
  - 説明文: "台本テキストを入力してください（最大10,000文字）"
  - 入力: MultiLineTextBox
  - 文字数表示: "1,234 / 10,000"
  - ボタン: [次へ]

Step 2: 抽出実行
  - 説明文: "辞書候補を抽出しています...（LLMで解析中）"
  - 進捗表示: ProgressRing（WPF-UI）+ "N件抽出済み"
  - 自動遷移: 完了後に次へ

Step 3: プレビュー
  - 説明文: "抽出された辞書候補を確認してください"
  - 表示: DataGrid（surface, pronunciation, accent_type, note）
  - 編集: 行の削除可能（追加は外部エディタで）
  - ボタン: [戻る] [保存して完了]

Step 4: ファイル出力
  - 説明文: "辞書候補をファイルに保存します"
  - 選択: ファイル形式（CSV/JSON）
  - 選択: 保存先ディレクトリ
  - ボタン: [保存] [キャンセル]

Step 5: 完了
  - 説明文: "辞書候補をファイルに保存しました"
  - 表示: 保存先パス、件数、所要時間
  - ボタン: [ファイルを開く] [終了]
```

#### モードB: 辞書API登録

```
Step 1: ファイル選択
  - 説明文: "辞書候補ファイル（CSV/JSON）を選択してください"
  - 入力: FilePickerボタン → OpenFileDialog
  - 表示: 選択されたファイルパス
  - ボタン: [次へ]

Step 2: バリデーション
  - 説明文: "ファイル内容を検証しています..."
  - 進捗表示: ProgressRing
  - 自動遷移: 検証成功 → 次へ / 失敗 → エラー表示

Step 3: 差分/実行プレビュー
  - 説明文: "以下の単語をVOICEVOXに登録します"
  - 表示: DataGrid（新規N件、更新M件、スキップL件）
  - ボタン: [戻る] [登録実行]

Step 4: 登録実行
  - 説明文: "VOICEVOXに登録しています...（N件中M件完了）"
  - 進捗表示: ProgressBar + 現在の単語surface
  - 自動遷移: 完了後に次へ

Step 5: 結果表示
  - 説明文: "辞書登録が完了しました"
  - 表示: 成功件数、失敗件数、失敗リスト（surface + エラー）
  - ボタン: [ログをエクスポート] [終了]
```

#### モードC: リライト

```
Step 1: 入力
  - 説明文: "リライトする台本を入力してください"
  - 入力: MultiLineTextBox
  - 文字数表示: "1,234 / 10,000"
  - ボタン: [次へ]

Step 2: リライト実行
  - 説明文: "台本をリライトしています...（LLMで変換中）"
  - 進捗表示: ProgressRing
  - 自動遷移: 完了後に次へ

Step 3: 結果表示/コピー
  - 説明文: "リライト結果"
  - 表示: 
    - 元の台本（上段、ReadOnlyTextBox）
    - リライト後（下段、ReadOnlyTextBox）
  - ボタン: [コピー] [戻る] [完了]

Step 4: 完了
  - 説明文: "リライトが完了しました"
  - 表示: 所要時間、トークン使用量
  - ボタン: [終了]
```

### 5. 設定設計

#### appsettings.json

| Section | Key | Type | Default | Description |
|---------|-----|------|---------|-------------|
| AzureOpenAI | Endpoint | string | (empty) | Azure OpenAIエンドポイント |
| AzureOpenAI | DeploymentName | string | "gpt-4" | デプロイメント名 |
| AzureOpenAI | ApiVersion | string | "2024-02-15-preview" | APIバージョン |
| AzureOpenAI | MaxTokens | int | 4096 | 最大トークン数 |
| AzureOpenAI | Temperature | double | 0.7 | 温度パラメータ |
| AzureOpenAI | Timeout | TimeSpan | "00:02:00" | タイムアウト |
| VoiceVox | BaseUrl | string | "http://127.0.0.1:50021" | VOICEVOX APIベースURL |
| VoiceVox | Timeout | TimeSpan | "00:00:30" | タイムアウト |
| VoiceVox | UpdateExisting | bool | true | 既存単語を更新するか |
| Dictionary | DefaultFormat | string | "CSV" | 既定のファイル形式 |
| Dictionary | DefaultEncoding | string | "UTF-8" | エンコーディング |
| Dictionary | MaxFileSize | long | 10485760 | ファイルサイズ上限（10MB） |
| Dictionary | OutputDirectory | string | "./output" | 出力ディレクトリ |
| Script | MaxLength | int | 10000 | 台本の最大文字数 |
| Script | EnableMasking | bool | true | 個人情報マスキング |

#### secrets.json

| Section | Key | Type | Description |
|---------|-----|------|-------------|
| AzureOpenAI | ApiKey | string | Azure OpenAI APIキー |

**読み込み順序**:
1. appsettings.json（デフォルト値）
2. secrets.json（APIキー等を上書き）

**運用**: 環境差し替えは`secrets.json`を差し替えることで実現。

### 6. データ形式（辞書案ファイル）

**正式採用**: CSV（基準フォーマット）、JSON（互換フォーマット）

#### CSV Schema

```csv
surface,pronunciation,accent_type,word_type,priority,note
東京,トウキョウ,0,PROPER_NOUN,5,地名
VOICEVOX,ボイスボックス,3,PROPER_NOUN,7,製品名
```

- **エンコーディング**: UTF-8 with BOM（Excel互換）
- **区切り文字**: カンマ
- **エスケープ**: RFC 4180準拠

#### JSON Schema

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

#### フィールド定義

| Field | Required | VOICEVOX API | Description |
|-------|----------|--------------|-------------|
| surface | Yes | 必須 | 単語の表層形 |
| pronunciation | Yes | 必須 | カタカナ読み |
| accent_type | Yes | 必須 | アクセント核位置（0=平板） |
| word_type | No | 任意 | 品詞（PROPER_NOUN等） |
| priority | No | 任意 | 優先度（0-10、推奨1-9） |
| note | No | - | 備考（API未送信） |
| uuid | No | 更新時 | 更新時に必要 |

### 7. 外部連携設計

#### A) Azure OpenAI

**3タスクのプロンプト方針**: 詳細は[contracts/azure-openai-prompts.md](contracts/azure-openai-prompts.md)を参照。

1. **辞書候補抽出**: システムプロンプトで「JSON形式で出力」を指示。`response_format: json_object`を使用。
2. **台本リライト**: プレーンテキスト出力。システムプロンプトで「元の意味を保持」を強調。

**構造化出力**: JSON mode（Azure OpenAI API `response_format`）を使用。パース失敗時はエラー終了（原則再試行しない）。

**トークン最適化**: 台本全体を1回の呼び出しで処理（分割しない）。システムプロンプトは共通化してキャッシュ効果を狙う。

**機密配慮**: 台本本文はログに記録しない。LLM送信前に個人情報をマスキング。

#### B) VOICEVOX エンジン（辞書API）

詳細は[contracts/voicevox-api.md](contracts/voicevox-api.md)を参照。

- **GET /user_dict**: 既存辞書取得（冪等性チェック）
- **POST /user_dict_word**: 新規単語追加
- **PUT /user_dict_word/{uuid}**: 既存単語更新
- **DELETE /user_dict_word/{uuid}**: 単語削除

**冪等性**: `UpdateExisting=true`の場合、同一`surface`の既存UUIDを取得して`PUT`で更新。`false`の場合はスキップ。

**部分成功時のレポート**: 成功リスト（UUID）と失敗リスト（surface + エラーメッセージ）を別々に記録し、UIに表示。

### 8. バリデーション/エラーハンドリング

#### 辞書案ファイルの検証

- **必須列**: surface, pronunciation, accent_type
- **accent_type範囲**: 0以上の整数（上限はVOICEVOXが検証）
- **pronunciation文字種**: カタカナと長音記号（ー）のみ
- **パースエラー時**: 1件でもエラーがあれば処理開始せず失敗（Fail-fast）

#### API失敗時の挙動

- **タイムアウト**: エラー終了（原則リトライしない）
- **接続不可**: エラー終了（"VOICEVOXが起動しているか確認してください"）
- **1件だけ失敗**: `FailureDetails`に記録し、次へ継続（部分成功）

#### ユーザー向けエラー文言

- **再実行案内**: "ネットワーク接続を確認して再実行してください"
- **対処手順**: "設定ファイルの修正方法: appsettings.jsonの`VoiceVox.BaseUrl`を確認"

### 9. ログ/監査/デバッグ容易性

#### ログレベル

| Level | 用途 |
|-------|------|
| Trace | LLM/APIリクエストの詳細（トークン数、所要時間） |
| Debug | 辞書登録の進捗（N件中M件完了） |
| Information | モード開始/終了、成否サマリ |
| Warning | 部分失敗（50/100件成功） |
| Error | API認証エラー、タイムアウト |
| Critical | 予期しない例外 |

#### 監査性

- **いつ**: タイムスタンプをUTC+ローカルで記録
- **どのモードで**: `ExecutionMode`を記録
- **何件処理し**: `TotalCount`, `SuccessCount`, `FailureCount`
- **成功/失敗がどうだったか**: `ExecutionStatus` + `FailureDetails`

#### 機密情報の扱い

- **台本本文**: 記録しない（文字数のみ）
- **APIキー**: 記録しない（認証エラー時も伏せる）
- **辞書候補**: surface/pronunciation/accent_typeは記録可（公開情報）

### 10. 実装技術の選定

| 技術 | 選定 | 理由 |
|------|------|------|
| UI Framework | WPF (.NET 10) | Windows標準、成熟したエコシステム |
| Theme | WPF-UI | Modern Fluent Design、MIT License |
| Host | Microsoft.Extensions.Hosting | DI/設定/ログ統合 |
| MVVM | CommunityToolkit.Mvvm | Source Generator対応、.NET公式推奨 |
| Navigation | Page/Frame | WPF標準、履歴管理容易 |
| LLM SDK | Azure.AI.OpenAI | 公式SDK、構造化出力対応 |
| HTTP Client | HttpClient | タイムアウト/例外処理 |
| CSV Parser | CsvHelper | 高速、RFC 4180準拠 |
| JSON Parser | System.Text.Json | .NET標準、高速 |

**不採用の例**:
- **Prism**: 過剰な機能、学習コスト高
- **Repository Pattern**: 本プロジェクトには過剰な抽象化（直接APIクライアントを注入）

---

## Next Steps

1. **Phase 2: Tasks**: `/speckit.tasks`コマンドで実装タスクリスト（tasks.md）を生成
2. **Implementation**: quickstart.mdに従って開発環境を構築し、タスクを順次実行
3. **Testing**: 各タスク完了後に対応するUnit Testを作成・実行
4. **Integration**: 全モードの実装完了後、Integration Testで全体動作を検証

---

## References

- [spec.md](spec.md) - Feature Specification
- [research.md](research.md) - Technology Research
- [data-model.md](data-model.md) - Data Model Design
- [quickstart.md](quickstart.md) - Development Setup Guide
- [contracts/voicevox-api.md](contracts/voicevox-api.md) - VOICEVOX API Contract
- [contracts/azure-openai-prompts.md](contracts/azure-openai-prompts.md) - LLM Prompt Templates
- [VOICEVOX Engine API Documentation](https://voicevox.github.io/voicevox_engine/api/)
- [Azure OpenAI .NET SDK](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.openai-readme)
