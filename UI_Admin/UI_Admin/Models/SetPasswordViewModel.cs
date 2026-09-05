using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models
{
    public class SetPasswordViewModel
    {

        [Required(ErrorMessage = "Link thiết lập mật khẩu không hợp lệ hoặc đã hết hạn.")]
        public string AccessToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-zA-Z])(?=.*\d)(?=.*[^a-zA-Z0-9\s])\S{6,}$",
            ErrorMessage = "Mật khẩu phải có chữ, số, ít nhất 1 chữ in hoa và 1 ký tự đặc biệt.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
