using System.ComponentModel.DataAnnotations;

namespace BIOHub.Models
{
    public class AnalysisFiles
    {


        [Key]
        public int AalysisFileID { get; set; }

        [Required]
        public int AnalysisJobID { get; set; }


        [Required]
        [StringLength(50)]
        public string FileType { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public DateTime UploadAt { get; set; } = DateTime.UtcNow;



    }
}
