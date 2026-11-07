using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.Areas.Painel.Controllers;

// Cupons de desconto (só gerente). Valem no site e no app.
[Area("Painel")]
[Authorize(Roles = Perfis.Gerente)]
public class CuponsController : Controller
{
    private readonly CupomNegocio _negocio;

    public CuponsController(CupomNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET /Painel/Cupons
    public IActionResult Index()
    {
        return View(_negocio.Listar().Select(CupomDto.De).ToList());
    }

    // GET /Painel/Cupons/Criar
    public IActionResult Criar() => View(new CupomSalvarDto());

    // POST /Painel/Cupons/Criar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Criar(CupomSalvarDto modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            _negocio.Cadastrar(modelo);
            TempData["Mensagem"] = "Cupom cadastrado.";
            return RedirectToAction(nameof(Index));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    // GET /Painel/Cupons/Editar/5
    public IActionResult Editar(int id)
    {
        var cupom = _negocio.ObterPorId(id);
        if (cupom is null) return NotFound();

        ViewData["Id"] = id;
        ViewData["UsosRealizados"] = cupom.UsosRealizados;
        return View(CupomSalvarDto.De(cupom));
    }

    // POST /Painel/Cupons/Editar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, CupomSalvarDto modelo)
    {
        ViewData["Id"] = id;
        ViewData["UsosRealizados"] = _negocio.ObterPorId(id)?.UsosRealizados;
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            _negocio.Atualizar(id, modelo);
            TempData["Mensagem"] = "Cupom atualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    // POST /Painel/Cupons/Excluir/5   (cupom já usado só é desativado)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Excluir(int id)
    {
        try
        {
            var resultado = _negocio.Remover(id);
            TempData["Mensagem"] = resultado == ResultadoExclusao.Excluido
                ? "Cupom excluído."
                : "O cupom já foi usado em pedidos, então foi desativado.";
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
