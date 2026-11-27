using System.ComponentModel.DataAnnotations;

namespace BIOHub.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "請輸入帳號")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage ="請輸入密碼"), DataType(DataType.Password)]
        public string Password { get; set; }= string.Empty;

        public bool RememberMe { get; set; }
    }
}
