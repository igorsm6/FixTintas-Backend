using System.Globalization;
using System.Text.RegularExpressions;
using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Repositorio;
using FixTintas.Repositorio.Interfaces;

namespace FixTintas.Negocio;

// ============================================================================
// Regras dos produtos: a vitrine do site, a lista de tintas do app e o cadastro
// do painel.
//
// Cada produto é UM item vendável:
//   - TINTA: tem tipo de tinta, marca e volume (ex.: 3,6 L). O cliente escolhe a cor.
//   - ACESSÓRIO: rolo, lixa, escada... sem tipo e sem volume; pode ter um tamanho.
// Na loja, os produtos com o mesmo nome, marca, tipo e categoria viram UM card com
// um botão para cada volume/tamanho (900 ml, 3,6 L, 18 L).
//
// Permissões: qualquer pessoa da equipe cadastra produtos e muda o estoque;
// só o GERENTE muda o preço de um produto já cadastrado e exclui produtos.
// ============================================================================
public class ProdutoNegocio
{
    // Pasta (dentro do wwwroot da Web) onde ficam as imagens enviadas pelo painel.
    private const string PastaImagens = "img/produtos";

    private static readonly Regex RegexSku = new("^[A-Z0-9_-]{2,40}$", RegexOptions.Compiled);
    private static readonly Regex RegexFracao = new(@"^(\d+)\s*/\s*(\d+)", RegexOptions.Compiled);
    private static readonly Regex RegexNumero = new(@"^\d+([.,]\d+)?", RegexOptions.Compiled);

    private readonly IProdutoRepositorio _repositorio;
    private readonly ICategoriaRepositorio _categorias;
    private readonly IMarcaRepositorio _marcas;
    private readonly ITipoTintaRepositorio _tipos;
    private readonly IArmazenamentoDeArquivos _arquivos;

    public ProdutoNegocio(IProdutoRepositorio repositorio, ICategoriaRepositorio categorias, IMarcaRepositorio marcas,
                          ITipoTintaRepositorio tipos, IArmazenamentoDeArquivos arquivos)
    {
        _repositorio = repositorio;
        _categorias = categorias;
        _marcas = marcas;
        _tipos = tipos;
        _arquivos = arquivos;
    }

    // ================================================================ VITRINE

    // Página de produtos do site (stored procedure sp_Produtos_Vitrine).
    public List<VitrineItemDto> Vitrine(ProdutoFiltroDto filtro)
    {
        var termo = Validacoes.TextoOpcional(filtro.Termo);
        if (termo is { Length: > 100 }) termo = termo[..100];

        var categoria = Validacoes.TextoOpcional(filtro.Categoria)?.ToLowerInvariant();
        var marcaId = filtro.MarcaId is > 0 ? filtro.MarcaId : null;
        var precoMin = filtro.PrecoMin is > 0 ? filtro.PrecoMin : null;
        var precoMax = filtro.PrecoMax is > 0 ? filtro.PrecoMax : null;
        if (precoMin is decimal minimo && precoMax is decimal maximo && minimo > maximo)
            (precoMin, precoMax) = (precoMax, precoMin);

        var itens = Agrupar(_repositorio.Vitrine(termo, categoria, marcaId, precoMin, precoMax, somentePromocoes: false));

        return (filtro.Ordem ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "menor" => itens.OrderBy(i => i.MaisBarata?.Preco ?? 0m).ThenBy(i => i.Titulo).ToList(),
            "maior" => itens.OrderByDescending(i => i.MaisBarata?.Preco ?? 0m).ThenBy(i => i.Titulo).ToList(),
            "az" => itens.OrderBy(i => i.Titulo).ToList(),
            // "Relevância": ofertas primeiro, depois em ordem alfabética.
            _ => itens.OrderByDescending(i => i.EmOferta).ThenBy(i => i.Titulo).ToList()
        };
    }

    // Ofertas da página inicial: produtos com preço "De", do maior desconto para o menor.
    public List<VitrineItemDto> Ofertas(int quantidade = 8) =>
        Agrupar(_repositorio.Vitrine(null, null, null, null, null, somentePromocoes: true))
            .OrderByDescending(PercentualDeDesconto)
            .ThenBy(i => i.Titulo)
            .Take(quantidade)
            .ToList();

