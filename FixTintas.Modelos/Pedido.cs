namespace FixTintas.Modelos;

// Entidade: uma linha da tabela Pedidos (uma compra, feita pelo site ou pelo app).
// Os valores ficam gravados no momento da compra: se o preço mudar depois,
// o pedido antigo continua com o valor que o cliente pagou.
public class Pedido
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public StatusPedido Status { get; set; } = StatusPedido.Pendente;

    // Soma dos itens (preço x quantidade).
    public decimal Subtotal { get; set; }

    // Desconto automático por volume de compra (regra em appsettings.json, seção "Loja").
    public decimal DescontoVolume { get; set; }

    // Desconto do cupom, calculado sobre o que sobrou depois do desconto por volume.
    public decimal DescontoCupom { get; set; }

    public decimal Total { get; set; }

    public int? CupomId { get; set; }
    public Cupom? Cupom { get; set; }

    // Código do cupom guardado como texto, para o histórico.
    public string? CupomCodigo { get; set; }

    // Preenchida pelo banco (valor padrão GETDATE()).
    public DateTime CriadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public List<PedidoItem> Itens { get; set; } = new();
}
