namespace VoicevoxHelper.Infrastructure.LlmService;

/// <summary>
/// プロンプトテンプレート。
/// </summary>
public static class PromptTemplates
{
        public const string DictionaryExtractionSystem =
                """
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
                """;

        public const string DictionaryExtractionUser =
                """
                以下の台本から、音声合成用の辞書登録が必要な単語を抽出してください。

                台本:
                """;

    public const string ScriptRewriteSystem =
        "あなたは台本を喋りやすくリライトするアシスタントです。";

    public const string ScriptRewriteUser =
        "以下の台本を、意味を変えずに読みやすくリライトしてください。";
}
