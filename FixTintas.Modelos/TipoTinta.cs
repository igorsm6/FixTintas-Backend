namespace FixTintas.Modelos;

// Entidade: uma linha da tabela TiposTinta (ex.: "Acrílica" com acabamento "Fosco").
// Guarda os números técnicos que a calculadora e a simulação do app usam,
// para a regra ficar só no servidor.
public class TipoTinta
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    // Fosco, Acetinado ou Brilhante (ver Acabamentos em Enums.cs).
    public string Acabamento { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    // Quantos m² um litro cobre em uma demão. Base do cálculo de litros.
    public decimal RendimentoM2PorLitro { get; set; } = 10m;

    // Demãos que o fabricante recomenda.
    public int DemaosRecomendadas { get; set; } = 2;

    // Parâmetro visual da simulação no app: quanto do sombreamento da foto é mantido
    // (fosco < acetinado < brilhante). O app só aplica o número que vem daqui.
    public decimal FatorContraste { get; set; } = 0.95m;

    public bool Inativo { get; set; }
}
