using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIOHub.ViewModels
{
    public class SurvivalDataCreateViewModel
    {
        [Display(Name = "整體存活時間（月）")]
        [Range(typeof(decimal), "0", "9999.99")]
        public decimal? OS_Month { get; set; }

        [Display(Name = "OS 事件狀態")]
        [Range(0, 1, ErrorMessage = "OS Status 只能輸入 0 或 1")]
        public byte? OS_Status { get; set; }

        [Display(Name = "無惡化存活時間（月）")]
        [Range(typeof(decimal), "0", "9999.99")]
        public decimal? PFS_Month { get; set; }

        [Display(Name = "PFS 事件狀態")]
        [Range(0, 1, ErrorMessage = "PFS Status 只能輸入 0 或 1")]
        public byte? PFS_Status { get; set; }

        [Display(Name = "最後追蹤日期")]
        [DataType(DataType.Date)]
        public DateTime? LastFollowUpDate { get; set; }

    }
}
