using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;

namespace FixTintas.Repositorio.Interfaces;

public interface IProdutoRepositorio
{
    // ---------- Dapper + stored procedures ----------

    // sp_Produtos_Vitrine: produtos à venda, com filtros (todos opcionais).
    List<ProdutoVitrineLinha> Vitrine(string? termo, string? categoriaSlug, int? marcaId,
                                      decimal? precoMin, decimal? precoMax, bool somentePromocoes);

    // sp_Produtos_EstoqueBaixo: o que precisa de reposição.
    List<ProdutoVitrineLinha> EstoqueBaixo();

    // ---------- Entity Framework Core ----------

    // Painel: todos os produtos (inclusive inativos), com filtros.
    // situacao: null (todos), "ativo", "inativo", "baixo" ou "zerado".
    List<Produto> ListarParaPainel(string? busca, int? categoriaId, string? situacao);

    // App: só as latas de tinta que estão à venda.
    List<Produto> ListarTintas(int? marcaId, int? tipoTintaId);

    // Calculadora: latas de um tipo de tinta, à venda e com estoque.
    List<Produto> ListarLatasDoTipo(int tipoTintaId, int? marcaId);

    // Carrinho: os produtos dos itens, com marca, tipo e categoria.
    List<Produto> ObterPorIds(IEnumerable<int> ids);

    Produto? ObterPorId(int id);
    bool SkuEmUso(string sku, int? ignorarId = null);

    // true = já existe o mesmo produto (nome, categoria, marca e tipo) com o mesmo
    // volume ou tamanho: seria a mesma opção repetida no card da loja.
    bool VariacaoEmUso(string nome, int categoriaId, int? marcaId, int? tipoTintaId,
                       decimal? volumeLitros, string? tamanho, int? ignorarId = null);

    bool PossuiPedidos(int id);
    ResumoEstoqueDto Resumo();

    void Adicionar(Produto produto);
    void Atualizar(Produto produto);
    void Remover(Produto produto);

    // Muda só o estoque (tela "Estoque" do painel). Devolve false se o produto não existe.
    bool AtualizarEstoque(int id, int estoque);
}
