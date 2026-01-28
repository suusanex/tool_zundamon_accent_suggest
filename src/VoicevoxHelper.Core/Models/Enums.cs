namespace VoicevoxHelper.Core.Models;

/// <summary>
/// 辞書候補の種別を示す列挙型。
/// </summary>
public enum WordType
{
    Unknown = 0,
    ProperNoun = 1,
    TechnicalTerm = 2,
    SpecialReading = 3
}

/// <summary>
/// 予測の信頼度を示す列挙型。
/// </summary>
public enum ConfidenceLevel
{
    Low = 0,
    Medium = 1,
    High = 2
}

/// <summary>
/// 実行モードを示す列挙型。
/// </summary>
public enum ExecutionMode
{
    DictionaryExtraction = 0,
    DictionaryRegistration = 1,
    ScriptRewrite = 2
}

/// <summary>
/// 実行結果の状態を示す列挙型。
/// </summary>
public enum ExecutionStatus
{
    Unknown = 0,
    Success = 1,
    Failed = 2
}

/// <summary>
/// マスキングの種別を示す列挙型。
/// </summary>
public enum MaskingType
{
    None = 0,
    Email = 1,
    PhoneNumber = 2,
    Address = 3,
    Other = 4
}
