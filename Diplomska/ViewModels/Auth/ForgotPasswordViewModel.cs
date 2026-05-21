using System.ComponentModel.DataAnnotations;

namespace Diplomska.ViewModels.Auth
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email е задолжително")]
        [EmailAddress(ErrorMessage = "Невалиден email")]
        public string Email { get; set; }
    }
}
