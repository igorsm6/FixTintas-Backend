using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FixTintas.Web.Areas.Painel.Controllers;

// ============================================================================
// Produtos e estoque (painel). Mesmo molde do ClientesController do professor:
// Index, Criar, Editar e Excluir, com os DTOs do Modelos.
//   Funcionário: cadastra, edita (sem mudar o preço) e muda o estoque.
//   Gerente: também muda preço e exclui.
// ============================================================================
[Area("Painel")]
[Authorize(Roles = Perfis.Equipe)]
public class ProdutosController : Controller
{
    private readonly ProdutoNegocio _negocio;
    private readonly CategoriaNegocio _categorias;
    private readonly MarcaNegocio _marcas;
    private readonly TipoTintaNegocio _tipos;

    public ProdutosController(ProdutoNegocio negocio, CategoriaNegocio categorias, MarcaNegocio marcas, TipoTintaNegocio tipos)
    {
        _negocio = negocio;
        _categorias = categorias;
        _marcas = marcas;
        _tipos = tipos;
    }

    // GET /Painel/Produtos?busca=suvinil&categoriaId=2&situacao=baixo
    public IActionResult Index(string? busca, int? categoriaId, string? situacao)
    {
        ViewData["Busca"] = busca;
        ViewData["Situacao"] = situacao;
        ViewData["Categorias"] = ListaDeCategorias(categoriaId);

        var produtos = _negocio.ListarParaPainel(busca, categoriaId, situacao);
        return View(produtos.Select(ProdutoDto.De).ToList());
    }

    // GET /Painel/Produtos/Criar
    public IActionResult Criar()
    {
        PrepararListas(null);
        return View(new ProdutoSalvarDto());
    }

    // POST /Painel/Produtos/Criar   (o formulário manda os campos e, se tiver, o arquivo "imagem")
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(ProdutoSalvarDto modelo, IFormFile? imagem)
    {
        if (!ModelState.IsValid)
        {
            PrepararListas(modelo);
            return View(modelo);
        }

        try
        {
            var produto = _negocio.Cadastrar(modelo.ParaProduto(), await LerImagem(imagem));
            TempData["Mensagem"] = $"Produto \"{produto.Nome}\" cadastrado.";
            return RedirectToAction(nameof(Index));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            PrepararListas(modelo);
            return View(modelo);
        }
    }

    // GET /Painel/Produtos/Editar/5
    public IActionResult Editar(int id)
    {
        var produto = _negocio.ObterPorId(id);
        if (produto is null) return NotFound();

        // O DTO de entrada não tem Id; a view usa este valor para montar a URL do formulário.
        ViewData["Id"] = id;
        ViewData["ImagemAtual"] = produto.ImagemUrl;
        ViewData["EstoqueAtual"] = produto.Estoque;
        PrepararListas(null);
        return View(ProdutoSalvarDto.De(produto));
    }

    // POST /Painel/Produtos/Editar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, ProdutoSalvarDto modelo, IFormFile? imagem, bool removerImagem)
    {
        var atual = _negocio.ObterPorId(id);
        if (atual is null) return NotFound();

        // Funcionário não muda preço: vale o preço que já está gravado
        // (o Negocio confere de novo, então nem chamando direto dá para burlar).
        if (!User.IsInRole(Perfis.Gerente))
        {
            modelo.Preco = atual.Preco;
            modelo.PrecoDe = atual.PrecoDe;
            ModelState.Remove(nameof(modelo.Preco));
            ModelState.Remove(nameof(modelo.PrecoDe));
        }

        ViewData["Id"] = id;
        ViewData["ImagemAtual"] = atual.ImagemUrl;
        ViewData["EstoqueAtual"] = atual.Estoque;

        if (!ModelState.IsValid)
        {
            PrepararListas(modelo);
            return View(modelo);
        }

        try
        {
            _negocio.Atualizar(modelo.ParaProduto(id), User.IsInRole(Perfis.Gerente), await LerImagem(imagem), removerImagem);
            TempData["Mensagem"] = "Produto atualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            PrepararListas(modelo);
            return View(modelo);
        }
    }

    // GET /Painel/Produtos/Excluir/5  -> tela de confirmação (só gerente)
    [Authorize(Roles = Perfis.Gerente)]
    public IActionResult Excluir(int id)
    {
        var produto = _negocio.ObterPorId(id);
        if (produto is null) return NotFound();
        return View(ProdutoDto.De(produto));
    }

    // POST /Painel/Produtos/Excluir/5   (produto já vendido é desativado, não apagado)
    [HttpPost, ActionName("Excluir")]
    [Authorize(Roles = Perfis.Gerente)]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmarExclusao(int id)
    {
        try
        {
            var resultado = _negocio.Remover(id);
            TempData["Mensagem"] = resultado == ResultadoExclusao.Excluido
                ? "Produto excluído."
                : "O produto já foi vendido, então foi desativado (não aparece mais na loja).";
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // GET /Painel/Produtos/Estoque?busca=...
    public IActionResult Estoque(string? busca)
    {
        ViewData["Busca"] = busca;
        return View(_negocio.ListarParaPainel(busca).Select(ProdutoDto.De).ToList());
    }

    // POST /Painel/Produtos/AtualizarEstoque/5   (nova quantidade contada na prateleira)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AtualizarEstoque(int id, int estoque, string? busca)
    {
        try
        {
            _negocio.AtualizarEstoque(id, estoque);
            TempData["Mensagem"] = "Estoque atualizado.";
        }
        catch (NegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Estoque), new { busca });
    }

    // ---------------------------------------------------------------- auxiliares

    // Lê o arquivo enviado (até 2 MB). O Negocio confere se é mesmo uma imagem.
    private static async Task<byte[]?> LerImagem(IFormFile? arquivo)
    {
        if (arquivo is null || arquivo.Length == 0) return null;
        if (arquivo.Length > ValidadorDeImagem.TamanhoMaximoProduto)
            throw new RegraDeNegocioException("A imagem deve ter até 2 MB.");

        using var memoria = new MemoryStream();
        await arquivo.CopyToAsync(memoria);
        return memoria.ToArray();
    }

    // As listas de escolha do formulário (categoria, marca e tipo de tinta).
    private void PrepararListas(ProdutoSalvarDto? modelo)
    {
        ViewData["Categorias"] = ListaDeCategorias(modelo?.CategoriaId);

        ViewData["Marcas"] = new SelectList(
            _marcas.Listar(incluirInativas: true).Select(m => new { m.Id, Nome = m.Inativa ? $"{m.Nome} (inativa)" : m.Nome }),
            "Id", "Nome", modelo?.MarcaId);

        ViewData["TiposTinta"] = new SelectList(
            _tipos.Listar(incluirInativos: true).Select(t => new { t.Id, Nome = $"{t.Nome} {t.Acabamento}" + (t.Inativo ? " (inativo)" : "") }),
            "Id", "Nome", modelo?.TipoTintaId);

        ViewData["EhGerente"] = User.IsInRole(Perfis.Gerente);
    }

    private SelectList ListaDeCategorias(int? selecionada) =>
        new(_categorias.ListarTodas().Select(c => new
            {
                c.Id,
                Nome = (c.CategoriaPai is null ? c.Nome : $"{c.CategoriaPai.Nome} › {c.Nome}") + (c.Inativa ? " (inativa)" : "")
            }),
            "Id", "Nome", selecionada);
}
