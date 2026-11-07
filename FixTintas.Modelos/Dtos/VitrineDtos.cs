using System.ComponentModel.DataAnnotations;

namespace FixTintas.Modelos.Dtos;

// ============================================================================
// DTOs DA VITRINE DO SITE (página de produtos, busca e ofertas da página inicial).
// ============================================================================

// ENTRADA: os filtros da página de produtos. Chegam pela URL:
//   /Produtos?categoria=exterior&termo=verniz&precoMin=10&precoMax=200&ordem=menor
// Tudo é opcional.
public class ProdutoFiltroDto
{
    [StringLength(100)]
    public string? Termo { get; set; }

    // Slug da categoria ou subcategoria ("exterior" ou "exterior-vernizes").
    [StringLength(80)]
    public string? Categoria { get; set; }

    public int? MarcaId { get; set; }

    public decimal? PrecoMin { get; set; }

    public decimal? PrecoMax { get; set; }

    // "" (relevância), "menor", "maior" ou "az" — os mesmos valores do <select> do site.
    [StringLength(10)]
    public string? Ordem { get; set; }
}

// Uma linha devolvida pela stored procedure sp_Produtos_Vitrine (e sp_Produtos_EstoqueBaixo).
// ATENÇÃO: o Dapper liga cada COLUNA do SELECT à PROPRIEDADE de mesmo nome.
// Se mudar um nome aqui, mude também no SQL (ObjetosDoBanco.cs).
public class ProdutoVitrineLinha
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? ImagemUrl { get; set; }
    public int CategoriaId { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public string CategoriaSlug { get; set; } = string.Empty;
    public string? CategoriaPaiNome { get; set; }
    public string? CategoriaPaiSlug { get; set; }
    public int? MarcaId { get; set; }
    public string? MarcaNome { get; set; }
    public int? TipoTintaId { get; set; }
    public string? TipoTintaNome { get; set; }
    public string? Acabamento { get; set; }
    public decimal? VolumeLitros { get; set; }
    public string? Tamanho { get; set; }
    public decimal Preco { get; set; }
    public decimal? PrecoDe { get; set; }
    public int Estoque { get; set; }
    public int EstoqueMinimo { get; set; }

    public string Rotulo => Formatacao.RotuloDoProduto(VolumeLitros, Tamanho);
}

// Uma opção de compra dentro de um card (ex.: a lata de 3,6 L).
public class VitrineVariacaoDto
{
    public int ProdutoId { get; set; }
    public string Rotulo { get; set; } = string.Empty;   // "3,6 L", "23 cm", "Unidade"
    public decimal Preco { get; set; }
    public decimal? PrecoDe { get; set; }
    public int Estoque { get; set; }
    public bool EmEstoque => Estoque > 0;
}

// Um card da vitrine. As latas da mesma tinta (mesma marca, tipo e categoria)
// viram um card só, com um botão para cada volume — como no site original.
public class VitrineItemDto
{
    public string Chave { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? ImagemUrl { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public string? MarcaNome { get; set; }
    public string? Acabamento { get; set; }

    // true = o cliente precisa escolher a cor.
    public bool EhTinta { get; set; }

    public List<VitrineVariacaoDto> Variacoes { get; set; } = new();

    // A opção mais barata (é o preço que aparece no card).
    public VitrineVariacaoDto? MaisBarata => Variacoes.OrderBy(v => v.Preco).FirstOrDefault();

    public bool EmOferta => Variacoes.Any(v => v.PrecoDe.HasValue);

    public bool EmEstoque => Variacoes.Any(v => v.EmEstoque);
}
