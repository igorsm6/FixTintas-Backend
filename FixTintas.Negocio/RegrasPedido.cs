using FixTintas.Modelos;

namespace FixTintas.Negocio;

// ============================================================================
// Para qual status um pedido pode ir, e quem pode cancelar. Fica só aqui.
//
//   Pendente -> Pago ou Cancelado
//   Pago     -> Enviado ou Cancelado
//   Enviado  -> Entregue ou Cancelado
//   Entregue e Cancelado são finais.
//
// O CLIENTE só cancela enquanto o pedido está Pendente (ainda não foi pago).
// A EQUIPE da loja segue a tabela acima.
// ============================================================================
public static class RegrasPedido
{
    private static readonly Dictionary<StatusPedido, StatusPedido[]> Transicoes = new()
    {
        [StatusPedido.Pendente] = new[] { StatusPedido.Pago, StatusPedido.Cancelado },
        [StatusPedido.Pago] = new[] { StatusPedido.Enviado, StatusPedido.Cancelado },
        [StatusPedido.Enviado] = new[] { StatusPedido.Entregue, StatusPedido.Cancelado },
        [StatusPedido.Entregue] = Array.Empty<StatusPedido>(),
        [StatusPedido.Cancelado] = Array.Empty<StatusPedido>()
    };

    public static bool ClientePodeCancelar(StatusPedido status) => status == StatusPedido.Pendente;

    public static bool TransicaoValida(StatusPedido de, StatusPedido para) =>
        Transicoes.TryGetValue(de, out var permitidos) && permitidos.Contains(para);

    // Os próximos status possíveis (botões do painel).
    public static IReadOnlyList<StatusPedido> Proximos(StatusPedido status) =>
        Transicoes.TryGetValue(status, out var permitidos) ? permitidos : Array.Empty<StatusPedido>();

    // Lê o texto do status ("pago", "Pago"...). Devolve null se não for um status conhecido.
    public static StatusPedido? Ler(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        return Enum.TryParse<StatusPedido>(texto.Trim(), ignoreCase: true, out var status) && Enum.IsDefined(status)
            ? status
            : null;
    }
}
