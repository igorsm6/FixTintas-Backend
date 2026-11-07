using System.ComponentModel.DataAnnotations;

namespace FixTintas.Modelos.Dtos;

// SAÍDA: um produto (painel do funcionário e GET /api/produtos do app).
// O app lê: id, sku, nome, marcaId, marcaNome, tipoTintaId, tipoTintaNome, acabamento,
// volumeLitros, preco, estoque, emEstoque. Os outros campos ele ignora.
// (O app só recebe tintas, então marcaId, tipoTintaId e volumeLitros vêm sempre preenchidos.)
public class ProdutoDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? ImagemUrl { get; set; }

    public int CategoriaId { get; set; }
    public string? CategoriaNome { get; set; }   // "Exterior › Vernizes"
    public string? CategoriaSlug { get; set; }

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
    public bool EmEstoque { get; set; }
    public bool EhTinta { get; set; }

    // "ok", "baixo" ou "zerado" (cores do painel).
    public string Situacao { get; set; } = "ok";

    public bool Inativo { get; set; }
    public DateTime DataCadastro { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // "3,6 L", "23 cm" ou "Unidade".
    public string Rotulo => Formatacao.RotuloDoProduto(VolumeLitros, Tamanho);

    // Precisa da Categoria (com a CategoriaPai), da Marca e do TipoTinta carregados (Include).
    public static ProdutoDto De(Produto p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Nome = p.Nome,
        Descricao = p.Descricao,
        ImagemUrl = p.ImagemUrl,
        CategoriaId = p.CategoriaId,
        CategoriaNome = p.Categoria is null ? null
            : p.Categoria.CategoriaPai is null ? p.Categoria.Nome
            : $"{p.Categoria.CategoriaPai.Nome} › {p.Categoria.Nome}",
        CategoriaSlug = p.Categoria?.Slug,
        MarcaId = p.MarcaId,
        MarcaNome = p.Marca?.Nome,
        TipoTintaId = p.TipoTintaId,
        TipoTintaNome = p.TipoTinta?.Nome,
        Acabamento = p.TipoTinta?.Acabamento,
        VolumeLitros = p.VolumeLitros,
        Tamanho = p.Tamanho,
        Preco = p.Preco,
        PrecoDe = p.PrecoDe,
        Estoque = p.Estoque,
        EstoqueMinimo = p.EstoqueMinimo,
        EmEstoque = p.Estoque > 0,
        EhTinta = p.TipoTintaId.HasValue,
        Situacao = SituacaoDoEstoque(p.Estoque, p.EstoqueMinimo),
        Inativo = p.Inativo,
        DataCadastro = p.DataCadastro,
        DataAtualizacao = p.DataAtualizacao
    };

    public static string SituacaoDoEstoque(int estoque, int minimo)
    {
        if (estoque <= 0) return "zerado";
        if (estoque <= minimo) return "baixo";
        return "ok";
    }
}

// ENTRADA: cadastro e edição de produto (formulário do painel e /api/admin/produtos).
//   Tinta:     escolha o Tipo de tinta, a Marca e o Volume (litros).
//   Acessório: deixe o Tipo de tinta vazio; o Tamanho é opcional ("23 cm").
public class ProdutoSalvarDto
{
    // Opcional: se ficar vazio, o sistema gera um código.
    [Display(Name = "Código (SKU)")]
    [StringLength(40, ErrorMessage = "O código pode ter no máximo 40 caracteres.")]
    public string? Sku { get; set; }

    // Opcional para tinta (o sistema monta "Marca Tipo Acabamento"). Não ponha o volume
    // no nome: as latas da mesma linha (900 ml, 3,6 L, 18 L) usam o MESMO nome e viram
    // um card só na loja, com um botão para cada volume.
    [Display(Name = "Nome do produto")]
    [StringLength(150, ErrorMessage = "O nome pode ter no máximo 150 caracteres.")]
    public string? Nome { get; set; }

    [Display(Name = "Descrição")]
    [StringLength(500, ErrorMessage = "A descrição pode ter no máximo 500 caracteres.")]
    public string? Descricao { get; set; }

