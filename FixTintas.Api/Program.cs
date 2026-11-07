using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using FixTintas.Api.Documentacao;
using FixTintas.Api.Erros;
using FixTintas.Api.Seguranca;
using FixTintas.Negocio;
using FixTintas.Repositorio;
using FixTintas.Repositorio.Contexto;
using FixTintas.Repositorio.Dados;
using FixTintas.Repositorio.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

// ============================================================================
// API DO FIXTINTAS — a única API do sistema, em JSON, para o aplicativo Flutter.
// (O site é o projeto FixTintas.Web, em Razor, e NÃO passa por aqui: ele chama o
// mesmo Negocio direto. As regras são as mesmas para os dois.)
// ============================================================================

// Números sempre com PONTO ("3.6"), como o app manda nos formulários.
// Sem isto, num Windows em português "3.6" viraria 36.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// A connection string vem do appsettings.json — nunca do código-fonte.
var connectionString = builder.Configuration.GetConnectionString("Padrao")
    ?? throw new InvalidOperationException("Connection string 'Padrao' não encontrada no appsettings.json.");

// ---------- Injeção de dependência ----------
// Esta é a LISTA DE MONTAGEM do sistema (igual à do exemplo do professor). Cada linha
// ensina o ASP.NET Core a criar uma peça. Quando um controller pede um PedidoNegocio
// no construtor, o ASP.NET monta a cadeia inteira sozinho:
//
//   PedidosController   pede  PedidoNegocio
//   PedidoNegocio       pede  IPedidoRepositorio   -> recebe um PedidoRepositorio
//   PedidoRepositorio   pede  AppDbContext         -> recebe com a connection string
//
// Ninguém escreve "new" para essas classes. Explicação passo a passo, com os
// erros mais comuns: INJECAO-DE-DEPENDENCIA.md, na pasta da solution.
//
// ATENÇÃO: este bloco é IGUAL no Program.cs da Web. Mudou aqui, mude lá também.

// 1) O banco: "quando alguém pedir um AppDbContext, crie um usando SQL Server
//    e esta connection string".
builder.Services.AddDbContext<AppDbContext>(opcoes => opcoes.UseSqlServer(connectionString));

// 2) e 3) Cada entidade tem duas linhas: o repositório (interface -> classe) e o negócio.
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<UsuarioNegocio>();

builder.Services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
builder.Services.AddScoped<CategoriaNegocio>();

builder.Services.AddScoped<IMarcaRepositorio, MarcaRepositorio>();
builder.Services.AddScoped<MarcaNegocio>();

builder.Services.AddScoped<ITipoTintaRepositorio, TipoTintaRepositorio>();
builder.Services.AddScoped<TipoTintaNegocio>();

builder.Services.AddScoped<ICorRepositorio, CorRepositorio>();
builder.Services.AddScoped<CorNegocio>();

builder.Services.AddScoped<IProdutoRepositorio, ProdutoRepositorio>();
builder.Services.AddScoped<ProdutoNegocio>();

builder.Services.AddScoped<ICupomRepositorio, CupomRepositorio>();
builder.Services.AddScoped<CupomNegocio>();

builder.Services.AddScoped<IPedidoRepositorio, PedidoRepositorio>();
builder.Services.AddScoped<PedidoNegocio>();      // usa também os repositórios de produto, cor e cupom

builder.Services.AddScoped<IFavoritoRepositorio, FavoritoRepositorio>();
builder.Services.AddScoped<FavoritoNegocio>();

builder.Services.AddScoped<ISimulacaoRepositorio, SimulacaoRepositorio>();
builder.Services.AddScoped<SimulacaoNegocio>();

builder.Services.AddScoped<CalculoNegocio>();     // só regras: usa os repositórios de tipo e de produto

// 4) As regras da loja (seção "Loja" do appsettings.json) e a pasta dos arquivos.
//    AddSingleton = um objeto só para o sistema inteiro.
//    Na Api, os arquivos (fotos dos projetos do app) ficam em App_Data: pasta que
//    NÃO é publicada, então as fotos só saem pelo endpoint, para o dono.
var regrasDaLoja = builder.Configuration.GetSection("Loja").Get<RegrasDaLoja>() ?? new RegrasDaLoja();
builder.Services.AddSingleton(regrasDaLoja);
builder.Services.AddSingleton<IArmazenamentoDeArquivos>(
    new ArmazenamentoDeArquivos(Path.Combine(builder.Environment.ContentRootPath, "App_Data")));

