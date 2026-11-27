using BIOHub.Data;
using BIOHub.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;

namespace BIOHub.AnalysisWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BioHub Analysis Worker 已啟動，開始監控佇列");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOneJobAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker 執行時發生未預期錯誤");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("BioHub Analysis Worker 已停止");
    }

    private async Task ProcessOneJobAsync(CancellationToken token)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BIOHubDbContext>();

        // 找出最早排隊的工作（這個版本以單一 Worker 運作）。
        var job = await db.AnalysisJob
            .AsNoTracking()
            .Where(j => j.JobStatus == "Queued")
            .OrderBy(j => j.CreateAt)
            .ThenBy(j => j.AnalysisJobID)
            .FirstOrDefaultAsync(token);

        if (job == null)
        {
            return;
        }

        int jobId = job.AnalysisJobID;

        // 原子認領：只有仍為 Queued 的工作可更新為 Running。
        int changed = await db.AnalysisJob
            .Where(j => j.AnalysisJobID == jobId && j.JobStatus == "Queued")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.JobStatus, "Running")
                .SetProperty(j => j.StartAt, DateTime.UtcNow)
                .SetProperty(j => j.FinishAt, (DateTime?)null)
                .SetProperty(j => j.ErrorMessage, (string?)null), token);

        if (changed != 1)
        {
            _logger.LogWarning("AnalysisJob {JobId} 已由其他 Worker 認領", jobId);
            return;
        }

        _logger.LogInformation("AnalysisJob {JobId}: Queued → Running", jobId);

        try
        {
            // 檔案與參數驗證放在認領之後的 try 區塊，
            // 若資料有問題，工作才能轉為 Failed，而不是卡在 Queued。
            var files = await db.AnalysisFiles
                .AsNoTracking()
                .Where(f => f.AnalysisJobID == jobId)
                .ToListAsync(token);

            if (files.Count != 2 ||
                files.Count(f => f.FileType == "CountsMatrix") != 1 ||
                files.Count(f => f.FileType == "SampleMetadata") != 1)
            {
                throw new InvalidOperationException("工作必須有一份 Counts Matrix 和一份 Metadata");
            }

            string countsName = files.Single(f => f.FileType == "CountsMatrix").StoredFileName;
            string metadataName = files.Single(f => f.FileType == "SampleMetadata").StoredFileName;

            static bool ValidName(string? name) =>
                name is { Length: 36 } &&
                name.EndsWith(".csv", StringComparison.Ordinal) &&
                Guid.TryParseExact(name[..32], "N", out _);

            if (!ValidName(countsName) || !ValidName(metadataName) ||
                string.IsNullOrWhiteSpace(job.ControlGroup) ||
                string.IsNullOrWhiteSpace(job.TreatmentGroup) ||
                string.Equals(job.ControlGroup, job.TreatmentGroup, StringComparison.OrdinalIgnoreCase) ||
                job.Species is not ("Human" or "Mouse" or "Rat"))
            {
                throw new InvalidOperationException("上傳檔名、分組或物種設定不正確");
            }

            string script = _config["RNAseqWorker:ScriptPath"]
                ?? throw new InvalidOperationException("缺少 ScriptPath");

            // 路徑必須和 MVC AnalysisJobController 的結果驗證規則一致。
            string baseDir = $"/data/biohub-analysis/AnalysisJobs/{jobId}";
            string outputDir = $"{baseDir}/Results/{Guid.NewGuid():N}";
            string resultPath = $"{outputDir}/DEG_results.csv";
            string plotPath = $"{outputDir}/VolcanoPlot.png";
            string countsPath = $"{baseDir}/{countsName}";
            string metadataPath = $"{baseDir}/{metadataName}";

            int exitCode;
            string output;
            string errors;

            if (OperatingSystem.IsLinux())
            {
                // Linux Docker：Worker Image 已包含 Rscript / DESeq2。
                // 不讀取 DockerImage、VolumeName 或 UID/GID 設定。
                if (!File.Exists(script))
                    throw new FileNotFoundException("Linux 容器找不到 R 腳本", script);

                if (!File.Exists(countsPath) || !File.Exists(metadataPath))
                    throw new FileNotFoundException("共用 Volume 中找不到 Counts 或 Metadata CSV");

                (exitCode, output, errors) = await RunProcessAsync(
                    "Rscript",
                    new[]
                    {
                        script, countsPath, metadataPath, outputDir,
                        job.ControlGroup!, job.TreatmentGroup!, job.Species!
                    }, token);
            }
            else
            {
                // Windows 開發模式：保留已驗證成功的 Docker CLI 執行流程。
                string image = _config["RNAseqWorker:DockerImage"]
                    ?? throw new InvalidOperationException("缺少 DockerImage");
                string volume = _config["RNAseqWorker:VolumeName"]
                    ?? throw new InvalidOperationException("缺少 VolumeName");
                string uid = _config["RNAseqWorker:ContainerUid"]
                    ?? throw new InvalidOperationException("缺少 UID");
                string gid = _config["RNAseqWorker:ContainerGid"]
                    ?? throw new InvalidOperationException("缺少 GID");

                if (!int.TryParse(uid, out int uidNumber) ||
                    !int.TryParse(gid, out int gidNumber) ||
                    uidNumber < 1 || gidNumber < 1)
                {
                    throw new InvalidOperationException("Docker UID/GID 設定不正確");
                }

                string fullScriptPath = Path.GetFullPath(script);
                if (!File.Exists(fullScriptPath))
                    throw new FileNotFoundException("Windows 找不到 R 腳本", fullScriptPath);

                string scriptDir = Path.GetDirectoryName(fullScriptPath)!;
                string scriptName = Path.GetFileName(fullScriptPath);

                (exitCode, output, errors) = await RunProcessAsync(
                    "docker",
                    new[]
                    {
                        "run", "--rm",
                        "--user", $"{uidNumber}:{gidNumber}",
                        "--network", "none",
                        "--mount", $"type=volume,source={volume},target=/data/biohub-analysis",
                        "--mount", $"type=bind,source={scriptDir},target=/scripts,readonly",
                        image,
                        $"/scripts/{scriptName}",
                        countsPath, metadataPath, outputDir,
                        job.ControlGroup!, job.TreatmentGroup!, job.Species!
                    }, token);
            }

            _logger.LogInformation("AnalysisJob {JobId} R exit code: {ExitCode}", jobId, exitCode);

            if (exitCode != 0)
            {
                _logger.LogError("AnalysisJob {JobId} R 錯誤摘要：{Error}",
                    jobId, errors.Length > 1500 ? errors[..1500] : errors);
                throw new InvalidOperationException("R / DESeq2 執行失敗");
            }

            if (!output.Split('\n').Any(line => line.Trim() == "STATUS=Completed"))
                throw new InvalidOperationException("R 沒有回報 STATUS=Completed");

            int totalGenes = ReadSummaryInt(output, "TOTAL_GENES");
            int significantGenes = ReadSummaryInt(output, "SIGNIFICANT_GENES");

            if (totalGenes < 0 || significantGenes < 0 || significantGenes > totalGenes)
                throw new InvalidOperationException("R 分析摘要數值不正確");

            bool filesValid;
            if (OperatingSystem.IsLinux())
            {
                // Linux Worker 直接檢查同一個 Volume 的輸出檔。
                filesValid = File.Exists(resultPath) && File.Exists(plotPath) &&
                    new FileInfo(resultPath).Length > 0 &&
                    new FileInfo(plotPath).Length > 0;
            }
            else
            {
                // Windows Worker 的結果在 Docker Volume 內，透過 Docker 驗證。
                string image = _config["RNAseqWorker:DockerImage"]!;
                string volume = _config["RNAseqWorker:VolumeName"]!;
                string uid = _config["RNAseqWorker:ContainerUid"]!;
                string gid = _config["RNAseqWorker:ContainerGid"]!;

                var (verifyCode, _, _) = await RunProcessAsync(
                    "docker",
                    new[]
                    {
                        "run", "--rm",
                        "--user", $"{uid}:{gid}",
                        "--network", "none",
                        "--mount", $"type=volume,source={volume},target=/data/biohub-analysis,readonly",
                        "--entrypoint", "/bin/sh",
                        image,
                        "-c", "test -s \"$1\" && test -s \"$2\"",
                        "verify", resultPath, plotPath
                    }, token);

                filesValid = verifyCode == 0;
            }

            if (!filesValid)
                throw new IOException("DEG CSV 或 Volcano PNG 不存在或是空檔案");

            // AnalysisResults 與 Completed 必須在同一個交易提交。
            await using var transaction = await db.Database.BeginTransactionAsync(token);

            db.AnalysisResults.Add(new AnalysisResults
            {
                AnalysisJobID = jobId,
                ResultFilePath = resultPath,
                VolcanPlotPath = plotPath,
                TotalGenes = totalGenes,
                SignificantGenes = significantGenes,
                CreateAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(token);

            int completed = await db.AnalysisJob
                .Where(j => j.AnalysisJobID == jobId && j.JobStatus == "Running")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(j => j.JobStatus, "Completed")
                    .SetProperty(j => j.FinishAt, DateTime.UtcNow), token);

            if (completed != 1)
                throw new InvalidOperationException("無法將 AnalysisJob 更新為 Completed");

            await transaction.CommitAsync(token);

            _logger.LogInformation(
                "AnalysisJob {JobId}: Running → Completed；總基因數 {Total}；顯著基因數 {Significant}",
                jobId, totalGenes, significantGenes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AnalysisJob {JobId} 分析失敗", jobId);

            try
            {
                // 使用新 Scope，避免前面失敗的交易或 DbContext 狀態干擾。
                await using var failScope = _scopeFactory.CreateAsyncScope();
                var failDb = failScope.ServiceProvider.GetRequiredService<BIOHubDbContext>();

                await failDb.AnalysisJob
                    .Where(j => j.AnalysisJobID == jobId && j.JobStatus == "Running")
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(j => j.JobStatus, "Failed")
                        .SetProperty(j => j.FinishAt, DateTime.UtcNow)
                        .SetProperty(j => j.ErrorMessage, "R 分析或結果儲存失敗"),
                        CancellationToken.None);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx,
                    "AnalysisJob {JobId} 無法更新 Failed 狀態", jobId);
            }

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);
        }
    }

    private async Task<(int ExitCode, string Output, string Error)> RunProcessAsync(
        string fileName,
        IEnumerable<string> arguments,
        CancellationToken token)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (string arg in arguments)
            startInfo.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
            throw new InvalidOperationException($"無法啟動 {fileName}");

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();

        int timeoutMinutes = Math.Clamp(
            _config.GetValue<int>("RNAseqWorker:TimeoutMinutes", 30), 1, 30);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromMinutes(timeoutMinutes));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            throw;
        }

        return (process.ExitCode, await outputTask, await errorTask);
    }

    private static int ReadSummaryInt(string output, string key)
    {
        string prefix = key + "=";
        string? line = output
            .Split('\n')
            .Select(x => x.Trim())
            .LastOrDefault(x => x.StartsWith(prefix, StringComparison.Ordinal));

        if (line == null ||
            !int.TryParse(line[prefix.Length..], NumberStyles.None,
                CultureInfo.InvariantCulture, out int value))
        {
            throw new InvalidOperationException("缺少 R 分析摘要：" + key);
        }

        return value;
    }
}
