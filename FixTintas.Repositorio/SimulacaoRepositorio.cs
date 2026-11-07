using FixTintas.Modelos;
using FixTintas.Repositorio.Contexto;
using FixTintas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixTintas.Repositorio;

// Única classe que conversa com a tabela Simulacoes (projetos do app).
public class SimulacaoRepositorio : ISimulacaoRepositorio
{
    private readonly AppDbContext _contexto;

    public SimulacaoRepositorio(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public List<Simulacao> ListarDoUsuario(int usuarioId) =>
        _contexto.Simulacoes.AsNoTracking()
            .Include(s => s.Cor)
            .Include(s => s.TipoTinta)
            .Where(s => s.UsuarioId == usuarioId)
            .OrderByDescending(s => s.CriadoEm).ThenByDescending(s => s.Id)
            .ToList();

    public Simulacao? ObterDoUsuario(int id, int usuarioId) =>
        _contexto.Simulacoes
            .Include(s => s.Cor)
            .Include(s => s.TipoTinta)
            .FirstOrDefault(s => s.Id == id && s.UsuarioId == usuarioId);

    public void Adicionar(Simulacao simulacao)
    {
        _contexto.Simulacoes.Add(simulacao);
        _contexto.Gravar();
    }

    public void Remover(Simulacao simulacao)
    {
        _contexto.Simulacoes.Remove(simulacao);
        _contexto.Gravar();
    }
}
