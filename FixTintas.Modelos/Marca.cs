namespace FixTintas.Modelos;

// Entidade: uma linha da tabela Marcas.
public class Marca
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    // Marca desativada some do catálogo, mas os pedidos antigos continuam valendo.
    public bool Inativa { get; set; }
}
