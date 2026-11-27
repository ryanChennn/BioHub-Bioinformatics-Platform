# BioHub

**Bioinformatics & Clinical Research Management Platform｜生物資訊與臨床研究管理平台**

BioHub 是一個結合**研究資料管理**與 **RNA-seq 差異基因表現分析**的全端網站作品。專案以 ASP.NET Core MVC、Vue 3、SQL Server、.NET Worker Service、R / DESeq2 和 Docker 建構，將資料上傳、工作排程、分析執行與結果展示整合為可操作的研究工作流程。

> **專案定位：** 以生物資訊分析與研究資料管理為核心，展示跨領域開發能力；實驗室服務或商務相關功能屬於後續擴充方向，未來將繼續進行開發。

## 功能總覽

| 模組               | 主要功能                                                                                       | 目前狀態                   |
| ------------------ | ---------------------------------------------------------------------------------------------- | -------------------------- |
| 公開網站           | Vue 3 製作首頁、平台介紹與功能展示；由「登入平台」導向 MVC 登入頁                              | 已於本機驗證               |
| 使用者系統         | 註冊、登入、Cookie Authentication、登入狀態管理                                                | 已於本機驗證               |
| 研究專案管理       | ResearchProjects 建立、查詢、修改、刪除（CRUD）                                                | 已於本機驗證               |
| RNA-seq 分析工作   | 上傳 Counts Matrix 與 Sample Metadata CSV、建立分析工作、佇列處理                              | 已於本機驗證               |
| 差異表現分析       | Linux AnalysisWorker 直接呼叫 Rscript / DESeq2，支援 Human、Mouse、Rat 的註解套件              | 已於本機驗證               |
| 分析結果           | DEG 結果 CSV、DEG Explorer、Volcano Plot                                                       | 已於本機驗證               |
| 容器化服務         | MVC、Vue/Nginx、AnalysisWorker 由 Docker Compose 啟動；SQL Server 目前使用既有獨立 Docker 容器 | 已於本機驗證               |
| 臨床及檢體完整模組 | 完整臨床研究資料與檢體管理工作流程                                                             | 持續開發／未作完整功能承諾 |

## 使用技術

| 類別     | 技術                                                               |
| -------- | ------------------------------------------------------------------ |
| 公開網站 | Vue 3、Vite、JavaScript、HTML、CSS、Bootstrap 5                    |
| 後端網站 | C#、ASP.NET Core MVC、.NET 10                                      |
| 資料存取 | SQL Server 2022、Entity Framework Core                             |
| 身分驗證 | ASP.NET Core Cookie Authentication（未採用 ASP.NET Core Identity） |
| 背景工作 | .NET Worker Service、資料庫工作佇列                                |
| 生物資訊 | R、DESeq2、AnnotationDbi、org.Hs.eg.db、org.Mm.eg.db、org.Rn.eg.db |
| 部署     | Docker、Docker Compose、Nginx                                      |

## 系統架構

```text
瀏覽器
  ├── Vue 3 + Nginx（公開網站，localhost:8081）
  │       └──「登入平台」連結至 MVC
  └── ASP.NET Core MVC（會員與研究平台，localhost:8080）
          ├── SQL Server（使用者、研究專案、分析工作及結果）
          └── AnalysisJob：Queued
                 ↓
             .NET AnalysisWorker（每隔一段時間輪詢工作）
                 ↓
             Rscript + DESeq2（Linux Docker）
                 ↓
             DEG_results.csv + VolcanoPlot.png
                 ↓
             SQL Server：AnalysisResults / Completed
                 ↓
             MVC：DEG Explorer / Volcano Plot / 結果下載
```

### 資料持久化

本機環境目前使用三個 Docker named volumes：

- `biohub_sql_data`：SQL Server 資料庫檔案。
- `biohub_analysis_data`：上傳 CSV 與 R 分析輸出，MVC 與 Worker 共用。
- `biohub_dpkeys`：ASP.NET Core Data Protection Keys，維持 Cookie 加解密所需金鑰。

以上 volumes **只存在執行 Docker 的主機**，不會隨 GitHub clone 自動產生既有資料。

## 已驗證的本機展示

| 服務           | 本機網址或執行方式                            |
| -------------- | --------------------------------------------- |
| Vue 公開網站   | http://localhost:8081                         |
| MVC 登入與後台 | http://localhost:8080/Account/Login           |
| AnalysisWorker | Docker Compose 背景服務，無對外網頁           |
| SQL Server     | 既有獨立容器 `biohub-sql`；僅供本機開發與測試 |

**本機驗證結果：** 已測試 Vue 首頁導向 MVC 登入、MVC 主要管理功能，以及 Linux Worker 執行 R / DESeq2 後，完成工作並於 MVC 查看 DEG Explorer 與 Volcano Plot。

## Docker Compose 操作（目前已驗證的開發電腦）

本專案已在作者的 Docker Desktop 環境驗證 MVC、Vue 與 AnalysisWorker 由 Docker Compose 執行。**目前 Compose 整合使用既有的本機 Images、SQL Server 獨立容器與 named volumes，尚非全新電腦的一鍵部署套件。**

在專案根目錄執行：

```powershell
docker compose --profile website config --quiet
```

啟動已設定的應用程式服務（依目前設定，SQL Server 須先由原有容器提供服務）：

```powershell
docker compose --profile website up -d --no-deps mvc vue
```

啟動或檢查 AnalysisWorker（具體依 `compose.yaml` 與 `.env` 設定）：

```powershell
docker compose up -d analysis-worker
docker compose --profile website ps
docker compose logs --tail 50 analysis-worker
```

關閉由 Compose 管理的服務時，請勿使用 `-v` 刪除資料：

```powershell
docker compose --profile website stop
```

### 在其他電腦執行前，仍需完成

1. 安裝 Docker Desktop（Linux containers 模式），取得專案原始碼。
2. 由 `.env.example` 建立 `.env`，填入自行設定的密碼、UID/GID 及環境參數；**勿提交 `.env`**。
3. 確認 MVC、Vue、Worker 的 Dockerfile 均可由原始碼建立，尤其 Worker 所需 R / DESeq2 基礎映像可重現建置或取得。
4. 建立及初始化 SQL Server 資料庫，套用 EF Core migrations；準備匿名化的示範帳號、CSV 和分析資料。
5. 調整 `compose.yaml` 中仍依賴作者主機的設定（例如既有容器、Images、volumes 或環境變數），再進行完整測試。

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
- **部署隔離：** Vue/Nginx、MVC、Worker、SQL Server 各自獨立。
- **實際限制：** 目前為作品展示及開發驗證版本；正式公開部署還需要進一步處理 HTTPS、權限控管、機密管理、資料備份、工作中斷恢復與監控。

## 後續規劃

- 補齊臨床資料與檢體管理工作流程及權限管理，增加實驗室服務販售的功能。
- 完善可在新主機重現的 Docker Compose 建置、資料庫初始化與示範資料。
- 改善工作逾時、重試、恢復及錯誤監控。
- 將配置、密碼及部署設定整理為適合正式環境的版本。

---

**BioHub — 以生物資訊專業經驗為出發點，整合研究資料管理、全端網站開發與 RNA-seq 分析流程的專案作品。**
