# BioHub

**生物資訊與臨床研究管理平台 (Bioinformatics & Clinical Research Management Platform)**

BioHub 是一個結合**研究資料管理**與 **RNA-seq 差異基因表現分析**的全端網站作品。專案以 ASP.NET Core MVC、Vue 3、SQL Server、.NET Worker Service、R / DESeq2 和 Docker 建構，將資料上傳、工作排程、分析執行與結果展示整合為可操作的研究工作流程。

> **專案定位：** 以生物資訊分析與研究資料管理為核心，展示跨領域開發能力；實驗室服務販售或商務相關功能於後續進行擴充。

## 功能總覽

| 模組               | 主要功能                                                                                                      | 目前狀態     |
| ------------------ | ------------------------------------------------------------------------------------------------------------- | ------------ |
| 公開網站與單一入口 | Vue 3 製作首頁、平台介紹與功能展示；Nginx Gateway 統一入口，透過「登入平台」導向同一網域的 MVC 登入頁         | 已於本機驗證 |
| 使用者系統         | 註冊、登入、Cookie Authentication、登入狀態管理                                                               | 已於本機驗證 |
| 研究專案管理       | ResearchProjects 建立、查詢、修改、刪除（CRUD）                                                               | 已於本機驗證 |
| RNA-seq 分析工作   | 上傳 Counts Matrix 與 Sample Metadata CSV、建立分析工作、佇列處理                                             | 已於本機驗證 |
| 差異表現分析       | Linux AnalysisWorker 直接呼叫 Rscript / DESeq2，支援 Human、Mouse、Rat 的註解套件                             | 已於本機驗證 |
| 分析結果           | DEG 結果 CSV、DEG Explorer、Volcano Plot                                                                      | 已於本機驗證 |
| 容器化服務         | Nginx Gateway、MVC、Vue/Nginx、AnalysisWorker 由 Docker Compose 啟動；SQL Server 目前使用既有獨立 Docker 容器 | 已於本機驗證 |
| 臨床及檢體完整模組 | 完整臨床研究資料與檢體管理工作流程                                                                            | 持續開發中   |

## 使用技術

| 類別     | 技術                                                               |
| -------- | ------------------------------------------------------------------ |
| 公開網站 | Vue 3、Vite、JavaScript、HTML、CSS、Bootstrap 5                    |
| 後端網站 | C#、ASP.NET Core MVC、.NET 10                                      |
| 資料存取 | SQL Server 2022、Entity Framework Core                             |
| 身分驗證 | ASP.NET Core Cookie Authentication（未採用 ASP.NET Core Identity） |
| 背景工作 | .NET Worker Service、資料庫工作佇列                                |
| 生物資訊 | R、DESeq2、AnnotationDbi、org.Hs.eg.db、org.Mm.eg.db、org.Rn.eg.db |
| 部署     | Docker、Docker Compose、Nginx Gateway（Reverse Proxy）、Nginx      |

## 系統架構

BioHub 採用 **Nginx Gateway 單一入口**。使用者在本機僅需開啟 `http://localhost:8090`，即可由 Vue 公開首頁進入 MVC 登入及研究管理平台，無須自行切換不同連接埠。

```text
瀏覽器：http://localhost:8090
              │
              ▼
     Nginx Gateway（反向代理）
              │
       ┌──────┴──────────────┐
       ▼                     ▼
  /public/               /Account/Login 等 MVC 路由
  Vue 3 + Nginx           ASP.NET Core MVC
  首頁／功能介紹            登入／Dashboard／研究管理
                             │
                ┌────────────┴─────────────┐
                ▼                          ▼
             SQL Server                AnalysisJob：Queued
         使用者／專案／結果                   │
                                           ▼
                                   .NET AnalysisWorker
                                           │
                                           ▼
                                      R / DESeq2
                                           │
                                           ▼
                               DEG_results.csv、VolcanoPlot.png
                                           │
                                           ▼
                               MVC：DEG Explorer／結果下載
```

