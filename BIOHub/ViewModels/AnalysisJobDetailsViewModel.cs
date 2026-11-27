namespace BIOHub.ViewModels
{
    public class AnalysisJobDetailsViewModel
    {
        public int AnalysisJobID { get; set; }
        public string JobName { get; set; } = string.Empty;
        public string ProjectName {  get; set; } = string.Empty;
        public string AnalysisMethod { get; set; } = string.Empty;
        public string JobStatus { get; set; } = string.Empty;
        public string? ControlGroup { get; set; }

        public string? TreatmentGroup { get; set; }

        public DateTime CreateAt { get; set; } 

        public DateTime? StartAt { get; set; }

        public DateTime? FinishAt { get; set; }

        public int? TotalGenes { get; set; }
        public int? SignificantGenes { get; set; }
        public bool HasResult { get; set; }


        public List<string> DegHeaders { get; set; } = new();
        public List<List<string>>DegRows { get; set; } = new();
        public string?DegPreviewMessage {  get; set; }
    }
}
