# Implementation Tasks: Core Workflow

**Feature**: `001-core-workflow`  
**Date**: 2026-01-27  
**Status**: Ready for Implementation

このドキュメントは実装タスクの完全なリストを提供する。各User Storyは独立して実装・テスト可能な単位として構成されている。

---

## Task Format

```
- [ ] [TaskID] [P] [StoryLabel] Description with file path
```

- **TaskID**: T001, T002... （実行順序）
- **[P]**: 並列実行可能（異なるファイル、依存なし）
- **[StoryLabel]**: User Story識別子（[US1], [US2], [US3]）
- **Description**: 明確なアクション + 具体的なファイルパス

---

## Phase 1: Setup（プロジェクト初期化）

**Goal**: 開発環境とプロジェクト構造を準備する。

- [ ] T001 ソリューションファイルを作成 (VoicevoxHelper.sln)
- [ ] T002 WPFアプリケーションプロジェクトを作成 (src/VoicevoxHelper.App)
- [ ] T003 コアライブラリプロジェクトを作成 (src/VoicevoxHelper.Core)
- [ ] T004 インフラストラクチャプロジェクトを作成 (src/VoicevoxHelper.Infrastructure)
- [ ] T005 ユニットテストプロジェクトを作成 (tests/VoicevoxHelper.Tests)
- [ ] T006 統合テストプロジェクトを作成 (tests/VoicevoxHelper.IntegrationTests)
- [ ] T007 プロジェクト参照を設定（App→Core/Infrastructure、Infrastructure→Core、Tests→Core/Infrastructure）
- [ ] T008 [P] NuGetパッケージをインストール（App: Generic Host, WPF-UI, MVVM Toolkit, Serilog）
- [ ] T009 [P] NuGetパッケージをインストール（Infrastructure: Azure.AI.OpenAI, CsvHelper）
- [ ] T010 [P] NuGetパッケージをインストール（Tests: NUnit, Moq）
- [ ] T011 appsettings.jsonを作成（src/VoicevoxHelper.App/appsettings.json）
- [ ] T012 .gitignoreを作成/更新（secrets.json, bin/, obj/, logs/ を除外）
- [ ] T013 README.mdを作成（プロジェクト概要、ビルド手順、実行方法）

**Independent Test**: ビルドが成功し、アプリケーションが起動すること。

---

## Phase 2: Foundational（共通基盤）

**Goal**: 全User Storyで共有される基盤コンポーネントを実装する。

### 2.1 ドメインモデル

- [ ] T014 [P] Enumsを定義 (src/VoicevoxHelper.Core/Models/Enums.cs: WordType, ConfidenceLevel, ExecutionMode, ExecutionStatus, MaskingType)
- [ ] T015 [P] DictionaryCandidateモデルを実装 (src/VoicevoxHelper.Core/Models/DictionaryCandidate.cs)
- [ ] T016 [P] Scriptモデルを実装 (src/VoicevoxHelper.Core/Models/Script.cs)
- [ ] T017 [P] MaskingResultモデルを実装 (src/VoicevoxHelper.Core/Models/MaskingResult.cs)
- [ ] T018 [P] ExecutionReportモデルを実装 (src/VoicevoxHelper.Core/Models/ExecutionReport.cs)
- [ ] T019 [P] ApplicationSettingsモデルを実装 (src/VoicevoxHelper.Core/Models/ApplicationSettings.cs)

### 2.2 バリデーション

- [ ] T020 [P] DictionaryCandidateValidatorを実装 (src/VoicevoxHelper.Core/Validators/DictionaryCandidateValidator.cs)
- [ ] T021 [P] ScriptValidatorを実装 (src/VoicevoxHelper.Core/Validators/ScriptValidator.cs)

### 2.3 例外

- [ ] T022 [P] カスタム例外クラスを実装 (src/VoicevoxHelper.Core/Exceptions/: LlmResponseParseException, ScriptTooLongException, VoicevoxApiException)

### 2.4 Generic Host & 設定

