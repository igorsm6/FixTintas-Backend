using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FixTintas.Web.Infra;

// Marca as ações que funcionam mesmo com senha temporária (trocar a senha e sair).
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PermitirSenhaTemporariaAttribute : Attribute { }

// Filtro que vale para TODAS as páginas (registrado no Program.cs).
// Funcionário criado pelo gerente entra com uma senha temporária: enquanto não
// trocar, qualquer página manda ele para "Alterar senha".
public sealed class ExigirTrocaDeSenhaFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext contexto)
    {
        var usuario = contexto.HttpContext.User;
        if (!usuario.EstaLogado() || !usuario.PrecisaTrocarSenha()) return;

        if (contexto.ActionDescriptor.EndpointMetadata.OfType<PermitirSenhaTemporariaAttribute>().Any()) return;

        contexto.Result = new RedirectToActionResult("AlterarSenha", "Conta", new { area = "" });
    }

    public void OnActionExecuted(ActionExecutedContext contexto) { }
}
