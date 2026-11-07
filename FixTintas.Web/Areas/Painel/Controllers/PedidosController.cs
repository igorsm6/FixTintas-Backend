using FixTintas.Modelos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.Areas.Painel.Controllers;

// Pedidos de todos os clientes (site e app), para a equipe acompanhar e mudar o status.
[Area("Painel")]
[Authorize(Roles = Perfis.Equipe)]
public class PedidosController : Controller
{
    private readonly PedidoNegocio _negocio;

    public PedidosController(PedidoNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET /Painel/Pedidos?status=Pendente
    public IActionResult Index(string? status)
    {
        try
        {
            var pedidos = _negocio.Listar(status);
            ViewData["Status"] = status;
            ViewData["Contagem"] = _negocio.ContarPorStatus();
            return View(pedidos);
        }
        catch (NegocioException ex)
        {
            // ?status=Inventado na URL: avisa e mostra todos.
            TempData["Erro"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // GET /Painel/Pedidos/Detalhes/5
    public IActionResult Detalhes(int id)
    {
        try
        {
            return View(_negocio.Obter(id));
        }
        catch (NaoEncontradoException)
        {
            return NotFound();
        }
    }

    // POST /Painel/Pedidos/AlterarStatus/5   (status = Pago, Enviado, Entregue ou Cancelado)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AlterarStatus(int id, string status)
    {
        try
        {
            var pedido = _negocio.AlterarStatus(id, status);
            TempData["Mensagem"] = $"Pedido nº {id} agora está \"{pedido.Status}\".";
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Detalhes), new { id });
    }
}