- [ ] T023 App.xaml.csでGeneric Host構成を実装 (src/VoicevoxHelper.App/App.xaml.cs)
- [ ] T024 appsettings.jsonとsecrets.jsonの読み込みロジックを実装 (App.xaml.cs内)
- [ ] T025 Serilogの設定を追加（Console + File出力）(App.xaml.cs内)
- [ ] T026 DI登録のベースを実装（ViewModels, Services, Clients）(App.xaml.cs内)

### 2.5 ナビゲーション

- [ ] T027 NavigationServiceインターフェースを定義 (src/VoicevoxHelper.Core/Interfaces/INavigationService.cs)
- [ ] T028 NavigationServiceを実装 (src/VoicevoxHelper.App/Services/NavigationService.cs)
- [ ] T029 MainWindow.xamlにFrameコントロールを追加 (src/VoicevoxHelper.App/Views/MainWindow.xaml)

### 2.6 UI共通コンポーネント

- [ ] T030 [P] エラー表示用UserControlを作成 (src/VoicevoxHelper.App/Views/Controls/ErrorDisplay.xaml)
- [ ] T031 [P] 進捗表示用UserControlを作成 (src/VoicevoxHelper.App/Views/Controls/ProgressDisplay.xaml)

**Independent Test**: 
- 全モデルクラスのUnit Test（バリデーション含む）
- Generic Hostが正常に起動し、DIコンテナが動作すること
- NavigationServiceでページ遷移ができること

---

## Phase 3: User Story 1 - 台本から辞書候補を抽出して編集 (P1)

**Goal**: 台本テキストを入力し、LLMで辞書候補を抽出し、CSV/JSONファイルとして出力する。

**Independent Test**: 台本を入力し、辞書候補ファイルが出力されること。ファイルを外部エディタで編集可能であること。

### 3.1 個人情報マスキング

- [ ] T032 IMaskingServiceインターフェースを定義 (src/VoicevoxHelper.Core/Interfaces/IMaskingService.cs)
- [ ] T033 PersonalInfoMaskingServiceを実装 (src/VoicevoxHelper.Infrastructure/Masking/PersonalInfoMaskingService.cs: 正規表現ベースのマスキング)
- [ ] T034 PersonalInfoMaskingServiceのUnit Testを作成 (tests/VoicevoxHelper.Tests/Infrastructure/Masking/PersonalInfoMaskingServiceTests.cs)

### 3.2 Azure OpenAI統合（辞書抽出）

- [ ] T035 ILlmServiceインターフェースを定義 (src/VoicevoxHelper.Core/Interfaces/ILlmService.cs)
- [ ] T036 PromptTemplatesクラスを実装（辞書抽出用プロンプト）(src/VoicevoxHelper.Infrastructure/LlmService/PromptTemplates.cs)
- [ ] T037 AzureOpenAIServiceを実装（辞書抽出機能）(src/VoicevoxHelper.Infrastructure/LlmService/AzureOpenAIService.cs)
- [ ] T038 AzureOpenAIServiceのUnit Testを作成（Mockレスポンス）(tests/VoicevoxHelper.Tests/Infrastructure/LlmService/AzureOpenAIServiceTests.cs)

### 3.3 ファイルI/O（CSV/JSON）

- [ ] T039 ICsvParserインターフェースを定義 (src/VoicevoxHelper.Core/Interfaces/ICsvParser.cs)
- [ ] T040 IJsonParserインターフェースを定義 (src/VoicevoxHelper.Core/Interfaces/IJsonParser.cs)
- [ ] T041 CsvDictionaryParserを実装 (src/VoicevoxHelper.Infrastructure/FileIO/CsvDictionaryParser.cs)
- [ ] T042 JsonDictionaryParserを実装 (src/VoicevoxHelper.Infrastructure/FileIO/JsonDictionaryParser.cs)
- [ ] T043 [P] CsvDictionaryParserのUnit Testを作成 (tests/VoicevoxHelper.Tests/Infrastructure/FileIO/CsvDictionaryParserTests.cs)
- [ ] T044 [P] JsonDictionaryParserのUnit Testを作成 (tests/VoicevoxHelper.Tests/Infrastructure/FileIO/JsonDictionaryParserTests.cs)

### 3.4 UI実装（辞書抽出ウィザード）

