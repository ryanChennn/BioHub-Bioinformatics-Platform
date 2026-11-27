using System.ComponentModel.DataAnnotations;

namespace BIOHub.ViewModels
{
    public class ClinicalDataCreateViewModel
    {
        public int ClinicalDataID { get; set; }

        [Required(ErrorMessage ="請輸入臨床診斷")]
        [StringLength(255)]
        [Display(Name = "臨床診斷")]
        public string Diagnosis { get; set; } = string.Empty;

        [Required(ErrorMessage ="請輸入疾病分期")]
        [StringLength(50)]
        [Display(Name = "疾病分期")]
        public string Stage { get; set; } = string.Empty;


        [StringLength(255)]
        [Display(Name = "治療方式")]
        public string Treatments { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "治療反應")]
        public string Responses { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "備註")]
        public string? Note { get; set; }

        
    }
}
