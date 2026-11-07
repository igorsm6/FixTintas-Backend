using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace FixTintas.Api.Documentacao;

// Ensina o documento OpenAPI (/openapi/v1.json) que a Api usa token "Bearer".
// Com isso, a tela do Scalar ganha um campo para colar o token do login e
// manda o cabeçalho  Authorization: Bearer <token>  sozinha nos testes.
// (É o exemplo da documentação oficial do .NET 9 para JWT + OpenAPI.)
internal sealed class EsquemaBearerTransformer : IOpenApiDocumentTransformer
{
    private readonly IAuthenticationSchemeProvider _esquemas;

    public EsquemaBearerTransformer(IAuthenticationSchemeProvider esquemas)
    {
        _esquemas = esquemas;
    }

    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var esquemasDeLogin = await _esquemas.GetAllSchemesAsync();
        if (!esquemasDeLogin.Any(e => e.Name == "Bearer")) return;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Description = "Cole aqui o \"token\" devolvido por POST /api/auth/login."
            }
        };

        // Marca todas as operações como "aceitam token" (as públicas simplesmente ignoram).
        foreach (var operacao in document.Paths.Values.SelectMany(caminho => caminho.Operations))
        {
            operacao.Value.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme }
                }] = Array.Empty<string>()
            });
        }
    }
}