- [ ] T045 [US1] ModeSelectionPage.xamlを作成（モード選択画面）(src/VoicevoxHelper.App/Views/ModeSelectionPage.xaml)
- [ ] T046 [US1] ModeSelectionViewModelを実装 (src/VoicevoxHelper.App/ViewModels/ModeSelectionViewModel.cs)
- [ ] T047 [US1] DictionaryExtraction/InputPage.xamlを作成 (src/VoicevoxHelper.App/Views/DictionaryExtraction/InputPage.xaml)
- [ ] T048 [US1] DictionaryExtraction/InputViewModelを実装 (src/VoicevoxHelper.App/ViewModels/DictionaryExtraction/InputViewModel.cs)
- [ ] T049 [US1] DictionaryExtraction/PreviewPage.xamlを作成 (src/VoicevoxHelper.App/Views/DictionaryExtraction/PreviewPage.xaml)
- [ ] T050 [US1] DictionaryExtraction/PreviewViewModelを実装 (src/VoicevoxHelper.App/ViewModels/DictionaryExtraction/PreviewViewModel.cs)
- [ ] T051 [US1] DictionaryExtraction/OutputPage.xamlを作成 (src/VoicevoxHelper.App/Views/DictionaryExtraction/OutputPage.xaml)
- [ ] T052 [US1] DictionaryExtraction/OutputViewModelを実装 (src/VoicevoxHelper.App/ViewModels/DictionaryExtraction/OutputViewModel.cs)

### 3.5 統合・テスト

- [ ] T053 [US1] DI登録を追加（IMaskingService, ILlmService, ICsvParser, IJsonParser）(src/VoicevoxHelper.App/App.xaml.cs)
- [ ] T054 [US1] 辞書抽出フロー全体のIntegration Testを作成（スタブ/モック使用。CIで実LLMへ接続しない）(tests/VoicevoxHelper.IntegrationTests/DictionaryExtractionFlowTests.cs)
- [ ] T055 [US1] 手動テスト：台本入力→抽出→ファイル出力→外部エディタで確認

**Acceptance Criteria**:
- ✅ 台本テキストを入力し、辞書候補が抽出される
- ✅ CSV/JSON形式でファイル保存が可能
- ✅ 外部エディタ（Excel等）で編集可能
- ✅ LLM処理失敗時にエラーメッセージが表示される

---

## Phase 4: User Story 2 - 辞書候補をVOICEVOX APIに一括登録 (P2)

**Goal**: 編集済み辞書候補ファイルを読み込み、VOICEVOX APIに登録する。

**Independent Test**: 辞書候補ファイル（CSV/JSON）を入力し、VOICEVOX APIへの登録が実行され、成功/失敗レポートが表示されること。

### 4.1 VOICEVOX API統合

- [ ] T056 IVoicevoxApiClientインターフェースを定義 (src/VoicevoxHelper.Core/Interfaces/IVoicevoxApiClient.cs)
- [ ] T057 VoicevoxDictionaryEntry DTOを作成 (src/VoicevoxHelper.Infrastructure/VoicevoxApi/Models/VoicevoxDictionaryEntry.cs)
- [ ] T058 VoicevoxApiClientを実装（GET /user_dict）(src/VoicevoxHelper.Infrastructure/VoicevoxApi/VoicevoxApiClient.cs)
- [ ] T059 VoicevoxApiClientを実装（POST /user_dict_word）(src/VoicevoxHelper.Infrastructure/VoicevoxApi/VoicevoxApiClient.cs)
- [ ] T060 VoicevoxApiClientを実装（PUT /user_dict_word/{uuid}）(src/VoicevoxHelper.Infrastructure/VoicevoxApi/VoicevoxApiClient.cs)
- [ ] T061 VoicevoxApiClientを実装（DELETE /user_dict_word/{uuid}）(src/VoicevoxHelper.Infrastructure/VoicevoxApi/VoicevoxApiClient.cs)
- [ ] T062 [P] VoicevoxApiClientのUnit Testを作成（Mockレスポンス）(tests/VoicevoxHelper.Tests/Infrastructure/VoicevoxApi/VoicevoxApiClientTests.cs)

### 4.2 辞書登録サービス

