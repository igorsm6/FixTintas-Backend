namespace FixTintas.Modelos;

// Situações de um pedido. Caminho normal: Pendente -> Pago -> Enviado -> Entregue.
// "Cancelado" devolve o estoque (procedure sp_Pedidos_Cancelar).
// No banco fica gravado o NOME ("Pendente"), não o número (ver AppDbContext).
public enum StatusPedido
{
    Pendente = 1,
    Pago = 2,
    Enviado = 3,
    Entregue = 4,
    Cancelado = 5
}

// Tipo de desconto de um cupom.
public enum TipoDesconto
{
    // Valor é uma porcentagem (10 = 10%).
    Percentual = 1,

    // Valor é em reais (50 = R$ 50,00).
    ValorFixo = 2
}

// Acabamentos de tinta aceitos (o site e o app mostram estes três).
public static class Acabamentos
{
    public const string Fosco = "Fosco";
    public const string Acetinado = "Acetinado";
    public const string Brilhante = "Brilhante";

    public static readonly IReadOnlyList<string> Todos = new[] { Fosco, Acetinado, Brilhante };

    // "fosco" -> "Fosco". Devolve null se não for um dos três.
    public static string? Normalizar(string? acabamento)
    {
        if (string.IsNullOrWhiteSpace(acabamento)) return null;
        var procurado = acabamento.Trim();
        return Todos.FirstOrDefault(a => a.Equals(procurado, StringComparison.OrdinalIgnoreCase));
    }
}
