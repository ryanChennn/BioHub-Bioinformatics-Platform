using BIOHub.Data;
using BIOHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BIOHub.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using BIOHub.Services;
using BIOHub.Constants;
using System.Globalization;
using System.Text;
using CsvHelper;
using System.Linq.Expressions;
using System.Diagnostics.SymbolStore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Storage.Json;
using System.Drawing;


namespace BIOHub.Controllers
{
    [Authorize]
    public class AnalysisJobController : Controller
    {
        private BIOHubDbContext _context;
        private IWebHostEnvironment _env;
        private RNAseqAnalysisService _analysisService;
        private readonly IConfiguration _configuration;
        public AnalysisJobController(BIOHubDbContext context,
            IWebHostEnvironment env,
            RNAseqAnalysisService analysisService,
            IConfiguration configuration)
        {
            _context = context;
            _env = env;
            _analysisService = analysisService;
            _configuration = configuration;
        }

        private string GetAnalysisStorageRoot()
        {
            string? configuredRoot =
                _configuration["AnalysisStorage:Root"];

            if (!string.IsNullOrWhiteSpace(configuredRoot))
            {
                return Path.GetFullPath(configuredRoot);
            }

            return Path.Combine(
                Directory.GetParent(_env.ContentRootPath)!.FullName,
                "BioHubPrivateUploads");
        }

        private string? GetSafeDegCsvPath(int jobId, string? storedPath)
        {
            // 1. 檢查路徑是否為空
            if (string.IsNullOrWhiteSpace(storedPath) ||
                !Path.IsPathFullyQualified(storedPath))
            {
                return null;
            }

            var parent = Directory.GetParent(_env.ContentRootPath);
            if (parent == null)
            {
                return null;
            }


            // 與 RNAseqAnalysisService 儲存結果的位置一致
            string resultsRoot = Path.GetFullPath(
                Path.Combine(

                
                GetAnalysisStorageRoot(),
                "AnalysisJobs",
                jobId.ToString(CultureInfo.InvariantCulture),
                "Results"));

            string fullPath = Path.GetFullPath(storedPath);

            string relativePath = Path.GetRelativePath(resultsRoot, fullPath);

            var parts = relativePath.Split(new[]
            {
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            },
            StringSplitOptions.RemoveEmptyEntries);

            // 必須符合：
            // Results/{32字元GUID}/DEG_results.csv
            if (parts.Length != 2 ||
                !Guid.TryParseExact(parts[0], "N", out _) ||
                !string.Equals(parts[1], "DEG_results.csv", StringComparison.Ordinal))
            {
                return null;
            }

            if (!System.IO.File.Exists(fullPath))
            {
                return null;
            }
            return fullPath;

        }



        private static byte[] BuildFilteredDegCsv(
            List<string> headers,
            List<List<string>> rows)
        {
            var sb = new StringBuilder();

            // 將一個值安全轉換成 CSV 欄位
            static string EscapeCsv(
                string? value,
                bool numericColumn)
            {
                value ??= "";

                // 防止試算表將外來文字誤認為公式
                string trimmed = value.TrimStart();

                bool isValidNumber =
                    numericColumn &&
                    double.TryParse(
                        value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double number) &&
                    double.IsFinite(number);

                if (!isValidNumber &&
                    trimmed.Length > 0 &&
                    "=+-@".Contains(trimmed[0]))
                {
                    value = "'" + value;
                }

                // 避免以 tab、換行控制字元起始的內容
                // 被試算表當成特殊輸入
                if (value.Length > 0 &&
                    (value[0] == '\t' ||
                     value[0] == '\r' ||
                     value[0] == '\n'))
                {
                    value = "'" + value;
                }

                // CSV 中的雙引號需要轉換成兩個雙引號
                value = value.Replace("\"", "\"\"");

                return "\"" + value + "\"";
            }

            // DEG 數值欄位
            var numericColumns = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
    {
        "baseMean",
        "log2FoldChange",
        "lfcSE",
        "stat",
        "pvalue",
        "padj"
    };

            // 1. CSV 標題列
            sb.AppendLine(string.Join(
                ",",
                headers.Select(h => EscapeCsv(h, false))
            ));

            // 2. CSV 資料列
            foreach (var row in rows)
            {
                var values = headers.Select((header, index) =>
                {
                    string value = index < row.Count
                        ? row[index]
                        : "";

                    return EscapeCsv(
                        value,
                        numericColumns.Contains(header));
                });

                sb.AppendLine(string.Join(",", values));
            }

            // 3. UTF-8 BOM，改善 Excel 開啟中文時的相容性
            byte[] bom = Encoding.UTF8.GetPreamble();
            byte[] content = Encoding.UTF8.GetBytes(sb.ToString());

            var result = new byte[bom.Length + content.Length];

            Buffer.BlockCopy(
                bom, 0,
                result, 0,
                bom.Length);

            Buffer.BlockCopy(
                content, 0,
                result, bom.Length,
                content.Length);

            return result;
        }



