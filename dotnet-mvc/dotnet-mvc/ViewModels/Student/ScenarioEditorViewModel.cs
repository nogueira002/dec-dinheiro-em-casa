using System.ComponentModel.DataAnnotations;
using dotnet_mvc.Models;

namespace dotnet_mvc.ViewModels.Student;

public class ScenarioEditorViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Indica o nome da família.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "O nome da família deve ter entre 3 e 120 caracteres.")]
    [Display(Name = "Nome da família")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica a localização.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "A localização deve ter entre 2 e 120 caracteres.")]
    [Display(Name = "Localização")]
    public string Location { get; set; } = string.Empty;

    [Range(1, 10, ErrorMessage = "O número de membros deve estar entre 1 e 10.")]
    [Display(Name = "Número de membros")]
    public int MembersCount { get; set; } = 2;

    [Required(ErrorMessage = "Indica o saldo inicial.")]
    [Range(typeof(decimal), "0", "999999999.99", ErrorMessage = "O saldo inicial deve ser igual ou superior a 0.")]
    [Display(Name = "Saldo inicial (€)")]
    public decimal? InitialBalance { get; set; }

    [Display(Name = "Tipo de família")]
    public ScenarioProfileType ProfileType { get; set; } = ScenarioProfileType.StudentDeslocado;

    public bool IsEditMode => Id.HasValue;
}
