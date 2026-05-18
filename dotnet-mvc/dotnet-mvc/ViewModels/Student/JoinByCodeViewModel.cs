using System.ComponentModel.DataAnnotations;

namespace dotnet_mvc.ViewModels.Student;

public class JoinByCodeViewModel
{
    [Required(ErrorMessage = "Indica o código de adesão.")]
    [StringLength(16, MinimumLength = 5, ErrorMessage = "O código de adesão parece inválido.")]
    [RegularExpression(@"^[A-Za-z0-9\-\s]+$", ErrorMessage = "O código de adesão só pode conter letras, números, espaços e hífen.")]
    [Display(Name = "Código de adesão")]
    public string AccessCode { get; set; } = string.Empty;
}
