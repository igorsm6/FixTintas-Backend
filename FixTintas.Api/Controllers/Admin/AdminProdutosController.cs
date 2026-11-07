using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers.Admin;

// ============================================================================
// Produtos e estoque para a EQUIPE da loja (as mesmas regras do painel do site).
// Funcionário: lista, cadastra, edita (sem mudar o preço) e muda o estoque.
// Gerente: tudo isso, mais mudar preço e excluir.
// ============================================================================
[ApiController]
[Route("api/admin/produtos")]
[Authorize(Roles = Perfis.Equipe)]
public class AdminProdutosController : ControllerBase
{
    private readonly ProdutoNegocio _negocio;

    public AdminProdutosController(ProdutoNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET api/admin/produtos?busca=suvinil&categoriaId=2&situacao=baixo
    //   situacao: ativo, inativo, baixo (estoque no mínimo) ou zerado
    [HttpGet]
    public ActionResult<IEnumerable<ProdutoDto>> Listar([FromQuery] string? busca, [FromQuery] int? categoriaId, [FromQuery] string? situacao) =>
        Ok(_negocio.ListarParaPainel(busca, categoriaId, situacao).Select(ProdutoDto.De));

    // GET api/admin/produtos/5
    [HttpGet("{id:int}")]
    public ActionResult<ProdutoDto> Obter(int id)
    {
        var produto = _negocio.ObterPorId(id);
        if (produto is null) return NotFound(new { mensagem = "Produto não encontrado." });
        return Ok(ProdutoDto.De(produto));
    }

    // GET api/admin/produtos/estoque-baixo   (stored procedure sp_Produtos_EstoqueBaixo)
    [HttpGet("estoque-baixo")]
    public ActionResult<IEnumerable<ProdutoVitrineLinha>> EstoqueBaixo() =>
        Ok(_negocio.EstoqueBaixo());

    // GET api/admin/produtos/resumo   números do painel (total, ativos, estoque baixo, zerados)
    [HttpGet("resumo")]
    public ActionResult<ResumoEstoqueDto> Resumo() =>
        Ok(_negocio.Resumo());

    // POST api/admin/produtos   -> 201
    [HttpPost]
    public ActionResult<ProdutoDto> Cadastrar(ProdutoSalvarDto dto)
    {
        var produto = _negocio.Cadastrar(dto.ParaProduto());
        var criado = _negocio.ObterPorId(produto.Id) ?? produto;
        return CreatedAtAction(nameof(Obter), new { id = produto.Id }, ProdutoDto.De(criado));
    }

    // PUT api/admin/produtos/5   -> 204
    // O estoque NÃO muda por aqui (use PUT .../estoque). Só o gerente muda o preço.
    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, ProdutoSalvarDto dto)
    {
        _negocio.Atualizar(dto.ParaProduto(id), podeAlterarPreco: User.IsInRole(Perfis.Gerente));
        return NoContent();
    }

    // PUT api/admin/produtos/5/estoque   { "estoque": 40 }   -> 204
    [HttpPut("{id:int}/estoque")]
    public IActionResult AtualizarEstoque(int id, EstoqueDto dto)
    {
        _negocio.AtualizarEstoque(id, dto.Estoque);
        return NoContent();
    }

    // DELETE api/admin/produtos/5   (só gerente). Produto já vendido é desativado, não apagado.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Gerente)]
    public IActionResult Remover(int id)
    {
        var resultado = _negocio.Remover(id);
        return Ok(new
        {
            resultado = resultado.ToString(),
            mensagem = resultado == ResultadoExclusao.Excluido
                ? "Produto excluído."
                : "O produto já foi vendido, então foi desativado (não aparece mais na loja)."
        });
    }
}
