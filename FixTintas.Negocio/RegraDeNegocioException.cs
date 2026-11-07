namespace FixTintas.Negocio;

// ============================================================================
// EXCEÇÕES DO NEGÓCIO
//
// Quando uma regra não é cumprida, o Negocio lança uma destas exceções com a
// mensagem pronta para o usuário ler. Quem mostra a mensagem é a camada de cima:
//   - Web (site): o controller pega a exceção (catch NegocioException) e mostra
//     a mensagem na tela, como no exemplo do professor.
//   - Api (app): a classe TratadorDeErros transforma a exceção em resposta JSON
//     { "mensagem": "..." } com o código HTTP de cada tipo:
//
//   RegraDeNegocioException       -> 400 Bad Request   (dado inválido, cupom vencido...)
//   CredenciaisInvalidasException -> 401 Unauthorized  (e-mail ou senha errados no login)
//   AcessoNegadoException         -> 403 Forbidden     (sem permissão para aquela ação)
//   NaoEncontradoException        -> 404 Not Found     (id que não existe)
//   ConflitoException             -> 409 Conflict      (e-mail repetido, estoque acabou...)
// ============================================================================

// Base de todas: "catch (NegocioException ex)" pega qualquer uma delas.
public abstract class NegocioException : Exception
{
    protected NegocioException(string mensagem) : base(mensagem) { }
}

// Uma regra de negócio foi violada (CPF inválido, preço negativo, cupom vencido...).
public class RegraDeNegocioException : NegocioException
{
    public RegraDeNegocioException(string mensagem) : base(mensagem) { }
}

// Login com e-mail ou senha errados. A mensagem é sempre a mesma, de propósito:
// assim ninguém descobre quais e-mails existem no sistema.
public class CredenciaisInvalidasException : NegocioException
{
    public CredenciaisInvalidasException() : base("E-mail ou senha incorretos.") { }
}

// O usuário está logado, mas não pode fazer aquilo (ex.: funcionário mudando preço).
public class AcessoNegadoException : NegocioException
{
    public AcessoNegadoException(string mensagem) : base(mensagem) { }
}

// O registro pedido não existe (ou não é deste usuário).
public class NaoEncontradoException : NegocioException
{
    public NaoEncontradoException(string mensagem) : base(mensagem) { }
}

// O pedido não combina com o que está gravado agora: e-mail já cadastrado,
// estoque que acabou, alguém alterou o mesmo registro ao mesmo tempo...
public class ConflitoException : NegocioException
{
    public ConflitoException(string mensagem) : base(mensagem) { }
}

// O que aconteceu num pedido de exclusão: quando o registro ainda é usado (um produto
// que já foi vendido, por exemplo), ele não é apagado, só desativado — assim os
// pedidos antigos continuam inteiros.
public enum ResultadoExclusao
{
    Excluido,
    Desativado
}
