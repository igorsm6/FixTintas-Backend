using System.Security.Claims;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FixTintas.Web.Infra;

// Roda em TODA página aberta por alguém logado (evento OnValidatePrincipal do cookie,
// ligado no Program.cs).
//
// Por que conferir o banco se o cookie é criptografado? Porque ele vale por horas.
// Sem esta conferência, um funcionário desativado pelo gerente continuaria entrando
// no painel até o cookie vencer. Assim, desativou -> na próxima página ele sai.
public static class ValidacaoDoLogin
{
    public static async Task ConferirUsuarioNoBanco(CookieValidatePrincipalContext contexto)
    {
        var principal = contexto.Principal;
        if (principal is null || !int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id))
        {
            await DesfazerLogin(contexto);
            return;
        }

        var negocio = contexto.HttpContext.RequestServices.GetRequiredService<UsuarioNegocio>();
        var usuario = negocio.ObterAtivo(id);

        // Conta desativada (ou apagada) ou perfil diferente: desfaz o login.
        if (usuario is null || principal.FindFirst(ClaimTypes.Role)?.Value != usuario.Perfil)
        {
            await DesfazerLogin(contexto);
            return;
        }

        // O nome mudou ou a senha temporária foi trocada: atualiza o cookie.
        var trocarSenha = principal.FindFirst(LoginDoSite.ClaimTrocarSenha)?.Value == "true";
        if (trocarSenha != usuario.TrocarSenha || principal.Identity?.Name != usuario.Nome)
        {
            contexto.ReplacePrincipal(LoginDoSite.CriarPrincipal(usuario));
            contexto.ShouldRenew = true;
        }
    }

    private static async Task DesfazerLogin(CookieValidatePrincipalContext contexto)
    {
        contexto.RejectPrincipal();
        await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
