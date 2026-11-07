namespace FixTintas.Modelos;

// Entidade: uma linha da tabela Cores (o catálogo de cores da loja).
// O RGB não é guardado: é calculado a partir do HEX (ver CorDto).
public class Cor
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    // Formato #RRGGBB, sempre em maiúsculas.
    public string CodigoHex { get; set; } = string.Empty;

    // Agrupamento para filtros: Branco, Azul, Verde...
    public string? Familia { get; set; }

    // Código Pantone equivalente, quando houver.
    public string? CodigoPantone { get; set; }

    public bool Inativa { get; set; }
}
