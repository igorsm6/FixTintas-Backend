using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using FixTintas.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.ViewComponents;

// ============================================================================
// VIEW COMPONENT do menu lateral (categorias, marcas e filtros).
// Um view component é um "pedaço de página com o seu próprio controller": o
// _Layout chama  @await Component.InvokeAsync("MenuCategorias")  e esta classe
// busca os dados (pelo Negocio) e desenha Views/Shared/Components/MenuCategorias.
// ============================================================================
public class MenuCategoriasViewComponent : ViewComponent
{
    private readonly CategoriaNegocio _categorias;
    private readonly MarcaNegocio _marcas;

    public MenuCategoriasViewComponent(CategoriaNegocio categorias, MarcaNegocio marcas)
    {
        _categorias = categorias;
        _marcas = marcas;
    }

    public IViewComponentResult Invoke()
    {
        var categorias = _categorias.ListarArvore().Select(CategoriaDto.De).ToList();
        var marcas = _marcas.Listar().Select(MarcaDto.De).ToList();

        // O que está escolhido vem da URL: /Produtos?categoria=exterior-vernizes&marcaId=2...
        var consulta = Request.Query;
        var slug = consulta["categoria"].ToString();
        int? marcaId = int.TryParse(consulta["marcaId"], out var idMarca) ? idMarca : null;

        // Abre sozinha a coluna 2 da categoria escolhida (ou a das marcas).
        string? colunaAberta = null;
        if (!string.IsNullOrEmpty(slug))
        {
            var principal = categorias.FirstOrDefault(c => c.Slug == slug || c.Subcategorias.Any(s => s.Slug == slug));
            if (principal is not null) colunaAberta = $"cat-{principal.Id}";
        }
        else if (marcaId is not null)
        {
            colunaAberta = "marcas";
        }

        var naVitrine = string.Equals(RouteData.Values["controller"]?.ToString(), "Produtos", StringComparison.OrdinalIgnoreCase);

        return View(new MenuCategoriasViewModel
        {
            Categorias = categorias,
            Marcas = marcas,
            CategoriaSelecionada = string.IsNullOrEmpty(slug) ? null : slug,
            MarcaSelecionada = marcaId,
            ColunaAberta = colunaAberta,
            MostrarFiltros = naVitrine,
            Filtro = new ProdutoFiltroDto
            {
                Termo = consulta["termo"].ToString(),
                Categoria = string.IsNullOrEmpty(slug) ? null : slug,
                MarcaId = marcaId,
                PrecoMin = decimal.TryParse(consulta["precoMin"], out var minimo) ? minimo : null,
                PrecoMax = decimal.TryParse(consulta["precoMax"], out var maximo) ? maximo : null,
                Ordem = consulta["ordem"].ToString()
            }
        });
    }
}
