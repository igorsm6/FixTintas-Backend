using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers.Admin;

// Cadastro só para o GERENTE. Os GET daqui trazem também os registros inativos.
// Excluir algo que ainda está em uso não apaga: desativa (a resposta diz qual dos dois).
[ApiController]
[Route("api/admin/categorias")]
[Authorize(Roles = Perfis.Gerente)]
public class AdminCategoriasController : ControllerBase
{
    private readonly CategoriaNegocio _negocio;

    public AdminCategoriasController(CategoriaNegocio negocio)
    {
        _negocio = negocio;
    }

    [HttpGet]
    public ActionResult<IEnumerable<CategoriaDto>> Listar() =>
        Ok(_negocio.ListarArvore(incluirInativas: true).Select(CategoriaDto.De));

    [HttpGet("{id:int}")]
    public ActionResult<CategoriaDto> Obter(int id)
    {
        var categoria = _negocio.ObterPorId(id);
        if (categoria is null) return NotFound(new { mensagem = "Categoria não encontrada." });
        return Ok(CategoriaDto.De(categoria));
    }

    [HttpPost]
    public ActionResult<CategoriaDto> Cadastrar(CategoriaSalvarDto dto)
    {
        var categoria = _negocio.Cadastrar(dto.ParaCategoria());
        return CreatedAtAction(nameof(Obter), new { id = categoria.Id }, CategoriaDto.De(categoria));
    }

    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, CategoriaSalvarDto dto)
    {
        _negocio.Atualizar(dto.ParaCategoria(id));
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
                ? "Categoria excluída."
                : "A categoria tem produtos ou subcategorias, então foi desativada."
        });
    }
}
