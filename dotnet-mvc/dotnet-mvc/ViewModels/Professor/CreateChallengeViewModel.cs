using System.ComponentModel.DataAnnotations;
namespace dotnet_mvc.ViewModels.Professor;

public class CreateChallengeViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Indica o título do desafio.")]
    [StringLength(160, MinimumLength = 3, ErrorMessage = "O título deve ter entre 3 e 160 caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Descreve o desafio.")]
    [MinLength(10, ErrorMessage = "A descrição deve ter pelo menos 10 caracteres.")]
    [Display(Name = "Descrição")]
    public string Description { get; set; } = string.Empty;

    public string GeneratedCodePreview { get; set; } = string.Empty;
    public bool IsEditMode => Id.HasValue;
}
