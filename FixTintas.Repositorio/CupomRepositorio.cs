using FixTintas.Modelos;
using FixTintas.Repositorio.Contexto;
using FixTintas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixTintas.Repositorio;

// Única classe que conversa com a tabela Cupons.
public class CupomRepositorio : ICupomRepositorio
{
    private readonly AppDbContext _contexto;

    public CupomRepositorio(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public List<Cupom> Listar() =>
        _contexto.Cupons.AsNoTracking().OrderBy(c => c.Codigo).ToList();

    public Cupom? ObterPorId(int id) =>
        _contexto.Cupons.FirstOrDefault(c => c.Id == id);

    // Sem AsNoTracking de propósito: ao fechar um pedido, o Negocio soma +1 em
    // UsosRealizados neste mesmo objeto, e o EF grava junto com o pedido.
    public Cupom? ObterPorCodigo(string codigo) =>
        _contexto.Cupons.FirstOrDefault(c => c.Codigo == codigo);

    public bool CodigoEmUso(string codigo, int? ignorarId = null) =>
        _contexto.Cupons.Any(c => c.Codigo == codigo && c.Id != ignorarId);

    public bool PossuiPedidos(int id) =>
        _contexto.Pedidos.Any(p => p.CupomId == id);

    public void Adicionar(Cupom cupom)
    {
        _contexto.Cupons.Add(cupom);
        _contexto.Gravar();
    }

    public void Atualizar(Cupom cupom)
    {
        if (_contexto.Entry(cupom).State == EntityState.Detached)
            _contexto.Cupons.Update(cupom);
        _contexto.Gravar();
    }

    public void Remover(Cupom cupom)
    {
        _contexto.Cupons.Remove(cupom);
        _contexto.Gravar();
    }
}
