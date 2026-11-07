using FixTintas.Modelos;
using FixTintas.Repositorio.Contexto;
using FixTintas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixTintas.Repositorio;

// Única classe que conversa com a tabela Categorias.
public class CategoriaRepositorio : ICategoriaRepositorio
{
    private readonly AppDbContext _contexto;

    public CategoriaRepositorio(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public List<Categoria> ListarArvore(bool incluirInativas)
    {
        var principais = _contexto.Categorias.AsNoTracking()
            .Include(c => c.Subcategorias)
            .Where(c => c.CategoriaPaiId == null && (incluirInativas || !c.Inativa))
            .OrderBy(c => c.Ordem).ThenBy(c => c.Nome)
            .ToList();

        // Ordena (e, se for o caso, esconde as inativas) as subcategorias de cada uma.
        foreach (var categoria in principais)
        {
            categoria.Subcategorias = categoria.Subcategorias
                .Where(s => incluirInativas || !s.Inativa)
                .OrderBy(s => s.Ordem).ThenBy(s => s.Nome)
                .ToList();
        }

        return principais;
    }

    public List<Categoria> ListarTodas() =>
        _contexto.Categorias.AsNoTracking()
            .Include(c => c.CategoriaPai)
            .ToList();

    public Categoria? ObterPorId(int id) =>
        _contexto.Categorias
            .Include(c => c.CategoriaPai)
            .Include(c => c.Subcategorias)
            .FirstOrDefault(c => c.Id == id);

    public Categoria? ObterPorSlug(string slug) =>
        _contexto.Categorias.AsNoTracking()
            .Include(c => c.CategoriaPai)
            .Include(c => c.Subcategorias)
            .FirstOrDefault(c => c.Slug == slug);

    public bool SlugEmUso(string slug, int? ignorarId = null) =>
        _contexto.Categorias.Any(c => c.Slug == slug && c.Id != ignorarId);

    public bool NomeEmUso(int? categoriaPaiId, string nome, int? ignorarId = null) =>
        _contexto.Categorias.Any(c => c.CategoriaPaiId == categoriaPaiId && c.Nome == nome && c.Id != ignorarId);

    public bool PossuiProdutos(int id) =>
        _contexto.Produtos.Any(p => p.CategoriaId == id);

    public bool PossuiSubcategorias(int id) =>
        _contexto.Categorias.Any(c => c.CategoriaPaiId == id);

    public void Adicionar(Categoria categoria)
    {
        _contexto.Categorias.Add(categoria);
        _contexto.Gravar();
    }

    public void Atualizar(Categoria categoria)
    {
        // Se a categoria veio do banco nesta mesma requisição (ObterPorId), o EF já
        // acompanha o que mudou e só grava. Se veio "de fora", o Update avisa o EF.
        if (_contexto.Entry(categoria).State == EntityState.Detached)
            _contexto.Categorias.Update(categoria);
        _contexto.Gravar();
    }

    public void Remover(Categoria categoria)
    {
        _contexto.Categorias.Remove(categoria);
        _contexto.Gravar();
    }
}