- [ ] T063 [US2] IDictionaryRegistrationServiceインターフェースを定義 (src/VoicevoxHelper.Core/Interfaces/IDictionaryRegistrationService.cs)
- [ ] T064 [US2] DictionaryRegistrationServiceを実装（冪等性チェック、部分成功対応）(src/VoicevoxHelper.Infrastructure/Services/DictionaryRegistrationService.cs)
- [ ] T065 [US2] DictionaryRegistrationServiceのUnit Testを作成 (tests/VoicevoxHelper.Tests/Infrastructure/Services/DictionaryRegistrationServiceTests.cs)

### 4.3 UI実装（辞書登録ウィザード）

- [ ] T066 [US2] DictionaryRegistration/FileSelectionPage.xamlを作成 (src/VoicevoxHelper.App/Views/DictionaryRegistration/FileSelectionPage.xaml)
- [ ] T067 [US2] DictionaryRegistration/FileSelectionViewModelを実装 (src/VoicevoxHelper.App/ViewModels/DictionaryRegistration/FileSelectionViewModel.cs)
- [ ] T068 [US2] DictionaryRegistration/ValidationPage.xamlを作成 (src/VoicevoxHelper.App/Views/DictionaryRegistration/ValidationPage.xaml)
- [ ] T069 [US2] DictionaryRegistration/ValidationViewModelを実装 (src/VoicevoxHelper.App/ViewModels/DictionaryRegistration/ValidationViewModel.cs)
- [ ] T070 [US2] DictionaryRegistration/RegistrationPage.xamlを作成（進捗表示、結果レポート）(src/VoicevoxHelper.App/Views/DictionaryRegistration/RegistrationPage.xaml)
- [ ] T071 [US2] DictionaryRegistration/RegistrationViewModelを実装 (src/VoicevoxHelper.App/ViewModels/DictionaryRegistration/RegistrationViewModel.cs)

### 4.4 統合・テスト

- [ ] T072 [US2] DI登録を追加（IVoicevoxApiClient, IDictionaryRegistrationService）(src/VoicevoxHelper.App/App.xaml.cs)
- [ ] T073 [US2] HttpClientのタイムアウト設定と例外ハンドリングを設定（原則リトライしない）(src/VoicevoxHelper.App/App.xaml.cs)
- [ ] T074 [US2] 辞書登録フロー全体のIntegration Testを作成（スタブ/モック使用。CIで実VOICEVOXへ接続しない）(tests/VoicevoxHelper.IntegrationTests/DictionaryRegistrationFlowTests.cs)
- [ ] T075 [US2] 手動テスト：CSVファイル選択→バリデーション→登録実行→結果確認

**Acceptance Criteria**:
- ✅ CSV/JSONファイルを選択し、内容が検証される
- ✅ VOICEVOX APIに順次登録され、進捗が表示される
- ✅ 成功件数・失敗件数・失敗詳細がレポート表示される
- ✅ API認証エラー時に適切なエラーメッセージが表示される
- ✅ 既存単語の更新/スキップが設定に従って動作する

---

## Phase 5: User Story 3 - 台本を喋りやすく自動リライト (P3)

**Goal**: 台本テキストを入力し、LLMでリライトし、結果をコピー可能な形で表示する。

**Independent Test**: 台本を入力し、リライト結果が表示され、クリップボードにコピー可能であること。

### 5.1 Azure OpenAI統合（リライト）

- [ ] T076 PromptTemplatesクラスにリライト用プロンプトを追加 (src/VoicevoxHelper.Infrastructure/LlmService/PromptTemplates.cs)
- [ ] T077 [US3] AzureOpenAIServiceにリライト機能を実装 (src/VoicevoxHelper.Infrastructure/LlmService/AzureOpenAIService.cs)
- [ ] T078 [US3] リライト機能のUnit Testを作成 (tests/VoicevoxHelper.Tests/Infrastructure/LlmService/AzureOpenAIServiceRewriteTests.cs)

### 5.2 UI実装（リライトウィザード）

