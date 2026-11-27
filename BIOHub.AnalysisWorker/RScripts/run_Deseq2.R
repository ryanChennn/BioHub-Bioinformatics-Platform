
# 隱藏載入套件時的啟動提示訊息。
# 1. 載入 DESeq2
suppressPackageStartupMessages(library(DESeq2))

# 2. 取得 C# 傳入的參數
args <- commandArgs(trailingOnly = TRUE)

# 現在必須有六個參數
if (length(args) != 6) {
  stop(
    paste0(
      "需要六個參數：",
      "counts、metadata、output、",
      "control、treatment、species。",
      "實際收到：", length(args), " 個"
    )
  )
}

# 3. 依順序取得參數
counts_path     <- args[1]
metadata_path   <- args[2]
output_dir      <- args[3]
control_group   <- args[4]
treatment_group <- args[5]
species         <- args[6]

# 4. 輸出偵錯資訊
message("RNA-seq Species: ", species)

# 5. 根據物種選擇註解資料庫
annotation_package <- switch(
  species,
  "Human" = "org.Hs.eg.db",
  "Mouse" = "org.Mm.eg.db",
  "Rat"   = "org.Rn.eg.db",
  stop("Unsupported species: ", species)
)

# 6. 確認套件是否存在
if (!requireNamespace(
    "AnnotationDbi",
    quietly = TRUE)) {
  stop("AnnotationDbi 尚未安裝")
}

if (!requireNamespace(
    annotation_package,
    quietly = TRUE)) {
  stop(
    "Annotation package not installed: ",
    annotation_package
  )
}

# 7. 載入註解資料庫
org_db <- getExportedValue(
  annotation_package,
  annotation_package
)

message(
  "Annotation package: ",
  annotation_package
)

if (control_group == treatment_group) {
  stop("對照組與實驗組不能相同")
}

if (!file.exists(counts_path) || !file.exists(metadata_path)) {
  stop("找不到上傳的 CSV 檔案")
}

# 1. 讀取資料
counts_df <- read.csv(
  counts_path,
  check.names = FALSE,
  stringsAsFactors = FALSE,
  fileEncoding = "UTF-8"
)

metadata <- read.csv(
  metadata_path,
  check.names = FALSE,
  stringsAsFactors = FALSE,
  fileEncoding = "UTF-8"
)

if (!identical(names(metadata), c("SampleID", "Group"))) {
  stop("Metadata 欄位必須是 SampleID,Group")
}

if (ncol(counts_df) < 5 || names(counts_df)[1] != "GeneID") {
  stop("Counts 格式錯誤，需有 GeneID 和至少四個樣本")
}

if (anyNA(counts_df) || anyNA(metadata)) {
  stop("CSV 不可有缺失值")
}

if (anyDuplicated(counts_df$GeneID) ||
    anyDuplicated(metadata$SampleID)) {
  stop("GeneID 或 SampleID 不可重複")
}

# 2. 建立 Counts Matrix
gene_ids <- counts_df$GeneID

counts_matrix <- as.matrix(counts_df[, -1, drop = FALSE])

if (!setequal(colnames(counts_matrix), metadata$SampleID)) {
  stop("Counts 和 Metadata 的樣本名稱不一致")
}

# 依 Counts Matrix 欄位順序排列 Metadata
metadata <- metadata[
  match(colnames(counts_matrix), metadata$SampleID),
  ,
  drop = FALSE
]

if (!all(metadata$Group %in% c(control_group, treatment_group))) {
  stop("Metadata 包含不符合 AnalysisJob 設定的分組")
}

if (sum(metadata$Group == control_group) < 2 ||
    sum(metadata$Group == treatment_group) < 2) {
  stop("每組至少需要兩個樣本")
}

# 確保矩陣是非負整數
raw_values <- suppressWarnings(
  matrix(
    as.numeric(counts_matrix),
    nrow = nrow(counts_matrix),
    dimnames = dimnames(counts_matrix)
  )
)

if (anyNA(raw_values) ||
    any(!is.finite(raw_values)) ||
    any(raw_values < 0) ||
    any(raw_values != floor(raw_values))) {
  stop("Counts 必須是非負整數")
}

if (any(raw_values > .Machine$integer.max)) {
  stop("Counts 數值超出 R 整數範圍")
}

storage.mode(raw_values) <- "integer"
rownames(raw_values) <- gene_ids

# 移除所有樣本 counts 都是零的基因
raw_values <- raw_values[rowSums(raw_values) > 0, , drop = FALSE]

