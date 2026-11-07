using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers;

// Latas de tinta à venda, com preço e estoque (o app só vende tinta;
// os acessórios — rolos, lixas... — ficam só no site).
[ApiController]
[Route("api/produtos")]
[AllowAnonymous]
public class ProdutosController : ControllerBase
{
    private readonly ProdutoNegocio _negocio;

    public ProdutosController(ProdutoNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET api/produtos              -> todas as latas
    // GET api/produtos?marcaId=1&tipoTintaId=2
    [HttpGet]
    public ActionResult<IEnumerable<ProdutoDto>> Listar([FromQuery] int? marcaId, [FromQuery] int? tipoTintaId) =>
        Ok(_negocio.ListarTintas(marcaId, tipoTintaId).Select(ProdutoDto.De));

    // GET api/produtos/5
    [HttpGet("{id:int}")]
    public ActionResult<ProdutoDto> Obter(int id)
    {
        var produto = _negocio.ObterTintaAVenda(id);
        if (produto is null) return NotFound(new { mensagem = "Produto não encontrado." });
        return Ok(ProdutoDto.De(produto));
    }
}
