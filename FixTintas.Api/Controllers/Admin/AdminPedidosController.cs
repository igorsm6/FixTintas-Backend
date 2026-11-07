using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers.Admin;

// Pedidos de todos os clientes, para a equipe da loja.
[ApiController]
[Route("api/admin/pedidos")]
[Authorize(Roles = Perfis.Equipe)]
public class AdminPedidosController : ControllerBase
{
    private readonly PedidoNegocio _negocio;

    public AdminPedidosController(PedidoNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET api/admin/pedidos?status=Pendente
    [HttpGet]
    public ActionResult<IEnumerable<PedidoResumoDto>> Listar([FromQuery] string? status) =>
        Ok(_negocio.Listar(status));

    // GET api/admin/pedidos/5   (traz "proximosStatus": para onde o pedido pode ir)
    [HttpGet("{id:int}")]
    public ActionResult<PedidoDto> Obter(int id) =>
        Ok(_negocio.Obter(id));

    // PUT api/admin/pedidos/5/status   { "status": "Pago" }
    // Pendente -> Pago -> Enviado -> Entregue; Cancelado devolve o estoque.
    [HttpPut("{id:int}/status")]
    public ActionResult<PedidoDto> AlterarStatus(int id, AlterarStatusDto dto) =>
        Ok(_negocio.AlterarStatus(id, dto.Status));
}
