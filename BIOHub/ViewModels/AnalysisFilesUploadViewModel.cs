using System.ComponentModel.DataAnnotations;

namespace BIOHub.ViewModels
{
    public class AnalysisFilesUploadViewModel
    {
        [Required(ErrorMessage ="請上傳 Counts Matrix")]
        [Display(Name ="Counts Matrix")]
        public IFormFile?CountsFiles { get; set; }

        [Required(ErrorMessage = "請上傳 Sample Metadata")]
        [Display(Name = "Sample Metadata")]
        public IFormFile?MetadataFile { get; set; }
    }
}
