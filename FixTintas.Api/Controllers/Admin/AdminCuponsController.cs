using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers.Admin;

// Cadastro só para o GERENTE. Os GET daqui trazem também os registros inativos.
// Excluir algo que ainda está em uso não apaga: desativa (a resposta diz qual dos dois).
[ApiController]
[Route("api/admin/cupons")]
[Authorize(Roles = Perfis.Gerente)]
public class AdminCuponsController : ControllerBase
{
    private readonly CupomNegocio _negocio;

    public AdminCuponsController(CupomNegocio negocio)
    {
        _negocio = negocio;
    }

    [HttpGet]
    public ActionResult<IEnumerable<CupomDto>> Listar() =>
        Ok(_negocio.Listar().Select(CupomDto.De));

    [HttpGet("{id:int}")]
    public ActionResult<CupomDto> Obter(int id)
    {
        var cupom = _negocio.ObterPorId(id);
        if (cupom is null) return NotFound(new { mensagem = "Cupom não encontrado." });
        return Ok(CupomDto.De(cupom));
    }

    //   { "codigo": "PINTE20", "tipo": "Percentual", "valor": 20, "valorMinimoPedido": 150, "validoAte": "2026-12-31" }
    [HttpPost]
    public ActionResult<CupomDto> Cadastrar(CupomSalvarDto dto)
    {
        var cupom = _negocio.Cadastrar(dto);
        return CreatedAtAction(nameof(Obter), new { id = cupom.Id }, CupomDto.De(cupom));
    }

    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, CupomSalvarDto dto)
    {
        _negocio.Atualizar(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Remover(int id)
    {
        var resultado = _negocio.Remover(id);
        return Ok(new
        {
            resultado = resultado.ToString(),
            mensagem = resultado == ResultadoExclusao.Excluido
                ? "Cupom excluído."
                : "O cupom já foi usado em pedidos, então foi desativado."
        });
    }
}
