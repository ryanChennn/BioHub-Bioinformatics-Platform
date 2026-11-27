using BIOHub.Constants;
using BIOHub.Data;
using BIOHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics;
using System.Globalization;

namespace BIOHub.Services
{
    public record AnalysisRunResult(bool Success, string Message);
    public class RNAseqAnalysisService
    {
        private BIOHubDbContext _context;
        private IWebHostEnvironment _env;
        private IConfiguration _configuration;
        private ILogger<RNAseqAnalysisService> _logger;

        public RNAseqAnalysisService(BIOHubDbContext context,
            IWebHostEnvironment env,
            IConfiguration configuration,
            ILogger<RNAseqAnalysisService> logger)
        {
            _context = context;
            _env = env;
            _configuration = configuration;
            _logger = logger;
        }
        public async Task<AnalysisRunResult>RunAsync(
            int jobId, int userId)
        {
            var job = await _context.AnalysisJob
                .AsNoTracking()
                .FirstOrDefaultAsync(j=>j.AnalysisJobID == jobId && j.UsersID == userId &&_context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == j.ResearchProjectsID && p.UsersID == userId));

            if(job == null)
            {
                return new(false, "找不到分析工作或沒有存取權限");
            }
            if(job.JobStatus != "Pending")
            {
                return new(false, "此分析工作目前不可執行。");
            }

            var files = await _context.AnalysisFiles
                .AsNoTracking()
                .Where(f => f.AnalysisJobID == jobId)
                .ToListAsync();

            if(files.Count !=2 || 
                files.Count(f=>f.FileType == "CountsMatrix") !=1 || 
                files.Count(f=>f.FileType == "SampleMetadata") != 1)
            {
                return new(false, "請先上傳 Counts 與 Metadata CSV");
            }
            //  只使用伺服器產生的儲存檔名
            string GetTrustedFilePath(AnalysisFiles file)
            {
                string stored = file.StoredFileName;

                // 必須符合原本由 Guid.NewGuid():N 產生的命名
                bool validName = stored.Length == 36 &&
                    stored.EndsWith(".csv", StringComparison.Ordinal) &&
                    Guid.TryParseExact(stored[..32], "N", out _);

                if (!validName)
                {
                    throw new InvalidOperationException("儲存檔名格式錯誤");
                }
                string? configuredRoot =
    _configuration["AnalysisStorage:Root"];

                string analysisRoot = !string.IsNullOrWhiteSpace(configuredRoot)
                    ? Path.GetFullPath(configuredRoot)
                    : Path.Combine(
                        Directory.GetParent(_env.ContentRootPath)!.FullName,
                        "BioHubPrivateUploads"
                    );

                return Path.Combine(
                    analysisRoot,
                    "AnalysisJobs",
                    jobId.ToString(CultureInfo.InvariantCulture),
                    stored);
            }

            string countsPath;
            string metadataPath;

            try
            {
                countsPath = GetTrustedFilePath(files.Single(f => f.FileType == "CountsMatrix"));
                metadataPath = GetTrustedFilePath(files.Single(f=>f.FileType == "SampleMetadata"));
            }
            catch (InvalidOperationException)
            {
                return new(false, "上傳記錄錯誤");
            }


            if (!File.Exists(countsPath) || !File.Exists(metadataPath)) {
                return new(false, "找不到上傳的 CSV 檔案");
            }


            string rscriptPath = _configuration["RNAseqSettings:RscriptPath"] ?? "";
            //string? rscriptPath = _configuration["RNAseqSettings:RscriptPath"];
            //string scriptRelativePath = _configuration["RNAseqSetting:ScriptRelativePath"] ?? "";
            string? scriptRelativePath = _configuration["RNAseqSettings:ScriptRelativePath"];
            if (string.IsNullOrWhiteSpace(scriptRelativePath)) {
                return new(false, "ScriptRelativePath 沒有設定，請檢查 appsettings.json");
            }
            

