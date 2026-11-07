using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers.Admin;

// Cadastro só para o GERENTE. Os GET daqui trazem também os registros inativos.
// Excluir algo que ainda está em uso não apaga: desativa (a resposta diz qual dos dois).
[ApiController]
[Route("api/admin/marcas")]
[Authorize(Roles = Perfis.Gerente)]
public class AdminMarcasController : ControllerBase
{
    private readonly MarcaNegocio _negocio;

    public AdminMarcasController(MarcaNegocio negocio)
    {
        _negocio = negocio;
    }

    [HttpGet]
    public ActionResult<IEnumerable<MarcaDto>> Listar() =>
        Ok(_negocio.Listar(incluirInativas: true).Select(MarcaDto.De));

    [HttpGet("{id:int}")]
    public ActionResult<MarcaDto> Obter(int id)
    {
        var marca = _negocio.ObterPorId(id);
        if (marca is null) return NotFound(new { mensagem = "Marca não encontrada." });
        return Ok(MarcaDto.De(marca));
    }

    [HttpPost]
    public ActionResult<MarcaDto> Cadastrar(MarcaSalvarDto dto)
    {
        var marca = _negocio.Cadastrar(dto.ParaMarca());
        return CreatedAtAction(nameof(Obter), new { id = marca.Id }, MarcaDto.De(marca));
    }

    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, MarcaSalvarDto dto)
    {
        _negocio.Atualizar(dto.ParaMarca(id));
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
                ? "Marca excluída."
                : "A marca tem produtos, então foi desativada."
        });
    }
}
