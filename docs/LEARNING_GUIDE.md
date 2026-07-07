# Docker / nginx / GitHub Actions 從零開始教學

這份文件用**這個專案裡真實存在的檔案**當教材，一行一行講解 Dockerfile、`docker-compose.yml`、`nginx.conf`、`ci.yml` 到底在做什麼、為什麼要這樣寫。目標是讓你看完之後，可以自己動手修改這些檔案，也能看懂類似專案的設定。

建議照順序看：Docker 概念 → Dockerfile → docker-compose.yml → nginx.conf → GitHub Actions CI。每一段後面都有「你可以自己試試看」的練習。

---

## 目錄

1. [Docker 到底是什麼](#1-docker-到底是什麼)
2. [Dockerfile 逐行教學](#2-dockerfile-逐行教學)
3. [docker-compose.yml 逐行教學](#3-docker-composeyml-逐行教學)
4. [nginx.conf 逐行教學](#4-nginxconf-逐行教學)
5. [GitHub Actions CI（ci.yml）逐行教學](#5-github-actions-ciciyml-逐行教學)
6. [這個專案踩過的坑（除錯心法）](#6-這個專案踩過的坑除錯心法)
7. [下一步練習建議](#7-下一步練習建議)

---

## 1. Docker 到底是什麼

先講三個名詞，之後所有東西都是圍繞這三個轉的：

- **Image（映像檔）**：一份「打包好的軟體快照」，裡面包含你的程式碼、需要的執行環境（例如 .NET、Node.js）、所有相依套件。你可以把它想成「一個做好的便當」。
- **Container（容器）**：把 Image 實際「跑起來」的東西。同一個 Image 可以同時跑出很多個 Container，就像同一份便當食譜可以做出很多個便當。
- **Dockerfile**：一份「食譜」，寫著要怎麼從零開始做出一個 Image（裝什麼軟體、複製哪些檔案、跑哪些指令）。

**為什麼要用它**：如果沒有 Docker，你要在自己電腦、同事電腦、雲端伺服器上，各自手動安裝「一模一樣版本」的 .NET、Node.js、nginx，還要確保路徑、環境變數都一致——非常容易「我電腦上明明可以跑」。Docker 把這些全部打包進 Image，`docker run` 到哪裡都是同一個結果。

`docker-compose.yml` 則是「同時管理好幾個 Container」的設定檔——這個專案有兩個服務（後端 API、前端網站），docker-compose 讓你一個指令 `docker compose up -d --build` 就把兩個都建好、串好、跑起來。

**你可以自己試試看**：
```bash
docker images        # 看電腦上已經有哪些 image
docker ps             # 看現在正在跑的 container
docker ps -a          # 看全部（包含已停止的）container
```

---

## 2. Dockerfile 逐行教學

### 2.1 後端：`backend/TodoList.Api/Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY TodoList.Api/TodoList.Api.csproj TodoList.Api/
RUN dotnet restore "TodoList.Api/TodoList.Api.csproj"
COPY TodoList.Api/. TodoList.Api/
WORKDIR /src/TodoList.Api
RUN dotnet publish "TodoList.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/data
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TodoList.Api.dll"]
```

逐行拆解：

| 行 | 意思 |
|---|---|
| `FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build` | 從微軟官方的「.NET 9 SDK」image 開始蓋。SDK 版包含完整編譯工具，體積大。`AS build` 是幫這個階段取名叫 `build`，之後可以引用它。 |
| `WORKDIR /src` | 之後的指令都在容器內的 `/src` 這個資料夾底下執行（沒有這個資料夾會自動建立）。 |
| `COPY TodoList.Api/TodoList.Api.csproj TodoList.Api/` | 只先複製 `.csproj`（專案設定檔，裡面列著要裝哪些 NuGet 套件），還沒複製全部程式碼。 |
| `RUN dotnet restore ...` | 下載 `.csproj` 裡列出的套件。 |
| `COPY TodoList.Api/. TodoList.Api/` | 這時候才複製「全部」程式碼進去。 |
| `WORKDIR /src/TodoList.Api` | 切換工作資料夾到專案目錄。 |
| `RUN dotnet publish ...` | 編譯並輸出成品到 `/app/publish`。`-c Release` 是用正式發布模式編譯（比開發模式快、體積小）。 |
| `FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final` | **重新從另一個、小很多的 image 開始**——這個叫 "multi-stage build"（多階段建置），下面會解釋為什麼。 |
| `WORKDIR /app` | 同上，切換工作目錄。 |
| `RUN apt-get update && apt-get install -y curl && ...` | 這個小 image 預設沒有 `curl` 指令，但 `docker-compose.yml` 的健康檢查要用到它，所以手動裝一下。`rm -rf /var/lib/apt/lists/*` 是裝完後清掉暫存的套件清單，讓 image 體積小一點。 |
| `COPY --from=build /app/publish .` | **重點**：從剛剛那個叫 `build` 的階段，把編譯好的成品複製過來（不是重新編譯一次）。 |
| `EXPOSE 8080` | 告訴大家這個容器內部監聽 8080 埠（純粹是文件用途的提示，不會真的幫你開埠）。 |
| `ENTRYPOINT ["dotnet", "TodoList.Api.dll"]` | 容器啟動時真正執行的指令，等同你在終端機打 `dotnet TodoList.Api.dll`。 |

**為什麼要分兩個 `FROM`（multi-stage build）？**

因為「編譯程式碼」跟「執行程式」需要的東西差很多：
- 編譯需要完整的 SDK（一大包編譯工具、範本、除錯工具），可能好幾百 MB。
- 執行只需要 Runtime（能跑編譯好的程式就好），小很多。

如果全部塞在同一個 image，你的正式環境會扛著一堆用不到的編譯工具，既肥大又增加被攻擊的面（多裝的東西 = 多一種可能出問題的地方）。用兩個 `FROM`，最後真正拿去部署的 image 只有 `final` 這個階段的內容，`build` 階段用完就丟掉，乾淨很多。

### 2.2 前端：`frontend/Dockerfile`

```dockerfile
FROM node:22-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY --from=build /src/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

跟後端的邏輯完全一樣的套路：

| 行 | 意思 |
|---|---|
| `FROM node:22-alpine AS build` | 用 Node.js 22 來「編譯」前端。`alpine` 是一種極小化的 Linux 發行版，image 體積小很多。 |
| `COPY package.json package-lock.json ./` | 先只複製這兩個檔案（列出要裝哪些 npm 套件）。 |
| `RUN npm ci` | 依照 `package-lock.json` **精確**安裝套件（比 `npm install` 更適合用在 CI/建置環境，版本不會意外跑掉）。 |
| `COPY . .` | 這時候才複製全部原始碼。 |
| `RUN npm run build` | 執行 `package.json` 裡定義的 `build` 指令，把 Vue/TypeScript 原始碼編譯成純 HTML/CSS/JS 靜態檔案，輸出到 `dist/` 資料夾。 |
| `FROM nginx:1.27-alpine AS final` | **重新開始**，這次用 nginx（靜態檔案伺服器兼反向代理）當基底，而不是 Node.js——**跑起來的最終容器裡面不需要 Node.js**，Vue 專案編譯完就只是一堆靜態檔案。 |
| `COPY --from=build /src/dist /usr/share/nginx/html` | 把剛剛編譯出來的靜態檔案複製到 nginx 預設會去讀的資料夾。 |
| `COPY nginx.conf /etc/nginx/conf.d/default.conf` | **重點**：把我們自己寫的 nginx 設定檔複製進去，蓋掉 nginx 預設的設定。 |

**這裡有個很重要、之前讓你踩坑的觀念**：`nginx.conf` 是用 `COPY` **烤進 image 裡面**的，不是掛載（mount）進去的。這代表：改了 `nginx.conf` 之後，單純 `docker restart` 或 `docker compose restart` 是沒用的——因為容器裡面用的還是「建置當下」複製進去的那份舊檔案。**一定要重新 build image**（`docker compose up -d --build`），新內容才會真的被打包進去。

反過來說，Mini-SSO 那邊的 `nginx.conf` 是用 `volumes` 掛進去的（bind mount），所以改完直接 `nginx -s reload` 就生效，不用重建——這是兩種完全不同的做法，要看清楚專案是用哪一種。

**你可以自己試試看**：
```bash
# 只建置某一個 service 的 image，不影響其他的
docker compose build web
docker compose build todo-api

# 建置 + 用新 image 重新建立容器（這才是修改 Dockerfile/nginx.conf 後正確的做法）
docker compose up -d --build web
```

---

## 3. docker-compose.yml 逐行教學

```yaml
services:
  todo-api:
    build:
      context: ./backend
      dockerfile: TodoList.Api/Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: "Data Source=/app/data/todolist.db"
      Jwt__Issuer: ${JWT_ISSUER}
      Jwt__Audience: ${JWT_AUDIENCE}
      Jwt__Key: ${JWT_KEY}
    volumes:
      - todo-data:/app/data
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/healthz"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  web:
    build:
      context: ./frontend
    ports:
      - "${WEB_PORT:-12090}:80"
    extra_hosts:
      - "host.docker.internal:host-gateway"
    depends_on:
      todo-api:
        condition: service_healthy
    restart: unless-stopped

volumes:
  todo-data:
```

逐段拆解：

- **`services:`**：底下列出這個 compose 檔案要管理哪幾個容器。這裡有兩個：`todo-api`（後端）跟 `web`（前端 + nginx）。名字是你自己取的，之後 `docker compose logs todo-api`、`docker exec -it todo-api-1 ...` 都是用這個名字。

- **`build:`**：
  - `context: ./backend`：去哪個資料夾找程式碼跟 Dockerfile。這個資料夾會整個被送進 Docker 的「建置上下文」——`Dockerfile` 裡的 `COPY` 都是相對這裡算的。
  - `dockerfile: TodoList.Api/Dockerfile`：如果 Dockerfile 不叫預設的 `Dockerfile` 或不在 context 的最上層，要額外指名路徑。

- **`environment:`**：容器啟動時會拿到的環境變數。`Jwt__Issuer: ${JWT_ISSUER}` 這種寫法是：容器內部會看到一個叫 `Jwt__Issuer` 的環境變數，它的值來自 `${JWT_ISSUER}`——也就是你 `.env` 檔案裡寫的 `JWT_ISSUER=xxx`。**雙底線 `__` 是 .NET 的慣例**：.NET 設定系統會自動把 `Jwt__Issuer` 這種環境變數，對應到 `appsettings.json` 裡的 `"Jwt": { "Issuer": "..." }` 巢狀結構。

- **`volumes:`**（在 service 底下）：`todo-data:/app/data` 的意思是「把叫做 `todo-data` 的資料卷，掛到容器內的 `/app/data` 路徑」。這樣即使容器被刪掉重建，SQLite 資料庫檔案還在，不會憑空消失。

- **`healthcheck:`**：定期（每 `interval` 秒）跑一次 `test` 裡的指令，判斷容器「健康」還是「不健康」。這裡是打 `/healthz` 這個網址，如果失敗會重試 `retries` 次，每次等 `timeout` 秒才算逾時。**這個健康狀態很重要**——底下 `web` 服務的 `depends_on` 會等它變成 healthy 才啟動。

- **`restart: unless-stopped`**：容器如果自己當掉，Docker 會自動幫你重開，除非你自己手動停掉它。

- **`web` 服務**：
  - `ports: - "${WEB_PORT:-12090}:80"`：把「容器內的 80 埠」對應到「主機的 `WEB_PORT` 這個埠」。`${WEB_PORT:-12090}` 意思是「如果 `.env` 沒設 `WEB_PORT`，就用預設值 `12090`」。
  - `extra_hosts: - "host.docker.internal:host-gateway"`：在容器裡面新增一筆「host 名稱對應」，讓容器可以透過 `host.docker.internal` 這個名字連回「執行 Docker 的那台主機」（在 Linux 上預設沒有這個功能，要靠這行手動加上去；Windows/Mac 的 Docker Desktop 通常內建就有）。這就是為什麼我們的 `nginx.conf` 能用 `host.docker.internal:12080` 連到跑在同一台機器、但不在同一個 docker-compose 專案裡的 Mini-SSO。
  - `depends_on: todo-api: condition: service_healthy`：`web` 要等 `todo-api` 通過健康檢查之後才啟動（不是「跑起來就好」，是「真的健康」才算）。

- **最下面的 `volumes:`**：宣告 `todo-data` 這個資料卷存在（跟上面 service 裡引用的要對應得起來）。

**你可以自己試試看**：
```bash
docker compose ps                 # 看目前這個 compose 專案有哪些容器、狀態如何
docker compose logs todo-api      # 看 todo-api 的 log
docker compose logs -f web        # -f 是持續盯著看（follow），Ctrl+C 離開
docker compose down               # 停掉並移除所有容器（volumes 預設不會被刪）
docker compose down -v            # 連 volumes 一起刪掉（資料庫資料會消失，小心用）
```

---

## 4. nginx.conf 逐行教學

先講觀念：nginx 在這個專案裡扮演兩個角色：
1. **靜態網站伺服器**：把 Vue 編譯出來的 HTML/CSS/JS 檔案，回應給瀏覽器。
2. **反向代理（Reverse Proxy）**：瀏覽器其實只跟 nginx 講話，nginx 再根據網址規則，把請求偷偷轉發給後面真正處理的服務（Mini-SSO、我們自己的 todo-api），瀏覽器完全不知道背後其實是兩個不同的服務在處理。

好處：瀏覽器全程只看到「同一個網址」（例如 `todo.matthewyu.uk`），不用處理跨網域（CORS）問題，Cookie 也能正常共用。

### 4.1 `map` 區塊

```nginx
map $http_x_forwarded_proto $effective_scheme {
    default $http_x_forwarded_proto;
    ''      $scheme;
}
```

`map` 是 nginx 的「條件對照表」，語法是 `map <輸入變數> <輸出變數> { 對照規則 }`。

這裡的意思：看 `$http_x_forwarded_proto`（也就是請求裡 `X-Forwarded-Proto` 這個標頭的值）：
- 如果有值（`default` 這條），`$effective_scheme` 就等於那個值。
- 如果是空字串 `''`（代表沒有這個標頭，例如你直接本機測試，沒有 Cloudflare 之類的東西在前面轉發），就退回用 `$scheme`（這個連線本身實際使用的協定，`http` 或 `https`）。

**為什麼需要這個？** 因為 Cloudflare Tunnel 之類的服務，會在「使用者瀏覽器」到「你的伺服器」之間先攔一手：使用者用 `https` 連 Cloudflare，但 Cloudflare 轉給你的伺服器時，很多設定其實是用 plain `http`（因為都在你自己的內網/本機，沒有加密的必要）。如果你直接用 `$scheme`，nginx 會誤以為整個連線都是 `http`，進而影響後端判斷「這個 Cookie 該不該加 `Secure` 屬性」「OAuth 的 callback 網址該用 `http://` 還是 `https://`」等等——這正是這次除錯過程中卡最久的一個坑。

### 4.2 `server` 區塊與 `location`

```nginx
server {
    listen 80;
    server_name _;

    root /usr/share/nginx/html;
    index index.html;
```

- `listen 80`：這個 server 監聽（容器內部的）80 埠。
- `server_name _`：底線是萬用字元，代表「不管請求的 Host 是什麼，都用這個 server 區塊處理」（因為我們只有一個網站，不需要依 Host 分流）。
- `root` / `index`：預設去哪個資料夾找檔案、找不到明確檔名時預設回應 `index.html`。

**`location` 是整份設定檔的核心**——依照「網址路徑」決定要怎麼處理這個請求。有三種寫法，優先順序不同：

| 寫法 | 範例 | 意思 | 比對規則 |
|---|---|---|---|
| `location = /path` | `location = /api/auth` | **精確比對**，網址必須完全等於 `/api/auth`，一個字都不能多 | 優先度最高 |
| `location /path/` | `location /api/auth/` | **前綴比對**，只要網址「開頭」是 `/api/auth/` 就算數，區分大小寫 | 中間，取「比對到最長」的那個 |
| `location ~ /regex/`<br>`location ~* /regex/` | `location ~ ^/api/Auth/` | **正規表示式比對**，`~` 區分大小寫、`~*` 不區分大小寫 | 只要「有任何一個」regex 比對成功，**永遠贏過前綴比對**（除非前綴用了 `= ` 或 `^~`） |

這解釋了這次遇到的 bug：Mini-SSO 內部有時候會產生路徑開頭是大寫 `/api/Auth/...` 的網址，但我們原本只寫了小寫的 `location /api/auth/`（前綴比對，**區分大小寫**），大寫的請求完全不會進到這個規則，就掉到最底下的「萬用」規則去了。修法是額外加一個 `location ~ ^/api/Auth/`（正規表示式，明確比對大寫開頭），這樣不管大小寫都有對應的規則接住。

### 4.3 一個典型的 `location` 區塊

```nginx
location /api/auth/ {
    proxy_pass http://host.docker.internal:12080/api/auth/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $effective_scheme;
}
```

- `proxy_pass http://host.docker.internal:12080/api/auth/;`：**把這個請求轉發到別的地方**。這一行是整個反向代理的靈魂。`host.docker.internal:12080` 是目的地（Mini-SSO），後面接的 `/api/auth/` 決定路徑怎麼重寫（詳見下面的重要規則）。

- `proxy_set_header X 標頭名稱 值;`：轉發出去的請求，會把原本某些資訊用「標頭」的形式夾帶給後面的服務，因為後面的服務直接看到的連線來源就是「這個 nginx」，看不到真正的使用者資訊，要靠這些標頭補回來：
  - `Host $host`：告訴後端「使用者原本打的網址主機名稱是什麼」（例如 `todo.matthewyu.uk`）。
  - `X-Real-IP $remote_addr`：使用者真正的 IP。
  - `X-Forwarded-For $proxy_add_x_forwarded_for`：使用者 IP 的「歷史紀錄」（如果中間經過好幾層 proxy，會一直往後接）。
  - `X-Forwarded-Proto`：使用者原本是用 `http` 還是 `https` 連過來的（用我們前面 `map` 算出來的 `$effective_scheme`，而不是直接用 `$scheme`）。

**`proxy_pass` 路徑重寫的規則，非常重要、也很容易搞混**：

- 如果 `location` 是「前綴比對」，`proxy_pass` 後面**有寫路徑**（像上面這個 `/api/auth/`）：nginx 會把請求路徑裡「符合 location 前綴的那一段」，換成 `proxy_pass` 寫的路徑，其餘照抄。這裡兩者剛好一樣，等於沒有真的改變路徑。
- 如果 `proxy_pass` 後面**完全沒寫路徑**（像我們給大寫 `/api/Auth/` 那個 regex location 用的 `proxy_pass http://host.docker.internal:12080;`）：nginx 會把「使用者原本打的完整路徑」**原封不動**轉發過去，不做任何改寫。這是搭配 regex location 時常用的寫法（因為 regex 沒辦法像前綴那樣做簡單替換）。

- `location = /api/auth { ... }`：為什麼要額外寫一個一模一樣、只是精確比對的？因為登入用的 `POST /api/auth` 這個網址，結尾**沒有斜線**，前綴規則 `location /api/auth/`（結尾**有**斜線）比對不到它（`/api/auth` 不是以 `/api/auth/` 開頭，兩者長度都不一樣，短的不可能是長的前綴）。所以另外開一個「精確等於 `/api/auth`」的規則來接住它。

### 4.4 SPA fallback

```nginx
location / {
    try_files $uri $uri/ /index.html;
}
```

這是給 Vue Router 這種「前端路由」用的固定寫法。`try_files` 依序嘗試：
1. `$uri`：有沒有一個檔案的路徑剛好等於這個網址（例如 `/favicon.svg`）？有的話直接回傳這個檔案。
2. `$uri/`：有沒有這個名字的「資料夾」？
3. 上面兩個都沒有的話，一律回傳 `/index.html`。

為什麼要這樣：Vue Router 的網址（像 `/todos`、`/login`）**在硬碟上根本沒有對應的檔案**，那些路由完全是瀏覽器裡的 JavaScript 自己決定要顯示什麼畫面。所以不管使用者打的網址是什麼（只要不是真的靜態檔案），都先把 `index.html` 丟回去，讓裡面的 JavaScript（Vue Router）自己接手判斷要顯示哪個畫面。

**這裡也是這次踩到的另一個坑的根源**：如果某個網址（例如 Google OAuth 導回來的 `/sso/callback`）沒有被前面任何 `location` 規則接住，就會掉到這裡，被當成「未知的前端路由」處理，回傳 `index.html`——如果 Vue Router 裡也沒有定義這個路由，畫面就會整個空白，卡在那邊什麼都不會發生。所以**每個外部服務（Mini-SSO）可能導回來的網址，都要在 nginx 裡有對應的規則，或者在前端路由裡有對應的頁面**，不然都會落到這個 fallback，行為通常是「看起來卡住、空白」。

**你可以自己試試看**：
```bash
# 進到 nginx 容器裡面看看目前實際生效的設定檔（確認你的修改真的有被打包進去）
docker exec -it todo-list-web-1 cat /etc/nginx/conf.d/default.conf

# 語法檢查（改完設定檔想確認有沒有打錯字時很好用）
docker exec todo-list-web-1 nginx -t
```

---

## 5. GitHub Actions CI（ci.yml）逐行教學

CI（持續整合）的目的：每次你 push 程式碼或開 PR，自動幫你「編譯一次、跑一次測試」，確保你沒有不小心弄壞東西，而不是要等到部署才發現。

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
```

- `name`：這個工作流程在 GitHub 網頁上顯示的名字。
- `on:`：**觸發時機**。這裡設定：有人 `push` 到 `main` 分支，或是開了一個目標分支是 `main` 的 pull request，就自動執行。

```yaml
jobs:
  backend:
    name: Backend Build & Test
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 9.0.x

      - name: Restore dependencies
        working-directory: backend
        run: dotnet restore TodoList.sln

      - name: Build
        working-directory: backend
        run: dotnet build TodoList.sln --configuration Release --no-restore

      - name: Test
        working-directory: backend
        run: dotnet test TodoList.sln --configuration Release --no-build
```

- `jobs:`：底下列出「有哪幾組獨立的工作」。這份設定有三組：`backend`、`frontend`、`docker`——**預設會平行跑**（除非用 `needs` 指定先後順序，下面會講）。
- `backend:`（工作的代號，自己取名）
  - `runs-on: ubuntu-latest`：這個工作要在「GitHub 提供的、乾淨的 Ubuntu 虛擬機器」上執行（每次都是全新的環境，跑完就丟掉）。
  - `steps:`：這組工作裡，依序要做哪些事：
    - `uses: actions/checkout@v4`：**這一步幾乎每個 workflow 都會有**，作用是把你的 repo 程式碼下載到這台乾淨的虛擬機器裡（不然它預設什麼都沒有，連你的程式碼都看不到）。
    - `uses: actions/setup-dotnet@v4` + `with: dotnet-version: 9.0.x`：`uses` 是「使用別人（或官方）寫好的現成腳本」，這個是官方提供的、負責安裝指定版本 .NET SDK 的腳本。`with:` 底下是要傳給這個腳本的參數。
    - `run: dotnet restore ...`：`run` 就是直接執行一段終端機指令，跟你在自己電腦上打指令一樣。`working-directory: backend` 是指定這個指令要在 `backend` 這個資料夾底下執行（不然預設是 repo 最上層）。
    - 依序 `restore`（下載套件）→ `build`（編譯，`--no-restore` 是說「不用再重新下載套件了，剛剛才下載過」）→ `test`（跑測試，`--no-build` 同理，不用再重新編譯）。

```yaml
  frontend:
    name: Frontend Build
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: 22
          cache: npm
          cache-dependency-path: frontend/package-lock.json

      - name: Install dependencies
        working-directory: frontend
        run: npm ci

      - name: Type-check & build
        working-directory: frontend
        run: npm run build
```

跟 `backend` 這組邏輯一樣，只是換成 Node.js 版本。多了 `cache: npm` 跟 `cache-dependency-path`：告訴 GitHub Actions「幫我把 npm 下載過的套件快取起來」，根據 `package-lock.json` 有沒有變化來判斷快取還能不能用——下次執行如果套件版本沒變，就不用重新下載一次，CI 跑起來快很多。

```yaml
  docker:
    name: Docker Build
    runs-on: ubuntu-latest
    needs: [backend, frontend]

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Build todo-api image
        run: docker build -f backend/TodoList.Api/Dockerfile -t todo-list-api:ci backend

      - name: Build web image
        run: docker build -t todo-list-web:ci frontend
```

- `needs: [backend, frontend]`：**這組工作要等 `backend` 跟 `frontend` 兩組都成功之後才會開始跑**。如果前面任何一組失敗，這組直接不會執行——這樣可以避免「程式碼本身有問題，卻還浪費時間去建置 Docker image」。
- `docker build -f <Dockerfile路徑> -t <幫這個image取的名字> <build context資料夾>`：跟你在自己電腦下 `docker build` 指令完全一樣，這裡只是驗證「Dockerfile 真的能成功建置出 image」，並沒有把 image 推到任何地方（沒有 push 到 Docker Hub 之類的動作）。

**你可以自己試試看**：
- 在 GitHub 上，push 一個小改動到你的分支，然後到 repo 的「Actions」分頁看它實際跑起來的畫面，每個 step 花多久、log 長怎樣。
- 故意把某段程式碼改到編譯不過，push 上去，看 CI 是不是真的會標紅、擋下來。

---

## 6. 這個專案踩過的坑（除錯心法）

這些是這次實際除錯過程中發生過的真實案例，整理成心法：

1. **改設定檔沒生效，先問自己：這個檔案是「COPY 進 image」還是「volume 掛進去」的？**
   - `COPY` 進去的（例如我們自己的 `frontend/nginx.conf`）：改完要 `docker compose up -d --build <service>` 重新建置。
   - `volumes` 掛進去的（例如 Mini-SSO 的 `nginx.conf`）：改完主機上的檔案，容器內看到的內容就跟著變了，通常只需要讓程式重新讀取設定（例如 `nginx -s reload`），不用重建 image。
   - 判斷方法：`docker inspect <container名稱> --format '{{json .Mounts}}'`，看有沒有列出這個檔案的掛載紀錄。

2. **nginx 的 `location` 是區分大小寫的（除非你用 `~*`）。** 如果背後串接的服務有時候會產生大小寫不一致的網址（像這次 Mini-SSO 的 `/api/Auth/...`），單靠小寫的前綴規則會漏接，掉到預設的 fallback 規則去，通常表現就是「畫面空白、卡住」。

3. **`nginx` 的 `$host` 變數不含埠號；`$http_host` 才是完整保留使用者原本打的網址（含埠號）。** 如果你的服務需要知道「使用者原本連的完整網址」（例如拿去產生 OAuth 的 callback 網址），要注意這個地雷。

4. **多層 nginx／反向代理疊在一起時，`X-Forwarded-Proto` 很容易在某一層被錯誤地蓋掉。** 每一層 nginx 如果都直接寫死 `proxy_set_header X-Forwarded-Proto $scheme;`，那一層看到的「自己跟上一層的連線」通常是 `http`（因為都在內網），會蓋掉最源頭瀏覽器實際用的 `https`。要用 `map` 加一層判斷：有上游轉來的值就沿用，沒有才退回自己這一層的 `$scheme`。

5. **OAuth（Google／GitHub）登入的「重新導向網址」必須跟你在 Google/GitHub 後台登記的網址逐字元一模一樣**（包含 `http` 還是 `https`、網域、路徑，一個字元都不能差）。這個網址是由你的後端服務算出來的，通常會受「它以為自己現在被瀏覽器用什麼協定/網域存取」影響——這正是第 3、4 點會影響到的地方。

6. **Google Cloud Console 這類第三方後台的設定變更，有時候不是「馬上生效」**，遇到明明設定看起來對、卻還是報錯的情況，先確認「有沒有真的按下儲存」，再考慮「是不是還在生效中，等個一兩分鐘再試」。

7. **想確認自己的改動有沒有真的生效，永遠優先相信「直接測試的結果」，而不是憑印象猜。** 這次很多次都是靠 `curl -D -` 直接看 HTTP 回應的標頭（尤其是 `Location`、`Set-Cookie`），才確認到底問題出在哪一層，比對著猜快很多也準很多。

8. **重開機後 502，不代表容器沒開，也不代表程式或設定有問題——先分層排查，不要一開始就懷疑程式碼。**

   這次的真實案例：Windows 重開機後，前端打 `/api/auth/*` 全部 502。表面看起來很像「後端服務掛了」或「IIS 資料夾權限跑掉」，但實際上：
   - `docker ps` 顯示 Mini-SSO 那三個容器（`nginx`、`api`、`db`）都是 `Up`/`healthy`。
   - 進到 `mini-sso-nginx` 容器內部，它能正常連到自己的上游 `api:8080/healthz`，代表**應用層完全正常**。
   - 問題出在再外面一層：Docker Desktop 幫容器做的「對外埠轉發」（`docker-compose.yml` 裡的 `ports: "12080:80"` 這種東西）。Windows 上這一層是靠 WSL2 裡的一個網路代理實作的，重開機／喚醒後偶爾會沒有正確重建，導致「容器裡面一切正常，但外面完全連不進去」，症狀是 `curl` 直接卡住、空回應、或連線被拒絕。

   **排查心法**：遇到「明明容器是 healthy，卻連不到」的情況，要分清楚三層，一層一層測，不要整包一起猜：
   1. **容器內部邏輯對不對**：`docker exec <容器> curl <它自己的上游>`，確認服務本身沒問題。
   2. **容器對外的埠轉發通不通**：直接在主機上 `curl http://localhost:<對外的埠>`，跳過所有應用層邏輯，只測「這條路通不通」。
   3. **中間的反向代理設定對不對**：如果上面兩層都通，才回頭檢查 nginx 的 `proxy_pass`、`location` 這些設定檔。

   這次卡在第 2 層，所以修法也很單純：`docker restart <容器名稱>`，讓 Docker 重新建立這個容器的埠轉發，不需要動任何程式碼或設定檔。

   **有沒有「一勞永逸」的解法？** 沒有能保證 100% 不再發生的方法（這是 Docker Desktop for Windows 底層 WSL2 網路實作的已知限制，不是這個專案的程式或設定問題），但有幾個能大幅降低發生機率、或讓你更快恢復的做法，依投入成本排序：
   - **治標（最快）**：每次遇到就 `docker restart <出問題的容器>`，通常幾秒鐘就好。可以寫成一個小腳本放桌面，下次不用重新翻 log 找原因。
   - **降低發生機率**：Docker Desktop 設定裡（Settings → Resources → Network）如果有「Networking mode: Mirrored」這個選項可以開啟——這是 Docker 官方針對「睡眠／重開機後 port 轉發、`localhost` 失效」這類已知問題推出的新網路模式，比預設的 NAT 模式穩定很多。
   - **從架構上根本減少影響範圍**：目前 Mini-SSO 是完全獨立的 docker-compose 專案，`todo-list` 這邊的 nginx 只能「盲打」`host.docker.internal:12080`，打不通就直接 502 給使用者看，沒有重試、沒有友善的錯誤畫面。如果之後想讓體驗更好，可以考慮：
     - 在前端 nginx 幫 `/api/auth/` 這條路加 `proxy_next_upstream` + 短暫重試，或至少在拿到 502 時前端顯示「登入服務暫時無法使用，請稍後再試」而不是整頁空白／噴錯誤訊息給使用者看。
     - 長期如果 Mini-SSO 一直是這個專案的固定依賴，可以考慮把它也收進同一個 `docker-compose.yml`（或用 Docker Compose 的多檔案 `-f` 合併），這樣就能用 `depends_on: condition: service_healthy` 讓 `web` 容器等 Mini-SSO 真的 healthy 之後才啟動，至少排除「開機順序」這個變因（但沒辦法排除 WSL2 網路本身重開機後失效的問題）。

---

## 7. 下一步練習建議

由簡單到困難，建議你可以照這個順序自己動手試：

1. **改一下前端的文字或顏色**（`frontend/src/`），跑 `docker compose up -d --build web`，重新整理網頁確認有變化。
2. **在 `docker-compose.yml` 加一個新的環境變數**給 `todo-api`，然後在後端程式碼裡讀取它（`builder.Configuration["你的變數名"]`），印出來看看有沒有讀到。
3. **在 `nginx.conf` 裡新增一個 `location`**，故意寫一個會回傳固定文字的測試路徑（像我們除錯時暫時加的 `/debug-headers`），驗證你真的看懂 `location` 怎麼比對、`proxy_pass` 怎麼轉發，測完記得刪掉。
4. **修改 `ci.yml`**，加一個新的 step（例如印出目前的資料夾結構 `run: ls -la`），push 上去在 GitHub Actions 分頁看它真的執行了。
5. **試著自己重現一次这次的踩坑經驗**：把 `nginx.conf` 裡某個 `proxy_set_header X-Forwarded-Proto` 改回 `$scheme`，觀察行為有什麼變化，加深印象。

如果之後想再深入，官方文件都寫得不錯，可以直接查：
- Docker：https://docs.docker.com/
- nginx：https://nginx.org/en/docs/
- GitHub Actions：https://docs.github.com/actions