            string scriptPath = Path.GetFullPath(
                Path.Combine(_env.ContentRootPath, scriptRelativePath));
            // 偵錯：顯示實際使用的路徑
            
            _logger.LogWarning(
                "RscriptPath = [{Path}]",
                rscriptPath);

            _logger.LogWarning(
                "File.Exists = {Exists}",
                File.Exists(rscriptPath));

            _logger.LogInformation(
                "ContentRootPath: {ContentRootPath}",
                _env.ContentRootPath);

            _logger.LogInformation(
                "RscriptPath: {RscriptPath}",
                rscriptPath);

            _logger.LogInformation(
                "ScriptPath: {ScriptPath}",
                scriptPath);

            // 分別檢查檔案
            if (!File.Exists(rscriptPath))
            {
                _logger.LogError(
                    "找不到 Rscript.exe：{Path}", rscriptPath);

                return new(false, "找不到 Rscript.exe，請檢查 appsettings.json。");
            }

            if (!File.Exists(scriptPath))
            {
                _logger.LogError(
                    "找不到 R 分析腳本：{Path}", scriptPath);

                return new(false, "找不到 run_Deseq2.R，請檢查 RScripts 路徑。");
            }
            //if (!File.Exists(rscriptPath) || 
            //    !File.Exists(scriptPath)) {

            //    return new(false, "Rscript.exe 或 R 分析腳本不存在");
            //}

            if (string.IsNullOrWhiteSpace(job.ControlGroup) ||
                string.IsNullOrWhiteSpace(job.TreatmentGroup) ||
                job.ControlGroup == job.TreatmentGroup)
            {
                return new(false, "組別設定錯誤");
             }

            if(string.IsNullOrWhiteSpace(job.Species) ||
                !AnalysisSpecies.Support.Contains(job.Species))
            {
                return new AnalysisRunResult(false, "請先確認此分析工作的物種設定");
            }

            // 將 Pending 改成 Running
            // 避免兩個請求同時執行同一筆工作

