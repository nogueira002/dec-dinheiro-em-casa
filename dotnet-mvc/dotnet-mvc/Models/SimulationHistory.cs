namespace dotnet_mvc.Models;

public class SimulationHistory
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Strategy { get; set; } = string.Empty;
    public decimal RendimentoMensal { get; set; }
    public decimal DespesasMensais { get; set; }
    public int Meses { get; set; }

    public decimal? TaxaEsforco { get; set; }
    public string? TaxaEsforcoClassificacao { get; set; }
    public decimal? MontanteFinal { get; set; }
    public decimal? PoupancaAcumulada { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
