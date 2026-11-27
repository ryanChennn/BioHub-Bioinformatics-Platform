using System.ComponentModel.DataAnnotations;

namespace BIOHub.Models
{
    public class AnalysisResults
    {
        [Key]
        public int AnalysisResultID { get; set; }

        [Required]
        public int AnalysisJobID { get; set; }

        [StringLength(1000)]
        public string? ResultFilePath { get; set; }

        public string? VolcanPlotPath { get; set; }

        public int? TotalGenes {  get; set; }

        public int?SignificantGenes { get; set; }

        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
    }
}
