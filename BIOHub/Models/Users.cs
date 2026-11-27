using System.ComponentModel.DataAnnotations;

namespace BIOHub.Models
{
    public class Users
    {
        public int UsersID { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name ="使用者名稱")]
        public string UserName { get; set; } = string.Empty;

        [Required,StringLength(500),Display(Name = "密碼雜湊")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, StringLength(100),Display(Name ="姓名")]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(20),Display(Name ="電話")]
        public string Phone {  get; set; } =string.Empty;

        [Required, StringLength(100),Display(Name ="電子郵件")]
        public string Email { get; set; } = string.Empty;

        [StringLength(100), Display(Name ="所屬單位")]
        public string? Organization { get; set; }

        [Required, Display(Name ="創立時間")]
        public DateTime CreateAt { get; set; }= DateTime.Now;

        [Display(Name ="最後上線時間")]
        public DateTime? LastAt { get; set; }

        [Display(Name ="帳號狀態")]
        public bool IsActive { get; set; } = true;

        public ICollection<ResearchProjects> ResearchProjects {  get; set; } = new List<ResearchProjects>();

    }
}
