using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers;

// Catálogo público (sem login). O app guarda uma cópia por alguns minutos (cache),
// então estes dados mudam no app pouco depois de mudarem no painel.
//
// GET api/tipos-tinta   -> com rendimento, demãos e fator de contraste (usados pelo app)
[ApiController]
[Route("api/tipos-tinta")]
[AllowAnonymous]
public class TiposTintaController : ControllerBase
{
    private readonly TipoTintaNegocio _negocio;

    public TiposTintaController(TipoTintaNegocio negocio)
    {
        _negocio = negocio;
    }

    [HttpGet]
    public ActionResult<IEnumerable<TipoTintaDto>> Listar() =>
        Ok(_negocio.Listar().Select(TipoTintaDto.De));
}
