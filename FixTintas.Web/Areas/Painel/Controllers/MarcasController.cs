using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.Areas.Painel.Controllers;

// Marcas (só gerente). Mesmo molde do ClientesController do professor.
[Area("Painel")]
[Authorize(Roles = Perfis.Gerente)]
public class MarcasController : Controller
{
    private readonly MarcaNegocio _negocio;

    public MarcasController(MarcaNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET /Painel/Marcas
    public IActionResult Index()
    {
        return View(_negocio.Listar(incluirInativas: true).Select(MarcaDto.De).ToList());
    }

    // GET /Painel/Marcas/Criar
    public IActionResult Criar() => View(new MarcaSalvarDto());

    // POST /Painel/Marcas/Criar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Criar(MarcaSalvarDto modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            _negocio.Cadastrar(modelo.ParaMarca());
            TempData["Mensagem"] = "Marca cadastrada.";
            return RedirectToAction(nameof(Index));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    // GET /Painel/Marcas/Editar/5
    public IActionResult Editar(int id)
    {
        var marca = _negocio.ObterPorId(id);
        if (marca is null) return NotFound();

        ViewData["Id"] = id;
        return View(MarcaSalvarDto.De(marca));
    }

    // POST /Painel/Marcas/Editar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, MarcaSalvarDto modelo)
    {
        ViewData["Id"] = id;
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            _negocio.Atualizar(modelo.ParaMarca(id));
            TempData["Mensagem"] = "Marca atualizada.";
            return RedirectToAction(nameof(Index));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    // POST /Painel/Marcas/Excluir/5   (marca com produtos só é desativada)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Excluir(int id)
    {
        try
        {
            var resultado = _negocio.Remover(id);
            TempData["Mensagem"] = resultado == ResultadoExclusao.Excluido
                ? "Marca excluída."
                : "A marca tem produtos, então foi desativada (some da loja e do app).";
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
