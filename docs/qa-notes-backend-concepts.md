# 後端概念 Q&A 筆記

整理自 `TodosController.cs` / `TodoService.cs` / `TodoDtos.cs` / `TodoItem.cs` 相關討論。

## 1. `[ServiceFilter(typeof(CsrfValidationFilter))]`

- `ServiceFilter` 表示這個 filter 透過 DI 容器解析（需在 `Program.cs` 用 `AddScoped<CsrfValidationFilter>()` 註冊），而不是直接 `new`。
- `CsrfValidationFilter`（[Auth/CsrfValidationFilter.cs](../backend/TodoList.Api/Auth/CsrfValidationFilter.cs)）是專案自製的 CSRF 防護：
  - 後端沒辦法用 ASP.NET Core 內建 Antiforgery（需要跟 Mini-SSO 共享 Data Protection keys），所以改用「非同源的表單 POST 無法自訂 header」這個特性
  - 只檢查 request 是否帶有非空的 `X-CSRF-TOKEN` header，沒有就回 403
  - 搭配「token」session cookie 為 `SameSite=Lax`（跨站不會被夾帶）作為主要防線
- 只掛在會修改資料的 action（POST/PUT/PATCH/DELETE），GET 不需要。

## 2. `CreatedAtAction`

```csharp
return CreatedAtAction(nameof(GetById), new { id = todo.Id }, todo);
```

回傳標準的 REST 201 Created 回應：
1. Status code 設為 `201 Created`
2. Response header 加上 `Location`，指向呼叫 `GetById` 並帶入 `id` 產生的 URL
3. Body 放入建立好的資源（`TodoDto`）

比起 `Ok(todo)` 更符合 REST 語意，client 可從 `Location` 知道新資源的存取路徑。

## 3. RESTful Create / Update 慣例

- **Create (POST)**：慣例回傳 `201 Created` + 新建立的內容（含伺服器產生的欄位，如 `Id`、`CreatedAt`）。
- **Update**：
  - PUT（完整替換）：`200 OK` + 更新後內容，或 `204 No Content`（因為 client 送的就是完整資源），兩種都合規
  - PATCH（部分更新）：通常回 `200 OK` + 合併後的完整內容，因為 client 只送了部分欄位
  - 沒有 RFC 強制規定，重點是團隊內部一致（本專案 Create/Update 都回傳完整內容，前端邏輯較一致）。

## 4. `ToDto` 手動映射 vs AutoMapper

```csharp
private static TodoDto ToDto(TodoItem t) => new(
    t.Id, t.Title, t.Description, t.IsCompleted, t.Priority, t.DueDate, t.CreatedAt, t.UpdatedAt);
```

- 功能上等同 AutoMapper 做的事，手刻版本的優點：
  - 沒有 reflection 開銷
  - 編譯期就能檢查欄位對應是否正確（AutoMapper 常見錯誤是 runtime 才爆）
- 缺點：DTO/Entity 對應關係一多會變囉唆。此專案規模小（一個 Entity 對一個 DTO），手刻合理，不需要額外依賴。

## 5. Enum 放置位置（`TodoPriority`）

目前 `TodoPriority` 直接定義在 `TodoItem.cs`（Entities namespace）內，同時被 Entity、Dto、Request 共用。

- 現況優點：只有一處使用者關心的話容易找、專案小不需要額外資料夾
- 潛在問題：DTO/Request 依賴了 `Entities` namespace，是輕微的分層洩漏
- 若要更嚴謹：可抽到獨立 `Enums` 資料夾或共用 namespace，讓三層平等依賴，而不是互相跨層引用
- 專案目前規模下維持現狀無妨，屬於「值得知道、不急著做」的重構點。

## 6. C# `record`（如 `TodoDto`）

- `record` 是語法糖，自動產生：
  1. Value-based equality（比較每個欄位值，而非參照）
  2. `ToString()` 自動印出所有屬性
  3. `with` 運算式可產生修改過部分欄位的複本
- Positional record（`record TodoDto(int Id, ...)`）產生的屬性預設是 `init`-only，行為上接近不可變資料容器。
- `record` 本身可以有方法/邏輯，只是專案裡把它當純 DTO 用（讀取後不需再修改），沒有額外行為——這是慣例而非語言限制。
- 對照：`CreateTodoRequest` / `UpdateTodoRequest` 用一般 `class` + `{ get; set; }`，因為要給 model binding 用，需要可寫入屬性。

## 7. `required`（C# 關鍵字）vs `[Required]`（Data Annotations）

兩者作用層次完全不同，同時使用不是重複而是互補：

| | `required`（C# 11 語言關鍵字） | `[Required]`（Data Annotations） |
|---|---|---|
| 檢查時機 | 編譯期（`new` 物件時）；.NET 7+ 也被 `System.Text.Json` 尊重，JSON 反序列化時檢查 | Model Validation 階段（HTTP request 反序列化**成功之後**） |
| 生效範圍 | 全語言範圍：任何 C# 程式碼建構這個物件都受約束，跟 ASP.NET Core / HTTP 無關 | 只在 ASP.NET Core MVC pipeline（或手動呼叫 Validator）才會被執行，脫離這個 pipeline（例如 unit test 直接 `new`）完全不生效 |
| 能擋住什麼 | 只擋「這個屬性完全沒被賦值 / JSON 完全沒有這個 key」 | 擋「值是 null 或空字串」（`AllowEmptyStrings` 預設 false） |
| 擋不住什麼 | **擋不住 `null` 或 `""`**——只要屬性「有被賦值」（即使值是 null/空字串），`required` 條件就滿足 | 擋不住「完全沒給 key」的情境如果反序列化本身沒有走到（例如更早被 `required` 攔截丟例外） |

### 執行順序（本專案 Title 欄位同時掛兩者時）

```
HTTP Request 進來
   ↓
JSON 反序列化 ← required 在此攔截「key 缺漏」，丟 JsonException，直接 400
   （key 有出現，不管值是 null 還是 "" 都會放行）
   ↓
Model Validation ← [Required] 在此攔截「值為 null / 空字串」，ModelState 無效，回 400
   ↓
Controller Action 開始執行 → Service → SaveChangesAsync()
```

兩者都在「request 進來、還沒碰資料庫」的階段被擋下，只是攔截的失敗情境不同：**完全沒給 key（required）vs 給了但值沒意義（[Required]）**。

### 補充：`required` 不會擋 `Title = null`

```csharp
new CreateTodoRequest { }              // 編譯錯誤：必須設定 Title（required 生效）
new CreateTodoRequest { Title = null } // 可以通過編譯（頂多是警告，不是 error）
```

真正對「賦值 null」有意見的是 **Nullable Reference Types（NRT）**，這是獨立的編譯器功能，預設只產生警告（`CS8625` 之類），除非專案設定把警告升級為 error。

### 為什麼微軟不用 `[Required]` 取代 `required`

`[Required]` 只是個標記，真正執行檢查的是 ASP.NET Core MVC 的 Model Validation pipeline：
- 只在「HTTP request → model binding」這條路才生效，直接在 C# 程式碼 `new` 物件（例如 unit test）完全不會被檢查，且依賴 reflection，執行期才發現問題
- `required` 是編譯器層級的保證，不管在哪個專案類型（Web API、console app、library）都生效，防的是「寫程式碼的人忘記賦值」；`[Required]` 防的是「呼叫 API 的外部使用者沒送資料」

兩者對象不同（開發者 vs 外部輸入），時機不同（編譯期 vs 執行期 HTTP 層），所以同時存在是合理的雙重保險。
