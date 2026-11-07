using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers;

// Catálogo público (sem login). O app guarda uma cópia por alguns minutos (cache),
// então estes dados mudam no app pouco depois de mudarem no painel.
//
// GET api/categorias   -> categorias principais, cada uma com as subcategorias
[ApiController]
[Route("api/categorias")]
[AllowAnonymous]
public class CategoriasController : ControllerBase
{
    private readonly CategoriaNegocio _negocio;

    public CategoriasController(CategoriaNegocio negocio)
    {
        _negocio = negocio;
    }

    [HttpGet]
    public ActionResult<IEnumerable<CategoriaDto>> Listar() =>
        Ok(_negocio.ListarArvore().Select(CategoriaDto.De));
}
