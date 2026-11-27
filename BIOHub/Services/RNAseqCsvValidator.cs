using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BIOHub.Services
{
    public static class RNAseqCsvValidator
    {
        private static StringComparer Comparer = StringComparer.Ordinal;
        public static async Task<string?>ValiateAsync (
            IFormFile countsFile,
            IFormFile metadataFile, 
            string controlGroup,
            string treatmentGroup )
        {
            try
            {
                var sampleIds = new HashSet<string>(Comparer);
                var geneIds = new HashSet<string>(Comparer);

                //驗證Counts Matrix
                using (var stream = countsFile.OpenReadStream())
                using (var reader = new StreamReader(
                    stream,
                    new UTF8Encoding(false, true),
                    detectEncodingFromByteOrderMarks: false))
                {
                    string? header = await reader.ReadLineAsync();
                    if (header == null || header.Contains('"'))
                        return "Counts Matrix標題格式不正確。";

                    var colums = header.Split(',');

                    if (colums.Length < 5 || colums.Length > 51 || colums[0] != "GeneID")
                    {
                        return "Counts Matrix 第一欄必須為 GeneID，且需有 4 至 50 個樣本。";
                    }
                    foreach (var name in colums.Skip(1))
                    {
                        if (string.IsNullOrWhiteSpace(name) ||
                            name != name.Trim() ||
                            !sampleIds.Add(name))
                        {

                            return "Counts Matrix 的樣本名稱不可空白、重複或包含前後空白。";
                        }

                    }

                    int geneCount = 0;
                    string? line;

                    while ((line = await reader.ReadLineAsync() )!= null)
                    {
                        if (string.IsNullOrWhiteSpace(line) ||
                            line.Contains('"'))
                        {
                            return "Counts Matrix 含有空白列或不支援的引號格式。";
                        }

                        var values = line.Split(",");
                        if (values.Length != colums.Length)
                            return "Counts Matrix 每列欄位數必須相同。";

                        string geneId = values[0];

                        if (string.IsNullOrWhiteSpace(geneId) ||
                            geneId != geneId.Trim() ||
                            !geneIds.Add(geneId))
                        {
                            return "GeneID 不可空白、重複或包含前後空白。";
                        }

                        foreach (var count in values.Skip(1))
                        {
                            if (!long.TryParse(count, System.Globalization.NumberStyles.None,
                                CultureInfo.InvariantCulture,
                                out long value) || value < 0)
                            {
                                return "Counts 必須是非負整數，不可有小數、負數或缺失值。";
                            }
                        }

                        geneCount++;

                        if (geneCount > 60000)
                            return "MVP 最多接受 60,000 個基因。";
                    }

                    if (geneCount < 2)
                        return "Counts Matrix 至少需要兩列基因資料。";
                }

                //驗證sample metadata
                var metadataSamples = new HashSet<string>(Comparer);
                int controlCount = 0;
                int treatmentCount = 0;

                using (var stream = metadataFile.OpenReadStream())
                using (var reader = new StreamReader(stream,
                        new UTF8Encoding(false, true),
                        detectEncodingFromByteOrderMarks: false))
                {
                    var header = await reader.ReadLineAsync();
                    if (header != "SampleID,Group")
                        return "Metadata 標題必須是 SampleID,Group。";
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line) ||
                            line.Contains('"'))
                        { return "Metadata 包含空白列或不支援的格式。"; }

                        var values = line.Split(',');
                        if (values.Length != 2)
                            return "Metadata 每列只能有 SampleID 和 Group。";


                        string sampleId = values[0];
                        string group = values[1];

                        if (string.IsNullOrWhiteSpace(sampleId) ||
                            sampleId != sampleId.Trim() ||
                            !metadataSamples.Add(sampleId))
                        {
                            return "Metadata 的 SampleID 不可空白或重複。";
                        }

                        if (!sampleIds.Contains(sampleId))
                            return $"Metadata 樣本 {sampleId} 不存在於 Counts Matrix。";

                        if (group == controlGroup)
                            controlCount++;
                        else if (group == treatmentGroup)
                            treatmentCount++;
                        else
                            return "Metadata 出現未設定的分析組別。";
                        if (metadataSamples.Count > 100)
                            return "樣本數超過 MVP 上限。";

                    }
                }
                //比對Counts and Metadata的所有樣本
                if (!sampleIds.SetEquals(metadataSamples))
                    return "Counts Matrix 與 Metadata 的樣本名稱不完全一致。";

                if (controlCount < 0 || treatmentCount < 2)
                    return "Control 和 Treatment 各至少需要兩個樣本。";


                return null;  //驗證成功
            }
            catch (DbUpdateException)
            {
                return "CSV 必須使用 UTF-8 編碼。";
            }
            catch (IOException)
            {
                return "無法讀取上傳檔案。";
            }
         }
    }  
}
