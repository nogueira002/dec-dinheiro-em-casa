using System.ComponentModel.DataAnnotations;

namespace dotnet_mvc.ViewModels.Student;

public class ProfileViewModel : IValidatableObject
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 120 caracteres.")]
    [Display(Name = "Nome completo")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Introduz um email válido.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Género")]
    public string Gender { get; set; } = string.Empty;

    [Display(Name = "Data de nascimento")]
    public DateOnly? BirthDate { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Nova password")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "As passwords não coincidem.")]
    [Display(Name = "Confirmar nova password")]
    public string? ConfirmNewPassword { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(NewPassword) && NewPassword.Length < 8)
            yield return new ValidationResult(
                "A nova password deve ter pelo menos 8 caracteres.",
                [nameof(NewPassword)]);
    }
}