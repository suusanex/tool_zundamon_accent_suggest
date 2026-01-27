# VOICEVOX User Dictionary API Contract

**Feature**: `001-core-workflow`  
**Date**: 2026-01-27  
**Base URL**: `http://127.0.0.1:50021`

本ファイルはVOICEVOX User Dictionary APIとの統合契約を定義する。

---

## 1. Get User Dictionary

### Endpoint

```
GET /user_dict
```

### Description

ユーザー辞書に登録されている全単語を取得する。冪等性チェック（既存単語の検出）に使用。

### Request

- **Headers**: なし
- **Query Parameters**: なし
- **Body**: なし

### Response

#### Success (200 OK)

```json
{
  "uuid1": {
    "surface": "東京",
    "pronunciation": "トウキョウ",
    "accent_type": 0,
    "word_type": "PROPER_NOUN",
    "priority": 5,
    "context_id": 1348,
    "part_of_speech": "名詞",
    "part_of_speech_detail_1": "固有名詞",
    "part_of_speech_detail_2": "地域",
    "part_of_speech_detail_3": "一般",
    "inflectional_type": "*",
    "inflectional_form": "*",
    "stem": "*",
    "yomi": "トウキョウ",
    "mora_count": 5,
    "accent_associative_rule": "*"
  },
  "uuid2": {
    ...
  }
}
```

### Error Responses

- **500 Internal Server Error**: VOICEVOX側のエラー

### C# Client Example

```csharp
public async Task<Dictionary<string, VoicevoxDictionaryEntry>> GetUserDictionaryAsync()
{
    var response = await _httpClient.GetAsync("/user_dict");
    response.EnsureSuccessStatusCode();
    
    var json = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<Dictionary<string, VoicevoxDictionaryEntry>>(json);
}
```

---

## 2. Add User Dictionary Word

### Endpoint

```
POST /user_dict_word
```

### Description

ユーザー辞書に新しい単語を追加する。

### Request

#### Query Parameters (required)

| Parameter | Type | Required | Constraints | Description |
|-----------|------|----------|-------------|-------------|
| surface | string | Yes | - | 単語の表層形 |
| pronunciation | string | Yes | カタカナのみ | 読み（カタカナ） |
| accent_type | integer | Yes | >= 0 | アクセント核位置 |

#### Query Parameters (optional)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| word_type | string | (empty) | PROPER_NOUN / COMMON_NOUN / VERB / ADJECTIVE / SUFFIX |
| priority | integer | 5 | 0-10の範囲（推奨: 1-9） |

### Response

#### Success (200 OK)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

返り値は新しく作成された単語のUUID（文字列）。

### Error Responses

- **422 Unprocessable Entity**: バリデーションエラー（必須パラメータ欠落、不正な値等）
- **500 Internal Server Error**: VOICEVOX側のエラー

### C# Client Example

```csharp
public async Task<string> AddUserDictWordAsync(DictionaryCandidate candidate)
{
    var queryParams = new Dictionary<string, string>
    {
        ["surface"] = candidate.Surface,
        ["pronunciation"] = candidate.Pronunciation,
        ["accent_type"] = candidate.AccentType.ToString()
    };
    
    if (candidate.WordType.HasValue)
        queryParams["word_type"] = candidate.WordType.Value.ToString().ToUpperInvariant();
    
    if (candidate.Priority.HasValue)
        queryParams["priority"] = candidate.Priority.Value.ToString();
    
    var uri = QueryHelpers.AddQueryString("/user_dict_word", queryParams);
    var response = await _httpClient.PostAsync(uri, null);
    response.EnsureSuccessStatusCode();
    
    var uuid = await response.Content.ReadAsStringAsync();
    return uuid.Trim('"'); // JSONの文字列としてエスケープされているため
}
```

---

## 3. Update User Dictionary Word

### Endpoint

```
PUT /user_dict_word/{word_uuid}
```

### Description

ユーザー辞書の既存単語を更新する。

### Request

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| word_uuid | string | Yes | 更新対象の単語UUID |

#### Query Parameters

同上（Add User Dictionary Wordと同じ）

### Response

#### Success (204 No Content)

レスポンスボディなし。

### Error Responses

- **422 Unprocessable Entity**: バリデーションエラー、またはUUIDが存在しない
- **500 Internal Server Error**: VOICEVOX側のエラー

### C# Client Example

