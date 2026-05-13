using System.ComponentModel.DataAnnotations;

namespace dotnet_mvc.ViewModels.Auth;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Indica o teu email.")]
    [EmailAddress(ErrorMessage = "Introduz um email válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    public bool IsConfirmationDisplayed { get; set; }
}
