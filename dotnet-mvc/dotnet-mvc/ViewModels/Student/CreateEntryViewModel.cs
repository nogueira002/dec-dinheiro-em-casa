using System.ComponentModel.DataAnnotations;

namespace dotnet_mvc.ViewModels.Student;

public class CreateEntryViewModel
{
    [Required(ErrorMessage = "Seleciona o tipo de lançamento.")]
    public string EntryType { get; set; } = "Expense";

    [Required(ErrorMessage = "Seleciona uma categoria.")]
    [MaxLength(80)]
    public string Category { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999.99",
        ErrorMessage = "O valor tem de ser maior que zero.",
        ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; set; }

    [Range(1, 12, ErrorMessage = "Seleciona um mês válido.")]
    public int Month { get; set; } = DateTime.Now.Month;

    [Range(2000, 2100, ErrorMessage = "Indica um ano válido.")]
    public int Year { get; set; } = DateTime.Now.Year;

    public bool IsRecurring { get; set; }

    public static readonly string[] IncomeCategories =
        ["Salário", "Subsídio", "Mesada", "Freelance", "Rendas", "Outros"];

    public static readonly string[] ExpenseCategories =
        ["Habitação", "Alimentação", "Transportes", "Serviços", "Saúde", "Educação",
         "Lazer", "Comunicações", "Seguros", "Vestuário", "Outros"];

    public static readonly string[] MonthNames =
        ["", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
         "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"];
}
