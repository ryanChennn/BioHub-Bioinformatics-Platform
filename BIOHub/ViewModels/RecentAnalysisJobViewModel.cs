namespace BIOHub.ViewModels
{
    public class RecentAnalysisJobViewModel
    {
        public int AnalysisJobID { get; set; }
        public string JobName { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string? Species { get; set; } 
        public string JobStatus { get; set; } = "";
        public DateTime CreateAt { get; set; }
    }
}
