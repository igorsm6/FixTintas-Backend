using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FixTintas.Web.Areas.Painel.Controllers;

// Categorias do menu da loja (só gerente). Mesmo molde do ClientesController do professor.
[Area("Painel")]
[Authorize(Roles = Perfis.Gerente)]
public class CategoriasController : Controller
{
    private readonly CategoriaNegocio _negocio;

    public CategoriasController(CategoriaNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET /Painel/Categorias
    public IActionResult Index()
    {
        return View(_negocio.ListarTodas().Select(CategoriaDto.De).ToList());
    }

    // GET /Painel/Categorias/Criar
    public IActionResult Criar()
    {
        PrepararListas(null, null);
        return View(new CategoriaSalvarDto());
    }

    // POST /Painel/Categorias/Criar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Criar(CategoriaSalvarDto modelo)
    {
        if (!ModelState.IsValid)
        {
            PrepararListas(modelo.CategoriaPaiId, null);
            return View(modelo);
        }

        try
        {
            _negocio.Cadastrar(modelo.ParaCategoria());
            TempData["Mensagem"] = "Categoria cadastrada.";
            return RedirectToAction(nameof(Index));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            PrepararListas(modelo.CategoriaPaiId, null);
            return View(modelo);
        }
    }

    // GET /Painel/Categorias/Editar/5
    public IActionResult Editar(int id)
    {
        var categoria = _negocio.ObterPorId(id);
        if (categoria is null) return NotFound();

        ViewData["Id"] = id;
        PrepararListas(categoria.CategoriaPaiId, id);
        return View(CategoriaSalvarDto.De(categoria));
    }

    // POST /Painel/Categorias/Editar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, CategoriaSalvarDto modelo)
    {
        ViewData["Id"] = id;
        if (!ModelState.IsValid)
        {
            PrepararListas(modelo.CategoriaPaiId, id);
            return View(modelo);
        }

        try
        {
            _negocio.Atualizar(modelo.ParaCategoria(id));
            TempData["Mensagem"] = "Categoria atualizada.";
            return RedirectToAction(nameof(Index));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            PrepararListas(modelo.CategoriaPaiId, id);
            return View(modelo);
        }
    }

    // POST /Painel/Categorias/Excluir/5   (com produtos ou subcategorias, só desativa)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Excluir(int id)
    {
        try
        {
            var resultado = _negocio.Remover(id);
            TempData["Mensagem"] = resultado == ResultadoExclusao.Excluido
                ? "Categoria excluída."
                : "A categoria tem produtos ou subcategorias, então foi desativada (some da loja).";
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // Só categorias principais podem ser "pai" (o menu tem dois níveis).
    private void PrepararListas(int? paiSelecionado, int? idDaPropria)
    {
        var principais = _negocio.ListarTodas()
            .Where(c => c.CategoriaPaiId is null && c.Id != idDaPropria)
            .Select(c => new { c.Id, c.Nome });

        ViewData["Pais"] = new SelectList(principais, "Id", "Nome", paiSelecionado);
    }
}
