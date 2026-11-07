using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FixTintas.Api.Seguranca;

// Marca os controllers/ações que funcionam mesmo com senha temporária
// (no FixTintas: /api/perfil, onde fica a troca de senha).
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PermitirSenhaTemporariaAttribute : Attribute { }

// Filtro que vale para TODOS os controllers (registrado no Program.cs).
//
// Funcionário criado pelo gerente nasce com senha temporária e precisa trocá-la.
// Esconder os botões na tela não é segurança (alguém poderia chamar a Api direto),
// então a própria Api responde 403 em qualquer rota protegida até a senha ser trocada.
public sealed class BloquearSenhaTemporariaFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext contexto)
    {
        var usuario = contexto.HttpContext.User;
        if (usuario.Identity?.IsAuthenticated != true) return;
        if (usuario.FindFirst(ClaimsDoToken.TrocarSenha)?.Value != "true") return;

        var metadados = contexto.HttpContext.GetEndpoint()?.Metadata;
        if (metadados is null) return;

        // Rota pública (sem [Authorize] ou com [AllowAnonymous]): deixa passar.
        if (metadados.GetMetadata<IAuthorizeData>() is null) return;
        if (metadados.GetMetadata<IAllowAnonymous>() is not null) return;

        // Rota liberada de propósito para quem precisa trocar a senha.
        if (metadados.GetMetadata<PermitirSenhaTemporariaAttribute>() is not null) return;

        contexto.Result = new ObjectResult(new { mensagem = "Troque a senha temporária antes de continuar." })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }

    public void OnActionExecuted(ActionExecutedContext contexto) { }
}
