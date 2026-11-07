using FixTintas.Api.Seguranca;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers;

// Dados da conta de quem está logado. Funciona também com senha temporária,
// porque é aqui que a senha é trocada.
[ApiController]
[Route("api/perfil")]
[Authorize]
[PermitirSenhaTemporaria]
public class PerfilController : ControllerBase
{
    private readonly UsuarioNegocio _negocio;

    public PerfilController(UsuarioNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET api/perfil
    [HttpGet]
    public ActionResult<PerfilDto> Obter() =>
        Ok(PerfilDto.De(_negocio.ObterPorId(User.ObterId())));

    // PUT api/perfil   { "nome": "...", "telefone": "..." }
    [HttpPut]
    public ActionResult<PerfilDto> Atualizar(AtualizarPerfilDto dto) =>
        Ok(PerfilDto.De(_negocio.AtualizarPerfil(User.ObterId(), dto)));

    // POST api/perfil/alterar-senha   { "senhaAtual": "...", "novaSenha": "..." }   -> 204
    [HttpPost("alterar-senha")]
    public IActionResult AlterarSenha(AlterarSenhaDto dto)
    {
        _negocio.AlterarSenha(User.ObterId(), dto);
        return NoContent();
    }
}