        public async Task<IActionResult> VolcanoData(int id)
        {

            string? UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue, out var userId))
            {
                return Challenge();
            }

            var job = await _context.AnalysisJob
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.AnalysisJobID == id &&
                j.UsersID == userId && j.JobStatus == "Completed" &&
                _context.ResearchProjects
                .Any(p => p.ResearchProjectsID == j.ResearchProjectsID &&
                p.UsersID == userId));

            if (job == null) {
                return NotFound();
            }

            var result = await _context.AnalysisResults
                .AsNoTracking()
                .FirstOrDefaultAsync(r=>r.AnalysisJobID == id);
            if (result == null) {
                return NotFound();
            }

            string? csvPath = GetSafeDegCsvPath(id, result.ResultFilePath);

            if (csvPath == null) {
                return NotFound();
            }

            const int maxRows = 1000000;
            var points = new List<object>();

            try
            {
                
                using var csv = CsvReader.FromFile(csvPath);
                if(!csv.TryGetFieldIndex("log2FoldChange", out int fcIndex) ||
                    !csv.TryGetFieldIndex("padj", out int padjIndex))
                {
                    return BadRequest("CSV 缺少 log2FoldChange 或 padj 欄位");
                }

                bool hasGeneId = csv.TryGetFieldIndex("GeneID", out int geneIdIndex);

                bool hasGeneSymbol = csv.TryGetFieldIndex("GeneSymbol", out int symbolIndex);

                int rowCount = 0;
                while (csv.Read())
                {
                    rowCount++;
                    if(rowCount > maxRows)
                    {
                        return BadRequest("基因資料超過 100,0000 筆上限");
                    }

                    string fcText = csv.GetString(fcIndex);
                    string padjText = csv.GetString(padjIndex);
                    if(!double.TryParse(
                        fcText,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double fc) ||
                        !double.IsFinite(fc))
                    {
                        continue;
                    }
                    if(!double.TryParse(
                        padjText,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double padj) ||
                        !double.IsFinite(padj) ||
                        padj <0 || padj > 1)
                    {
                        continue;
                    }

                    string geneId = hasGeneId ? csv.GetString(geneIdIndex) : "";
                    string geneSymbol = hasGeneSymbol ? csv.GetString(symbolIndex) : "";

                    double y = -Math.Log10(Math.Max(padj, 1e-300));
                    points.Add(new
                    {
                        geneId,
                        geneSymbol,
                        log2FoldChange = fc,
                        padj,
                        negLog10Padj = y

                    });
                }
            }
            catch (IOException)
            {
                return StatusCode(
            500, "讀取 DEG CSV 失敗");
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(
            500, "沒有讀取檔案的權限");
            }
            catch (CsvHelper.CsvFormatException)
            {
                return BadRequest(
            "DEG CSV 格式不正確");
            }

            Response.Headers.CacheControl = "private, no-store";
            return Ok(new
            {
                count = points.Count,
                points
            });
        }


        public async Task<IActionResult> GetDegResults(
            int id, 
            [FromQuery] DegExplorerQueryViewModel query,
            [FromQuery]bool exportCsv = false)
        {
            string? UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue, out var userId))
            {
                return Challenge();
            }


            var job =await  _context.AnalysisJob
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.AnalysisJobID == id &&
                j.UsersID == userId &&
                j.JobStatus == "Completed" &&
                _context.ResearchProjects.Any(p => p.ResearchProjectsID == j.ResearchProjectsID &&
                p.UsersID == userId));

            if(job == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest("篩選參數格式不正確");
            }

            if(query.Page <1 ||
                query.PageSize <1 ||
                query.PageSize > 100)
            {
                return BadRequest("分頁參數不正確");
            }

            var allowdColumns = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                "GeneID", "GeneSymbol", "GeneName",
                 "baseMean", "log2FoldChange", "lfcSE",
                 "stat", "pvalue", "padj"
            };

            if (!allowdColumns.Contains(query.SortColumn))
            {
                return BadRequest("不支援的排序欄位");
            }
            if(query.SortDirection != "asc" && query.SortDirection != "desc")
            {
                return BadRequest("排序方向不正確");
            }

            var result = await _context.AnalysisResults
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.AnalysisJobID == id);

            if(result == null)
            {
                return NotFound();
            }
            string? csvPath = GetSafeDegCsvPath(id,result.ResultFilePath);
            if(csvPath == null)
            {
                return NotFound("找不到DEG CSV");
            }

            var numberFillters = new[]
            {
                ("baseMean", query.BaseMeanMin, query.BaseMeanMax),
                ("log2FoldChange", query.Log2FCMin, query.Log2FCMax),
                ("lfcSE", query.LfcSEMin, query.LfcSEMax),
                ("stat", query.StatMin, query.StatMax),
                ("pvalue", query.PValueMin, query.PValueMax),
                ("padj", query.PadjMin, query.PadjMax)
            };

            foreach(var filter in numberFillters)
            {
                if((filter.Item2.HasValue && 
                    !double.IsFinite(filter.Item2.Value))||
                    (filter.Item3.HasValue &&
                    !double.IsFinite(filter.Item3.Value))||
                    (filter.Item2.HasValue &&
                    filter.Item3.HasValue &&
                    filter.Item2.Value > filter.Item3.Value)){
                    return BadRequest("數值範圍不正確");
                }
            }


            try
            {
                using var csv = CsvReader.FromFile(csvPath);
                var headers = csv.Headers.ToList();
                var headerIndex = headers
                    .Select((name, index) => new { name, index })
                    .ToDictionary(
                    x => x.name,
                    x => x.index,
                    StringComparer.OrdinalIgnoreCase);


                if (!allowdColumns.All(headerIndex.ContainsKey))
                {
                    return BadRequest("CSV 缺少 DEG Explorer 必要欄位");
                }

                var textFilters = new[]
                {
                     ("GeneID", query.GeneID),
                    ("GeneSymbol", query.GeneSymbol),
                    ("GeneName", query.GeneName)
                };

                var matchRows = new List<List<string>>();

                double?GetNumber(List<string>row,string column)
                {
                    string value = row[headerIndex[column]];
                    if(double.TryParse(
                        value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double number) &&
                        double.IsFinite(number))
                    {
                        return number;
                    }
                    return null;
                }

                while (csv.Read())
                {
                    var row = new List<string>();
                    for (int i = 0; i < headers.Count; i++)
                    {
                        row.Add(csv.GetString(i) ?? "");
                    }


                    bool textMatch = textFilters.All(filter => string.IsNullOrWhiteSpace(filter.Item2) ||
                    row[headerIndex[filter.Item1]].Contains(
                        filter.Item2.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (!textMatch)
                        continue;

                    bool numberMatch = true;
                    foreach (var filter in numberFillters)
                    {
                        if (!filter.Item2.HasValue && !filter.Item3.HasValue)
                            continue;

                        double? value = GetNumber(row, filter.Item1);

                        if (!value.HasValue ||
                            (filter.Item2.HasValue &&
                            value.Value < filter.Item2.Value) ||
                            (filter.Item3.HasValue &&
                            value.Value > filter.Item3.Value))
                        {
                            numberMatch = false;
                            break;
                        }
                    }
                        if (numberMatch)
                        {
                            matchRows.Add(row);
                        }

                    }


                    bool descending = query.SortDirection == "desc";

                    bool numericSort = numberFillters.Any(f => string.Equals(f.Item1,
                        query.SortColumn,
                        StringComparison.OrdinalIgnoreCase));

                    int sortIndex = headerIndex[query.SortColumn];

                    if (numericSort)
                    {
                        matchRows.Sort((a, b) =>
                        {
                            double? av = GetNumber(a, query.SortColumn);
                            double? bv = GetNumber(b, query.SortColumn);
                            if (!av.HasValue && !bv.HasValue) return 0;
                            if (!av.HasValue) return 1;
                            if (!bv.HasValue) return -1;

                            int comparison = av.Value.CompareTo(bv.Value);
                            return descending ? -comparison : comparison;
                        });
                    }
                    else
                    {
                        matchRows.Sort((a, b) =>
                        {
                            int comparison = string.Compare(
                                a[sortIndex],
                                b[sortIndex],
                                StringComparison.OrdinalIgnoreCase);

                            return descending ? -comparison : comparison;
                        });
                    }

                if (exportCsv)
                {
                    byte[] csvBytes = BuildFilteredDegCsv(
                headers,
                matchRows);

                    string downloadFileName = $"DEG_filtered_job_{id}.csv";

                    return File(
                                    csvBytes,
                                    "text/csv; charset=utf-8",
                                    downloadFileName);
                }



                    int totalCount = matchRows.Count;
                    long skip = ((long)query.Page - 1) * query.PageSize;

                    var pageRows = skip >= totalCount ? new List<List<string>>()
                        : matchRows
                        .Skip((int)skip)
                        .Take(query.PageSize)
                        .ToList();

                    return Json(new
                    {
                        headers,
                        rows = pageRows,
                        totalCount,
                        page = query.Page,
                        pageSize = query.PageSize,
                        totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
                    });
            }
            catch(IOException)
            {
                return StatusCode(500, "讀取CSV失敗");
            }
            catch(UnauthorizedAccessException)
            {
                return StatusCode(500, "沒有權限讀取CSV");
            }
            catch (CsvFormatException)
            {
                return StatusCode(500, "格式錯誤");
            }
                

            

        }






        [HttpGet]
        public async Task<IActionResult> ResultFile(int id, string type)
        {


            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(UserIdValue, out var userId))
            {
                return Challenge();
            }


            var job = await _context.AnalysisJob
                .AsNoTracking()
                .FirstOrDefaultAsync(
                j => j.AnalysisJobID == id &&
                j.UsersID == userId &&
                j.JobStatus == "Completed" &&
                _context.ResearchProjects
                .Any(p => p.ResearchProjectsID == j.ResearchProjectsID &&
                p.UsersID == userId));



            if (job == null)
            {
                return NotFound();
            }

            var result = await _context.AnalysisResults
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.AnalysisJobID == id);

            if (result == null)
            {
                return NotFound();
            }

            //只允許兩種檔案類型
            string? storedPath;
            string expectedFileName;
            string contentType;

            if (type == "csv")
            {
                storedPath = result.ResultFilePath;
                expectedFileName = "DEG_results.csv";
                contentType = "text/csv";

            }
            else if (type == "png")
            {
                storedPath = result.VolcanPlotPath;
                expectedFileName = "VolcanoPlot.png";
                contentType = "image/png";
            }
            else
            {
                return BadRequest("不支援的檔案類型");
            }

            if (string.IsNullOrWhiteSpace(storedPath) ||
                !Path.IsPathFullyQualified(storedPath))
            {
                return NotFound();
            }

            //建立可信任的 AnalysisJob 結果目錄
            var parent = Directory.GetParent(_env.ContentRootPath);

            if (parent == null)
            {
                return NotFound();
            }

            string resultsRoot = Path.GetFullPath(
                Path.Combine(
                    
                    GetAnalysisStorageRoot(),
                    "AnalysisJobs",    //這是資料夾名稱，不是Controller名稱
                    id.ToString(CultureInfo.InvariantCulture),
                    "Results"));

            string fullPath = Path.GetFullPath(storedPath);

            //確認路徑位於指定 Results 目錄下
            string relativePath = Path.GetRelativePath(resultsRoot, fullPath);

            var parts = relativePath.Split(
                new[]
                {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                }, StringSplitOptions.RemoveEmptyEntries);


            // 必須符合 Results/{執行GUID}/指定結果檔名
            if (parts.Length != 2 ||
                !Guid.TryParseExact(parts[0], "N", out _) ||
                !string.Equals(
                    parts[1],
                    expectedFileName,
                    StringComparison.Ordinal))
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }
            //不讓瀏覽器快取研究結果
            Response.Headers.CacheControl = "private, no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";


            //  PNG 在網頁內顯示，CSV 下載
            if (type == "png")
            {
                return PhysicalFile(fullPath, contentType);

            }






            return PhysicalFile(
                fullPath,
                contentType,
                expectedFileName);
        }



        

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(UserIdValue, out var userId))
            {
                return Challenge();
            }

            var job = await (
                from j in _context.AnalysisJob.AsNoTracking()
                join p in _context.ResearchProjects.AsNoTracking()
                on j.ResearchProjectsID equals p.ResearchProjectsID
                where j.AnalysisJobID == id &&
                j.UsersID == userId &&
                p.UsersID == userId

                select new AnalysisJobDetailsViewModel
                {
                    AnalysisJobID = j.AnalysisJobID,
                    JobName = j.JobName,
                    ProjectName = p.ProjectName,
                    AnalysisMethod = j.AnalysisMethod,
                    JobStatus = j.JobStatus,
                    ControlGroup = j.ControlGroup,
                    TreatmentGroup = j.TreatmentGroup,
                    CreateAt = j.CreateAt,
                    StartAt = j.StartAt,
                    FinishAt = j.FinishAt


                }).FirstOrDefaultAsync();

            if (job == null)
            {
                return NotFound();
            }

            if (job.JobStatus == "Completed")
            {
                var result = await _context.AnalysisResults
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.AnalysisJobID == id);

                if (result != null)
                {
                    job.TotalGenes = result.TotalGenes;
                    job.SignificantGenes = result.SignificantGenes;
                    job.HasResult = true;


                    string? csvPath = GetSafeDegCsvPath(id, result.ResultFilePath);

                    if (csvPath == null)
                    {
                        job.DegPreviewMessage = "找不到DEG結果檔案";
                    }
                    else
                    {
                        try
                        {
                            using var csv = CsvReader.FromFile(csvPath);

                            var headers = csv.Headers;

                            job.DegHeaders = headers
                                .Select((name, index) => 
                                string.IsNullOrWhiteSpace(name) ? $"Column{index + 1}" : name)
                                .ToList();


                            int count = 0;

                            while (csv.Read() && count < 20)
                            {
                                var row = new List<string>();
                                for (int i = 0; i < headers.Count; i++)
                                {
                                    row.Add(csv.GetString(i));
                                }
                                job.DegRows.Add(row);
                                count++;
                            }


                            if (job.DegRows.Count == 0)
                            {
                                job.DegPreviewMessage = "DEG CSV 沒有可預覽的資料";
                            }
                        }
                        catch (IOException)
                        {
                            job.DegPreviewMessage = "目前無法讀取 DEG 結果檔案";
                        }
                        catch (UnauthorizedAccessException)
                        {
                            job.DegPreviewMessage = "伺服器沒有權限讀取結果檔案";
                        }
                        catch (CsvFormatException)
                        {
                            job.DegPreviewMessage = "DEG CSV 格式有誤，無法預覽";
                        }
                    }
                }


}
                return View(job);
                }

            




                [HttpPost]
                [ValidateAntiForgeryToken]
                public async Task<IActionResult> Run(int id)
                {

                    string? UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!int.TryParse(UserIdValue, out var userId))
                    {
                        return Challenge();
                    }

                    var job = await _context.AnalysisJob
                        .AsNoTracking()
                        .FirstOrDefaultAsync(j => j.AnalysisJobID == id && j.UsersID == userId &&
                        _context.ResearchProjects
                        .Any(p => p.ResearchProjectsID == j.ResearchProjectsID && p.UsersID == userId));

                    if (job == null)
                    {
                        return NotFound();
                    }

                    if (job.JobStatus != "Pending")
                    {
                TempData["ErrorMessage"] ="此分析工作目前不可執行。";

                return RedirectToAction(nameof(Index));
            }


                    bool hasCountMatrix = await _context.AnalysisFiles
                        .AnyAsync(f => f.AnalysisJobID == id && f.FileType == AnalysisFileTypes.CountsMatrix);


                    bool hasMetadata = await _context.AnalysisFiles
                        .AnyAsync(f => f.AnalysisJobID == id && f.FileType == AnalysisFileTypes.SampleMetadata);

                    if (!hasCountMatrix || !hasMetadata)
                    {
                        TempData["ErrorMessage"] = "請先上傳 Counts Matrix 與 Sample Metadata。";
                        return RedirectToAction(nameof(Index));
                    }

            // 5. 使用原子更新，避免重複加入佇列
            int changed = await _context.AnalysisJob
                .Where(j =>
                    j.AnalysisJobID == id &&
                    j.UsersID == userId &&
                    j.JobStatus == "Pending" &&
                    _context.ResearchProjects.Any(p =>
                        p.ResearchProjectsID == j.ResearchProjectsID &&
                        p.UsersID == userId))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(j => j.JobStatus, "Queued")
                    .SetProperty(j => j.StartAt, (DateTime?)null)
                    .SetProperty(j => j.FinishAt, (DateTime?)null)
                    .SetProperty(j => j.ErrorMessage, (string?)null));

            if (changed != 1)
            {
                TempData["ErrorMessage"] =
                    "此工作已加入佇列，或已由其他請求處理。";

                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] =
                "分析工作已加入佇列，等待 R Worker 執行。";

            return RedirectToAction(nameof(Index));
        }











                [HttpPost]
                [ValidateAntiForgeryToken]
                [RequestSizeLimit(5 * 1024 * 1024)]
                [RequestFormLimits(MultipartBodyLengthLimit = 5 * 1024 * 1024)]
                public async Task<IActionResult> Upload(int id, AnalysisFilesUploadViewModel model)
                {
                    const long maxFileBytes = 2 * 1024 * 1024;


                    var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!int.TryParse(UserIdValue, out var userId))
                    {
                        return Challenge();
                    }

                    var job = await _context.AnalysisJob
                        .AsNoTracking()
                        .FirstOrDefaultAsync(j => j.AnalysisJobID == id && j.UsersID == userId &&
                        _context.ResearchProjects.Any(p => p.ResearchProjectsID == j.ResearchProjectsID && p.UsersID == userId));


                    if (job == null)
                    {
                        return NotFound();
                    }

                    if (job.JobStatus != "Pending")
                        return BadRequest("此工作已不可上傳檔案");

                    ViewBag.JobName = job.JobName;
                    ViewBag.ControlGroup = job.ControlGroup;
                    ViewBag.TreatmentGroup = job.TreatmentGroup;
                    ViewBag.AnalysisJobID = job.AnalysisJobID;
                    bool hasFile = await _context.AnalysisFiles
                        .AnyAsync(f => f.AnalysisJobID == id);

                    if (hasFile)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "此分析工作已經上傳檔案，請勿重複上傳。");

                        return View(model);

                    }
                    if (!ModelState.IsValid)
                    {
                        return View(model);
                    }


                    IFormFile? countsFiles = model.CountsFiles;
                    IFormFile? metadataFile = model.MetadataFile;

                    if (countsFiles == null || metadataFile == null)
                    {
                        ModelState.AddModelError(string.Empty, "請同時上傳 Counts Matrix 與 Sample Metadata");
                        return View(model);
                    }

                    var uploads = new[]
                    {
                        (File: countsFiles, Type: AnalysisFileTypes.CountsMatrix),
                        (File: metadataFile, Type: AnalysisFileTypes.SampleMetadata)
                    };




                    foreach (var upload in uploads)
                    {
                        IFormFile file = upload.File;
                        if (file.Length == 0 || file.Length > maxFileBytes)
                        {
                            ModelState.AddModelError(
                                string.Empty, "每個 CSV 檔案大小必須大於 0 且不超過 2 MB。");
                        }

                        if (!string.Equals(
                            Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
                        {
                            ModelState.AddModelError(string.Empty, "僅接受 .csv 格式。");
                        }

                        string safeDisplayName = Path.GetFileName(file.FileName.Replace('\\', '/'));

                        if (string.IsNullOrWhiteSpace(safeDisplayName) || safeDisplayName.Length > 255)
                        {
                            ModelState.AddModelError(
                                string.Empty, "原始檔名不合法或過長");
                        }

                    }

                    if (!ModelState.IsValid)
                    {
                        return View(model);
                    }

                    string? validationError = await RNAseqCsvValidator.ValiateAsync(
                        countsFiles!,
                        metadataFile!,
                        job.ControlGroup ?? "",
                        job.TreatmentGroup ?? "");

                    if (validationError != null)
                    {
                        ModelState.AddModelError(
                            string.Empty, validationError);
                        return View(model);
                    }

                    // 6. 建立私有檔案目錄
                    // 使用專案 ContentRoot 的上一層，
                    // 避免檔案存放在 wwwroot。
                    //將檔案放在專案目錄上一層的 BioHubPrivateUploads 資料夾。
                    string parentDirectory = Directory.GetParent(_env.ContentRootPath)!.FullName;

                    string uploadDirectory = Path.Combine(
                        parentDirectory,
                        GetAnalysisStorageRoot(),
                        "AnalysisJobs",
                        job.AnalysisJobID.ToString(CultureInfo.InvariantCulture));

                    var savedPaths = new List<string>();
                    try
                    {
                        Directory.CreateDirectory(uploadDirectory);
                        foreach (var upload in uploads)
                        {
                            var file = upload.File;
                            //使用隨機檔名
                            string storedFileName = $"{Guid.NewGuid():N}.csv";
                            string fullPath = Path.Combine(uploadDirectory, storedFileName);

                            await using (var stream = new FileStream(
                                fullPath,
                                FileMode.CreateNew,
                                FileAccess.Write,
                                FileShare.None
                                ))
                            {
                                savedPaths.Add(fullPath);
                                await file.CopyToAsync(stream);
                            }

                            string originalFileName = Path.GetFileName(
                                file.FileName.Replace('\\', '/'));

                            _context.AnalysisFiles.Add(new AnalysisFiles
                            {
                                AnalysisJobID = job.AnalysisJobID,
                                FileType = upload.Type,
                                OriginalFileName = originalFileName,
                                StoredFileName = storedFileName,
                                FilePath = fullPath,
                                FileSize = file.Length,
                                UploadAt = DateTime.UtcNow,
                            });


                        }
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex) when (
                    ex is IOException ||
                    ex is UnauthorizedAccessException ||
                    ex is DbUpdateException)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);

                        foreach (string path in savedPaths)
                        {
                            try
                            {
                                if (System.IO.File.Exists(path))
                                    System.IO.File.Delete(path);
                            }
                            catch (IOException cleanupEx)
                            {
                                System.Diagnostics.Debug.WriteLine(cleanupEx.Message);
                            }
                            catch (UnauthorizedAccessException cleanupEx)
                            {
                                System.Diagnostics.Debug.WriteLine(cleanupEx.Message);
                            }
                        }

                        ModelState.AddModelError(
                            string.Empty, "檔案上傳或資料庫儲存失敗，請檢查伺服器設定。");
                        return View(model);
                    }

                    TempData["SuccessMessage"] = "Counts Matrix 與 Sample Metadata 上傳成功！";
                    return RedirectToAction(nameof(Index));

                }















                [HttpGet]
                public async Task<IActionResult> Upload(int id)
                {
                    var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!int.TryParse(UserIdValue, out var userId))
                    {
                        return Challenge();
                    }

                    var job = await _context.AnalysisJob
                        .AsNoTracking()
                        .FirstOrDefaultAsync(j => j.AnalysisJobID == id && j.UsersID == userId &&
                        _context.ResearchProjects.Any(p => p.ResearchProjectsID == j.ResearchProjectsID &&
                        p.UsersID == userId));


                    if (job == null)
                    {
                        return NotFound();
                    }

                    if (job.JobStatus != "Pending")
                    {
                        return RedirectToAction(nameof(Index));
                    }

                    bool hasFiles = await _context.AnalysisFiles.AnyAsync(f => f.AnalysisJobID == id);
                    if (hasFiles)
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    ViewBag.JobName = job.JobName;
                    ViewBag.ControlGroup = job.ControlGroup;
                    ViewBag.TreatmentGroup = job.TreatmentGroup;
                    ViewBag.AnalysisJobID = job.AnalysisJobID;
                    return View(new AnalysisFilesUploadViewModel());
                }

                [HttpPost]
                [ValidateAntiForgeryToken]
                public async Task<IActionResult> Create(AnalysisJobCreateViewModel model)
                {
                    var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!int.TryParse(UserIdValue, out var userId))
                    {
                        return Challenge();
                    }



                    // 重新建立選單，避免驗證失敗時下拉選單消失
                    async Task LoadProjectsAsync()
                    {
                        var project = await _context.ResearchProjects
                            .AsNoTracking()
                            .Where(p => p.UsersID == userId)
                            .OrderBy(p => p.ProjectName)
                            .ToListAsync();


                        ViewBag.Projects = new SelectList(
                            project,
                            "ResearchProjectsID",
                            "ProjectName",
                            model.ResearchProjectsID);

                    }
                    if (!ModelState.IsValid) {
                        await LoadProjectsAsync();
                        return View(model);

                    }


                    if (!model.ResearchProjectsID.HasValue)
                    {
                        ModelState.AddModelError(nameof(model.ResearchProjectsID), "請選擇研究計畫");

                        await LoadProjectsAsync();
                        return View(model);
                    }


                    int projectId = model.ResearchProjectsID.Value;

                    // 驗證研究計畫屬於目前使用者
                    bool projectExist = await _context.ResearchProjects
                        .AnyAsync(p => p.ResearchProjectsID == projectId && p.UsersID == userId);

                    if (!projectExist)
                    {
                        return NotFound();
                    }







                    string jobName = model.JobName.Trim() ?? "";
                    string control = model.ControlGroup.Trim() ?? "";
                    string treatment = model.TreatmentGroup.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(jobName))
                        ModelState.AddModelError(nameof(model.JobName), "分析名稱不可為空白");

                    if (string.IsNullOrWhiteSpace(control))
                        ModelState.AddModelError(nameof(model.ControlGroup), "對照組名稱不可為空白");

                    if (string.IsNullOrWhiteSpace(treatment))
                        ModelState.AddModelError(nameof(model.TreatmentGroup), "實驗組不可為空白");

                    if (string.Equals(control, treatment, StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(nameof(model.TreatmentGroup), "實驗組與對照組不能相同");
                    }

                    if(string.IsNullOrWhiteSpace(model.Species) ||
                !AnalysisSpecies.Support.Contains(model.Species))
            {
                ModelState.AddModelError(nameof(model.Species), "請選擇RNA-seq物種");
            }

                    if (!ModelState.IsValid)
                    {
                        await LoadProjectsAsync();
                        return View(model);
                    }

                    var job = new AnalysisJob
                    {
                        ResearchProjectsID = projectId,
                        UsersID = userId,
                        JobName = jobName,
                        //Species = model.Species! 這裡使用 ! 是在前面已經完成 ModelState 與物種白名單檢查後，告訴編譯器此值非 null；但 ! 本身不會驗證資料。
                        Species = model.Species!,
                        AnalysisMethod = "Deseq2",
                        JobStatus = "Pending",
                        ControlGroup = control,
                        TreatmentGroup = treatment,
                        CreateAt = DateTime.UtcNow
                    };

                    _context.AnalysisJob.Add(job);

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.GetBaseException().Message);


                        ModelState.AddModelError(
                            string.Empty, "建立分析工作失敗，請確認資料是否正確。");

                        await LoadProjectsAsync();
                        return View(model);
                    }


                    return RedirectToAction(nameof(Index));
                }

                [HttpGet]
                public async Task<IActionResult> Create()
                {
                    var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!int.TryParse(UserIdValue, out var userId))
                    {
                        return Challenge();
                    }

                    var projects = await _context.ResearchProjects
                        .AsNoTracking()
                        .Where(p => p.UsersID == userId)
                        .OrderBy(p => p.ProjectName)
                        .ToListAsync();

                    ViewBag.Projects = new SelectList(
                        projects,
                        "ResearchProjectsID",
                        "ProjectName"
                        );

                    return View(new AnalysisJobCreateViewModel());
                }

                [HttpGet]
                public async Task<IActionResult> Index(int? researchProjectId)
                {
                    string? UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!int.TryParse(UserIdValue, out var userId))
                    {
                        return Challenge();
                    }

            // AnalysisJobs 與 ResearchProjects 關聯查詢

            if (researchProjectId.HasValue)
            {
                var project = await _context.ResearchProjects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ResearchProjectsID == researchProjectId.Value &&
                    p.UsersID == userId);

                if(project == null)
                {
                    return NotFound();
                }
                ViewBag.FilterProjectName = project.ProjectName;
                ViewBag.ResearchProjectId = project.ResearchProjectsID;

            }


                    var jobs = await (
                        from job in _context.AnalysisJob.AsNoTracking()
                        join project in _context.ResearchProjects.AsNoTracking()
                        on job.ResearchProjectsID
                        equals project.ResearchProjectsID

                        where job.UsersID == userId && project.UsersID == userId

                        && (!researchProjectId.HasValue ||
                                job.ResearchProjectsID == researchProjectId.Value)


                        orderby job.CreateAt descending,
                        job.AnalysisJobID descending

                        select new AnalysisJobListViewModel
                        {
                            AnalysisJobID = job.AnalysisJobID,
                            JobName = job.JobName,
                            ProjectName = project.ProjectName,
                            AnalysisMethod = job.AnalysisMethod,
                            JobStatus = job.JobStatus,
                            CreateAt = job.CreateAt,


                            HasCountMatrix = _context.AnalysisFiles
                            .Any(f => f.AnalysisJobID == job.AnalysisJobID && f.FileType == AnalysisFileTypes.CountsMatrix),

                            HasSampleMetadata = _context.AnalysisFiles
                            .Any(f => f.AnalysisJobID == job.AnalysisJobID && f.FileType == AnalysisFileTypes.SampleMetadata)
                        }
                        ).ToListAsync();
                    return View(jobs);

                }


            }
        }  
