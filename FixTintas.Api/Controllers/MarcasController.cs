using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers;

// Catálogo público (sem login). O app guarda uma cópia por alguns minutos (cache),
// então estes dados mudam no app pouco depois de mudarem no painel.
//
// GET api/marcas
[ApiController]
[Route("api/marcas")]
[AllowAnonymous]
public class MarcasController : ControllerBase
{
    private readonly MarcaNegocio _negocio;

    public MarcasController(MarcaNegocio negocio)
    {
        _negocio = negocio;
    }

    [HttpGet]
    public ActionResult<IEnumerable<MarcaDto>> Listar() =>
        Ok(_negocio.Listar().Select(MarcaDto.De));
}
