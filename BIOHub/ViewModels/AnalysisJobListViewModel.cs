namespace BIOHub.ViewModels
{
    public class AnalysisJobListViewModel
    {
        public int AnalysisJobID { get; set; }
        public string JobName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string AnalysisMethod { get; set; } = string.Empty;
        public string JobStatus { get; set; }= string.Empty;

        public bool HasCountMatrix {  get; set; }
        public bool HasSampleMetadata { get; set; } 
        public bool HasFiles => HasCountMatrix && HasSampleMetadata;
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
    }
}