if (nrow(raw_values) < 10) {
  stop("有效基因數量不足，請使用較完整的測試資料集")
}

# 3. 建立分組
metadata$Group <- factor(
  metadata$Group,
  levels = c(control_group, treatment_group)
)

rownames(metadata) <- metadata$SampleID

# 4. DESeq2 差異表現分析
dds <- DESeqDataSetFromMatrix(
  countData = raw_values,
  colData = metadata,
  design = ~ Group
)

dds <- DESeq(dds, quiet = TRUE)

res <- results(
  dds,
  contrast = c("Group", treatment_group, control_group)
)

# 5. 整理結果
result_df <- as.data.frame(res)
result_df$GeneID <- rownames(result_df)

ensembl_ids <- sub(
  "\\.[0-9]+$",
  "",
  result_df$GeneID
)

# 取得註解資料庫支援的 Ensembl ID
valid_ids <- intersect(
  unique(ensembl_ids),
  AnnotationDbi::keys(org_db, keytype = "ENSEMBL")
)

# 預設為 NA，確保未註解的基因也能保留
result_df$GeneSymbol <- NA_character_
result_df$GeneName <- NA_character_

if (length(valid_ids) > 0) {

  symbol_map <- AnnotationDbi::mapIds(
    org_db,
    keys = valid_ids,
    column = "SYMBOL",
    keytype = "ENSEMBL",
    multiVals = "first"
  )

  name_map <- AnnotationDbi::mapIds(
    org_db,
    keys = valid_ids,
    column = "GENENAME",
    keytype = "ENSEMBL",
    multiVals = "first"
  )

  result_df$GeneSymbol <- unname(
    symbol_map[ensembl_ids]
  )

  result_df$GeneName <- unname(
    name_map[ensembl_ids]
  )
}

message(
  "Annotated genes: ",
  sum(!is.na(result_df$GeneSymbol)),
  "/",
  nrow(result_df)
)


result_df <- result_df[
  ,
  c(
    "GeneID",
    "GeneSymbol",
    "GeneName",
    "baseMean",
    "log2FoldChange",
    "lfcSE",
    "stat",
    "pvalue",
    "padj"
  )
]

result_df <- result_df[
  order(result_df$padj, na.last = TRUE),
  ,
  drop = FALSE
]

dir.create(output_dir, recursive = TRUE, showWarnings = FALSE)

result_path <- file.path(output_dir, "DEG_results.csv")
plot_path <- file.path(output_dir, "VolcanoPlot.png")

write.csv(
  result_df,
  result_path,
  row.names = FALSE,
  na = "NA"
)

# 6. 火山圖（Base R，不需要 ggplot2）
valid <- is.finite(result_df$log2FoldChange) &
         is.finite(result_df$pvalue) &
         result_df$pvalue >= 0

x <- result_df$log2FoldChange[valid]
y <- -log10(pmax(result_df$pvalue[valid], 1e-300))

significant <- !is.na(result_df$padj[valid]) &
  result_df$padj[valid] < 0.05 &
  abs(x) >= 1

png(plot_path, width = 1000, height = 750, res = 120)

if (length(x) > 0) {
  plot(
    x, y,
    pch = 16,
    col = ifelse(significant, "#D9534F", "#A0A0A0"),
    xlab = "log2 Fold Change (Treatment / Control)",
    ylab = "-log10(p-value)",
    main = "RNA-seq Volcano Plot"
  )

  abline(v = c(-1, 1), lty = 2, col = "gray50")
  abline(h = -log10(0.05), lty = 2, col = "gray50")

  legend(
    "topright",
    legend = c("Significant (padj < 0.05, |log2FC| >= 1)",
               "Other genes"),
    col = c("#D9534F", "#A0A0A0"),
    pch = 16,
    bty = "n",
    cex = 0.7
  )
} else {
  plot.new()
  text(0.5, 0.5, "No valid p-values for Volcano Plot")
}

dev.off()

# 7. 輸出分析摘要
total_genes <- nrow(result_df)

significant_genes <- sum(
  !is.na(result_df$padj) &
  result_df$padj < 0.05 &
  !is.na(result_df$log2FoldChange) &
  abs(result_df$log2FoldChange) >= 1
)

cat("STATUS=Completed\n")
cat(paste0("TOTAL_GENES=", total_genes, "\n"))
cat(paste0("SIGNIFICANT_GENES=", significant_genes, "\n"))
cat(paste0("RESULT_FILE=", result_path, "\n"))
cat(paste0("PLOT_FILE=", plot_path, "\n"))

