using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIOHub.Models
{
    public class Samples
    {
        public int SamplesID { get; set; }

        [Required]
        public int SubjectsID { get; set; }


        [Required]
        [StringLength(100)]
        [Display(Name = "檢體編號")]
        public string SampleCode { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "採集日期")]
        public DateTime CollectionDate { get; set; }


        [Column(TypeName = "decimal(5,1)")]
        [Range(typeof(decimal), "-200", "100", ErrorMessage = "儲存溫度必須介於 -200°C 至 100°C")]
        [Display(Name = "儲存溫度")]
        public decimal StorageTemperature { get; set; }

        [StringLength(100)]
        [Display(Name = "檢體狀態")]
        public string? SampleStatus { get; set; }

        [StringLength(255)]
        [Display(Name = "備註")]
        public string? Remarks { get; set; }


        public DateTime CreateAt { get; set; } = DateTime.Now;


        [ForeignKey(nameof(SubjectsID))]
        public Subjects Subjects { get; set; } = null!;


    }
}
