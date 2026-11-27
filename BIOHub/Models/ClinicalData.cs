using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIOHub.Models
{
    public class ClinicalData
    {
        public int ClinicalDataID { get; set; }

        [Required]
        public int SubjectsID { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "臨床診斷")]
        public string Diagnosis { get; set; } = string.Empty;

        [Required]
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

        [StringLength(100)]
        [Display(Name = "建立者")]
        public string CreateBy { get; set; } = string.Empty;


        public DateTime CreateAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(SubjectsID))]
        public Subjects? Subjects { get; set; }
    }
}
