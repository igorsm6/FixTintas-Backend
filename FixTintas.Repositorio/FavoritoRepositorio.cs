using FixTintas.Modelos;
using FixTintas.Repositorio.Contexto;
using FixTintas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixTintas.Repositorio;

// Única classe que conversa com a tabela Favoritos.
public class FavoritoRepositorio : IFavoritoRepositorio
{
    private readonly AppDbContext _contexto;

    public FavoritoRepositorio(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public List<Cor> ListarCores(int usuarioId) =>
        _contexto.Favoritos.AsNoTracking()
            .Where(f => f.UsuarioId == usuarioId && !f.Cor!.Inativa)
            .OrderByDescending(f => f.CriadoEm)
            .Select(f => f.Cor!)
            .ToList();

    public List<int> IdsDasCores(int usuarioId) =>
        _contexto.Favoritos.AsNoTracking()
            .Where(f => f.UsuarioId == usuarioId)
            .Select(f => f.CorId)
            .ToList();

    public bool Existe(int usuarioId, int corId) =>
        _contexto.Favoritos.Any(f => f.UsuarioId == usuarioId && f.CorId == corId);

    public void Adicionar(Favorito favorito)
    {
        _contexto.Favoritos.Add(favorito);
        _contexto.Gravar();
    }

    public void Remover(int usuarioId, int corId)
    {
        // DELETE direto no banco (ExecuteDelete, EF Core 7+).
        _contexto.Favoritos
            .Where(f => f.UsuarioId == usuarioId && f.CorId == corId)
            .ExecuteDelete();
    }
}
