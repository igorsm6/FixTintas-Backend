using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using FixTintas.Web.Infra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FixTintas.Web.Controllers;

// ============================================================================
// Conta do usuário no site: entrar, cadastrar, sair, meus dados e senha.
// As regras (senha forte, CPF válido, 18 anos, e-mail único...) são do
// UsuarioNegocio — as mesmas do cadastro pelo app.
// ============================================================================
public class ContaController : Controller
{
    private readonly UsuarioNegocio _usuarios;

    public ContaController(UsuarioNegocio usuarios)
    {
        _usuarios = usuarios;
    }

    // GET /Conta/Entrar
    public IActionResult Entrar(string? returnUrl)
    {
        if (User.EstaLogado()) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginDto());
    }

    // POST /Conta/Entrar
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Entrar(LoginDto modelo, string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            var usuario = _usuarios.Autenticar(modelo.Email, modelo.Senha);
            await LoginDoSite.Entrar(HttpContext, usuario);

            if (usuario.TrocarSenha)
                return RedirectToAction(nameof(AlterarSenha));

            TempData["Mensagem"] = $"Bem-vindo, {usuario.Nome.Split(' ')[0]}!";

            if (Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl!);
            if (FixTintas.Modelos.Perfis.EhEquipe(usuario.Perfil))
                return RedirectToAction("Index", "Inicio", new { area = "Painel" });
            return RedirectToAction("Index", "Home");
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    // GET /Conta/Cadastrar
    public IActionResult Cadastrar(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new CadastroClienteDto());
    }

    // POST /Conta/Cadastrar   (no site, CPF, nascimento, telefone e confirmação são obrigatórios)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Cadastrar(CadastroClienteDto modelo, string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            var usuario = _usuarios.CadastrarCliente(modelo, exigirDadosPessoais: true);
            await LoginDoSite.Entrar(HttpContext, usuario);
            TempData["Mensagem"] = "Cadastro realizado! Você já está logado.";

            if (Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl!);
            return RedirectToAction("Index", "Home");
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    // POST /Conta/Sair
    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermitirSenhaTemporaria]
    public async Task<IActionResult> Sair()
    {
        await LoginDoSite.Sair(HttpContext);
        HttpContext.Session.Clear();
        TempData["Mensagem"] = "Você saiu da sua conta.";
        return RedirectToAction("Index", "Home");
    }

    // GET /Conta/MeusDados
    [Authorize]
    public IActionResult MeusDados()
    {
        var usuario = _usuarios.ObterPorId(User.ObterId());
        ViewData["Perfil"] = PerfilDto.De(usuario);
        return View(AtualizarPerfilDto.De(usuario));
    }

    // POST /Conta/MeusDados
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult MeusDados(AtualizarPerfilDto modelo)
    {
        ViewData["Perfil"] = PerfilDto.De(_usuarios.ObterPorId(User.ObterId()));
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            _usuarios.AtualizarPerfil(User.ObterId(), modelo);
            TempData["Mensagem"] = "Dados atualizados.";
            return RedirectToAction(nameof(MeusDados));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    // GET /Conta/AlterarSenha   (também é a tela obrigatória de quem está com senha temporária)
    [Authorize]
    [PermitirSenhaTemporaria]
    public IActionResult AlterarSenha()
    {
        return View(new AlterarSenhaDto());
    }

    // POST /Conta/AlterarSenha
    [HttpPost]
    [Authorize]
    [PermitirSenhaTemporaria]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaDto modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        if (string.IsNullOrEmpty(modelo.ConfirmarNovaSenha))
        {
            ModelState.AddModelError(nameof(modelo.ConfirmarNovaSenha), "Confirme a nova senha.");
            return View(modelo);
        }

        try
        {
            var usuario = _usuarios.AlterarSenha(User.ObterId(), modelo);

            // Grava o cookie de novo, agora sem a marca de "senha temporária".
            await LoginDoSite.Entrar(HttpContext, usuario);
            TempData["Mensagem"] = "Senha alterada com sucesso.";

            if (FixTintas.Modelos.Perfis.EhEquipe(usuario.Perfil))
                return RedirectToAction("Index", "Inicio", new { area = "Painel" });
            return RedirectToAction("Index", "Home");
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    // GET /Conta/AcessoNegado   (logado, mas sem permissão para a página)
    [PermitirSenhaTemporaria]
    public IActionResult AcessoNegado()
    {
        return View();
    }
}
