using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.Areas.Painel.Controllers;

// Catálogo de cores (só gerente): as mesmas cores do site e do app.
[Area("Painel")]
[Authorize(Roles = Perfis.Gerente)]
public class CoresController : Controller
{
    private readonly CorNegocio _negocio;

    public CoresController(CorNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET /Painel/Cores
    public IActionResult Index()
    {
        return View(_negocio.Listar(incluirInativas: true).Select(CorDto.De).ToList());
    }

    // GET /Painel/Cores/Criar
    public IActionResult Criar() => View(new CorSalvarDto());

    // POST /Painel/Cores/Criar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Criar(CorSalvarDto modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            _negocio.Cadastrar(modelo.ParaCor());
            TempData["Mensagem"] = "Cor cadastrada.";
            return RedirectToAction(nameof(Index));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    // GET /Painel/Cores/Editar/5
    public IActionResult Editar(int id)
    {
        var cor = _negocio.ObterPorId(id);
        if (cor is null) return NotFound();

        ViewData["Id"] = id;
        return View(CorSalvarDto.De(cor));
    }

    // POST /Painel/Cores/Editar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, CorSalvarDto modelo)
    {
        ViewData["Id"] = id;
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            _negocio.Atualizar(modelo.ParaCor(id));
            TempData["Mensagem"] = "Cor atualizada.";
            return RedirectToAction(nameof(Index));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    // POST /Painel/Cores/Excluir/5   (cor já vendida só é desativada)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Excluir(int id)
    {
        try
        {
            var resultado = _negocio.Remover(id);
            TempData["Mensagem"] = resultado == ResultadoExclusao.Excluido
                ? "Cor excluída."
                : "A cor já foi vendida (ou está num projeto do app), então foi desativada.";
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
