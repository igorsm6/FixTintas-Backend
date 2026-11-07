using FixTintas.Modelos;

namespace FixTintas.Repositorio.Interfaces;

public interface IPedidoRepositorio
{
    // Grava o pedido e os itens. A trigger TR_PedidoItens_BaixaEstoque baixa o estoque.
    // Se o estoque acabou no mesmo instante (ou o cupom esgotou), o banco recusa
    // tudo e lança ConflitoDeGravacaoException.
    void Adicionar(Pedido pedido);

    // Pedido completo: itens (com o produto) e o cliente.
    Pedido? ObterPorId(int id);

    List<Pedido> ListarDoUsuario(int usuarioId);
    List<Pedido> Listar(StatusPedido? status);
    Dictionary<StatusPedido, int> ContarPorStatus();

    // Muda o status só se o pedido ainda estiver no status "de" (ninguém mexeu antes).
    bool AtualizarStatus(int id, StatusPedido de, StatusPedido para);

    // Stored procedure sp_Pedidos_Cancelar (Dapper): devolve o estoque e o uso do cupom.
    // Devolve false se o pedido já estava cancelado ou entregue.
    bool Cancelar(int id);
}
