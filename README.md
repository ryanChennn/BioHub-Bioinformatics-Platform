# BioHub — Bioinformatics & Clinical Research Management Platform

**生物資訊與臨床研究管理平台｜全端開發 × RNA-seq 分析 × Docker 容器化部署**

BioHub 是一個以生物資訊與研究資料管理為核心的作品，整合 **Vue 3 公開網站、ASP.NET Core MVC 管理平台、SQL Server、.NET AnalysisWorker 與 R / DESeq2**。使用者可登入平台、管理研究專案、上傳 RNA-seq Counts Matrix 與 Sample Metadata、建立差異表現分析工作，並在網頁中查看 DEG Explorer 與 Volcano Plot。

本專案以 **Nginx Gateway 提供單一網站入口**，並使用 Docker Compose 管理服務及全新資料庫初始化，方便在另一個 Docker 環境重現展示流程。

> **專案定位：** 展示生物資訊工作流程與全端系統設計的整合能力。實驗室服務商務功能、完整臨床／檢體資料管理與進階權限系統為後續擴充方向。

## 功能與完成狀態

| 模組                   | 功能                                                        | 驗證狀態                                                          |
| ---------------------- | ----------------------------------------------------------- | ----------------------------------------------------------------- |
| 公開網站               | Vue 3 首頁、功能介紹、關於平台                              | 已驗證                                                            |
| 統一入口               | Nginx Gateway 代理 `/public/` 至 Vue、其餘路徑至 MVC        | 已驗證                                                            |
| 使用者驗證             | 自訂 `Users`、密碼雜湊、Cookie Authentication、登入／註冊   | 已驗證                                                            |
| 研究專案管理           | ResearchProjects CRUD                                       | 已驗證                                                            |
| RNA-seq 資料輸入       | 上傳 Counts Matrix、Sample Metadata CSV，建立 AnalysisJob   | 已驗證                                                            |
| 背景運算               | .NET Worker 取得工作，呼叫 Linux 容器中的 Rscript / DESeq2  | 已驗證                                                            |
| 結果視覺化             | DEG Explorer、Volcano Plot、分析結果檔案                    | 已驗證                                                            |
| 資料庫初始化           | 全新 SQL Server Volume、EF Core Migrations、Demo 帳號初始化 | 已在獨立 Compose 專案測試成功                                     |
| 跨環境容器部署         | MVC／Vue 建置、Worker 由 GHCR 下載                          | 已在重新 Clone 的目錄與全新 Volume 驗證；尚未在第二台實體電腦測試 |
| 臨床／檢體完整管理流程 | 規劃與持續開發                                              | 尚未宣稱完整驗證                                                  |

## 技術架構

| 層級     | 技術                                                                                             |
| -------- | ------------------------------------------------------------------------------------------------ |
| 公開前端 | Vue 3、Vite、JavaScript、HTML、CSS                                                               |
| 管理介面 | ASP.NET Core MVC、Razor、Bootstrap 5、C#、.NET 10                                                |
| 資料庫   | Microsoft SQL Server 2022、Entity Framework Core Migrations                                      |
| 身分驗證 | ASP.NET Core Cookie Authentication、`PasswordHasher<Users>`（不使用 ASP.NET Core Identity 系統） |
| 背景分析 | .NET Worker Service、Rscript、DESeq2                                                             |
| 分析註解 | AnnotationDbi 與 Human／Mouse／Rat 註解套件                                                      |
| 基礎設施 | Docker Compose、Nginx Gateway、Docker named volumes、GitHub Container Registry（GHCR）           |

### 系統運作示意

```text
                  瀏覽器
          http://localhost:8090
                      │
                      ▼
               Nginx Gateway
                      │
          ┌───────────┴───────────┐
          │                       │
          ▼                       ▼
       /public/             MVC 管理路由
      Vue 3 網站           ASP.NET Core MVC
       (Nginx)             登入／研究管理
                                  │
                           ┌──────┴─────────┐
                           │                │
                           ▼                ▼
                       SQL Server     AnalysisJob
                       使用者/專案        Queued
                       工作/結果            │
                                            ▼
                                   .NET AnalysisWorker
                                            │
                                            ▼
                                      R / DESeq2
                                            │
                                            ▼
                               DEG CSV / Volcano Plot
                                            │
                                            ▼
                                   MVC 結果檢視與下載
```

Gateway 路由說明：

| 路徑                                    | 頁面或功能                                    |
| --------------------------------------- | --------------------------------------------- |
| `http://localhost:8090`                 | 單一入口，自動轉至 `/public/`                 |
| `http://localhost:8090/public/`         | Vue 公開首頁                                  |
| `http://localhost:8090/public/features` | 功能介紹                                      |
| `http://localhost:8090/public/about`    | 關於平台                                      |
| `http://localhost:8090/Account/Login`   | ASP.NET Core MVC 登入頁                       |
| 其他 MVC 路由                           | Dashboard、研究專案管理、RNA-seq 分析與視覺化 |

