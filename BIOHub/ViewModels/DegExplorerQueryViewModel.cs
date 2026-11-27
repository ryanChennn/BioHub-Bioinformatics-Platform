namespace BIOHub.ViewModels
{

    //這個類別是用來接收瀏覽器傳給後端的篩選條件。
    public class DegExplorerQueryViewModel
    {
        public string? GeneID { get; set; }

        public string? GeneSymbol { get; set; }
        public string? GeneName { get; set; }

        public double? StatMin { get; set; }
        public double? StatMax { get; set; }


        public double? BaseMeanMin { get; set; }
        public double? BaseMeanMax { get; set; }


        public double? Log2FCMin { get; set; }
        public double? Log2FCMax { get; set; }

        public double? LfcSEMin { get; set; }
        public double? LfcSEMax { get; set; }

        public double? PValueMin { get; set; }
        public double? PValueMax { get; set; }

        public double? PadjMin { get; set; }

        public double? PadjMax { get; set; }

        public string SortColumn { get; set; } = "padj";
        public string SortDirection { get; set; } = "asc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;






    }
}