**入口路由：**

| 路徑               | 實際功能                                        |
| ------------------ | ----------------------------------------------- |
| `/`                | 重新導向 `/public/`（Vue 公開首頁）             |
| `/public/`         | Vue 首頁                                        |
| `/public/features` | 平台功能介紹                                    |
| `/public/about`    | 關於 BioHub                                     |
| `/Account/Login`   | ASP.NET Core MVC 登入                           |
| MVC 其他管理路由   | Dashboard、ResearchProjects、AnalysisJob 等功能 |

Vue 的 Vite `base` 設為 `/public/`，Vue Router 使用 `createWebHistory(import.meta.env.BASE_URL)`；公開首頁的登入連結採用相對路徑 `/Account/Login`，由 Gateway 轉送至 MVC，避免寫死 `8080` 連接埠。

### 資料持久化

本機環境目前使用三個 Docker named volumes：

- `biohub_sql_data`：SQL Server 資料庫檔案。
- `biohub_analysis_data`：上傳 CSV 與 R 分析輸出，MVC 與 Worker 共用。
- `biohub_dpkeys`：ASP.NET Core Data Protection Keys，維持 Cookie 加解密所需金鑰。

以上 volumes **只存在執行 Docker 的主機**，不會隨 GitHub clone 自動產生既有資料。

## 已驗證的本機展示

**主要入口：<http://localhost:8090>**（在執行 Docker 的電腦上開啟）。

| 網址／服務                            | 用途                                                                 |
| ------------------------------------- | -------------------------------------------------------------------- |
| `http://localhost:8090`               | **唯一建議入口**，自動轉至 Vue 公開首頁                              |
| `http://localhost:8090/public/`       | Vue 首頁（經 Gateway）                                               |
| `http://localhost:8090/Account/Login` | MVC 登入（經 Gateway）                                               |
| `http://localhost:8080`               | MVC 原有直連埠，僅供本機維護／排錯                                   |
| `http://localhost:8081`               | Vue 原有直連埠，僅供本機維護／排錯；其資源現已配置為 `/public/` 前綴 |
| AnalysisWorker                        | Docker Compose 背景服務，無對外網頁                                  |
| SQL Server                            | 既有獨立容器 `biohub-sql`，目前使用本機 `127.0.0.1:14333`            |

**本機驗證結果：** 已確認 Nginx Gateway 可提供 Vue 公開首頁與 MVC 統一登入入口，並驗證 MVC 研究管理功能及 Linux Worker 執行 R / DESeq2 的分析流程。Gateway 的新入口不代表已完成公共網域或 HTTPS 部署。

## Docker Compose 操作（目前已驗證的開發電腦）

作者已在 Docker Desktop 上驗證 **Gateway、MVC、Vue 和 AnalysisWorker** 的 Compose 運作。SQL Server 仍由既有獨立容器提供服務，Compose 使用本機已建立的 Images 和 named volumes；

前置條件：

- Docker Desktop 已啟動，`biohub-sql` 可正常連線，三個 named volumes 已存在。
- 根目錄 `.env` 已依 `.env.example` 設定；請勿將含有密碼的 `.env` 提交 GitHub。
- 本機已有對應的 MVC、Vue 與 Worker Images；Gateway 使用 `nginx:alpine`。
- `compose.yaml` 已包含 `gateway` 服務與 `./gateway/default.conf` 掛載，並以 `/public/` 轉發 Vue、其他路徑轉發 MVC。

在專案根目錄檢查設定：

```powershell
docker compose --profile website config --quiet
```

確認既有 SQL Server 已運行後，啟動應用程式及 Gateway（不額外啟動 `sql` profile）：

```powershell
docker compose --profile website up -d analysis-worker mvc vue gateway
```

若只需要更新 Gateway 設定，先確認 `gateway/default.conf` 正確，再檢查與重新載入：

