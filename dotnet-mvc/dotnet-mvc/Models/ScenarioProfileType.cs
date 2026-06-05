using System.ComponentModel.DataAnnotations;

namespace dotnet_mvc.Models;

public enum ScenarioProfileType
{
    [Display(Name = "Estudante deslocado")]
    StudentDeslocado = 1,

    [Display(Name = "Casal")]
    Casal = 2,

    [Display(Name = "Família monoparental")]
    Monoparental = 3
}
