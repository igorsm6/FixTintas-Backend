using FixTintas.Negocio;
using FixTintas.Web.Infra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.Controllers;

// "Meus pedidos" do cliente logado.
[Authorize]
public class PedidosController : Controller
{
    private readonly PedidoNegocio _pedidos;

    public PedidosController(PedidoNegocio pedidos)
    {
        _pedidos = pedidos;
    }

    // GET /Pedidos
    public IActionResult Index()
    {
        return View(_pedidos.ListarDoCliente(User.ObterId()));
    }

    // GET /Pedidos/Detalhes/5
    public IActionResult Detalhes(int id)
    {
        try
        {
            return View(_pedidos.ObterDoCliente(id, User.ObterId()));
        }
        catch (NaoEncontradoException)
        {
            return NotFound();
        }
    }

    // POST /Pedidos/Cancelar/5   (só enquanto o pedido está Pendente; o estoque volta)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancelar(int id)
    {
        try
        {
            _pedidos.CancelarPeloCliente(id, User.ObterId());
            TempData["Mensagem"] = $"Pedido nº {id} cancelado.";
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Detalhes), new { id });
    }
}
