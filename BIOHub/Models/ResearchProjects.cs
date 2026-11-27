using System.ComponentModel.DataAnnotations;

namespace BIOHub.Models
{
    public class ResearchProjects
    {
        public int ResearchProjectsID { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "專案編號")]
        public string ProjectCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "專案名稱")]
        public string ProjectName { get; set; } = string.Empty;


        [Display(Name = "計畫說明")]
        public string? Description { get; set; }

        [StringLength(100)]
        [Display(Name = "計畫主持人")]
        public string? PrincipalInvestigator { get; set; }

        [StringLength(100)]
        [Display(Name = "研究機構")]
        public string? Institution { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "開始日期")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "結束日期")]
        public DateTime? EndDate { get; set; }

        [StringLength(30)]
        [Display(Name = "計畫狀態")]
        public string? ProjectStatus { get; set; } = "Active";

        [Required]
        [StringLength(100)]
        [Display(Name = "申請人")]
        public string CreatedBy { get; set; } = string.Empty;

        // 使用者外鍵，可空，暫時兼容既有資料
        public int? UsersID {  get; set; }

        // 導覽屬性：研究計畫的建立者
        public Users? User {  get; set; }


        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;

        // Navigation Property
        public ICollection<Subjects> Subjects { get; set; }
            = new List<Subjects>();

    }
}
