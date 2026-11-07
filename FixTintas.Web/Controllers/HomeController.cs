using System.Diagnostics;
using FixTintas.Negocio;
using FixTintas.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.Controllers;

// Página inicial da loja: banner, ofertas, "Sobre" e "Contato".
public class HomeController : Controller
{
    private readonly ProdutoNegocio _produtos;

    // INJEÇÃO DE DEPENDÊNCIA: o controller pede o ProdutoNegocio no construtor e o
    // ASP.NET Core entrega pronto (lista de montagem no Program.cs).
    public HomeController(ProdutoNegocio produtos)
    {
        _produtos = produtos;
    }

    // GET /
    public IActionResult Index()
    {
        return View(new InicioViewModel { Ofertas = _produtos.Ofertas() });
    }

    // Página de erro (fora do ambiente de desenvolvimento).
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // GET /Home/Status/404   (endereço que não existe, sem permissão...)
    public IActionResult Status(int id)
    {
        ViewData["Codigo"] = id;
        return View();
    }
}