```powershell
docker exec biohub-gateway-1 nginx -t
docker exec biohub-gateway-1 nginx -s reload
```

檢查服務狀態與記錄：

```powershell
docker compose --profile website ps
docker compose --profile website logs --tail 50 gateway mvc vue analysis-worker
```

測試統一入口：

```powershell
curl.exe -I http://localhost:8090/
curl.exe -I http://localhost:8090/public/
curl.exe -I http://localhost:8090/Account/Login
```

第一項預期是導向 `/public/` 的 302，後兩項應能正常回應；實際登入應在瀏覽器中測試，並確認 Dashboard 與分析結果可開啟。**若 Nginx 將首頁導向 `http://localhost/public/` 丟失埠號**，需確認 Gateway 的 `absolute_redirect off;` 設定。

停止 Compose 應用程式請使用：

```powershell
docker compose --profile website stop
```

請勿執行 `docker compose down -v` 或刪除既有 volumes，以免遺失測試資料。

### 在其他電腦執行前，仍需完成

1. 安裝 Docker Desktop（Linux containers 模式），取得專案原始碼。
2. 由 `.env.example` 建立 `.env`，填入自行設定的密碼、UID/GID 及環境參數；**勿提交 `.env`**。
3. 確認 MVC、Vue、Worker 的 Dockerfile 均可由原始碼建立，尤其 Worker 所需 R / DESeq2 基礎映像可重現建置或取得。
4. 建立及初始化 SQL Server 資料庫，套用 EF Core migrations；準備匿名化的示範帳號、CSV 和分析資料。
5. 調整 `compose.yaml` 中仍依賴作者主機的設定（例如既有容器、Images、volumes 或環境變數），再進行完整測試。

**目前尚未驗證「另一台全新電腦 clone 後執行 `docker compose up -d --build` 即可運作」。** 在完成以上準備前，請以原始碼與本機成果說明為主。

## RNA-seq 分析流程

1. 使用者登入 MVC 平台並建立 AnalysisJob。
2. 上傳 Counts Matrix CSV 及 Sample Metadata CSV，設定對照組、實驗組與物種。
3. 系統將工作設為 `Queued`；Worker 認領後更新成 `Running`。
4. Worker 在 Linux 容器直接執行 `/app/RScripts/run_Deseq2.R`。
5. DESeq2 分析產生 `DEG_results.csv` 與 `VolcanoPlot.png`，寫入共用分析資料目錄。
6. 成功時儲存分析結果並更新為 `Completed`；失敗時記錄 `Failed`。
7. 使用者在 MVC 查看 DEG Explorer、火山圖及結果檔案。

## 專案設計重點

- **資料分層：** MVC / EF Core 管理應用程式資料，R 專責 RNA-seq 分析。
- **非同步工作處理：** 運算較重的分析由獨立 Worker 執行，避免使用者請求一直等待。
- **共用持久化資料：** 使用 Docker named volume 交換上傳檔案與分析輸出。
- **統一入口：** Nginx Gateway 以單一網址整合 Vue 公開網站與 MVC 管理系統；內部服務仍維持獨立部署。
- **實際限制：** 目前為作品展示及開發驗證版本；正式公開部署還需要進一步處理 HTTPS、權限控管、機密管理、資料備份、工作中斷恢復與監控。

## 專案安全與資料聲明

- 測試資料應為自製、公開授權或充分去識別化的資料；若使用外部資料集，請標明來源與授權。

## 後續規劃

- 補齊臨床資料與檢體管理工作流程及權限管理。
- 完善可在新主機重現的 Docker Compose 建置、資料庫初始化與示範資料。
- 改善工作逾時、重試、恢復及錯誤監控。
- 將配置、密碼、HTTPS 反向代理及部署設定整理為適合正式環境的版本。

---

**BioHub — 以生物資訊專業經驗為出發點，整合研究資料管理、全端網站開發與 RNA-seq 分析流程的專案作品。**
