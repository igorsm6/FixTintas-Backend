namespace FixTintas.Api.Seguranca;

// "Claims" são as informações que vão DENTRO do token (assinadas: ninguém altera).
// Os nomes curtos ("sub", "name"...) são os padrão do JWT.
public static class ClaimsDoToken
{
    public const string Id = "sub";      // id do usuário
    public const string Nome = "name";
    public const string Email = "email";
    public const string Perfil = "role"; // Cliente | Funcionario | Gerente

    // Esta NÃO vai no token: é acrescentada a cada requisição, lendo o banco
    // (ver ValidacaoDoToken), para refletir na hora a troca da senha temporária.
    public const string TrocarSenha = "trocar_senha";
}