    // Junta as linhas da procedure em cards: mesmo nome + marca + tipo + categoria = um card.
    public static List<VitrineItemDto> Agrupar(IEnumerable<ProdutoVitrineLinha> linhas) =>
        linhas
            .GroupBy(l => (Tipo: l.TipoTintaId ?? 0, Marca: l.MarcaId ?? 0, l.CategoriaId, Nome: l.Nome.Trim().ToUpperInvariant()))
            .Select(grupo =>
            {
                var primeira = grupo.First();
                var variacoes = grupo
                    .OrderBy(l => l.VolumeLitros ?? 0m)
                    .ThenBy(l => OrdemDoTamanho(l.Tamanho))
                    .ThenBy(l => l.Preco)
                    .Select(l => new VitrineVariacaoDto
                    {
                        ProdutoId = l.Id,
                        Rotulo = l.Rotulo,
                        Preco = l.Preco,
                        PrecoDe = l.PrecoDe,
                        Estoque = l.Estoque
                    })
                    .ToList();

                return new VitrineItemDto
                {
                    Chave = $"p{variacoes[0].ProdutoId}",
                    Titulo = primeira.Nome,
                    Descricao = grupo.Select(l => l.Descricao).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d)),
                    ImagemUrl = grupo.Select(l => l.ImagemUrl).FirstOrDefault(u => !string.IsNullOrWhiteSpace(u)),
                    CategoriaNome = primeira.CategoriaPaiNome is null
                        ? primeira.CategoriaNome
                        : $"{primeira.CategoriaPaiNome} › {primeira.CategoriaNome}",
                    MarcaNome = primeira.MarcaNome,
                    Acabamento = primeira.Acabamento,
                    EhTinta = primeira.TipoTintaId.HasValue,
                    Variacoes = variacoes
                };
            })
            .ToList();

    private static decimal PercentualDeDesconto(VitrineItemDto item) =>
        item.Variacoes
            .Where(v => v.PrecoDe is > 0)
            .Select(v => (v.PrecoDe!.Value - v.Preco) / v.PrecoDe.Value)
            .DefaultIfEmpty(0m)
            .Max();

    // Ordena tamanhos escritos como texto: 3/4" < 1" < 2" < 6";  3 degraus < 10 degraus.
    private static decimal OrdemDoTamanho(string? tamanho)
    {
        if (string.IsNullOrWhiteSpace(tamanho)) return 0m;
        var texto = tamanho.Trim();

        var fracao = RegexFracao.Match(texto);
        if (fracao.Success)
        {
            var numerador = decimal.Parse(fracao.Groups[1].Value, CultureInfo.InvariantCulture);
            var denominador = decimal.Parse(fracao.Groups[2].Value, CultureInfo.InvariantCulture);
            if (denominador != 0) return numerador / denominador;
        }

        var numero = RegexNumero.Match(texto);
        if (numero.Success && decimal.TryParse(numero.Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var valor))
            return valor;

        return decimal.MaxValue;
    }

    // ============================================================ PAINEL E APP

    public List<Produto> ListarParaPainel(string? busca = null, int? categoriaId = null, string? situacao = null) =>
        _repositorio.ListarParaPainel(
            Validacoes.TextoOpcional(busca),
            categoriaId is > 0 ? categoriaId : null,
            Validacoes.TextoOpcional(situacao)?.ToLowerInvariant());

    public Produto? ObterPorId(int id) => _repositorio.ObterPorId(id);

    public ResumoEstoqueDto Resumo() => _repositorio.Resumo();

    // Produtos no estoque mínimo ou zerados (stored procedure sp_Produtos_EstoqueBaixo).
    public List<ProdutoVitrineLinha> EstoqueBaixo() => _repositorio.EstoqueBaixo();

    // App Flutter: só as latas de tinta à venda.
    public List<Produto> ListarTintas(int? marcaId = null, int? tipoTintaId = null) =>
        _repositorio.ListarTintas(marcaId is > 0 ? marcaId : null, tipoTintaId is > 0 ? tipoTintaId : null);

    public Produto? ObterTintaAVenda(int id)
    {
        var produto = _repositorio.ObterPorIds(new[] { id }).FirstOrDefault();
        return produto is not null && produto.EhTinta && EstaAVenda(produto) ? produto : null;
    }

    // Produto ativo, com categoria, marca e tipo ativos. Precisa dos Includes (ObterPorIds traz).
    public static bool EstaAVenda(Produto produto) =>
        !produto.Inativo
        && produto.Categoria is { Inativa: false }
        && (produto.Categoria.CategoriaPai is null || !produto.Categoria.CategoriaPai.Inativa)
        && (produto.Marca is null || !produto.Marca.Inativa)
        && (produto.TipoTinta is null || !produto.TipoTinta.Inativo);

    // ================================================================ CADASTRO

    // "imagem" = bytes do arquivo enviado pelo painel (opcional).
    public Produto Cadastrar(Produto produto, byte[]? imagem = null)
    {
        Normalizar(produto);
        Validar(produto, ignorarId: null);

        string? imagemSalva = null;
        if (imagem is { Length: > 0 })
        {
            imagemSalva = SalvarImagem(imagem);
            produto.ImagemUrl = imagemSalva;
        }

        try
        {
            _repositorio.Adicionar(produto);
        }
        catch (ConflitoDeGravacaoException)
        {
            ExcluirImagemEnviada(imagemSalva);
            throw new ConflitoException("Já existe um produto com este código (SKU) ou com este mesmo volume/tamanho.");
        }
        catch
        {
            ExcluirImagemEnviada(imagemSalva);
            throw;
        }

        return produto;
    }

    // podeAlterarPreco: true só para o gerente.
    // O ESTOQUE não muda por aqui: ele tem a tela própria e muda sozinho a cada venda
    // (se mudasse por aqui, o número antigo do formulário apagaria as vendas do meio tempo).
    public void Atualizar(Produto dados, bool podeAlterarPreco, byte[]? novaImagem = null, bool removerImagem = false)
    {
        var existente = _repositorio.ObterPorId(dados.Id)
            ?? throw new NaoEncontradoException("Produto não encontrado.");

        Normalizar(dados);

        if (!podeAlterarPreco && (dados.Preco != existente.Preco || dados.PrecoDe != existente.PrecoDe))
            throw new AcessoNegadoException("Só o gerente pode mudar o preço de um produto já cadastrado.");

        dados.Estoque = existente.Estoque;
        Validar(dados, ignorarId: dados.Id);

        string? imagemNova = null;
        if (novaImagem is { Length: > 0 }) imagemNova = SalvarImagem(novaImagem);

        var imagemAntiga = existente.ImagemUrl;
        var imagemFinal = imagemNova ?? (removerImagem ? null : dados.ImagemUrl ?? existente.ImagemUrl);

        existente.Sku = dados.Sku;
        existente.Nome = dados.Nome;
        existente.Descricao = dados.Descricao;
        existente.ImagemUrl = imagemFinal;
        existente.CategoriaId = dados.CategoriaId;
        existente.MarcaId = dados.MarcaId;
        existente.TipoTintaId = dados.TipoTintaId;
        existente.VolumeLitros = dados.VolumeLitros;
        existente.Tamanho = dados.Tamanho;
        existente.Preco = dados.Preco;
        existente.PrecoDe = dados.PrecoDe;
        existente.EstoqueMinimo = dados.EstoqueMinimo;
        existente.Inativo = dados.Inativo;

        try
        {
            _repositorio.Atualizar(existente);
        }
        catch (ConflitoDeGravacaoException ex)
        {
            ExcluirImagemEnviada(imagemNova);
            throw ex.Concorrencia
                ? new ConflitoException("O produto foi alterado por outra pessoa (ou acabou de ser vendido). Abra de novo e repita a alteração.")
                : new ConflitoException("Já existe um produto com este código (SKU) ou com este mesmo volume/tamanho.");
        }
        catch
        {
            ExcluirImagemEnviada(imagemNova);
            throw;
        }

        if (imagemAntiga != imagemFinal) ExcluirImagemEnviada(imagemAntiga);
    }

    // Produto que já foi vendido não é apagado (os pedidos antigos precisam dele): é desativado.
    public ResultadoExclusao Remover(int id)
    {
        var produto = _repositorio.ObterPorId(id)
            ?? throw new NaoEncontradoException("Produto não encontrado.");

        if (_repositorio.PossuiPedidos(id))
        {
            produto.Inativo = true;
            _repositorio.Atualizar(produto);
            return ResultadoExclusao.Desativado;
        }

        var imagem = produto.ImagemUrl;
        try
        {
            _repositorio.Remover(produto);
        }
        catch (ConflitoDeGravacaoException)
        {
            throw new ConflitoException("Não foi possível excluir o produto. Desative-o em vez de excluir.");
        }

        ExcluirImagemEnviada(imagem);
        return ResultadoExclusao.Excluido;
    }

    // Tela "Estoque" do painel (funcionário e gerente).
    public void AtualizarEstoque(int id, int estoque)
    {
        if (estoque is < 0 or > 1_000_000)
            throw new RegraDeNegocioException("O estoque deve ser um número de 0 a 1.000.000.");

        if (!_repositorio.AtualizarEstoque(id, estoque))
            throw new NaoEncontradoException("Produto não encontrado.");
    }

    // Guarda a imagem enviada em wwwroot/img/produtos e devolve a URL ("/img/produtos/xxx.jpg").
    public string SalvarImagem(byte[] conteudo)
    {
        var extensao = ValidadorDeImagem.Validar(conteudo, ValidadorDeImagem.TamanhoMaximoProduto, aceitarWebp: true);
        return "/" + _arquivos.Salvar(PastaImagens, extensao, conteudo);
    }

    // Só apaga imagens enviadas pelo painel; as imagens que vieram com o site ficam.
    private void ExcluirImagemEnviada(string? url)
    {
        if (url is not null && url.StartsWith("/" + PastaImagens + "/", StringComparison.OrdinalIgnoreCase))
            _arquivos.Excluir(url);
    }

    // ================================================================= REGRAS

    private static void Normalizar(Produto produto)
    {
        produto.Sku = (produto.Sku ?? string.Empty).Trim().ToUpperInvariant();
        produto.Nome = Validacoes.NormalizarTexto(produto.Nome);
        produto.Descricao = Validacoes.TextoOpcional(produto.Descricao);
        produto.ImagemUrl = Validacoes.TextoOpcional(produto.ImagemUrl);
        produto.Tamanho = Validacoes.TextoOpcional(produto.Tamanho);
        if (produto.MarcaId is <= 0) produto.MarcaId = null;
        if (produto.TipoTintaId is <= 0) produto.TipoTintaId = null;
        produto.Preco = Precificador.Arredondar(produto.Preco);
        if (produto.PrecoDe is decimal precoDe)
            produto.PrecoDe = precoDe <= 0 ? null : Precificador.Arredondar(precoDe);
    }

    private void Validar(Produto produto, int? ignorarId)
    {
        var categoria = _categorias.ObterPorId(produto.CategoriaId)
            ?? throw new RegraDeNegocioException("Escolha uma categoria válida.");

        Marca? marca = null;
        if (produto.MarcaId is int marcaId)
            marca = _marcas.ObterPorId(marcaId) ?? throw new RegraDeNegocioException("A marca escolhida não existe.");

        if (produto.TipoTintaId is int tipoId)
        {
            // ---- TINTA: tipo + marca + volume
            var tipo = _tipos.ObterPorId(tipoId) ?? throw new RegraDeNegocioException("O tipo de tinta escolhido não existe.");

            if (marca is null)
                throw new RegraDeNegocioException("Tinta precisa de marca: escolha a marca.");

            if (produto.VolumeLitros is not decimal volume)
                throw new RegraDeNegocioException("Tinta precisa do volume da lata, em litros (ex.: 3,6).");

            if (volume is < 0.1m or > 50m)
                throw new RegraDeNegocioException("O volume deve estar entre 0,1 e 50 litros.");

            // A calculadora trabalha em décimos de litro (0,9 · 3,6 · 18).
            if (decimal.Round(volume, 1) != volume)
                throw new RegraDeNegocioException("Informe o volume com no máximo uma casa decimal (ex.: 0,9 ou 3,6).");

            produto.Tamanho = null; // a tinta usa o volume

            if (produto.Nome.Length == 0)
                produto.Nome = $"{marca.Nome} {tipo.Nome} {tipo.Acabamento}";
        }
        else
        {
            // ---- ACESSÓRIO: sem volume
            produto.VolumeLitros = null;

            if (produto.Nome.Length == 0)
                throw new RegraDeNegocioException("Informe o nome do produto.");
        }

        if (produto.Nome.Length is < 2 or > 150)
            throw new RegraDeNegocioException("O nome deve ter de 2 a 150 caracteres.");
        if (produto.Descricao?.Length > 500)
            throw new RegraDeNegocioException("A descrição pode ter no máximo 500 caracteres.");
        if (produto.Tamanho?.Length > 30)
            throw new RegraDeNegocioException("O tamanho pode ter no máximo 30 caracteres.");
        if (Validacoes.TemCaracterePerigoso(produto.Nome) || Validacoes.TemCaracterePerigoso(produto.Descricao)
            || Validacoes.TemCaracterePerigoso(produto.Tamanho))
            throw new RegraDeNegocioException("Os textos não podem ter os caracteres < ou >.");

        if (produto.ImagemUrl is not null && !ImagemUrlValida(produto.ImagemUrl))
            throw new RegraDeNegocioException("Imagem inválida: use um endereço que comece com / (do próprio site) ou com https://.");

        if (produto.Preco is <= 0 or > 999_999.99m)
            throw new RegraDeNegocioException("Informe um preço maior que zero.");
        if (produto.PrecoDe is decimal de && (de <= produto.Preco || de > 999_999.99m))
            throw new RegraDeNegocioException("O preço \"De\" (antes da oferta) precisa ser maior que o preço atual.");

        if (produto.Estoque is < 0 or > 1_000_000)
            throw new RegraDeNegocioException("O estoque deve ser um número de 0 a 1.000.000.");
        if (produto.EstoqueMinimo is < 0 or > 100_000)
            throw new RegraDeNegocioException("O estoque mínimo não pode ser negativo.");

        if (produto.Sku.Length == 0)
            produto.Sku = GerarSku(marca, categoria);
        else if (!RegexSku.IsMatch(produto.Sku))
            throw new RegraDeNegocioException("O código (SKU) pode ter de 2 a 40 letras, números, - ou _ (sem espaços).");

        if (_repositorio.SkuEmUso(produto.Sku, ignorarId))
            throw new ConflitoException("Já existe um produto com este código (SKU).");

        if (_repositorio.VariacaoEmUso(produto.Nome, produto.CategoriaId, produto.MarcaId, produto.TipoTintaId,
                                       produto.VolumeLitros, produto.Tamanho, ignorarId))
            throw new ConflitoException("Este produto já está cadastrado com este mesmo volume/tamanho. Edite o que já existe.");
    }

    private static bool ImagemUrlValida(string url) =>
        url.Length <= 300
        && !url.Any(c => char.IsWhiteSpace(c) || c is '"' or '\'' or '<' or '>')
        && ((url.StartsWith('/') && !url.StartsWith("//"))
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

    // Código automático: 3 letras da marca (ou da categoria) + 6 números. Ex.: SUV-482913
    private string GerarSku(Marca? marca, Categoria categoria)
    {
        var prefixo = Validacoes.SlugDe(marca?.Nome ?? categoria.Nome).Replace("-", string.Empty).ToUpperInvariant();
        if (prefixo.Length > 3) prefixo = prefixo[..3];
        if (prefixo.Length < 2) prefixo = "FT";

        string sku;
        do
        {
            sku = $"{prefixo}-{Random.Shared.Next(100_000, 1_000_000)}";
        } while (_repositorio.SkuEmUso(sku));

        return sku;
    }
}