## 快速開始：Docker Compose

### 前置需求

- Git
- Docker Desktop，並使用 **Linux containers**（或具備 Linux 容器能力的 Docker Engine 與 Compose）
- 足夠的磁碟空間與記憶體，以下載 SQL Server 及大型 R / DESeq2 Worker Image；首次啟動需能連接 Docker Registry / GHCR
- 本機的 `8090` 埠尚未被其他服務使用

### 1. Clone 專案

```powershell
git clone https://github.com/ryanChennn/BioHub-Bioinformatics-Platform.git
cd BioHub-Bioinformatics-Platform
```

### 2. 一行啟動

在專案根目錄執行：

```powershell
docker compose up -d --build
```

此流程會：

1. 下載／啟動 SQL Server 2022，建立 Docker named volume。
2. 透過 `volume-init` 處理分析資料與 ASP.NET Core 金鑰目錄的權限。
3. 使用 MVC Image 的 `db-init` 模式套用 EF Core Migrations，初始化展示帳號。
4. 從 GHCR 下載已建立的 AnalysisWorker Image：`ghcr.io/ryanchennn/biohub-analysis-worker:v1`。
5. 建置並啟動 MVC、Vue／Nginx，以及 Nginx Gateway。

**首次啟動需要下載及建置容器，請確認服務已啟動、`db-init` 已成功結束後再開啟網站。**

### 3. 開啟 BioHub

**入口：<http://localhost:8090>**

### 4. 檢查容器與資料庫初始化

```powershell
docker compose ps -a
```

```powershell
docker compose logs --tail 80 db-init
```

分析 Worker 日誌：

```powershell
docker compose logs --tail 80 analysis-worker
```

停止服務（保留資料）：

```powershell
docker compose stop
```

## RNA-seq 分析工作流程

1. 登入平台，進入研究／分析功能。
2. 上傳 **Counts Matrix CSV** 與 **Sample Metadata CSV**，選擇分析組別與物種。
3. MVC 建立 AnalysisJob，工作狀態為 `Queued`。
4. .NET AnalysisWorker 取得工作並執行 Linux 容器內的 `/app/RScripts/run_Deseq2.R`。
5. R / DESeq2 產生差異基因表現結果，如 `DEG_results.csv` 與 `VolcanoPlot.png`。
6. 工作狀態更新為 `Completed`（失敗時顯示 `Failed`）。
7. 使用者可在 MVC 查看 DEG Explorer、Volcano Plot 與結果檔案。

### 核心設計特色

- **跨技術整合：** 以 C# 管理網站與工作狀態，由 R / DESeq2 專責生物資訊運算。
- **背景工作解耦：** 長時間分析不阻塞 MVC 的使用者請求。
- **單一入口：** Vue 與 MVC 分別部署，透過 Nginx Gateway 對外提供統一網址。
- **資料持久化：** SQL 資料、分析輸入／輸出、Data Protection Keys 分別儲存於 Docker named volumes。
- **部署可重現性：** SQL 初始化由 EF Core Migrations 完成，R Worker 由 GHCR 發佈，無須在每台新電腦手動安裝 DESeq2。

## 專案目錄（主要組件）

```text
BioHub-Bioinformatics-Platform/
├── compose.yaml                    # 本機展示版 Docker Compose
├── .env.example                    # 環境變數參考（非私密設定）
├── gateway/
│   └── default.conf               # Nginx Gateway 路由
├── BIOHub/
│   ├── Dockerfile                  # ASP.NET Core MVC Image
│   ├── Program.cs
│   └── Services/
│       └── BioHubDemoInitializer.cs
├── BIOHub.AnalysisWorker/
│   └── Dockerfile                  # Worker 原始建置設定
└── BioHub_Vue/
    └── Dockerfile                  # Vue／Nginx Image
```

## 驗證紀錄與限制

- 已在 Docker Desktop 上完成 Vue／MVC Gateway 整合、登入、研究管理、RNA-seq 分析與結果視覺化測試。
- 已從 GitHub 重新 Clone，使用**獨立 Compose 專案名稱及全新 SQL Server Volume**，驗證資料庫初始化與 Demo 登入；此環境的 Bootstrap 靜態資源問題已修復。
- 現階段定位為**面試展示／開發作品**，不是已經完成安全強化的對外正式服務。正式上線仍需加入 HTTPS、機密管理、最小權限資料庫帳號、細緻授權、備份／還原、監控與工作恢復機制。
- 臨床資料若涉及個資或病患資訊，不應直接使用本展示版儲存真實資料；僅使用自製、公開授權或充分去識別化之測試資料。

## 後續規劃

- 完善臨床資料與檢體管理模組，以及角色與權限控管。
- 強化背景工作的重試、逾時、錯誤追蹤與監控。
- 加入更完整的可重現分析範例資料、CI/CD、自動測試與安全部署方式。

---

**BioHub — 以生物資訊專業為基礎，整合研究資料管理、全端網站開發與 RNA-seq 差異表現分析的作品。**
