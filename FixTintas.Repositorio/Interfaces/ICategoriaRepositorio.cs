using FixTintas.Modelos;

namespace FixTintas.Repositorio.Interfaces;

public interface ICategoriaRepositorio
{
    // Categorias principais, cada uma com as suas subcategorias (menu lateral do site).
    List<Categoria> ListarArvore(bool incluirInativas);

    // Todas, numa lista só, cada uma com a CategoriaPai carregada (painel e listas de escolha).
    List<Categoria> ListarTodas();

    Categoria? ObterPorId(int id);
    Categoria? ObterPorSlug(string slug);
    bool SlugEmUso(string slug, int? ignorarId = null);
    bool NomeEmUso(int? categoriaPaiId, string nome, int? ignorarId = null);
    bool PossuiProdutos(int id);
    bool PossuiSubcategorias(int id);

    void Adicionar(Categoria categoria);
    void Atualizar(Categoria categoria);
    void Remover(Categoria categoria);
}
