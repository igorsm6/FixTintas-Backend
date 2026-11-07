using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using FixTintas.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.Controllers;

// Vitrine da loja. Os filtros chegam pela URL e a busca é feita pela stored
// procedure sp_Produtos_Vitrine (pelo ProdutoNegocio).
public class ProdutosController : Controller
{
    private readonly ProdutoNegocio _produtos;
    private readonly CategoriaNegocio _categorias;
    private readonly MarcaNegocio _marcas;

    public ProdutosController(ProdutoNegocio produtos, CategoriaNegocio categorias, MarcaNegocio marcas)
    {
        _produtos = produtos;
        _categorias = categorias;
        _marcas = marcas;
    }

    // GET /Produtos
    // GET /Produtos?categoria=exterior-vernizes
    // GET /Produtos?termo=verniz&precoMin=10&precoMax=200&ordem=menor
    // GET /Produtos?marcaId=2
    public IActionResult Index(ProdutoFiltroDto filtro)
    {
        var itens = _produtos.Vitrine(filtro);

        // Título da página: o que o cliente está vendo.
        var titulo = "Todos os Produtos";
        if (!string.IsNullOrWhiteSpace(filtro.Termo))
        {
            titulo = $"Resultados para \"{filtro.Termo.Trim()}\"";
        }
        else if (_categorias.ObterPorSlug(filtro.Categoria) is { } categoria)
        {
            titulo = categoria.CategoriaPai is null ? categoria.Nome : $"{categoria.CategoriaPai.Nome} › {categoria.Nome}";
        }
        else if (filtro.MarcaId is int marcaId && _marcas.ObterPorId(marcaId) is { } marca)
        {
            titulo = marca.Nome;
        }

        return View(new VitrineViewModel { Itens = itens, Filtro = filtro, Titulo = titulo });
    }
}
