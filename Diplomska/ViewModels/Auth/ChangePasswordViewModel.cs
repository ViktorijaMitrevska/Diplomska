using System.ComponentModel.DataAnnotations;

namespace Diplomska.ViewModels.Auth
{
    public class ChangePasswordViewModel
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }
    }
}
