using FixTintas.Modelos;
using FixTintas.Repositorio.Contexto;
using FixTintas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixTintas.Repositorio;

// Única classe que conversa com a tabela Cores.
public class CorRepositorio : ICorRepositorio
{
    private readonly AppDbContext _contexto;

    public CorRepositorio(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public List<Cor> Listar(string? familia, string? busca, bool incluirInativas)
    {
        var consulta = _contexto.Cores.AsNoTracking().Where(c => incluirInativas || !c.Inativa);

        if (!string.IsNullOrWhiteSpace(familia))
            consulta = consulta.Where(c => c.Familia == familia);

        if (!string.IsNullOrWhiteSpace(busca))
            consulta = consulta.Where(c => c.Nome.Contains(busca));

        // Pela ordem de cadastro: as cores iniciais já vêm agrupadas por família.
        return consulta.OrderBy(c => c.Id).ToList();
    }

    public List<string> ListarFamilias() =>
        _contexto.Cores.AsNoTracking()
            .Where(c => !c.Inativa && c.Familia != null)
            .Select(c => c.Familia!)
            .Distinct()
            .OrderBy(f => f)
            .ToList();

    public Cor? ObterPorId(int id) =>
        _contexto.Cores.FirstOrDefault(c => c.Id == id);

    public List<Cor> ObterPorIds(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        return _contexto.Cores.AsNoTracking().Where(c => lista.Contains(c.Id)).ToList();
    }

    public bool NomeEmUso(string nome, int? ignorarId = null) =>
        _contexto.Cores.Any(c => c.Nome == nome && c.Id != ignorarId);

    public bool EmUso(int id) =>
        _contexto.PedidoItens.Any(i => i.CorId == id) || _contexto.Simulacoes.Any(s => s.CorId == id);

    public void Adicionar(Cor cor)
    {
        _contexto.Cores.Add(cor);
        _contexto.Gravar();
    }

    public void Atualizar(Cor cor)
    {
        if (_contexto.Entry(cor).State == EntityState.Detached)
            _contexto.Cores.Update(cor);
        _contexto.Gravar();
    }

    public void Remover(Cor cor)
    {
        _contexto.Cores.Remove(cor);
        _contexto.Gravar();
    }
}
