using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers;

// POST api/calculos/tinta: quanta tinta comprar (público, não precisa de login).
[ApiController]
[Route("api/calculos")]
[AllowAnonymous]
public class CalculosController : ControllerBase
{
    private readonly CalculoNegocio _negocio;

    public CalculosController(CalculoNegocio negocio)
    {
        _negocio = negocio;
    }

    //   { "alturaM": 2.8, "larguraM": 4, "areaDescontarM2": 1.6, "tipoTintaId": 1 }
    [HttpPost("tinta")]
    public ActionResult<CalculoTintaResultadoDto> CalcularTinta(CalculoTintaEntradaDto dto) =>
        Ok(_negocio.CalcularTinta(dto));
}
