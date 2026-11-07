using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using FixTintas.Web.Infra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.Areas.Painel.Controllers;

// A equipe da loja (só gerente). Funcionário só é criado aqui; nasce com senha
// temporária, que o gerente vê UMA vez e entrega pessoalmente.
[Area("Painel")]
[Authorize(Roles = Perfis.Gerente)]
public class FuncionariosController : Controller
{
    private readonly UsuarioNegocio _negocio;

    public FuncionariosController(UsuarioNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET /Painel/Funcionarios
    public IActionResult Index()
    {
        ViewData["MeuId"] = User.ObterId();
        return View(_negocio.ListarEquipe().Select(FuncionarioDto.De).ToList());
    }

    // GET /Painel/Funcionarios/Criar
    public IActionResult Criar() => View(new FuncionarioCriarDto());

    // POST /Painel/Funcionarios/Criar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Criar(FuncionarioCriarDto modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            var criado = _negocio.CriarFuncionario(modelo);
            MostrarSenhaTemporaria(criado);
            return RedirectToAction(nameof(Index));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    // POST /Painel/Funcionarios/AlterarAtivo/5   (ativo = false desativa; true reativa)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AlterarAtivo(int id, bool ativo)
    {
        try
        {
            var usuario = _negocio.AlterarAtivo(User.ObterId(), id, ativo);
            TempData["Mensagem"] = ativo ? $"{usuario.Nome} foi reativado(a)." : $"{usuario.Nome} foi desativado(a).";
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // POST /Painel/Funcionarios/RedefinirSenha/5   (gera uma nova senha temporária)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RedefinirSenha(int id)
    {
        try
        {
            MostrarSenhaTemporaria(_negocio.RedefinirSenha(User.ObterId(), id));
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // A senha temporária aparece uma vez só, no topo da lista (TempData some depois de lido).
    private void MostrarSenhaTemporaria(FuncionarioCriadoDto criado)
    {
        TempData["SenhaTemporaria"] = criado.SenhaTemporaria;
        TempData["SenhaPara"] = $"{criado.Funcionario.Nome} ({criado.Funcionario.Email})";
    }
}
