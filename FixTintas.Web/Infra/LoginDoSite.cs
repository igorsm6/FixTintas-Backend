using System.Security.Claims;
using FixTintas.Modelos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FixTintas.Web.Infra;

// ============================================================================
// LOGIN DO SITE (cookie)
//
// O site é Razor: quem desenha as páginas é o servidor, então o login é guardado
// num COOKIE criptografado (o navegador manda o cookie em toda página).
// A Api do app usa token (JWT); o site usa cookie. As regras (senha, perfil,
// conta ativa) são as mesmas, porque os dois chamam o mesmo UsuarioNegocio.
//
// Dentro do cookie vão as "claims": id, nome, e-mail, perfil e se a senha é temporária.
// ============================================================================
public static class LoginDoSite
{
    public const string ClaimTrocarSenha = "trocar_senha";

    public static ClaimsPrincipal CriarPrincipal(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Perfil),
            new(ClaimTrocarSenha, usuario.TrocarSenha ? "true" : "false")
        };

        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identidade);
    }

    // Grava o cookie de login (depois que o UsuarioNegocio conferiu e-mail e senha).
    public static Task Entrar(HttpContext contexto, Usuario usuario) =>
        contexto.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, CriarPrincipal(usuario));

    public static Task Sair(HttpContext contexto) =>
        contexto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}

// Atalhos para os controllers e as views lerem quem está logado:
//   User.ObterId()   User.EhEquipe()   User.EhGerente()   User.PrimeiroNome()
public static class UsuarioLogado
{
    public static bool EstaLogado(this ClaimsPrincipal usuario) => usuario.Identity?.IsAuthenticated == true;

    public static int ObterId(this ClaimsPrincipal usuario)
    {
        var texto = usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(texto, out var id)) return id;
        throw new InvalidOperationException("O login não tem a identificação do usuário.");
    }

    public static string Perfil(this ClaimsPrincipal usuario) =>
        usuario.FindFirst(ClaimTypes.Role)?.Value ?? Perfis.Cliente;

    public static bool EhGerente(this ClaimsPrincipal usuario) => usuario.IsInRole(Perfis.Gerente);

    public static bool EhEquipe(this ClaimsPrincipal usuario) =>
        usuario.IsInRole(Perfis.Funcionario) || usuario.IsInRole(Perfis.Gerente);

    public static bool PrecisaTrocarSenha(this ClaimsPrincipal usuario) =>
        usuario.FindFirst(LoginDoSite.ClaimTrocarSenha)?.Value == "true";

    public static string PrimeiroNome(this ClaimsPrincipal usuario)
    {
        var nome = usuario.Identity?.Name ?? "Conta";
        var espaco = nome.IndexOf(' ');
        return espaco > 0 ? nome[..espaco] : nome;
    }
}