// AddScoped = um objeto novo para cada requisição, jogado fora quando a resposta sai.
// O AddDbContext funciona do mesmo jeito: cada requisição ganha o seu contexto.
// --------------------------------------------

// ---------- Login com token (JWT) ----------
// Lê a seção "Jwt" do appsettings.json (chave secreta, emissor, validade).
var configuracaoJwt = builder.Configuration.GetSection("Jwt").Get<ConfiguracaoJwt>() ?? new ConfiguracaoJwt();
if (Encoding.UTF8.GetByteCount(configuracaoJwt.Chave) < 32)
    throw new InvalidOperationException("A chave 'Jwt:Chave' do appsettings.json precisa ter pelo menos 32 caracteres.");

builder.Services.AddSingleton(configuracaoJwt);
builder.Services.AddSingleton<GeradorDeToken>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        // Mantém os nomes curtos das claims ("sub", "role"...) como foram gravados no token.
        opcoes.MapInboundClaims = false;

        // O que a Api confere em cada token recebido:
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,               // foi esta Api que emitiu?
            ValidIssuer = configuracaoJwt.Emissor,
            ValidateAudience = true,             // foi emitido para o app?
            ValidAudience = configuracaoJwt.Publico,
            ValidateIssuerSigningKey = true,     // a assinatura bate com a chave secreta?
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracaoJwt.Chave)),
            ValidateLifetime = true,             // ainda não venceu?
            ClockSkew = TimeSpan.FromMinutes(1),

            // Onde estão o nome e o perfil dentro do token.
            // É isso que faz o [Authorize(Roles = Perfis.Gerente)] funcionar.
            NameClaimType = ClaimsDoToken.Nome,
            RoleClaimType = ClaimsDoToken.Perfil
        };

        opcoes.Events = new JwtBearerEvents
        {
            // Depois de conferir a assinatura, confere no banco se a conta continua ativa.
            OnTokenValidated = ValidacaoDoToken.ConferirUsuarioNoBanco,

            // Sem login (401) e sem permissão (403) também respondem no formato
            // { "mensagem": "..." }, igual aos outros erros (senão a resposta sai vazia).
            OnChallenge = async contexto =>
            {
                contexto.HandleResponse();
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                contexto.Response.Headers.WWWAuthenticate = "Bearer";
                var mensagem = contexto.AuthenticateFailure switch
                {
                    SecurityTokenExpiredException => "Sua sessão expirou. Faça login de novo.",
                    AuthenticationFailureException falha => falha.Message,   // conta desativada, perfil mudou...
                    _ => "Faça login para continuar."
                };
                await contexto.Response.WriteAsJsonAsync(new { mensagem });
            },
            OnForbidden = contexto =>
            {
                contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                return contexto.Response.WriteAsJsonAsync(new { mensagem = "Você não tem permissão para fazer isso." });
            }
        };
    });

builder.Services.AddAuthorization();

