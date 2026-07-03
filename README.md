# Todo List

一個與 [Mini-SSO](https://github.com/yuchiyang123/Mini-SSO) 整合的 Todo List 應用，前後端分離、monorepo 管理，支援一鍵 Docker 部署。

- **後端**：ASP.NET Core 9 Web API + Entity Framework Core（SQLite）
- **前端**：Vue 3 + Vite + TypeScript + Pinia + Vue Router，手刻簡約 CSS，不依賴額外 UI 套件
- **驗證**：不重複實作登入機制，完全信任並串接既有的 Mini-SSO（帳號密碼登入／註冊、Google／GitHub OAuth、HttpOnly Cookie JWT、CSRF 雙重提交、IP 鎖定、登出撤銷）
- **授權**：MIT License

## 架構

```
瀏覽器
  │
  ▼
web（nginx，對外埠 8081）
  ├─ /              → 提供編譯後的 Vue3 靜態檔案
  ├─ /api/auth/*     → 反向代理至 Mini-SSO（host.docker.internal:12080）
  └─ /api/todos/*    → 反向代理至 todo-api（僅限內部網路）

todo-api（ASP.NET Core 9，僅限內部網路）
  ├─ 使用與 Mini-SSO 相同的 Jwt:Key / Issuer / Audience 設定，
  │  直接驗證 Mini-SSO 簽發、存放於 HttpOnly Cookie 的 JWT，
  │  不需每次請求都呼叫 Mini-SSO（標準 JWT SSO 信任模式）
  ├─ CSRF 保護：JWT 認證 Cookie 為 SameSite=Lax（跨站 fetch/XHR 不會帶上），
  │  todo-api 另要求請求需帶有非空的 X-CSRF-TOKEN 標頭作為第二層防護
  │  （Mini-SSO 自身的 CSRF 採用 ASP.NET Core Antiforgery，Cookie 與
  │  Token 是加密配對而非簡單字串比對，todo-api 沒有 Mini-SSO 的
  │  Data Protection 金鑰，無法複製那組驗證，因此改採此輕量防護）
  ├─ SQLite（獨立資料庫，僅儲存 Todo 資料）
  └─ /healthz 健康檢查
```

因為前端只呼叫同源的 `/api/...`，瀏覽器會自動在兩個後端服務間帶上同一份 Cookie（Cookie 只依主機名稱範圍，不分埠號），不需要額外的跨網域設定。

> **Google／GitHub OAuth 例外**：第三方登入是整頁導向，Google/GitHub 會導回一個「事先在後台登記好」的固定網址，因此登入按鈕不透過本專案的 gateway 代理，而是直接連到 Mini-SSO 本身的公開位址（`VITE_MINI_SSO_ORIGIN`，預設 `http://localhost:12080`）。這是必要的：nginx 的 `$host` 變數不含埠號，若透過多層 reverse proxy 轉發，ASP.NET Core 算出來的 `redirect_uri` 會遺失埠號而導致 Google 回報 `redirect_uri_mismatch`。登入完成後 Mini-SSO 設定的 Cookie 一樣能在本專案的 `web`（不同埠號）上使用。

> **已知限制**：todo-api 以簽章／效期驗證 JWT，不會即時查詢 Mini-SSO 的撤銷清單（RevokedToken），因此使用者在 Mini-SSO 登出後、token 到期前，todo-api 仍可能短暫接受該 token（Cookie 已被清除，正常操作流程不會重送舊 token）。

## 前置需求

1. **Mini-SSO 必須已經在別處啟動**，且可以從本機的 `host.docker.internal:12080` 存取（Mini-SSO 專案自己的 `docker compose up -d --build`）。
2. Todo List 的 `.env` 中的 `JWT_ISSUER` / `JWT_AUDIENCE` / `JWT_KEY` 必須與 Mini-SSO 的設定完全一致，todo-api 才能驗證同一顆 JWT。
3. Docker / Docker Compose、（本機開發用）.NET 9 SDK、Node.js 22+。

## 一鍵部署

```bash
cp .env.example .env
# 編輯 .env，填入與 Mini-SSO 相同的 Jwt:Key / Issuer / Audience

docker compose up -d --build
```

啟動後開啟 `http://localhost:8081`（或 `.env` 中設定的 `WEB_PORT`）。todo-api 啟動時會自動套用 EF Core migrations。

## 本機開發

**後端**

```bash
cd backend/TodoList.Api
dotnet user-secrets set "Jwt:Key" "<與 Mini-SSO 相同的 key>"
dotnet user-secrets set "Jwt:Issuer" "<與 Mini-SSO 相同>"
dotnet user-secrets set "Jwt:Audience" "<與 Mini-SSO 相同>"
dotnet run
```

**前端**

```bash
cd frontend
npm install
npm run dev
```

Vite dev server 已設定 proxy：`/api/auth/*` 轉發到 `http://localhost:12080`（Mini-SSO），`/api/todos/*` 轉發到 `http://localhost:5080`（本機執行的 todo-api），詳見 [vite.config.ts](frontend/vite.config.ts)。

## 功能

- 帳號密碼登入／註冊、Google／GitHub 第三方登入（全部由 Mini-SSO 提供）
- 待辦事項 CRUD：新增、編輯、刪除、標記完成／取消完成
- 依狀態（全部／待完成／已完成）與關鍵字篩選
- 優先度（低／中／高）與到期日，逾期以醒目樣式標示
- 每位使用者的待辦事項彼此隔離（依 JWT 中的使用者 ID 區隔）

## 測試

```bash
cd backend
dotnet test
```

涵蓋 Todo CRUD 邏輯與跨使用者存取隔離（一個使用者不能讀取／修改／刪除他人的待辦事項）。

## 專案結構

```
Todo-List/
├── backend/
│   ├── TodoList.Api/            # ASP.NET Core 9 Web API
│   └── TodoList.Api.Tests/      # xUnit 測試
├── frontend/                    # Vue 3 + Vite
├── docker-compose.yml
├── .env.example
└── LICENSE
```

## 授權

本專案採用 [MIT License](LICENSE)。
