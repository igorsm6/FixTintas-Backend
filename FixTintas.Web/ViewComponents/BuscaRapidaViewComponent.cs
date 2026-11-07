using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using FixTintas.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.ViewComponents;

// Lista de nomes de produto para as sugestões que aparecem enquanto o cliente
// digita na busca do cabeçalho (o site antigo fazia isso com uma lista fixa no script.js;
// agora a lista vem do banco).
public class BuscaRapidaViewComponent : ViewComponent
{
    private readonly ProdutoNegocio _produtos;

    public BuscaRapidaViewComponent(ProdutoNegocio produtos)
    {
        _produtos = produtos;
    }

    public IViewComponentResult Invoke()
    {
        var sugestoes = _produtos.Vitrine(new ProdutoFiltroDto { Ordem = "az" })
            .Select(i => new SugestaoBuscaDto { Nome = i.Titulo, Sub = i.CategoriaNome })
            .ToList();

        return View(sugestoes);
    }
}
