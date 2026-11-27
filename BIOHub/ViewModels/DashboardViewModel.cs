namespace BIOHub.ViewModels
{
    public class DashboardViewModel
    {

        public int TotalProjects { get; set; }
        public int TotalSubjects {  get; set; }
        public int TotalSamples { get; set; }
        public int TotalClinicalData { get; set; }
        public int TotalSurvivalData { get; set; }
       
        //RNAseq Analysis job
        public int TotalAnalysisJobs { get; set; }
        public int PendingJobs { get; set; }
        public int RunningJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int FailedJobs { get; set; }

        public List<RecentAnalysisJobViewModel> RecentAnalysisJobs { get; set; }
        = new();

        public List<RecentProjectsViewModel> RecentProjects { get; set; } 
            = new ();

    }


    public class RecentProjectsViewModel
    {
        public int ResearchProjectsID { get; set; }

        public string ProjectCode { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

       

        public string? ProjectStatus { get; set; }

        public DateTime CreateAt { get; set; }
    }
}
