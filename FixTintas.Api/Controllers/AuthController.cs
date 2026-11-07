using FixTintas.Api.Seguranca;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FixTintas.Api.Controllers;

// Login e cadastro do app. Rotas públicas, com limite de tentativas por minuto
// (contra quem testa senhas em sequência).
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[EnableRateLimiting(LimiteDeTentativas.Politica)]
public class AuthController : ControllerBase
{
    private readonly UsuarioNegocio _negocio;
    private readonly GeradorDeToken _tokens;

    // INJEÇÃO DE DEPENDÊNCIA: o controller pede o UsuarioNegocio (e o gerador de
    // token) no construtor e o ASP.NET Core entrega prontos (lista no Program.cs).
    public AuthController(UsuarioNegocio negocio, GeradorDeToken tokens)
    {
        _negocio = negocio;
        _tokens = tokens;
    }

    // POST api/auth/login   { "email": "...", "senha": "..." }
    // 200 com o token, ou 401 "E-mail ou senha incorretos."
    [HttpPost("login")]
    public ActionResult<LoginRespostaDto> Login(LoginDto dto)
    {
        var usuario = _negocio.Autenticar(dto.Email, dto.Senha);
        var (token, expiraEm) = _tokens.Gerar(usuario);

        return Ok(new LoginRespostaDto
        {
            Token = token,
            ExpiraEm = expiraEm,
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Perfil = usuario.Perfil,
            TrocarSenha = usuario.TrocarSenha
        });
    }

    // POST api/auth/registrar   { "nome": "...", "email": "...", "senha": "..." }
    // Contas novas são sempre "Cliente". No app, CPF, nascimento e telefone são opcionais.
    // 201 com os dados da conta, 400 (dado inválido) ou 409 (e-mail já cadastrado).
    [HttpPost("registrar")]
    public ActionResult<PerfilDto> Registrar(CadastroClienteDto dto)
    {
        var usuario = _negocio.CadastrarCliente(dto, exigirDadosPessoais: false);
        return Created("/api/perfil", PerfilDto.De(usuario));
    }
}
