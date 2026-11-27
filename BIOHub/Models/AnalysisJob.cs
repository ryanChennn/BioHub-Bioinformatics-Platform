using System.ComponentModel.DataAnnotations;

namespace BIOHub.Models
{
    public class AnalysisJob
    {
        [Key]
        public int AnalysisJobID { get; set; }

        [Required]
        public int ResearchProjectsID { get; set; }

        //確認該計畫屬於登入者，避免使用者任意指定別人的計畫
        [Required]
        public int UsersID { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name ="分析工作名稱")]
        public string JobName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string? Species { get; set; } 

        [Required]
        [StringLength(50)]
        public string AnalysisMethod { get; set; } = "Deseq2";

        [Required]
        [StringLength(20)]
        public string JobStatus { get; set; } = "Pending";

        [StringLength(100)]
        public string? ControlGroup { get; set; }

        [StringLength(100)]
        public string? TreatmentGroup { get; set; }

        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartAt { get; set; }

        public DateTime? FinishAt { get; set; }

        [StringLength(2000)]
        public string? ErrorMessage { get; set; }
    }
}
