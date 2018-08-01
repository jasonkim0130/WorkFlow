using System.ComponentModel.DataAnnotations;
namespace WorkFlow.Models
{
    public class LogOnViewModel
    {
        [Required(ErrorMessage = "*")]
        [Display(Name = "用戶名")]
        public string Username { get; set; }

        [Required(ErrorMessage = "*")]
        [Display(Name = "密碼")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}