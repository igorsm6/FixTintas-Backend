using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.Areas.Painel.Controllers;

// Tipos de tinta (só gerente): rendimento e demãos usados pela calculadora do app.
[Area("Painel")]
[Authorize(Roles = Perfis.Gerente)]
public class TiposTintaController : Controller
{
    private readonly TipoTintaNegocio _negocio;

    public TiposTintaController(TipoTintaNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET /Painel/TiposTinta
    public IActionResult Index()
    {
        return View(_negocio.Listar(incluirInativos: true).Select(TipoTintaDto.De).ToList());
    }

    // GET /Painel/TiposTinta/Criar
    public IActionResult Criar() => View(new TipoTintaSalvarDto());

    // POST /Painel/TiposTinta/Criar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Criar(TipoTintaSalvarDto modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            _negocio.Cadastrar(modelo.ParaTipoTinta());
            TempData["Mensagem"] = "Tipo de tinta cadastrado.";
            return RedirectToAction(nameof(Index));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    // GET /Painel/TiposTinta/Editar/5
    public IActionResult Editar(int id)
    {
        var tipo = _negocio.ObterPorId(id);
        if (tipo is null) return NotFound();

        ViewData["Id"] = id;
        return View(TipoTintaSalvarDto.De(tipo));
    }

    // POST /Painel/TiposTinta/Editar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, TipoTintaSalvarDto modelo)
    {
        ViewData["Id"] = id;
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            _negocio.Atualizar(modelo.ParaTipoTinta(id));
            TempData["Mensagem"] = "Tipo de tinta atualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    // POST /Painel/TiposTinta/Excluir/5   (em uso, só desativa)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Excluir(int id)
    {
        try
        {
            var resultado = _negocio.Remover(id);
            TempData["Mensagem"] = resultado == ResultadoExclusao.Excluido
                ? "Tipo de tinta excluído."
                : "O tipo de tinta está em uso, então foi desativado.";
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