    [Display(Name = "Imagem")]
    [StringLength(300)]
    public string? ImagemUrl { get; set; }

    [Display(Name = "Categoria")]
    [Required(ErrorMessage = "Escolha a categoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "Escolha a categoria.")]
    public int CategoriaId { get; set; }

    [Display(Name = "Marca")]
    public int? MarcaId { get; set; }

    [Display(Name = "Tipo de tinta")]
    public int? TipoTintaId { get; set; }

    [Display(Name = "Volume (litros)")]
    [Range(0.1, 50, ErrorMessage = "O volume deve estar entre 0,1 e 50 litros.")]
    public decimal? VolumeLitros { get; set; }

    [Display(Name = "Tamanho")]
    [StringLength(30, ErrorMessage = "O tamanho pode ter no máximo 30 caracteres.")]
    public string? Tamanho { get; set; }

    [Display(Name = "Preço (R$)")]
    [Required(ErrorMessage = "Informe o preço.")]
    [Range(0.01, 999999.99, ErrorMessage = "Informe um preço válido.")]
    public decimal Preco { get; set; }

    [Display(Name = "Preço \"De\" (R$, opcional)")]
    [Range(0.01, 999999.99, ErrorMessage = "Informe um preço \"De\" válido.")]
    public decimal? PrecoDe { get; set; }

    [Display(Name = "Quantidade em estoque")]
    [Required(ErrorMessage = "Informe a quantidade em estoque.")]
    [Range(0, 1000000, ErrorMessage = "O estoque não pode ser negativo.")]
    public int Estoque { get; set; }

    [Display(Name = "Estoque mínimo (alerta)")]
    [Required(ErrorMessage = "Informe o estoque mínimo.")]
    [Range(0, 100000, ErrorMessage = "O estoque mínimo não pode ser negativo.")]
    public int EstoqueMinimo { get; set; } = 5;

    [Display(Name = "Inativo (não aparece no site nem no app)")]
    public bool Inativo { get; set; }

    // Converte o que foi digitado na entidade que o Negocio entende.
    // No cadastro o id é 0 (o banco gera); na alteração vem da URL.
    public Produto ParaProduto(int id = 0) => new()
    {
        Id = id,
        Sku = Sku ?? string.Empty,
        Nome = Nome ?? string.Empty,
        Descricao = Descricao,
        ImagemUrl = ImagemUrl,
        CategoriaId = CategoriaId,
        MarcaId = MarcaId,
        TipoTintaId = TipoTintaId,
        VolumeLitros = VolumeLitros,
        Tamanho = Tamanho,
        Preco = Preco,
        PrecoDe = PrecoDe,
        Estoque = Estoque,
        EstoqueMinimo = EstoqueMinimo,
        Inativo = Inativo
    };

    public static ProdutoSalvarDto De(Produto p) => new()
    {
        Sku = p.Sku,
        Nome = p.Nome,
        Descricao = p.Descricao,
        ImagemUrl = p.ImagemUrl,
        CategoriaId = p.CategoriaId,
        MarcaId = p.MarcaId,
        TipoTintaId = p.TipoTintaId,
        VolumeLitros = p.VolumeLitros,
        Tamanho = p.Tamanho,
        Preco = p.Preco,
        PrecoDe = p.PrecoDe,
        Estoque = p.Estoque,
        EstoqueMinimo = p.EstoqueMinimo,
        Inativo = p.Inativo
    };
}

// ENTRADA de PUT /api/admin/produtos/{id}/estoque   { "estoque": 40 }
public class EstoqueDto
{
    [Range(0, 1000000, ErrorMessage = "O estoque não pode ser negativo.")]
    public int Estoque { get; set; }
}

// SAÍDA: números dos cartões do painel.
public class ResumoEstoqueDto
{
    public int Total { get; set; }
    public int Ativos { get; set; }
    public int EstoqueBaixo { get; set; }
    public int SemEstoque { get; set; }
}
