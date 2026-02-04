# Quickstart: Core Workflow Development

**Feature**: `001-core-workflow`  
**Date**: 2026-01-27

本ガイドは開発開始時の最小限の手順を示す。完全な開発環境構築とファーストコミットまでを網羅。

---

## Prerequisites

### Required

- **Windows 10/11** (x64)
- **.NET 10 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- **Visual Studio 2025** or **Visual Studio Code**
- **Git** (バージョン管理)

### Optional (Development)

- **VOICEVOX** ([Download](https://voicevox.hiroshiba.jp/)) - ローカルテスト用
- **Azure OpenAI** アカウント（API Key取得済み）

---

## Step 1: Repository Setup

### Clone Repository

```powershell
git clone <repository-url>
cd tool_zundamon_accent_suggest
```

### Create Feature Branch

```powershell
git checkout -b 001-core-workflow
```

---

## Step 2: Solution & Project Creation

### Create Solution

```powershell
dotnet new sln -n VoicevoxHelper
```

### Create WPF Application

```powershell
dotnet new wpf -n VoicevoxHelper.App -f net10.0-windows
dotnet sln add VoicevoxHelper.App
```

### Create Class Libraries

```powershell
# ドメインモデル・ビジネスロジック
dotnet new classlib -n VoicevoxHelper.Core -f net10.0
dotnet sln add VoicevoxHelper.Core

# 外部API統合（VOICEVOX, Azure OpenAI）
dotnet new classlib -n VoicevoxHelper.Infrastructure -f net10.0
dotnet sln add VoicevoxHelper.Infrastructure

# ユニットテスト
dotnet new nunit -n VoicevoxHelper.Tests -f net10.0
dotnet sln add VoicevoxHelper.Tests
```

### Add Project References

```powershell
dotnet add VoicevoxHelper.App reference VoicevoxHelper.Core
dotnet add VoicevoxHelper.App reference VoicevoxHelper.Infrastructure
dotnet add VoicevoxHelper.Infrastructure reference VoicevoxHelper.Core
dotnet add VoicevoxHelper.Tests reference VoicevoxHelper.Core
dotnet add VoicevoxHelper.Tests reference VoicevoxHelper.Infrastructure
```

---

## Step 3: Install NuGet Packages

### VoicevoxHelper.App

```powershell
cd VoicevoxHelper.App

# Generic Host
dotnet add package Microsoft.Extensions.Hosting --version 10.0.0

# WPF-UI (Fluent Design)
dotnet add package WPF-UI --version 3.0.5

# MVVM Toolkit
dotnet add package CommunityToolkit.Mvvm --version 8.4.0

# 設定・ログ
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Logging
dotnet add package Serilog.Extensions.Hosting
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Console
```

### VoicevoxHelper.Infrastructure

```powershell
cd ../VoicevoxHelper.Infrastructure

# Azure OpenAI
dotnet add package Azure.AI.OpenAI --version 2.1.0

# HTTP Client
dotnet add package Microsoft.Extensions.Http

# CSV Parser
dotnet add package CsvHelper --version 33.0.1

# JSON (標準)
# System.Text.Json は .NET 10 に含まれる
```

### VoicevoxHelper.Tests

```powershell
cd ../VoicevoxHelper.Tests

# モックライブラリ
dotnet add package Moq --version 4.20.72
dotnet add package NUnit --version 4.2.2
dotnet add package NUnit3TestAdapter --version 4.6.0
dotnet add package Microsoft.NET.Test.Sdk --version 17.13.0
```

---

## Step 4: Configuration Files

### appsettings.json

`VoicevoxHelper.App/appsettings.json`を作成：

```json
{
  "Llm": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "",
    "Deployment": "gpt-4",
    "TimeoutSeconds": 120,
    "PromptCostPer1kTokens": 0,
    "CompletionCostPer1kTokens": 0
  },
  "Voicevox": {
    "BaseUrl": "http://127.0.0.1:50021",
    "ApiKey": "",
    "TimeoutSeconds": 30,
    "UpdateExistingWords": true
  },
  "Dictionary": {
    "UseCsvAsDefault": true
  },
  "Logging": {
    "MinimumLevel": "Information"
  }
}
```

**ビルドアクション**を`Content`、**出力ディレクトリにコピー**を`新しい場合はコピー`に設定。

### secrets.json

`%APPDATA%\VoicevoxHelper\secrets.json`を作成：

```json
{
  "Llm": {
    "ApiKey": "your-api-key-here"
  }
}
```

**注意**: このファイルはGitリポジトリに含めないこと（`.gitignore`に追加）。

### .gitignore

プロジェクトルートに`.gitignore`を作成（既存の場合は追記）：

```
# Secrets
secrets.json
**/secrets.json

# Build outputs
bin/
obj/
publish/

# User-specific files
*.user
*.suo
*.userosscache
*.sln.docstates

# Visual Studio
.vs/
*.vsidx
*.vspscc

# Logs
logs/
*.log
```

---

## Step 5: Minimal Working App

### App.xaml.cs (Generic Host Setup)

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Windows;

namespace VoicevoxHelper.App;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Generic Host
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                
                var secretsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "VoicevoxHelper",
                    "secrets.json");
                
                config.AddJsonFile(secretsPath, optional: true, reloadOnChange: true);
            })
            .UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/voicevox-helper-.log", rollingInterval: RollingInterval.Day))
            .ConfigureServices((context, services) =>
            {
                // ViewModels
                services.AddTransient<MainViewModel>();
                
                // Windows
                services.AddTransient<MainWindow>();
                
                // TODO: Add services (LLM, VOICEVOX API clients, etc.)
            })
            .Build();

        _host.Start();

        // Show Main Window
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}
```

### MainWindow.xaml

```xml
<Window x:Class="VoicevoxHelper.App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
        Title="VOICEVOX Helper" Height="600" Width="900">
    <Grid>
        <TextBlock Text="VOICEVOX Helper - Ready to Implement" 
                   HorizontalAlignment="Center" 
                   VerticalAlignment="Center"
                   FontSize="24" />
    </Grid>
