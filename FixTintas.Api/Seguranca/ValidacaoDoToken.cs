using System.Security.Claims;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace FixTintas.Api.Seguranca;

// Roda em TODA requisição que chega com token, logo depois que a assinatura foi
// conferida (evento OnTokenValidated, ligado no Program.cs).
//
// Por que conferir o banco se o token já é assinado? Porque o token vale por horas.
// Sem esta conferência, um funcionário desativado pelo gerente continuaria usando a
// Api até o token vencer. Assim, desativou -> na próxima chamada ele leva 401.
public static class ValidacaoDoToken
{
    public static Task ConferirUsuarioNoBanco(TokenValidatedContext contexto)
    {
        var principal = contexto.Principal;
        if (principal is null || !int.TryParse(principal.FindFirst(ClaimsDoToken.Id)?.Value, out var id))
        {
            contexto.Fail("Token sem identificação do usuário.");
            return Task.CompletedTask;
        }

        var negocio = contexto.HttpContext.RequestServices.GetRequiredService<UsuarioNegocio>();
        var usuario = negocio.ObterAtivo(id);

        if (usuario is null)
        {
            contexto.Fail("Conta inativa ou inexistente.");
            return Task.CompletedTask;
        }

        if (principal.FindFirst(ClaimsDoToken.Perfil)?.Value != usuario.Perfil)
        {
            contexto.Fail("O perfil da conta mudou. Entre novamente.");
            return Task.CompletedTask;
        }

        // Acrescenta, só nesta requisição, se o usuário ainda está com senha temporária.
        // Quem usa essa informação é o BloquearSenhaTemporariaFilter.
        if (principal.Identity is ClaimsIdentity identidade)
            identidade.AddClaim(new Claim(ClaimsDoToken.TrocarSenha, usuario.TrocarSenha ? "true" : "false"));

        return Task.CompletedTask;
    }
}
