using System.Security.Claims;
using System.Text;
using FixTintas.Modelos;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FixTintas.Api.Seguranca;

// ============================================================================
// GERADOR DE TOKEN (JWT)
//
// Depois do login, a Api entrega ao app um TOKEN: um texto assinado que diz
// "este é o usuário 7, perfil Cliente, válido até 18h". O app manda esse texto
// em toda chamada, no cabeçalho  Authorization: Bearer <token>.
//
// A Api não guarda sessão: ela só confere a ASSINATURA. Se alguém mudar uma
// letra do token (por exemplo, trocar "Cliente" por "Gerente"), a assinatura não
// bate e a Api responde 401.
// ============================================================================
public class GeradorDeToken
{
    private readonly ConfiguracaoJwt _configuracao;
    private readonly SigningCredentials _credenciais;

    public GeradorDeToken(ConfiguracaoJwt configuracao)
    {
        _configuracao = configuracao;

        // HMAC-SHA256: assinatura com a chave secreta do appsettings.json.
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracao.Chave));
        _credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);
    }

    public (string Token, DateTime ExpiraEm) Gerar(Usuario usuario)
    {
        var expiraEm = DateTime.UtcNow.AddHours(_configuracao.ExpiracaoHoras);

        var descritor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimsDoToken.Id, usuario.Id.ToString()),
                new Claim(ClaimsDoToken.Nome, usuario.Nome),
                new Claim(ClaimsDoToken.Email, usuario.Email),
                new Claim(ClaimsDoToken.Perfil, usuario.Perfil)
            }),
            Issuer = _configuracao.Emissor,
            Audience = _configuracao.Publico,
            Expires = expiraEm,
            SigningCredentials = _credenciais
        };

        return (new JsonWebTokenHandler().CreateToken(descritor), expiraEm);
    }
}
