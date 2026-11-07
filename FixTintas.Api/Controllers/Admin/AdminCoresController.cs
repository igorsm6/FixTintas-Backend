using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers.Admin;

// Cadastro só para o GERENTE. Os GET daqui trazem também os registros inativos.
// Excluir algo que ainda está em uso não apaga: desativa (a resposta diz qual dos dois).
[ApiController]
[Route("api/admin/cores")]
[Authorize(Roles = Perfis.Gerente)]
public class AdminCoresController : ControllerBase
{
    private readonly CorNegocio _negocio;

    public AdminCoresController(CorNegocio negocio)
    {
        _negocio = negocio;
    }

    [HttpGet]
    public ActionResult<IEnumerable<CorDto>> Listar() =>
        Ok(_negocio.Listar(incluirInativas: true).Select(CorDto.De));

    [HttpGet("{id:int}")]
    public ActionResult<CorDto> Obter(int id)
    {
        var cor = _negocio.ObterPorId(id);
        if (cor is null) return NotFound(new { mensagem = "Cor não encontrada." });
        return Ok(CorDto.De(cor));
    }

    [HttpPost]
    public ActionResult<CorDto> Cadastrar(CorSalvarDto dto)
    {
        var cor = _negocio.Cadastrar(dto.ParaCor());
        return CreatedAtAction(nameof(Obter), new { id = cor.Id }, CorDto.De(cor));
    }

    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, CorSalvarDto dto)
    {
        _negocio.Atualizar(dto.ParaCor(id));
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
                ? "Cor excluída."
                : "A cor já foi vendida (ou está num projeto do app), então foi desativada."
        });
    }
}
