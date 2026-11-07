namespace FixTintas.Api.Seguranca;

// Nome da regra de "limite de tentativas" usada no login e no cadastro
// (configurada no Program.cs com AddRateLimiter).
// Sem isso, um robô poderia testar milhares de senhas por minuto.
public static class LimiteDeTentativas
{
    public const string Politica = "auth";

    // Quantas chamadas cada endereço IP pode fazer por minuto nessas rotas.
    public const int PorMinuto = 20;
}
