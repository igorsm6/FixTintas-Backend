using FixTintas.Modelos;
using FixTintas.Repositorio.Contexto;
using FixTintas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixTintas.Repositorio;

// Única classe que conversa com a tabela TiposTinta.
public class TipoTintaRepositorio : ITipoTintaRepositorio
{
    private readonly AppDbContext _contexto;

    public TipoTintaRepositorio(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public List<TipoTinta> Listar(bool incluirInativos) =>
        _contexto.TiposTinta.AsNoTracking()
            .Where(t => incluirInativos || !t.Inativo)
            .OrderBy(t => t.Nome).ThenBy(t => t.Acabamento)
            .ToList();

    public TipoTinta? ObterPorId(int id) =>
        _contexto.TiposTinta.FirstOrDefault(t => t.Id == id);

    public bool NomeEmUso(string nome, string acabamento, int? ignorarId = null) =>
        _contexto.TiposTinta.Any(t => t.Nome == nome && t.Acabamento == acabamento && t.Id != ignorarId);

    public bool EmUso(int id) =>
        _contexto.Produtos.Any(p => p.TipoTintaId == id) || _contexto.Simulacoes.Any(s => s.TipoTintaId == id);

    public void Adicionar(TipoTinta tipo)
    {
        _contexto.TiposTinta.Add(tipo);
        _contexto.Gravar();
    }

    public void Atualizar(TipoTinta tipo)
    {
        if (_contexto.Entry(tipo).State == EntityState.Detached)
            _contexto.TiposTinta.Update(tipo);
        _contexto.Gravar();
    }

    public void Remover(TipoTinta tipo)
    {
        _contexto.TiposTinta.Remove(tipo);
        _contexto.Gravar();
    }
}
