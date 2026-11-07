using FixTintas.Api.Seguranca;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers;

// Carrinho, compra e histórico do cliente logado.
// Todos os valores são calculados pelo PedidoNegocio: o app só manda produto, cor e quantidade.
[ApiController]
[Route("api/pedidos")]
[Authorize]
public class PedidosController : ControllerBase
{
    private readonly PedidoNegocio _negocio;

    public PedidosController(PedidoNegocio negocio)
    {
        _negocio = negocio;
    }

    // POST api/pedidos/orcamento   "Quanto custa?" (não grava nada e não exige login)
    //   { "itens": [ { "produtoId": 3, "corId": 10, "quantidade": 2 } ], "cupom": "BEMVINDO10" }
    [HttpPost("orcamento")]
    [AllowAnonymous]
    public ActionResult<OrcamentoDto> Orcamento(CarrinhoDto dto) =>
        Ok(_negocio.Orcar(dto));

    // POST api/pedidos   fecha a compra -> 201 com o pedido
    [HttpPost]
    public ActionResult<PedidoDto> Criar(CarrinhoDto dto)
    {
        var pedido = _negocio.Criar(User.ObterId(), dto);
        return CreatedAtAction(nameof(Obter), new { id = pedido.Id }, pedido);
    }

    // GET api/pedidos   histórico (mais recentes primeiro)
    [HttpGet]
    public ActionResult<IEnumerable<PedidoResumoDto>> Listar() =>
        Ok(_negocio.ListarDoCliente(User.ObterId()));

    // GET api/pedidos/5
    [HttpGet("{id:int}")]
    public ActionResult<PedidoDto> Obter(int id) =>
        Ok(_negocio.ObterDoCliente(id, User.ObterId()));

    // POST api/pedidos/5/cancelar   (só enquanto está Pendente; o estoque volta)
    [HttpPost("{id:int}/cancelar")]
    public ActionResult<PedidoDto> Cancelar(int id) =>
        Ok(_negocio.CancelarPeloCliente(id, User.ObterId()));
}
