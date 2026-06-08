using System.ComponentModel.DataAnnotations;

namespace dotnet_mvc.ViewModels.Student;

public class CreateObjectiveViewModel
{
    [Required(ErrorMessage = "Descreve o objetivo.")]
    [MaxLength(200, ErrorMessage = "Máximo 200 caracteres.")]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "1", "9999999.99",
        ErrorMessage = "O valor alvo tem de ser maior que zero.",
        ParseLimitsInInvariantCulture = true)]
    public decimal TargetAmount { get; set; }

    [Range(1, 120, ErrorMessage = "O prazo deve ser entre 1 e 120 meses.")]
    public int DeadlineMonths { get; set; }
}