</Window>
```

### MainWindow.xaml.cs

```csharp
using System.Windows;

namespace VoicevoxHelper.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

### MainViewModel.cs

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace VoicevoxHelper.App;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _welcomeMessage = "VOICEVOX Helper へようこそ";
}
```

---

## Step 6: Verify Build

### Build Solution

```powershell
dotnet build
```

期待出力:

```
ビルドに成功しました。
    0 個の警告
    0 エラー
```

### Run Application

```powershell
dotnet run --project VoicevoxHelper.App
```

ウィンドウが起動し、"VOICEVOX Helper - Ready to Implement"と表示されることを確認。

---

## Step 7: First Unit Test

### Create Test Class

`VoicevoxHelper.Tests/Core/DictionaryCandidateTests.cs`を作成：

```csharp
using NUnit.Framework;
using VoicevoxHelper.Core.Models;

namespace VoicevoxHelper.Tests.Core;

[TestFixture]
public class DictionaryCandidateTests
{
    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var candidate = new DictionaryCandidate
        {
            Surface = "東京",
            Pronunciation = "トウキョウ",
            AccentType = 0
        };

        // Assert
        Assert.That(candidate.Surface, Is.EqualTo("東京"));
        Assert.That(candidate.Pronunciation, Is.EqualTo("トウキョウ"));
        Assert.That(candidate.AccentType, Is.EqualTo(0));
    }

    [Test]
    public void Pronunciation_WithHiragana_ShouldFailValidation()
    {
        // TODO: バリデーションロジック実装後にテスト追加
        Assert.Pass("Validation not implemented yet");
    }
}
```

### Run Tests

```powershell
dotnet test
```

期待出力:

```
成功!   -失敗:     0、合格:     1、スキップ:     0、合計:     1
```

---

## Step 8: Commit & Push

### Stage Changes

```powershell
git add .
```

### Commit

```powershell
git commit -m "feat: 001-core-workflow の初期プロジェクト構成を作成

- WPFアプリケーション、コアライブラリ、インフラストラクチャ、テストプロジェクトを追加
- Generic Host + WPF-UI + MVVM Toolkit 統合
- appsettings.json と secrets.json の設定構造を定義
- 最小限の動作確認（ビルド・起動・テスト成功）
"
```

### Push to Remote

```powershell
git push -u origin 001-core-workflow
```

---

## Next Steps

### Phase 2: Implementation Tasks

1. **Data Models**: `DictionaryCandidate`, `Script`, `ExecutionReport`の実装
2. **VOICEVOX Client**: `IVoicevoxApiClient`インターフェースと実装
3. **Azure OpenAI Client**: `ILlmService`インターフェースと実装
4. **UI Wizard Pages**: モード選択、辞書抽出、API登録、リライトの画面実装
5. **CSV/JSON I/O**: `ICsvParser`, `IJsonParser`の実装
6. **Personal Info Masking**: `IMaskingService`の実装
7. **Unit Tests**: 各レイヤーのテストカバレッジ80%以上

詳細は`tasks.md`（Phase 2で作成）を参照。

---

## Troubleshooting

### Issue: "FileNotFoundException: appsettings.json"

**Solution**: `appsettings.json`のビルドアクションが`Content`、出力ディレクトリにコピーが`新しい場合はコピー`に設定されているか確認。

### Issue: "Cannot find WPF-UI controls"

**Solution**:
1. NuGetパッケージが正しくインストールされているか確認
2. プロジェクトを再ビルド（Clean → Rebuild）

### Issue: "Azure OpenAI authentication failed"

**Solution**: `secrets.json`のApiKeyが正しく設定されているか確認。エンドポイントURLが正しいか確認。

---

## References

- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10)
- [WPF-UI GitHub](https://github.com/lepoco/wpfui)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Generic Host in WPF](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines)
- [Serilog Documentation](https://serilog.net/)