// ---------- Limite de tentativas (login e cadastro) ----------
// Cada endereço IP pode tentar no máximo 20 vezes por minuto; depois recebe 429.
builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opcoes.OnRejected = async (contexto, cancelamento) =>
    {
        await contexto.HttpContext.Response.WriteAsJsonAsync(
            new { mensagem = "Muitas tentativas seguidas. Aguarde um minuto e tente de novo." },
            cancelamento);
    };
    opcoes.AddPolicy(LimiteDeTentativas.Politica, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = LimiteDeTentativas.PorMinuto,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---------- CORS ----------
// Só importa se o app rodar no NAVEGADOR (Flutter Web): o navegador bloqueia a chamada
// para outro endereço, a menos que a Api permita. No celular e no emulador não importa.
// Libera os endereços da lista "Cors:Origens" do appsettings.json; com a lista vazia,
// em desenvolvimento, libera qualquer endereço.
const string PoliticaCors = "App";
var origensPermitidas = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(opcoes => opcoes.AddPolicy(PoliticaCors, politica =>
{
    if (origensPermitidas.Length > 0)
        politica.WithOrigins(origensPermitidas);
    else if (builder.Environment.IsDevelopment())
        politica.AllowAnyOrigin();

    politica.AllowAnyHeader().AllowAnyMethod();
}));

// ---------- Controllers e JSON ----------
// Os nomes saem em camelCase ("PrecoUnitario" vira "precoUnitario"), como o app espera.
builder.Services
    .AddControllers(opcoes =>
    {
        // Vale para todos os controllers: bloqueia quem ainda está com senha temporária.
        opcoes.Filters.Add<BloquearSenhaTemporariaFilter>();
    })
    .ConfigureApiBehaviorOptions(opcoes =>
    {
        // Quando um DTO chega inválido ([Required], [Range]...), responde 400 no
        // mesmo formato dos outros erros: { "mensagem": "...", "erros": { campo: [...] } }.
        opcoes.InvalidModelStateResponseFactory = contexto =>
        {
            var erros = contexto.ModelState
                .Where(campo => campo.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    campo => campo.Key,
                    campo => campo.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Valor inválido." : e.ErrorMessage)
                        .ToArray());

            var primeiraMensagem = erros.Values.SelectMany(m => m).FirstOrDefault() ?? "Dados inválidos.";
            return new BadRequestObjectResult(new { mensagem = primeiraMensagem, erros });
        };
    });

// ---------- Erros ----------
// Toda exceção vira { "mensagem": "..." } com o código HTTP certo (ver Erros/TratadorDeErros.cs).
builder.Services.AddExceptionHandler<TratadorDeErros>();
builder.Services.AddProblemDetails();

// OpenAPI: gera a documentação da API (quais endpoints existem, o que cada um
// recebe e devolve) num arquivo JSON padrão, em /openapi/v1.json.
// O transformer acrescenta o login por token, para testar as rotas protegidas no Scalar.
builder.Services.AddOpenApi(opcoes => opcoes.AddDocumentTransformer<EsquemaBearerTransformer>());

var app = builder.Build();

// ---------- Pipeline: o caminho de cada requisição, NESTA ordem ----------

// 1) Captura qualquer exceção dos passos seguintes (usa o TratadorDeErros).
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Publica o documento só durante o desenvolvimento.
    app.MapOpenApi();

    // Scalar: tela gráfica que lê o documento OpenAPI acima e permite ver e
    // testar cada endpoint pelo navegador, em /scalar.
    app.MapScalarApiReference();
}
else
{
    // Fora do desenvolvimento, força https. Em desenvolvimento não, porque o app
    // chama http://localhost:5000 (ou http://10.0.2.2:5000 no emulador Android).
    app.UseHttpsRedirection();
}

// 2) Descobre qual controller vai atender, aplica CORS, lê o token, confere a
//    permissão e o limite de tentativas.
app.UseRouting();
app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

// ---------- Dados iniciais ----------
// Com o banco vazio, cria o catálogo de exemplo e o gerente do primeiro acesso.
// Se o banco ainda não foi criado (você não rodou o "database update"), só avisa
// no console em vez de derrubar a Api.
using (var escopo = app.Services.CreateScope())
{
    try
    {
        DadosIniciais.Popular(escopo.ServiceProvider.GetRequiredService<AppDbContext>());

        var gerente = app.Configuration.GetSection("GerenteInicial");
        var email = gerente["Email"] ?? "gerente@fixtintas.com";
        var criado = escopo.ServiceProvider.GetRequiredService<UsuarioNegocio>().GarantirGerenteInicial(
            gerente["Nome"] ?? "Gerente FixTintas",
            email,
            gerente["Senha"] ?? "Gerente@123");

        if (criado)
            app.Logger.LogWarning("Gerente inicial criado: {Email}, com a senha do appsettings.json. Troque a senha no primeiro acesso.", email);

        // As triggers e procedures vêm da migration (ObjetosDoBanco.Criar). Se faltar alguma, avisa.
        var faltando = ObjetosDoBanco.Faltando(escopo.ServiceProvider.GetRequiredService<AppDbContext>());
        if (faltando.Count > 0)
            app.Logger.LogError("Faltam no banco: {Objetos}. Cole ObjetosDoBanco.Criar(migrationBuilder); no fim do Up() da migration e rode o database update de novo (veja o README).",
                string.Join(", ", faltando));
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("Dados iniciais não carregados ({Motivo}). Você já rodou o 'dotnet ef database update'?", ex.Message);
    }
}

app.Run();
