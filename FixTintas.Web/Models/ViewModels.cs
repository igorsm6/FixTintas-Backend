using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;

namespace FixTintas.Web.Models;

// ============================================================================
// VIEW MODELS: o pacote de dados que cada página precisa, quando é mais do que
// um DTO só. Ficam na Web porque são coisas de tela (os DTOs ficam no Modelos).
// ============================================================================

// Página inicial: as ofertas.
public class InicioViewModel
{
    public List<VitrineItemDto> Ofertas { get; set; } = new();
}

// Página de produtos (vitrine com filtros).
public class VitrineViewModel
{
    public List<VitrineItemDto> Itens { get; set; } = new();
    public ProdutoFiltroDto Filtro { get; set; } = new();
    public string Titulo { get; set; } = "Todos os Produtos";
}

// Menu lateral de categorias (view component MenuCategorias).
public class MenuCategoriasViewModel
{
    public List<CategoriaDto> Categorias { get; set; } = new();
    public List<MarcaDto> Marcas { get; set; } = new();

    // O que está escolhido agora (vem da URL).
    public string? CategoriaSelecionada { get; set; }
    public int? MarcaSelecionada { get; set; }

    // Qual coluna 2 abre sozinha ("cat-3" ou "marcas"), ou null.
    public string? ColunaAberta { get; set; }

    // Os filtros (busca, preço, ordem) só aparecem na página de produtos.
    public bool MostrarFiltros { get; set; }
    public ProdutoFiltroDto Filtro { get; set; } = new();
}

// Janela "Adicionar ao carrinho" (view component ModalProduto).
public class ModalProdutoViewModel
{
    public List<CorDto> Cores { get; set; } = new();
    public string VoltarPara { get; set; } = "/";
}

// Sugestões da busca do cabeçalho (view component BuscaRapida).
public class SugestaoBuscaDto
{
    public string Nome { get; set; } = string.Empty;
    public string Sub { get; set; } = string.Empty;
}

// Página de cores (com os favoritos do cliente).
public class CoresViewModel
{
    public List<CorDto> Cores { get; set; } = new();
    public List<string> Familias { get; set; } = new();
    public string? FamiliaAtual { get; set; }
    public bool SoFavoritas { get; set; }
    public HashSet<int> Favoritas { get; set; } = new();
    public bool Logado { get; set; }
}

// Início do painel: os números e as listas do dia.
public class PainelInicioViewModel
{
    public ResumoEstoqueDto Resumo { get; set; } = new();
    public Dictionary<StatusPedido, int> PedidosPorStatus { get; set; } = new();
    public int Clientes { get; set; }
    public List<ProdutoVitrineLinha> EstoqueBaixo { get; set; } = new();
    public List<PedidoResumoDto> PedidosEmAberto { get; set; } = new();

    public int Pedidos(StatusPedido status) => PedidosPorStatus.TryGetValue(status, out var total) ? total : 0;
}
