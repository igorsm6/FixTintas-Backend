using FixTintas.Api.Seguranca;
using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers.Admin;

// A equipe da loja (funcionários e gerentes). Só o gerente usa.
[ApiController]
[Route("api/admin/funcionarios")]
[Authorize(Roles = Perfis.Gerente)]
public class AdminFuncionariosController : ControllerBase
{
    private readonly UsuarioNegocio _negocio;

    public AdminFuncionariosController(UsuarioNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET api/admin/funcionarios
    [HttpGet]
    public ActionResult<IEnumerable<FuncionarioDto>> Listar() =>
        Ok(_negocio.ListarEquipe().Select(FuncionarioDto.De));

    // POST api/admin/funcionarios   { "nome": "...", "email": "...", "perfil": "Funcionario" }
    // Devolve a senha temporária UMA vez (sem "senhaTemporaria" no corpo, o sistema gera uma).
    [HttpPost]
    public ActionResult<FuncionarioCriadoDto> Criar(FuncionarioCriarDto dto)
    {
        var criado = _negocio.CriarFuncionario(dto);
        return Created($"/api/admin/funcionarios/{criado.Funcionario.Id}", criado);
    }

    // PATCH api/admin/funcionarios/5   { "ativo": false }
    [HttpPatch("{id:int}")]
    public ActionResult<FuncionarioDto> AlterarAtivo(int id, FuncionarioAtivoDto dto) =>
        Ok(FuncionarioDto.De(_negocio.AlterarAtivo(User.ObterId(), id, dto.Ativo ?? true)));

    // POST api/admin/funcionarios/5/redefinir-senha   -> nova senha temporária
    [HttpPost("{id:int}/redefinir-senha")]
    public ActionResult<FuncionarioCriadoDto> RedefinirSenha(int id) =>
        Ok(_negocio.RedefinirSenha(User.ObterId(), id));
}