- [ ] T079 [US3] ScriptRewrite/InputPage.xamlを作成 (src/VoicevoxHelper.App/Views/ScriptRewrite/InputPage.xaml)
- [ ] T080 [US3] ScriptRewrite/InputViewModelを実装 (src/VoicevoxHelper.App/ViewModels/ScriptRewrite/InputViewModel.cs)
- [ ] T081 [US3] ScriptRewrite/ResultPage.xamlを作成（元の台本とリライト結果を並べて表示）(src/VoicevoxHelper.App/Views/ScriptRewrite/ResultPage.xaml)
- [ ] T082 [US3] ScriptRewrite/ResultViewModelを実装（クリップボードコピー機能）(src/VoicevoxHelper.App/ViewModels/ScriptRewrite/ResultViewModel.cs)

### 5.3 統合・テスト

- [ ] T083 [US3] DI登録を確認（ILlmServiceは既に登録済み）
- [ ] T084 [US3] リライトフロー全体のIntegration Testを作成（スタブ/モック使用。CIで実LLMへ接続しない）(tests/VoicevoxHelper.IntegrationTests/ScriptRewriteFlowTests.cs)
- [ ] T085 [US3] 手動テスト：台本入力→リライト実行→結果表示→コピー

**Acceptance Criteria**:
- ✅ 台本テキストを入力し、リライトが実行される
- ✅ 元の台本とリライト結果が並べて表示される
- ✅ リライト結果をワンクリックでコピー可能
- ✅ LLM処理失敗時にエラーメッセージが表示され、元の台本が保持される

---

## Phase 6: Polish & Cross-Cutting Concerns

**Goal**: 全体の品質向上、エラーハンドリング強化、ログ改善、ドキュメント整備。

### 6.1 エラーハンドリング強化

- [ ] T086 [P] 全ViewModelに共通エラーハンドリングを実装（try-catch + ログ出力）
- [ ] T087 [P] タイムアウト/接続エラー時のユーザー向けメッセージを改善
- [ ] T088 [P] バリデーションエラー時の具体的な箇所表示（行番号、カラム名）

### 6.2 ログ改善

- [ ] T089 [P] トークン使用量の詳細ログを追加（プロンプト/完了トークン、推定コスト）
- [ ] T090 [P] 機密情報（台本本文、APIキー）がログに出力されていないことを確認
- [ ] T091 [P] ExecutionReportをJSON形式でログファイルにエクスポート

### 6.3 パフォーマンス最適化

- [ ] T092 [P] 重複単語の排除ロジックを最適化（HashSet使用）
- [ ] T093 [P] CSV/JSONパース時の大量データ対応（ストリーム処理）

### 6.4 UI/UX改善

- [ ] T094 [P] WPF-UIテーマの適用確認（Fluent Design）
- [ ] T095 [P] 全画面で一貫したエラー表示スタイルを適用
- [ ] T096 [P] 進捗表示のアニメーション改善（ProgressRing使用）
- [ ] T097 [P] ファイル保存時のデフォルトファイル名を自動生成（例: dictionary_2026-01-27_123456.csv）

### 6.5 テストカバレッジ向上

- [ ] T098 [P] Core層のUnit Testカバレッジを80%以上にする
- [ ] T099 [P] Infrastructure層のUnit Testカバレッジを70%以上にする
- [ ] T100 [P] Integration Testを全User Storyで実行し、成功することを確認

### 6.6 ドキュメント整備

- [ ] T101 [P] USER_GUIDE.mdを作成（ユーザー向け使用方法）
- [ ] T102 [P] DEVELOPER_GUIDE.mdを作成（開発者向けセットアップ・ビルド手順）
- [ ] T103 [P] API_REFERENCE.mdを作成（主要インターフェース・クラスのリファレンス）
- [ ] T104 [P] README.mdを更新（badges、スクリーンショット、機能概要）

### 6.7 CI/CD

- [ ] T105 [P] GitHub Actions workflowを作成（ビルド・テスト自動実行）(.github/workflows/ci.yml)
- [ ] T106 [P] Coverlet + Coveralls統合（テストカバレッジレポート）

---

## Additions (Policy Alignment)

