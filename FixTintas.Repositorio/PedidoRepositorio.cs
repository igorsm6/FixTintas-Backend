using System.Data;
using Dapper;
using FixTintas.Modelos;
using FixTintas.Repositorio.Contexto;
using FixTintas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixTintas.Repositorio;

// Única classe que conversa com as tabelas Pedidos e PedidoItens.
public class PedidoRepositorio : IPedidoRepositorio
{
    private readonly AppDbContext _contexto;

    public PedidoRepositorio(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public void Adicionar(Pedido pedido)
    {
        // Um único SaveChanges grava o pedido, os itens e o +1 de uso do cupom
        // (o cupom foi lido por este mesmo contexto), tudo numa transação só.
        _contexto.Pedidos.Add(pedido);
        _contexto.Gravar();
    }

    public Pedido? ObterPorId(int id) =>
        _contexto.Pedidos.AsNoTracking()
            .Include(p => p.Itens).ThenInclude(i => i.Produto)
            .Include(p => p.Usuario)
            .FirstOrDefault(p => p.Id == id);

    public List<Pedido> ListarDoUsuario(int usuarioId) =>
        _contexto.Pedidos.AsNoTracking()
            .Include(p => p.Itens)
            .Where(p => p.UsuarioId == usuarioId)
            .OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.Id)
            .ToList();

    public List<Pedido> Listar(StatusPedido? status)
    {
        var consulta = _contexto.Pedidos.AsNoTracking()
            .Include(p => p.Itens)
            .Include(p => p.Usuario)
            .AsQueryable();

        if (status is StatusPedido filtro)
            consulta = consulta.Where(p => p.Status == filtro);

        return consulta
            .OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.Id)
            .ToList();
    }

    public Dictionary<StatusPedido, int> ContarPorStatus() =>
        _contexto.Pedidos.AsNoTracking()
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Total = g.Count() })
            .ToList()
            .ToDictionary(x => x.Status, x => x.Total);

    public bool AtualizarStatus(int id, StatusPedido de, StatusPedido para)
    {
        var agora = DateTime.Now;
        var alterados = _contexto.Pedidos
            .Where(p => p.Id == id && p.Status == de)
            .ExecuteUpdate(s => s
                .SetProperty(p => p.Status, para)
                .SetProperty(p => p.AtualizadoEm, agora));

        return alterados == 1;
    }

    public bool Cancelar(int id)
    {
        // Mesma conexão do EF; o parâmetro vai separado do comando (sem SQL Injection).
        var conexao = _contexto.Database.GetDbConnection();
        return conexao.QuerySingle<bool>(
            ObjetosDoBanco.ProcedureCancelarPedido,
            new { PedidoId = id },
            commandType: CommandType.StoredProcedure);
    }
}
