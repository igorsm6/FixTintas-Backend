using FixTintas.Negocio;
using FixTintas.Repositorio;
using Microsoft.AspNetCore.Diagnostics;

namespace FixTintas.Api.Erros;

// ============================================================================
// TRATADOR DE ERROS — um lugar só para transformar exceção em resposta HTTP.
//
// No exemplo do professor, cada action tinha o seu try/catch:
//     catch (RegraDeNegocioException ex) { return BadRequest(new { erro = ex.Message }); }
// Com muitos controllers isso se repete dezenas de vezes. Aqui, qualquer exceção
// que escapar de qualquer controller passa por esta classe (ligada no Program.cs
// com AddExceptionHandler + UseExceptionHandler) e vira:
//
//     { "mensagem": "texto do erro" }
//
// "mensagem" é o campo que o app Flutter lê para mostrar o erro na tela.
// ============================================================================
public class TratadorDeErros : IExceptionHandler
{
    private readonly ILogger<TratadorDeErros> _logger;
    private readonly IHostEnvironment _ambiente;

    public TratadorDeErros(ILogger<TratadorDeErros> logger, IHostEnvironment ambiente)
    {
        _logger = logger;
        _ambiente = ambiente;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excecao, CancellationToken cancelamento)
    {
        var (status, mensagem) = excecao switch
        {
            RegraDeNegocioException e => (StatusCodes.Status400BadRequest, e.Message),
            CredenciaisInvalidasException e => (StatusCodes.Status401Unauthorized, e.Message),
            AcessoNegadoException e => (StatusCodes.Status403Forbidden, e.Message),
            NaoEncontradoException e => (StatusCodes.Status404NotFound, e.Message),
            ConflitoException e => (StatusCodes.Status409Conflict, e.Message),
            ConflitoDeGravacaoException => (StatusCodes.Status409Conflict, "Os dados mudaram enquanto você salvava. Atualize e tente de novo."),
            BadHttpRequestException e => (e.StatusCode, e.StatusCode == StatusCodes.Status413PayloadTooLarge
                ? "Arquivo grande demais."
                : "Requisição inválida."),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno no servidor. Tente novamente em instantes.")
        };

        contexto.Response.StatusCode = status;

        if (status == StatusCodes.Status500InternalServerError)
        {
            // Erro inesperado (banco fora do ar, bug...): grava no console da Api.
            _logger.LogError(excecao, "Erro não tratado em {Metodo} {Caminho}", contexto.Request.Method, contexto.Request.Path);

            // Em desenvolvimento, devolve também o detalhe, para facilitar achar o problema.
            if (_ambiente.IsDevelopment())
            {
                await contexto.Response.WriteAsJsonAsync(new { mensagem, detalhe = excecao.Message }, cancelamento);
                return true;
            }
        }

        await contexto.Response.WriteAsJsonAsync(new { mensagem }, cancelamento);
        return true;
    }
}
