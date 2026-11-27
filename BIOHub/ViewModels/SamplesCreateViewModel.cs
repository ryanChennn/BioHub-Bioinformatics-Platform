using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIOHub.ViewModels
{
    public class SamplesCreateViewModel
    {
        [Required(ErrorMessage ="請輸入檢體編號")]
        [StringLength(100)]
        [Display(Name = "檢體編號")]
        public string SampleCode { get; set; } = string.Empty;

        [Required(ErrorMessage ="請輸入採集日期")]
        [DataType(DataType.Date)]
        [Display(Name = "採集日期")]
        public DateTime? CollectionDate { get; set; }


        [Required(ErrorMessage ="請輸入溫度")]
        [Range(typeof(decimal), "-200", "100", ErrorMessage = "儲存溫度必須介於 -200°C 至 100°C")]
        [Display(Name = "儲存溫度（°C）")]
        public decimal? StorageTemperature { get; set; }

        [StringLength(100)]
        [Display(Name = "檢體狀態")]
        public string? SampleStatus { get; set; }

        [StringLength(255)]
        [Display(Name = "備註")]
        public string? Remarks { get; set; }
    }
}
