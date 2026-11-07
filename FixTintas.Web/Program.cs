using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using FixTintas.Negocio;
using FixTintas.Repositorio;
using FixTintas.Repositorio.Contexto;
using FixTintas.Repositorio.Dados;
using FixTintas.Repositorio.Interfaces;
using FixTintas.Web.Infra;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;

// ============================================================================
// SITE DO FIXTINTAS (ASP.NET Core MVC com Razor): a loja e o painel da equipe.
// O site NÃO chama a Api: os controllers chamam o Negocio direto, pela injeção de
// dependência. A Api (FixTintas.Api) existe só para o app Flutter. Os dois usam o
// mesmo Negocio, então as regras são as mesmas no site e no app.
// ============================================================================

// Números sempre com PONTO ("3.6"): é assim que os campos numéricos do navegador
// mandam os valores. Para MOSTRAR, as telas usam o formato brasileiro (Formatacao).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// A connection string vem do appsettings.json — nunca do código-fonte.
var connectionString = builder.Configuration.GetConnectionString("Padrao")
    ?? throw new InvalidOperationException("Connection string 'Padrao' não encontrada no appsettings.json.");

// ---------- Injeção de dependência ----------
// Esta é a LISTA DE MONTAGEM do sistema (igual à do exemplo do professor). Cada linha
// ensina o ASP.NET Core a criar uma peça. Quando um controller pede um ProdutoNegocio
// no construtor, o ASP.NET monta a cadeia inteira sozinho:
//
//   ProdutosController  pede  ProdutoNegocio
//   ProdutoNegocio      pede  IProdutoRepositorio  -> recebe um ProdutoRepositorio
//   ProdutoRepositorio  pede  AppDbContext         -> recebe com a connection string
//
// Ninguém escreve "new" para essas classes. Explicação passo a passo, com os
// erros mais comuns: INJECAO-DE-DEPENDENCIA.md, na pasta da solution.
//
// ATENÇÃO: este bloco é IGUAL no Program.cs da Api. Mudou aqui, mude lá também.

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
//    No site, os arquivos (imagens dos produtos) ficam no wwwroot, que é publicado:
//    a imagem enviada pelo painel aparece na loja em /img/produtos/....
var regrasDaLoja = builder.Configuration.GetSection("Loja").Get<RegrasDaLoja>() ?? new RegrasDaLoja();
builder.Services.AddSingleton(regrasDaLoja);

var pastaWwwroot = builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
builder.Services.AddSingleton<IArmazenamentoDeArquivos>(new ArmazenamentoDeArquivos(pastaWwwroot));

// 5) O app para Android (o app Flutter) que o site oferece para download em /App.
//    Seção "AppAndroid" do appsettings.json; o .apk fica na pasta Aplicativo (ver Infra/AppAndroid.cs).
builder.Services.AddSingleton<AppAndroid>();

// AddScoped = um objeto novo para cada requisição (cada clique), jogado fora quando a
// resposta sai. O AddDbContext funciona do mesmo jeito: cada requisição ganha o seu contexto.
// --------------------------------------------

// ---------- Carrinho na sessão ----------
// A sessão guarda o carrinho de quem está navegando (mesmo sem login).
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opcoes =>
{
    opcoes.Cookie.Name = "FixTintas.Sessao";
    opcoes.Cookie.HttpOnly = true;
    opcoes.Cookie.IsEssential = true;
    opcoes.IdleTimeout = TimeSpan.FromHours(4);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CarrinhoDaSessao>();

// ---------- Login (cookie) ----------
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.LoginPath = "/Conta/Entrar";
        opcoes.LogoutPath = "/Conta/Sair";
        opcoes.AccessDeniedPath = "/Conta/AcessoNegado";
        opcoes.Cookie.Name = "FixTintas.Login";
        opcoes.Cookie.HttpOnly = true;
        opcoes.Cookie.SameSite = SameSiteMode.Lax;
        opcoes.ExpireTimeSpan = TimeSpan.FromHours(8);
        opcoes.SlidingExpiration = true;

        // A cada página, confere no banco se a conta continua ativa (ver Infra/ValidacaoDoLogin.cs).
        opcoes.Events.OnValidatePrincipal = ValidacaoDoLogin.ConferirUsuarioNoBanco;
    });
builder.Services.AddAuthorization();

// ---------- Limite de tentativas (login e cadastro) ----------
// Cada endereço IP pode tentar no máximo 20 vezes por minuto.
builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opcoes.AddPolicy("login", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---------- MVC ----------
// Acentos saem como texto normal no HTML ("Início"), e não como códigos ("In&#xED;cio").
// Os caracteres perigosos (< > & ") continuam sendo trocados, então a proteção contra XSS continua.
builder.Services.Configure<WebEncoderOptions>(opcoes =>
    opcoes.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddControllersWithViews(opcoes =>
{
    // Todo formulário POST precisa do token anti-falsificação (o <form> do Razor já põe).
    opcoes.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Quem está com senha temporária é levado para "Alterar senha" (ver Infra).
    opcoes.Filters.Add<ExigirTrocaDeSenhaFilter>();

    // Mensagens em português quando o valor digitado nem chega a ser lido
    // (ex.: letras num campo de número). As outras mensagens estão nos DTOs.
    var mensagens = opcoes.ModelBindingMessageProvider;
    mensagens.SetValueMustNotBeNullAccessor(_ => "Preencha este campo.");
    mensagens.SetMissingBindRequiredValueAccessor(campo => $"Preencha o campo {campo}.");
    mensagens.SetMissingKeyOrValueAccessor(() => "Preencha este campo.");
    mensagens.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"O valor \"{valor}\" não é válido para {campo}.");
    mensagens.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"O valor \"{valor}\" não é válido.");
    mensagens.SetUnknownValueIsInvalidAccessor(campo => $"O valor informado não é válido para {campo}.");
    mensagens.SetNonPropertyUnknownValueIsInvalidAccessor(() => "O valor informado não é válido.");
    mensagens.SetValueIsInvalidAccessor(valor => $"O valor \"{valor}\" não é válido.");
    mensagens.SetValueMustBeANumberAccessor(campo => $"O campo {campo} precisa ser um número.");
    mensagens.SetNonPropertyValueMustBeANumberAccessor(() => "Informe um número.");
});

var app = builder.Build();

// ---------- Pipeline: o caminho de cada requisição, NESTA ordem ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Página amigável para 404 (endereço que não existe) e outros códigos de erro.
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");

app.UseHttpsRedirection();

// Arquivos do wwwroot (css, js, imagens). UseStaticFiles (e não MapStaticAssets)
// porque as imagens enviadas pelo painel chegam com o site já rodando.
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Painel da equipe: /Painel/Produtos, /Painel/Pedidos... (pasta Areas/Painel).
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Inicio}/{action=Index}/{id?}");

// Loja: /, /Produtos, /Carrinho, /Conta/Entrar...
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ---------- Dados iniciais ----------
// Com o banco vazio, cria o catálogo de exemplo e o gerente do primeiro acesso.
// Se o banco ainda não foi criado (você não rodou o "database update"), só avisa
// no console em vez de derrubar o site.
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
