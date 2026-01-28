# 開発者ガイド

## 必要環境
- Windows 10/11
- .NET 8 SDK

## セットアップ
1. リポジトリをクローンします。
2. appsettings.json と secrets.json を設定します。
3. ソリューションをビルドします。

## テスト
```
dotnet test VoicevoxHelper.slnx
```

## 注意事項
- CIでは実LLM/VOICEVOX APIへ接続しません。テストはスタブ/モックで実行します。
- OS依存処理はインターフェースで抽象化してください。