            int changed = await _context.AnalysisJob
                .Where(j => j.AnalysisJobID == jobId && j.UsersID == userId && j.JobStatus == "Pending")
                .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.JobStatus, "Running")
                .SetProperty(j => j.StartAt, DateTime.UtcNow)
                .SetProperty(j => j.FinishAt, (DateTime?)null)
                .SetProperty(j => j.ErrorMessage, (string?)null));


            if (changed != 1) {
                return new(false, "分析工作已由其他請求開始執行");

                }
            try
            {
                // 每次執行使用全新的結果資料夾
                string jobDirectoy = Path.GetDirectoryName(countsPath)!;

                string outputDir = Path.Combine(
                    jobDirectoy,
                    "Results",
                    Guid.NewGuid().ToString("N"));

                Directory.CreateDirectory(outputDir);
                string resultPath = Path.Combine(outputDir, "DEG_results.csv");
                string plotPath = Path.Combine(outputDir, "VolcanoPlot.png");

                // 建立 Rscript 程序
                var startInfo = new ProcessStartInfo
                {
                    FileName = rscriptPath,
                    WorkingDirectory = _env.ContentRootPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                startInfo.ArgumentList.Add(scriptPath);
                startInfo.ArgumentList.Add(countsPath);
                startInfo.ArgumentList.Add(metadataPath);
                startInfo.ArgumentList.Add(outputDir);
                startInfo.ArgumentList.Add(job.ControlGroup);
                startInfo.ArgumentList.Add(job.TreatmentGroup);
                startInfo.ArgumentList.Add(job.Species);

                using var process = new Process
                {
                    StartInfo = startInfo
                };
                process.Start();

                if (!process.Start())
                {
                    throw new InvalidOperationException("無法啟動 Rscript.exe");

                }

                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();

                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                int timeoutMinutes = _configuration.GetValue<int>("RNAseqSettings:TimeoutMinutes", 5);

                timeoutMinutes = Math.Clamp(timeoutMinutes, 1, 30);

                using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(timeoutMinutes));

                try
                {
                    await process.WaitForExitAsync(timeout.Token);
                }
                catch (OperationCanceledException)
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }

                    await process.WaitForExitAsync();
                    throw new TimeoutException("R 分析逾時，已終止分析程序");
                }

                string output = await outputTask;
                string errors = await errorTask;

                // 不將完整 R 日誌直接傳給前端
                _logger.LogInformation("AnalysisJob {JobId} R Output:{Output}", jobId,output);
                _logger.LogInformation("AnalysisJob {JobId} R Error:{Error}", jobId, errors);

                _logger.LogInformation("AnalysisJob{JobId} : R exit code = {ExitCode}", jobId, process.ExitCode);

                if (process.ExitCode != 0)
                {
                    _logger.LogError("AnalysisJob {JobId} : R failed : {Errors}", jobId, errors.Length > 4000 ? errors[..4000] : errors);

                    throw new InvalidOperationException("R 分析失敗，請查看伺服器日誌。");

                }

                if (!File.Exists(resultPath) || !File.Exists(plotPath))
                {
                    throw new IOException("R 未產生預期的結果檔案。");
                }

                //從 R 的標準輸出解析分析摘要
                int totalGenes = ReadSummaryInt(output, "TOTAL_GENES");

                int significantGenes = ReadSummaryInt(output, "SIGNIFICANT_GENES");

                if (totalGenes < 0 || significantGenes < 0 || significantGenes > totalGenes)
                {
                    throw new InvalidOperationException("R 分析摘要數值不正確");
                }

                //在同一個資料庫交易中儲存結果與狀態
                await using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    _context.AnalysisResults.Add(new AnalysisResults
                    {
                        AnalysisJobID = jobId,
                        ResultFilePath = resultPath,
                        VolcanPlotPath = plotPath,
                        TotalGenes = totalGenes,
                        SignificantGenes = significantGenes,
                        CreateAt = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();
                    int update =  await _context.AnalysisJob
                        .Where(j => j.AnalysisJobID == jobId && j.JobStatus == "Running")
                        .ExecuteUpdateAsync(setters => setters
                        .SetProperty(j => j.JobStatus, "Completed")
                        .SetProperty(j => j.FinishAt, DateTime.UtcNow));

                    if(update != 1)
                    {
                        throw new InvalidOperationException(
                         $"AnalysisJob {jobId} 無法更新為 Completed。");
                    }

                    await transaction.CommitAsync();
                }

                return new(true, "RNAseq分析成功");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "AnalysisJob {JobId} failed", jobId);

                // 失敗時更新狀態，避免工作一直停在 Running

                try
                {
                    await _context.AnalysisJob
                        .Where(j => j.AnalysisJobID == jobId && j.JobStatus == "Running")
                        .ExecuteUpdateAsync(setters => setters.SetProperty(j => j.JobStatus, "Failed")
                        .SetProperty(j => j.FinishAt, DateTime.UtcNow)
                        .SetProperty(j => j.ErrorMessage, "R 分析或結果儲存失敗"));
                }
                catch(Exception saveEx)
                {
                    _logger.LogError(saveEx, "Failed to update AnalysisJob{JobId} status", jobId);
                }

                return new(false, "RNA-seq 分析失敗，請查看伺服器日誌");


            }
        }

        private static int ReadSummaryInt(string output, string key)
        {
            string prefix = key + "=";

            var line = output
                .Split('\n')
                .Select(x => x.Trim())
                .LastOrDefault(x => x.StartsWith(prefix, StringComparison.Ordinal));

            if (line == null || !int.TryParse(line[prefix.Length..],
                NumberStyles.None,
                CultureInfo.InvariantCulture, out int value))
            {

                throw new InvalidOperationException("缺少 R 分析摘要：" + key);

            }
            return value;
        }
    }
}
