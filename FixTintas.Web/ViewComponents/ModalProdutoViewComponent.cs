using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using FixTintas.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.ViewComponents;

// A janela "Adicionar ao carrinho" (cor, volume, quantidade). As cores vêm do banco,
// as mesmas do app. Usada na página inicial e na página de produtos.
public class ModalProdutoViewComponent : ViewComponent
{
    private readonly CorNegocio _cores;

    public ModalProdutoViewComponent(CorNegocio cores)
    {
        _cores = cores;
    }

    public IViewComponentResult Invoke()
    {
        return View(new ModalProdutoViewModel
        {
            Cores = _cores.Listar().Select(CorDto.De).ToList(),
            // Depois de adicionar, o cliente volta para a mesma página (com os mesmos filtros).
            VoltarPara = Request.Path + Request.QueryString
        });
    }
}