```csharp
public async Task UpdateUserDictWordAsync(string uuid, DictionaryCandidate candidate)
{
    var queryParams = new Dictionary<string, string>
    {
        ["surface"] = candidate.Surface,
        ["pronunciation"] = candidate.Pronunciation,
        ["accent_type"] = candidate.AccentType.ToString()
    };
    
    if (candidate.WordType.HasValue)
        queryParams["word_type"] = candidate.WordType.Value.ToString().ToUpperInvariant();
    
    if (candidate.Priority.HasValue)
        queryParams["priority"] = candidate.Priority.Value.ToString();
    
    var uri = QueryHelpers.AddQueryString($"/user_dict_word/{uuid}", queryParams);
    var response = await _httpClient.PutAsync(uri, null);
    response.EnsureSuccessStatusCode();
}
```

---

## 4. Delete User Dictionary Word

### Endpoint

```
DELETE /user_dict_word/{word_uuid}
```

### Description

ユーザー辞書から単語を削除する。

### Request

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| word_uuid | string | Yes | 削除対象の単語UUID |

### Response

#### Success (204 No Content)

レスポンスボディなし。

### Error Responses

- **422 Unprocessable Entity**: UUIDが存在しない
- **500 Internal Server Error**: VOICEVOX側のエラー

### C# Client Example

```csharp
public async Task DeleteUserDictWordAsync(string uuid)
{
    var response = await _httpClient.DeleteAsync($"/user_dict_word/{uuid}");
    response.EnsureSuccessStatusCode();
}
```

---

## 5. Error Handling Strategy

### Fail-fast Policy

- 原則として自動リトライは行わない（失敗したらエラー終了）。
- タイムアウト/到達不可/HTTPステータス（422/500等）を分類して表示する。

### User-Facing Error Messages

| HTTP Status | Error Message | User Action |
|-------------|---------------|-------------|
| 422 | 「単語の登録に失敗しました。入力内容を確認してください。」 | 辞書候補ファイルの該当行を修正 |
| 500 | 「VOICEVOX APIでエラーが発生しました。VOICEVOXが正常に動作しているか確認してください。」 | VOICEVOXを再起動 |
| Timeout | 「VOICEVOX APIへの接続がタイムアウトしました。VOICEVOXが起動しているか確認してください。」 | VOICEVOXを起動 |
| Connection Refused | 「VOICEVOX APIに接続できませんでした。BaseURLの設定を確認してください。」 | appsettings.jsonの`VoiceVox.BaseUrl`を確認 |

---

## 6. Integration Tests

### Test Scenarios

1. **TS-V1**: GET /user_dict で既存辞書を取得できる
2. **TS-V2**: POST /user_dict_word で新規単語を追加し、UUIDが返る
3. **TS-V3**: PUT /user_dict_word/{uuid} で既存単語を更新できる（204 No Content）
4. **TS-V4**: DELETE /user_dict_word/{uuid} で単語を削除できる（204 No Content）
5. **TS-V5**: 存在しないUUIDに対してPUT/DELETEすると422エラー
6. **TS-V6**: 必須パラメータを欠落させたPOSTは422エラー
7. **TS-V7**: accent_typeに負の値を指定したPOSTは422エラー

### Mock Strategy

- **Unit Test**: IHttpClientFactoryをMockし、固定レスポンスを返す
- **Integration Test**: VOICEVOX Engineのモックサーバー（WireMock.Net等）を使用

---

## 7. Data Type Mapping

| VOICEVOX API Type | C# Type | Notes |
|-------------------|---------|-------|
| surface | string | そのまま |
| pronunciation | string | カタカナバリデーションを追加 |
| accent_type | int | 0以上 |
| word_type | string? | enum→string変換（大文字） |
| priority | int? | 0-10の範囲チェック |
| uuid | string | GUID形式（ハイフン区切り） |

---

## 8. Logging

### Request Logging

```csharp
_logger.LogDebug("VOICEVOX API呼び出し: {Method} {Endpoint}", method, endpoint);
```

### Response Logging

```csharp
_logger.LogDebug("VOICEVOX API応答: {StatusCode} {ReasonPhrase}", statusCode, reasonPhrase);
```

### Error Logging

```csharp
_logger.LogError(exception, "VOICEVOX API呼び出し失敗: {Method} {Endpoint}", method, endpoint);
```

**注意**: `surface`, `pronunciation`はログに記録してもよい（公開情報のため）。
