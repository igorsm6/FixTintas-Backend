using FixTintas.Modelos;
using FixTintas.Negocio;
using FixTintas.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.Areas.Painel.Controllers;

// ============================================================================
// PAINEL DA EQUIPE (área "Painel" do site: /Painel/...).
// [Authorize(Roles = Perfis.Equipe)] = só entra quem está logado como Funcionario
// ou Gerente. Cliente que tentar abrir cai em "Acesso negado".
// ============================================================================
[Area("Painel")]
[Authorize(Roles = Perfis.Equipe)]
public class InicioController : Controller
{
    private readonly ProdutoNegocio _produtos;
    private readonly PedidoNegocio _pedidos;
    private readonly UsuarioNegocio _usuarios;

    public InicioController(ProdutoNegocio produtos, PedidoNegocio pedidos, UsuarioNegocio usuarios)
    {
        _produtos = produtos;
        _pedidos = pedidos;
        _usuarios = usuarios;
    }

    // GET /Painel
    public IActionResult Index()
    {
        var emAberto = _pedidos.Listar()
            .Where(p => p.Status == nameof(StatusPedido.Pendente) || p.Status == nameof(StatusPedido.Pago))
            .Take(10)
            .ToList();

        return View(new PainelInicioViewModel
        {
            Resumo = _produtos.Resumo(),
            PedidosPorStatus = _pedidos.ContarPorStatus(),
            Clientes = _usuarios.ContarClientes(),
            EstoqueBaixo = _produtos.EstoqueBaixo().Take(10).ToList(),
            PedidosEmAberto = emAberto
        });
    }
}
