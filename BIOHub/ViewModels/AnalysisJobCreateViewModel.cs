using System.ComponentModel.DataAnnotations;

namespace BIOHub.ViewModels
{
    public class AnalysisJobCreateViewModel
    {
        [Required(ErrorMessage ="請選擇研究計畫")]
        [Display(Name ="研究計畫")]
        public int ?ResearchProjectsID { get; set; }

        [Required(ErrorMessage = "請輸入分析名稱")]
        [Display(Name = "分析工作")]
        [StringLength(100)]
        public string JobName {  get; set; } = string.Empty;


        [Required(ErrorMessage ="請選擇RNA-seq物種")]
        public string?Species {  get; set; }

        [Required(ErrorMessage = "請輸入對照組名稱")]
        [Display(Name = "對照組")]
        [StringLength(100)]
        public string ControlGroup { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入實驗組名稱")]
        [Display(Name = "實驗組")]
        [StringLength(100)]
        public string TreatmentGroup { get; set; } = string.Empty;
    }
}
