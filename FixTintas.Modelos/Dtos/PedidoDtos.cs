using System.ComponentModel.DataAnnotations;

namespace FixTintas.Modelos.Dtos;

// ============================================================================
// DTOs DO CARRINHO E DOS PEDIDOS.
// O formato é o mesmo que o app Flutter já usa, e o carrinho do site
// (guardado na sessão) usa as mesmas classes.
// ============================================================================

// Uma linha do carrinho, como o site e o app ENVIAM.
// Repare: NÃO existe preço. O preço sempre vem do banco.
public class ItemCarrinhoDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Produto inválido.")]
    public int ProdutoId { get; set; }

    // Obrigatória para tinta; vazia para acessório.
    public int? CorId { get; set; }

    [Range(1, 1000, ErrorMessage = "A quantidade deve ser de 1 a 1000.")]
    public int Quantidade { get; set; } = 1;
}

// O carrinho inteiro: POST /api/pedidos/orcamento e POST /api/pedidos.
//   { "itens": [ { "produtoId": 3, "corId": 10, "quantidade": 2 } ], "cupom": "BEMVINDO10" }
public class CarrinhoDto
{
    [Required(ErrorMessage = "O carrinho está vazio.")]
    [MinLength(1, ErrorMessage = "O carrinho está vazio.")]
    [MaxLength(50, ErrorMessage = "O carrinho aceita até 50 itens diferentes.")]
    public List<ItemCarrinhoDto> Itens { get; set; } = new();

    [StringLength(40, ErrorMessage = "Cupom inválido.")]
    public string? Cupom { get; set; }
}

// Uma linha de orçamento ou de pedido, como o sistema DEVOLVE.
// Para acessórios (sem cor): corId = 0, corNome = "", corHex = "" e volumeLitros = 0,
// porque o app não aceita esses campos vazios (null).
public class LinhaPedidoDto
{
    public int ProdutoId { get; set; }
    public string ProdutoDescricao { get; set; } = string.Empty;
    public decimal VolumeLitros { get; set; }
    public int CorId { get; set; }
    public string CorNome { get; set; } = string.Empty;
    public string CorHex { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total { get; set; }

    // Extras para o site (o app ignora).
    public string? ImagemUrl { get; set; }
    public bool EhTinta { get; set; }

    public static LinhaPedidoDto De(PedidoItem i) => new()
    {
        ProdutoId = i.ProdutoId,
        ProdutoDescricao = i.ProdutoDescricao,
        VolumeLitros = i.VolumeLitros ?? 0m,
        CorId = i.CorId ?? 0,
        CorNome = i.CorNome ?? string.Empty,
        CorHex = i.CorHex ?? string.Empty,
        Quantidade = i.Quantidade,
        PrecoUnitario = i.PrecoUnitario,
        Total = i.PrecoUnitario * i.Quantidade,
        ImagemUrl = i.Produto?.ImagemUrl,
        EhTinta = i.CorId.HasValue
    };
}

// "Quanto custa?": preços, descontos e total calculados pelo Negocio.
public class OrcamentoDto
{
    public List<LinhaPedidoDto> Itens { get; set; } = new();
    public decimal LitrosTotais { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DescontoVolume { get; set; }
    public decimal DescontoCupom { get; set; }
    public decimal Total { get; set; }
    public string? Cupom { get; set; }
    public int QuantidadeItens { get; set; }
}

// Uma linha do histórico de pedidos (GET /api/pedidos e "Meus pedidos").
public class PedidoResumoDto
{
    public int Id { get; set; }
    public DateTime CriadoEm { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int QuantidadeItens { get; set; }

    // Extras para o painel (o app ignora).
    public int ClienteId { get; set; }
    public string? ClienteNome { get; set; }
    public string? ClienteEmail { get; set; }

    public static PedidoResumoDto De(Pedido p) => new()
    {
        Id = p.Id,
        CriadoEm = p.CriadoEm,
        Status = p.Status.ToString(),
        Total = p.Total,
        QuantidadeItens = p.Itens.Sum(i => i.Quantidade),
        ClienteId = p.UsuarioId,
        ClienteNome = p.Usuario?.Nome,
        ClienteEmail = p.Usuario?.Email
    };
}

// Um pedido completo. "podeCancelar" diz se o CLIENTE ainda pode cancelar
// (a regra fica no Negocio; o app só mostra ou esconde o botão).
public class PedidoDto
{
    public int Id { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<LinhaPedidoDto> Itens { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DescontoVolume { get; set; }
    public decimal DescontoCupom { get; set; }
    public decimal Total { get; set; }
    public string? Cupom { get; set; }
    public bool PodeCancelar { get; set; }

    // Extras para o painel (o app ignora).
    public int ClienteId { get; set; }
    public string? ClienteNome { get; set; }
    public string? ClienteEmail { get; set; }

    // Para quais status a equipe pode mudar este pedido agora.
    public List<string> ProximosStatus { get; set; } = new();
}

// ENTRADA: mudar o status de um pedido (painel e /api/admin/pedidos/{id}/status).
public class AlterarStatusDto
{
    // Pendente, Pago, Enviado, Entregue ou Cancelado.
    [Required(ErrorMessage = "Informe o novo status.")]
    public string Status { get; set; } = string.Empty;
}
