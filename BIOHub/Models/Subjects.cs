using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIOHub.Models
{
    public class Subjects
    {
        public int SubjectsID { get; set; }

        [Required]
        public int ResearchProjectsID { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "受試者編號")]
        public string SubjectCode { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "性別")]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [Display(Name = "年齡")]
        [Range(0, 120, ErrorMessage = "年齡須介於0-120歲")]
        public int ?Age { get; set; } 

        [Required]
        [Column(TypeName = "date")]
        [DataType(DataType.Date)]
        [Display(Name = "生日")]
        public DateTime Birth { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "收案日期")]
        public DateTime EnrollmentDate { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "受試者狀態")]
        public string Status { get; set; } = string.Empty;

        
        [Range(0, 1, ErrorMessage = "OS Status 只能輸入 0 或 1")]
        [Display(Name = "OS Status")]
        public byte? OS_status { get; set; }


        [Column(TypeName = "decimal(6,2)")]
        [Range(0, 9999)]
        [Display(Name = "OS Month")]
        public decimal? OS_Month { get; set; }

        
        [Range(0, 1, ErrorMessage = "PFS Status 只能輸入 0 或 1")]
        [Display(Name = "PFS Status")]
        public byte? PFS_status { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        [Range(0, 9999)]
        [Display(Name = "PFS Month")]
        public decimal? PSF_Month { get; set; }

        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ResearchProjectsID))]
        public ResearchProjects? ResearchProjects { get; set; }

        public ICollection<ClinicalData> ClinicalData { get; set; }
            = new List<ClinicalData>();

        public ICollection<Samples>Samples { get; set; } = new List<Samples>(); 

        

    }
}
