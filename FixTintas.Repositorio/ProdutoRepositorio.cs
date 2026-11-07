using System.Data;
using System.Linq.Expressions;
using Dapper;
using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Repositorio.Contexto;
using FixTintas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixTintas.Repositorio;

// Única classe que conversa com a tabela Produtos.
// Combinação sugerida no manual (igual ao exemplo do professor):
//   - Entity Framework Core para o cadastro e as consultas comuns;
//   - Dapper para as stored procedures.
public class ProdutoRepositorio : IProdutoRepositorio
{
    private readonly AppDbContext _contexto;

    public ProdutoRepositorio(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    // "Está à venda": produto ativo, categoria (e categoria pai) ativas, e marca e tipo
    // de tinta ativos quando existirem. Usada em várias consultas abaixo.
    private static readonly Expression<Func<Produto, bool>> AVenda = p =>
        !p.Inativo
        && !p.Categoria!.Inativa
        && (p.Categoria.CategoriaPai == null || !p.Categoria.CategoriaPai.Inativa)
        && (p.Marca == null || !p.Marca.Inativa)
        && (p.TipoTinta == null || !p.TipoTinta.Inativo);

    // ---------- Dapper + stored procedures ----------

    public List<ProdutoVitrineLinha> Vitrine(string? termo, string? categoriaSlug, int? marcaId,
                                             decimal? precoMin, decimal? precoMax, bool somentePromocoes)
    {
        // Reaproveita a mesma conexão (e a mesma connection string) do EF.
        var conexao = _contexto.Database.GetDbConnection();

        // Os valores vão separados do comando: isso é o que impede SQL Injection.
        return conexao.Query<ProdutoVitrineLinha>(
            ObjetosDoBanco.ProcedureVitrine,
            new
            {
                Termo = termo,
                Categoria = categoriaSlug,
                MarcaId = marcaId,
                PrecoMin = precoMin,
                PrecoMax = precoMax,
                SomentePromocoes = somentePromocoes
            },
            commandType: CommandType.StoredProcedure).ToList();
    }

    public List<ProdutoVitrineLinha> EstoqueBaixo()
    {
        var conexao = _contexto.Database.GetDbConnection();
        return conexao.Query<ProdutoVitrineLinha>(
            ObjetosDoBanco.ProcedureEstoqueBaixo,
            commandType: CommandType.StoredProcedure).ToList();
    }

    // ---------- Entity Framework Core ----------

    // Produto com tudo o que as telas mostram: categoria (e a categoria pai), marca e tipo.
    private IQueryable<Produto> ComDetalhes() =>
        _contexto.Produtos
            .Include(p => p.Categoria).ThenInclude(c => c!.CategoriaPai)
            .Include(p => p.Marca)
            .Include(p => p.TipoTinta);

    public List<Produto> ListarParaPainel(string? busca, int? categoriaId, string? situacao)
    {
        var consulta = ComDetalhes().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busca))
            consulta = consulta.Where(p => p.Nome.Contains(busca)
                                           || p.Sku.Contains(busca)
                                           || (p.Marca != null && p.Marca.Nome.Contains(busca)));

        // A categoria escolhida pode ser a principal (Interior) ou a subcategoria.
        if (categoriaId is int idCategoria)
            consulta = consulta.Where(p => p.CategoriaId == idCategoria || p.Categoria!.CategoriaPaiId == idCategoria);

        consulta = situacao switch
        {
            "ativo" => consulta.Where(p => !p.Inativo),
            "inativo" => consulta.Where(p => p.Inativo),
            "baixo" => consulta.Where(p => !p.Inativo && p.Estoque > 0 && p.Estoque <= p.EstoqueMinimo),
            "zerado" => consulta.Where(p => !p.Inativo && p.Estoque <= 0),
            _ => consulta
        };

        return consulta
            .OrderBy(p => p.Nome).ThenBy(p => p.VolumeLitros).ThenBy(p => p.Tamanho)
            .ToList();
    }

    public List<Produto> ListarTintas(int? marcaId, int? tipoTintaId)
    {
        var consulta = ComDetalhes().AsNoTracking()
            .Where(AVenda)
            .Where(p => p.TipoTintaId != null && p.MarcaId != null && p.VolumeLitros != null);

        if (marcaId is int idMarca) consulta = consulta.Where(p => p.MarcaId == idMarca);
        if (tipoTintaId is int idTipo) consulta = consulta.Where(p => p.TipoTintaId == idTipo);

        return consulta
            .OrderBy(p => p.Nome).ThenBy(p => p.VolumeLitros)
            .ToList();
    }

    public List<Produto> ListarLatasDoTipo(int tipoTintaId, int? marcaId)
    {
        var consulta = ComDetalhes().AsNoTracking()
            .Where(AVenda)
            .Where(p => p.TipoTintaId == tipoTintaId && p.VolumeLitros != null && p.Estoque > 0);

        if (marcaId is int idMarca) consulta = consulta.Where(p => p.MarcaId == idMarca);

        return consulta.ToList();
    }

    public List<Produto> ObterPorIds(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        return ComDetalhes().AsNoTracking()
            .Where(p => lista.Contains(p.Id))
            .ToList();
    }

    public Produto? ObterPorId(int id) =>
        ComDetalhes().FirstOrDefault(p => p.Id == id);

    public bool SkuEmUso(string sku, int? ignorarId = null) =>
        _contexto.Produtos.Any(p => p.Sku == sku && p.Id != ignorarId);

    public bool VariacaoEmUso(string nome, int categoriaId, int? marcaId, int? tipoTintaId,
                              decimal? volumeLitros, string? tamanho, int? ignorarId = null) =>
        _contexto.Produtos.Any(p => p.Nome == nome
                                    && p.CategoriaId == categoriaId
                                    && p.MarcaId == marcaId
                                    && p.TipoTintaId == tipoTintaId
                                    && p.VolumeLitros == volumeLitros
                                    && p.Tamanho == tamanho
                                    && p.Id != ignorarId);

    public bool PossuiPedidos(int id) =>
        _contexto.PedidoItens.Any(i => i.ProdutoId == id);

    public ResumoEstoqueDto Resumo() => new()
    {
        Total = _contexto.Produtos.Count(),
        Ativos = _contexto.Produtos.Count(p => !p.Inativo),
        EstoqueBaixo = _contexto.Produtos.Count(p => !p.Inativo && p.Estoque > 0 && p.Estoque <= p.EstoqueMinimo),
        SemEstoque = _contexto.Produtos.Count(p => !p.Inativo && p.Estoque <= 0)
    };

    public void Adicionar(Produto produto)
    {
        _contexto.Produtos.Add(produto);
        _contexto.Gravar();
    }

    public void Atualizar(Produto produto)
    {
        if (_contexto.Entry(produto).State == EntityState.Detached)
            _contexto.Produtos.Update(produto);
        _contexto.Gravar();
    }

    public void Remover(Produto produto)
    {
        _contexto.Produtos.Remove(produto);
        _contexto.Gravar();
    }

    public bool AtualizarEstoque(int id, int estoque)
    {
        // UPDATE direto no banco, sem carregar o produto (ExecuteUpdate, EF Core 7+).
        // A trigger TR_Produtos_DataAtualizacao grava a data da alteração.
        var alterados = _contexto.Produtos
            .Where(p => p.Id == id)
            .ExecuteUpdate(s => s.SetProperty(p => p.Estoque, estoque));

        return alterados > 0;
    }
}
