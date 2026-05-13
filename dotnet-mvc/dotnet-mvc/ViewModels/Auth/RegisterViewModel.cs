using System.ComponentModel.DataAnnotations;
using dotnet_mvc.Security;

namespace dotnet_mvc.ViewModels.Auth;

public class RegisterViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Indica o teu nome.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 120 caracteres.")]
    [Display(Name = "Nome completo")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica o teu email.")]
    [EmailAddress(ErrorMessage = "Introduz um email válido.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica uma password.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "A password deve ter entre 8 e 128 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma a tua password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "As passwords não coincidem.")]
    [Display(Name = "Confirmar password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleciona o teu perfil.")]
    [Display(Name = "Perfil")]
    public string Role { get; set; } = UserRoles.Student;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Tens de aceitar os termos de utilização.")]
    public bool Terms { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Role) &&
            Role != UserRoles.Student &&
            Role != UserRoles.Professor)
        {
            yield return new ValidationResult("O perfil selecionado não é válido.", [nameof(Role)]);
        }
    }
}
