using FixTintas.Api.Seguranca;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers;

// Cores favoritas do cliente logado (as mesmas do site).
[ApiController]
[Route("api/favoritos")]
[Authorize]
public class FavoritosController : ControllerBase
{
    private readonly FavoritoNegocio _negocio;

    public FavoritosController(FavoritoNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET api/favoritos
    [HttpGet]
    public ActionResult<IEnumerable<CorDto>> Listar() =>
        Ok(_negocio.Listar(User.ObterId()).Select(CorDto.De));

    // PUT api/favoritos/7   -> 204 (marcar de novo não dá erro)
    [HttpPut("{corId:int}")]
    public IActionResult Adicionar(int corId)
    {
        _negocio.Adicionar(User.ObterId(), corId);
        return NoContent();
    }

    // DELETE api/favoritos/7   -> 204
    [HttpDelete("{corId:int}")]
    public IActionResult Remover(int corId)
    {
        _negocio.Remover(User.ObterId(), corId);
        return NoContent();
    }
}
