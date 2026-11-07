using FixTintas.Modelos;
using FixTintas.Repositorio;
using FixTintas.Repositorio.Interfaces;

namespace FixTintas.Negocio;

// ============================================================================
// Regras das categorias (o menu lateral do site).
//   - Dois níveis só: categoria (Interior) e subcategoria (Interior › Látex).
//   - O "slug" é o nome que vai na URL; se ficar vazio, é gerado a partir do nome:
//       categoria "Móveis"            -> "moveis"
//       subcategoria "Tintas Spray"   -> "moveis-tintas-spray"
//   - Não há duas categorias com o mesmo nome dentro da mesma categoria pai.
// ============================================================================
public class CategoriaNegocio
{
    private readonly ICategoriaRepositorio _repositorio;

    public CategoriaNegocio(ICategoriaRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    // Menu do site (só as ativas) ou árvore do painel (todas).
    public List<Categoria> ListarArvore(bool incluirInativas = false) => _repositorio.ListarArvore(incluirInativas);

    // Lista "achatada" na ordem do menu: cada categoria seguida das suas subcategorias.
    public List<Categoria> ListarTodas()
    {
        var todas = _repositorio.ListarTodas();
        var principais = todas.Where(c => c.CategoriaPaiId is null).OrderBy(c => c.Ordem).ThenBy(c => c.Nome);

        var resultado = new List<Categoria>();
        foreach (var principal in principais)
        {
            resultado.Add(principal);
            resultado.AddRange(todas
                .Where(c => c.CategoriaPaiId == principal.Id)
                .OrderBy(c => c.Ordem).ThenBy(c => c.Nome));
        }
        return resultado;
    }

    public Categoria? ObterPorId(int id) => _repositorio.ObterPorId(id);

    public Categoria? ObterPorSlug(string? slug) =>
        string.IsNullOrWhiteSpace(slug) ? null : _repositorio.ObterPorSlug(slug.Trim().ToLowerInvariant());

    public Categoria Cadastrar(Categoria categoria)
    {
        Normalizar(categoria);
        var pai = ValidarPai(categoria, idDaPropria: null);
        categoria.Slug = DefinirSlug(categoria, pai, ignorarId: null);
        Validar(categoria, ignorarId: null);

        try
        {
            _repositorio.Adicionar(categoria);
        }
        catch (ConflitoDeGravacaoException)
        {
            throw new ConflitoException("Já existe uma categoria com este nome ou este slug.");
        }
        return categoria;
    }

    public void Atualizar(Categoria categoria)
    {
        var existente = _repositorio.ObterPorId(categoria.Id)
            ?? throw new NaoEncontradoException("Categoria não encontrada.");

        Normalizar(categoria);
        var pai = ValidarPai(categoria, idDaPropria: categoria.Id);

        // Uma categoria que tem subcategorias não pode virar subcategoria (seriam três níveis).
        if (categoria.CategoriaPaiId is not null && existente.Subcategorias.Count > 0)
            throw new RegraDeNegocioException("Esta categoria tem subcategorias, então não pode ficar dentro de outra.");

        categoria.Slug = DefinirSlug(categoria, pai, ignorarId: categoria.Id);
        Validar(categoria, ignorarId: categoria.Id);

        existente.Nome = categoria.Nome;
        existente.Slug = categoria.Slug;
        existente.CategoriaPaiId = categoria.CategoriaPaiId;
        existente.Ordem = categoria.Ordem;
        existente.Inativa = categoria.Inativa;

        try
        {
            _repositorio.Atualizar(existente);
        }
        catch (ConflitoDeGravacaoException)
        {
            throw new ConflitoException("Já existe uma categoria com este nome ou este slug.");
        }
    }

    // Se ainda tiver produtos ou subcategorias, a categoria é só desativada
    // (some do site, mas nada se perde).
    public ResultadoExclusao Remover(int id)
    {
        var categoria = _repositorio.ObterPorId(id)
            ?? throw new NaoEncontradoException("Categoria não encontrada.");

        if (_repositorio.PossuiProdutos(id) || _repositorio.PossuiSubcategorias(id))
        {
            categoria.Inativa = true;
            _repositorio.Atualizar(categoria);
            return ResultadoExclusao.Desativado;
        }

        _repositorio.Remover(categoria);
        return ResultadoExclusao.Excluido;
    }

    // ---------------------------------------------------------------- regras

    private static void Normalizar(Categoria categoria)
    {
        categoria.Nome = Validacoes.NormalizarTexto(categoria.Nome);
        categoria.Slug = Validacoes.SlugDe(categoria.Slug);
        if (categoria.CategoriaPaiId is <= 0) categoria.CategoriaPaiId = null;
    }

    private Categoria? ValidarPai(Categoria categoria, int? idDaPropria)
    {
        if (categoria.CategoriaPaiId is not int paiId) return null;

        if (paiId == idDaPropria)
            throw new RegraDeNegocioException("Uma categoria não pode ficar dentro dela mesma.");

        var pai = _repositorio.ObterPorId(paiId)
            ?? throw new RegraDeNegocioException("A categoria pai escolhida não existe.");

        if (pai.CategoriaPaiId is not null)
            throw new RegraDeNegocioException("Escolha uma categoria principal como pai (o menu tem só dois níveis).");

        return pai;
    }

    // Slug informado (já limpo) ou gerado a partir do nome; se o gerado já existir,
    // acrescenta -2, -3...
    private string DefinirSlug(Categoria categoria, Categoria? pai, int? ignorarId)
    {
        if (categoria.Slug.Length > 0) return categoria.Slug;

        var baseSlug = Validacoes.SlugDe(categoria.Nome);
        if (pai is not null) baseSlug = $"{pai.Slug}-{baseSlug}";
        if (baseSlug.Length == 0) baseSlug = "categoria";

        var slug = baseSlug;
        for (var n = 2; _repositorio.SlugEmUso(slug, ignorarId); n++)
            slug = $"{baseSlug}-{n}";
        return slug;
    }

    private void Validar(Categoria categoria, int? ignorarId)
    {
        if (categoria.Nome.Length < 2)
            throw new RegraDeNegocioException("Informe o nome da categoria.");
        if (categoria.Nome.Length > 60)
            throw new RegraDeNegocioException("O nome pode ter no máximo 60 caracteres.");
        if (Validacoes.TemCaracterePerigoso(categoria.Nome))
            throw new RegraDeNegocioException("O nome não pode ter os caracteres < ou >.");
        if (categoria.Slug.Length > 80)
            throw new RegraDeNegocioException("O slug pode ter no máximo 80 caracteres.");
        if (categoria.Ordem is < 0 or > 999)
            throw new RegraDeNegocioException("A ordem deve ser de 0 a 999.");

        if (_repositorio.SlugEmUso(categoria.Slug, ignorarId))
            throw new ConflitoException($"O slug \"{categoria.Slug}\" já é usado por outra categoria.");

        if (_repositorio.NomeEmUso(categoria.CategoriaPaiId, categoria.Nome, ignorarId))
            throw new ConflitoException("Já existe uma categoria com este nome neste lugar do menu.");
    }
}
