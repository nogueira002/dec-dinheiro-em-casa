using System.ComponentModel.DataAnnotations;

namespace dotnet_mvc.ViewModels.Professor;

public class CreateClassViewModel
{
    [Required(ErrorMessage = "Indica o nome da turma.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "O nome da turma deve ter entre 3 e 120 caracteres.")]
    [Display(Name = "Nome da turma")]
    public string ClassName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "A descrição não pode exceder 500 caracteres.")]
    [Display(Name = "Descrição")]
    public string? Description { get; set; }

    public string GeneratedCodePreview { get; set; } = string.Empty;
}
