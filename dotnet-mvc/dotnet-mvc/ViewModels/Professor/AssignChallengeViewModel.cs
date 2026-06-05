using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace dotnet_mvc.ViewModels.Professor;

public class AssignChallengeViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Seleciona um desafio.")]
    [Display(Name = "Desafio")]
    public int? ChallengeId { get; set; }

    [Required(ErrorMessage = "Seleciona uma turma.")]
    [Display(Name = "Turma")]
    public int? ClassId { get; set; }

    [Required(ErrorMessage = "Indica a data de entrega.")]
    [DataType(DataType.Date)]
    [Display(Name = "Prazo de entrega")]
    public DateTime? Deadline { get; set; }

    [Display(Name = "Notificar os alunos por email")]
    public bool NotifyStudents { get; set; } = true;

    public string? ChallengeCode { get; set; }
    public string? ChallengeTitle { get; set; }
    public IReadOnlyList<SelectListItem> AvailableChallenges { get; set; } = [];
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
