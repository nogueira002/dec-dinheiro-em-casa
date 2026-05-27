using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace dotnet_mvc.ViewModels.Professor;

public class CreateChallengeViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Indica o título do desafio.")]
    [StringLength(160, MinimumLength = 3, ErrorMessage = "O título deve ter entre 3 e 160 caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Descreve o desafio.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "A descrição deve ter entre 10 e 2000 caracteres.")]
    [Display(Name = "Descrição")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleciona uma turma.")]
    [Display(Name = "Turma")]
    public int? ClassId { get; set; }

    [Required(ErrorMessage = "Indica a data de entrega.")]
    [DataType(DataType.Date)]
    [Display(Name = "Prazo de entrega")]
    public DateTime? Deadline { get; set; }

    [Display(Name = "Notificar os alunos por email")]
    public bool NotifyStudents { get; set; } = true;

    public string GeneratedCodePreview { get; set; } = string.Empty;
    public IReadOnlyList<SelectListItem> AvailableClasses { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Deadline.HasValue && Deadline.Value.Date < DateTime.Today)
        {
            yield return new ValidationResult(
                "O prazo de entrega não pode ser no passado.",
                [nameof(Deadline)]);
        }
    }
}
