using System.ComponentModel.DataAnnotations;

namespace dotnet_mvc.ViewModels.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Indica o teu email.")]
    [EmailAddress(ErrorMessage = "Introduz um email válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica a tua password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Manter sessão iniciada")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
