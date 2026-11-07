namespace FixTintas.Api.Seguranca;

// Espelho da seção "Jwt" do appsettings.json:
//   "Jwt": { "Chave": "...", "Emissor": "FixTintas.Api", "Publico": "FixTintas.App", "ExpiracaoHoras": 8 }
public class ConfiguracaoJwt
{
    // Segredo usado para ASSINAR o token. Quem tem a chave consegue fabricar tokens,
    // então ela nunca vai para o app. Precisa ter pelo menos 32 caracteres.
    public string Chave { get; set; } = string.Empty;

    // Quem emitiu o token (esta Api) e para quem ele serve (o app).
    public string Emissor { get; set; } = "FixTintas.Api";
    public string Publico { get; set; } = "FixTintas.App";

    // Depois disso o token vence e o app pede login de novo.
    public int ExpiracaoHoras { get; set; } = 8;
}
