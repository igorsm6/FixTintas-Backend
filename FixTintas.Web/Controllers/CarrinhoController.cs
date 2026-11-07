using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using FixTintas.Web.Infra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.Controllers;

// ============================================================================
// Carrinho e fechamento da compra.
// O carrinho fica na sessão (CarrinhoDaSessao); TODAS as regras (produto à venda,
// cor obrigatória para tinta, estoque, cupom, valores) são do PedidoNegocio —
// as mesmas que o app usa pela Api.
// ============================================================================
public class CarrinhoController : Controller
{
    private readonly PedidoNegocio _pedidos;
    private readonly CarrinhoDaSessao _carrinho;

    public CarrinhoController(PedidoNegocio pedidos, CarrinhoDaSessao carrinho)
    {
        _pedidos = pedidos;
        _carrinho = carrinho;
    }

    // GET /Carrinho
    public IActionResult Index()
    {
        return View(_carrinho.Resumo());
    }

    // POST /Carrinho/Adicionar   (a janela do produto manda produto, cor e quantidade)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Adicionar(ItemCarrinhoDto item, string? voltarPara)
    {
        var destino = Url.IsLocalUrl(voltarPara) ? voltarPara! : Url.Action("Index", "Produtos")!;

        if (!ModelState.IsValid)
        {
            TempData["Erro"] = PrimeiroErro();
            return LocalRedirect(destino);
        }

        try
        {
            _carrinho.Gravar(_pedidos.AdicionarItem(_carrinho.Ler(), item));
            TempData["Mensagem"] = "Produto adicionado ao carrinho!";
            TempData["AbrirCarrinho"] = "1";
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return LocalRedirect(destino);
    }

    // POST /Carrinho/Atualizar   (nova quantidade de um item; 0 tira do carrinho)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Atualizar(int produtoId, int? corId, int quantidade)
    {
        try
        {
            _carrinho.Gravar(_pedidos.AlterarQuantidade(_carrinho.Ler(), produtoId, corId, quantidade));
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // POST /Carrinho/Remover
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remover(int produtoId, int? corId, string? voltarPara)
    {
        _carrinho.Gravar(_pedidos.RemoverItem(_carrinho.Ler(), produtoId, corId));
        TempData["Mensagem"] = "Item removido do carrinho.";

        if (Url.IsLocalUrl(voltarPara)) return LocalRedirect(voltarPara!);
        return RedirectToAction(nameof(Index));
    }

    // POST /Carrinho/AplicarCupom
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AplicarCupom(string? cupom)
    {
        try
        {
            _carrinho.Gravar(_pedidos.AplicarCupom(_carrinho.Ler(), cupom));
            TempData["Mensagem"] = "Cupom aplicado!";
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // POST /Carrinho/RemoverCupom
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoverCupom()
    {
        var carrinho = _carrinho.Ler();
        carrinho.Cupom = null;
        _carrinho.Gravar(carrinho);
        return RedirectToAction(nameof(Index));
    }

    // GET /Carrinho/Finalizar   (quem chegar aqui pelo navegador volta para o carrinho)
    [HttpGet]
    public IActionResult Finalizar() => RedirectToAction(nameof(Index));

    // POST /Carrinho/Finalizar   fecha a compra (precisa estar logado)
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [ActionName("Finalizar")]
    public IActionResult ConfirmarCompra()
    {
        try
        {
            var pedido = _pedidos.Criar(User.ObterId(), _carrinho.Ler());
            _carrinho.Limpar();
            TempData["Mensagem"] = $"Pedido nº {pedido.Id} realizado com sucesso!";
            return RedirectToAction("Detalhes", "Pedidos", new { id = pedido.Id });
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    private string PrimeiroErro() =>
        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
        ?? "Dados inválidos.";
}
