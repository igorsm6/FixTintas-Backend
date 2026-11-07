using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using FixTintas.Web.Infra;
using FixTintas.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.Controllers;

// Catálogo de cores e as cores favoritas do cliente (as mesmas favoritas do app).
public class CoresController : Controller
{
    private readonly CorNegocio _cores;
    private readonly FavoritoNegocio _favoritos;

    public CoresController(CorNegocio cores, FavoritoNegocio favoritos)
    {
        _cores = cores;
        _favoritos = favoritos;
    }

    // GET /Cores                 todas
    // GET /Cores?familia=Azul    só uma família
    // GET /Cores?favoritas=true  só as favoritas (precisa estar logado)
    public IActionResult Index(string? familia, bool favoritas = false)
    {
        var logado = User.EstaLogado();
        var idsFavoritas = logado ? _favoritos.IdsDasCores(User.ObterId()) : new HashSet<int>();

        var cores = favoritas && logado
            ? _favoritos.Listar(User.ObterId())
            : _cores.Listar(familia);

        return View(new CoresViewModel
        {
            Cores = cores.Select(CorDto.De).ToList(),
            Familias = _cores.ListarFamilias(),
            FamiliaAtual = favoritas ? null : familia,
            SoFavoritas = favoritas && logado,
            Favoritas = idsFavoritas,
            Logado = logado
        });
    }

    // POST /Cores/Favoritar/7
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult Favoritar(int id, string? voltarPara)
    {
        try
        {
            _favoritos.Adicionar(User.ObterId(), id);
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return Voltar(voltarPara);
    }

    // POST /Cores/Desfavoritar/7
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult Desfavoritar(int id, string? voltarPara)
    {
        _favoritos.Remover(User.ObterId(), id);
        return Voltar(voltarPara);
    }

    private IActionResult Voltar(string? voltarPara) =>
        Url.IsLocalUrl(voltarPara) ? LocalRedirect(voltarPara!) : RedirectToAction(nameof(Index));
}
