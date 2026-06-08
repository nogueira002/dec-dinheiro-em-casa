namespace dotnet_mvc.ViewModels.Student;

public class SimulationHistoryItemViewModel
{
    public int Id { get; set; }
    public string Strategy { get; set; } = string.Empty;
    public string StrategyLabel { get; set; } = string.Empty;
    public decimal RendimentoMensal { get; set; }
    public decimal DespesasMensais { get; set; }
    public int Meses { get; set; }
    public decimal? TaxaEsforco { get; set; }
    public string? TaxaEsforcoClassificacao { get; set; }
    public decimal? MontanteFinal { get; set; }
    public decimal? PoupancaAcumulada { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
