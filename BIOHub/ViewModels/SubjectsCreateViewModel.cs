using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIOHub.ViewModels
{
    public class SubjectsCreateViewModel
    {
        [Required(ErrorMessage ="請輸入受試者編號")]
        [StringLength(50)]
        [Display(Name = "受試者編號")]
        public string SubjectCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇性別")]
        [StringLength(20)]
        [Display(Name = "性別")]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [Display(Name = "年齡")]
        [Range(0, 120, ErrorMessage = "年齡須介於0-120歲")]
        public int? Age { get; set; }

        [Required(ErrorMessage ="請輸入出生年月日")]
        [DataType(DataType.Date)]
        [Display(Name = "生日")]
        public DateTime? Birth { get; set; }


        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "收案日期")]
        public DateTime? EnrollmentDate { get; set; }


        [Required]
        [StringLength(20)]
        [Display(Name = "受試者狀態")]
        public string Status { get; set; } = string.Empty;


       }
}
