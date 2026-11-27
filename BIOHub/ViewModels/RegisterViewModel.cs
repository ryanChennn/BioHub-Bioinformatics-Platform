using System.ComponentModel.DataAnnotations;

namespace BIOHub.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "請輸入帳號"), StringLength(100, MinimumLength = 5)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入Email"), EmailAddress(ErrorMessage = "Email格式錯誤")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage ="請輸入名稱"),StringLength(100)]
        public string FullName {  get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入電話號碼"), StringLength(100)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入密碼"), MinLength(10, ErrorMessage = "密碼至少需要10個字元"),
            DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage ="請再次輸入密碼"), Compare("Password", ErrorMessage ="兩次密碼不一致"),
            DataType(DataType.Password)]
        public string ConfirmPassword {  get; set; } = string.Empty;

    }
}
