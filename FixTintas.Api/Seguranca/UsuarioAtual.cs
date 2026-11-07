using System.Security.Claims;

namespace FixTintas.Api.Seguranca;

// Atalho para os controllers lerem quem está logado, a partir do token:
//   User.ObterId()
// ("User" é a propriedade do ControllerBase com os dados do token já conferido.)
public static class UsuarioAtual
{
    public static int ObterId(this ClaimsPrincipal usuario)
    {
        var texto = usuario.FindFirst(ClaimsDoToken.Id)?.Value;
        if (int.TryParse(texto, out var id)) return id;

        throw new InvalidOperationException("O token não tem a identificação do usuário.");
    }
}
