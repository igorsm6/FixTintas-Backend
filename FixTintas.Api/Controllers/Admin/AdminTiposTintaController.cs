using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers.Admin;

// Cadastro só para o GERENTE. Os GET daqui trazem também os registros inativos.
// Excluir algo que ainda está em uso não apaga: desativa (a resposta diz qual dos dois).
[ApiController]
[Route("api/admin/tipos-tinta")]
[Authorize(Roles = Perfis.Gerente)]
public class AdminTiposTintaController : ControllerBase
{
    private readonly TipoTintaNegocio _negocio;

    public AdminTiposTintaController(TipoTintaNegocio negocio)
    {
        _negocio = negocio;
    }

    [HttpGet]
    public ActionResult<IEnumerable<TipoTintaDto>> Listar() =>
        Ok(_negocio.Listar(incluirInativos: true).Select(TipoTintaDto.De));

    [HttpGet("{id:int}")]
    public ActionResult<TipoTintaDto> Obter(int id)
    {
        var tipo = _negocio.ObterPorId(id);
        if (tipo is null) return NotFound(new { mensagem = "Tipo de tinta não encontrado." });
        return Ok(TipoTintaDto.De(tipo));
    }

    [HttpPost]
    public ActionResult<TipoTintaDto> Cadastrar(TipoTintaSalvarDto dto)
    {
        var tipo = _negocio.Cadastrar(dto.ParaTipoTinta());
        return CreatedAtAction(nameof(Obter), new { id = tipo.Id }, TipoTintaDto.De(tipo));
    }

    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, TipoTintaSalvarDto dto)
    {
        _negocio.Atualizar(dto.ParaTipoTinta(id));
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
                ? "Tipo de tinta excluído."
                : "O tipo de tinta está em uso, então foi desativado."
        });
    }
}