- [ ] T107 [P] FR-021 台本上限超過時の「分割方法」ガイダンス文言を確定し、ScriptValidatorのエラーメッセージに含める (src/VoicevoxHelper.Core/Validators/ScriptValidator.cs)
- [ ] T108 [P] T107のUnit Testを追加（ガイダンス文言を含むことを検証）(tests/VoicevoxHelper.Tests/Core/Validators/ScriptValidatorTests.cs)
- [ ] T109 [P] FR-024 PromptTemplatesに「辞書候補抽出のみ」等の制約文を必須化し、Unit Testで固定文言の存在を検証 (src/VoicevoxHelper.Infrastructure/LlmService/PromptTemplates.cs, tests/VoicevoxHelper.Tests/Infrastructure/LlmService/PromptTemplatesTests.cs)
- [ ] T110 [P] Success Criteriaの手動検証手順（時間/コスト/再現率の計測）をチェックリスト化 (specs/001-core-workflow/checklists/verification.md)

**Independent Test**: 
- 全User StoriesのAcceptance Scenariosが成功すること
- テストカバレッジが目標値を達成すること
- CI/CDパイプラインが正常に動作すること

---

## Dependencies（ストーリー完了順序）

```
Phase 1: Setup
  ↓
Phase 2: Foundational (全User Storyの前提)
  ↓
Phase 3: US1 (辞書抽出) ←┐
  ↓                      │  並行実装可能
Phase 4: US2 (API登録) ←┤  （US1の出力ファイルを使用）
  ↓                      │
Phase 5: US3 (リライト) ←┘  （独立、辞書不要）
  ↓
Phase 6: Polish
```

- **US1 → US2**: US2はUS1の出力ファイル（CSV/JSON）を入力とするため、US1完了後にテスト可能
- **US1 ⊥ US3**: US3はUS1と独立（辞書候補を使用しない）ため、並行実装可能
- **US2 ⊥ US3**: US2とUS3は独立のため、並行実装可能

---

## Parallel Execution Examples（並行実装の例）

### Phase 2: Foundational（最大並行度: 3）

```
Developer 1: ドメインモデル（T014-T019）
Developer 2: バリデーション・例外（T020-T022）
Developer 3: Generic Host設定・ナビゲーション（T023-T029）
```

### Phase 3: US1（最大並行度: 3）

```
Developer 1: マスキング（T032-T034）
Developer 2: LLM統合（T035-T038）
Developer 3: ファイルI/O（T039-T044）

→ 完了後、UI実装（T045-T052）は順次実行
```

### Phase 4-5: US2 & US3（最大並行度: 2）

```
Developer 1: US2（VOICEVOX API統合 + UI）（T056-T075）
Developer 2: US3（リライト機能 + UI）（T076-T085）
```

---

## Implementation Strategy（実装戦略）

### MVP Scope（最小可能プロダクト）

**MVP = User Story 1のみ**（辞書候補抽出）

- 台本入力 → LLM抽出 → CSV出力 → 完了
- US2（API登録）とUS3（リライト）は拡張機能として後から追加

### Incremental Delivery（段階的デリバリー）

1. **Week 1**: Phase 1-2（Setup + Foundational）→ 基盤完成
2. **Week 2**: Phase 3（US1）→ MVP完成
3. **Week 3**: Phase 4（US2）→ フル機能版
4. **Week 4**: Phase 5（US3）+ Phase 6（Polish）→ 完成版

---

## Task Summary

| Phase | Task Count | Parallel Opportunities | Story Labels |
|-------|-----------|------------------------|--------------|
| 1: Setup | 13 | 3 | - |
| 2: Foundational | 18 | 10 | - |
| 3: US1 | 24 | 8 | [US1] |
| 4: US2 | 20 | 5 | [US2] |
| 5: US3 | 10 | 3 | [US3] |
| 6: Polish | 21 | 21 | - |
| **Total** | **106** | **50** | 3 Stories |

---

## Format Validation

✅ **All tasks follow the checklist format**:
- Checkbox: `- [ ]`
- Task ID: Sequential (T001-T106)
- [P] marker: Added to parallelizable tasks
- [Story] label: Added to US1/US2/US3 tasks
- Description: Clear action with file path

---

## Next Steps

1. `quickstart.md`に従って開発環境をセットアップ
2. Phase 1のタスクから順次実行
3. 各タスク完了後にチェックボックスを`[x]`に更新
4. Phase 2完了後、Phase 3-5を並行実装検討
5. Phase 6で品質向上・ドキュメント整備

**Ready to implement!** 🚀
