using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers;

// Catálogo público (sem login). O app guarda uma cópia por alguns minutos (cache),
// então estes dados mudam no app pouco depois de mudarem no painel.
//
// GET api/cores?familia=Azul&busca=oceano   e   GET api/cores/5
[ApiController]
[Route("api/cores")]
[AllowAnonymous]
public class CoresController : ControllerBase
{
    private readonly CorNegocio _negocio;

    public CoresController(CorNegocio negocio)
    {
        _negocio = negocio;
    }

    [HttpGet]
    public ActionResult<IEnumerable<CorDto>> Listar([FromQuery] string? familia, [FromQuery] string? busca) =>
        Ok(_negocio.Listar(familia, busca).Select(CorDto.De));

    [HttpGet("{id:int}")]
    public ActionResult<CorDto> Obter(int id)
    {
        var cor = _negocio.ObterAtiva(id);
        if (cor is null) return NotFound(new { mensagem = "Cor não encontrada." });
        return Ok(CorDto.De(cor));
    }
}
