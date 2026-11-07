namespace FixTintas.Modelos;

// Entidade: uma linha da tabela PedidoItens (um produto dentro de um pedido).
// Ao gravar, a trigger TR_PedidoItens_BaixaEstoque diminui o estoque do produto.
public class PedidoItem
{
    public int Id { get; set; }

    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    // Cor escolhida (só tinta). null para acessórios.
    public int? CorId { get; set; }
    public Cor? Cor { get; set; }

    public int Quantidade { get; set; }

    // ----- Dados "congelados" na hora da compra -----
    public decimal PrecoUnitario { get; set; }
    public string ProdutoDescricao { get; set; } = string.Empty;
    public decimal? VolumeLitros { get; set; }
    public string? CorNome { get; set; }
    public string? CorHex { get; set; }
}
