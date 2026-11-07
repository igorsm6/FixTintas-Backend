using FixTintas.Api.Seguranca;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Api.Controllers;

// O que o app manda no formulário (multipart/form-data) ao salvar um projeto.
// Fica na Api (e não no Modelos) porque IFormFile é um tipo do ASP.NET.
public class NovaSimulacaoFormulario
{
    public int CorId { get; set; }
    public int TipoTintaId { get; set; }
    public string? Nome { get; set; }
    public decimal? AreaM2 { get; set; }
    public IFormFile? Imagem { get; set; }
}

// "Meus projetos" do app: a foto já pintada, a cor e o tipo de tinta.
// Tudo é privado do dono.
[ApiController]
[Route("api/simulacoes")]
[Authorize]
public class SimulacoesController : ControllerBase
{
    private readonly SimulacaoNegocio _negocio;

    public SimulacoesController(SimulacaoNegocio negocio)
    {
        _negocio = negocio;
    }

    // GET api/simulacoes   (mais recentes primeiro)
    [HttpGet]
    public ActionResult<IEnumerable<SimulacaoDto>> Listar() =>
        Ok(_negocio.Listar(User.ObterId()).Select(SimulacaoDto.De));

    // GET api/simulacoes/5/imagem   a foto (só o dono recebe; para os outros é 404)
    [HttpGet("{id:int}/imagem")]
    public IActionResult Imagem(int id)
    {
        var arquivo = _negocio.ObterImagem(id, User.ObterId());
        Response.Headers.CacheControl = "private, max-age=3600";
        return PhysicalFile(arquivo.CaminhoFisico, arquivo.TipoConteudo);
    }

    // POST api/simulacoes   campos: corId, tipoTintaId, nome (opcional), areaM2 (opcional) e o arquivo "imagem"
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12_000_000)]
    public async Task<ActionResult<SimulacaoDto>> Criar([FromForm] NovaSimulacaoFormulario formulario)
    {
        if (formulario.Imagem is null || formulario.Imagem.Length == 0)
            throw new RegraDeNegocioException("Envie a foto do projeto no campo \"imagem\".");

        if (formulario.Imagem.Length > ValidadorDeImagem.TamanhoMaximoSimulacao)
            throw new RegraDeNegocioException("A imagem deve ter até 10 MB.");

        // Lê o arquivo para a memória e entrega os bytes ao Negocio (que confere o formato).
        using var memoria = new MemoryStream();
        await formulario.Imagem.CopyToAsync(memoria);

        var simulacao = _negocio.Criar(User.ObterId(), new NovaSimulacaoDto
        {
            CorId = formulario.CorId,
            TipoTintaId = formulario.TipoTintaId,
            Nome = formulario.Nome,
            AreaM2 = formulario.AreaM2,
            Imagem = memoria.ToArray()
        });

        return Created($"/api/simulacoes/{simulacao.Id}", SimulacaoDto.De(simulacao));
    }

    // DELETE api/simulacoes/5   apaga o projeto e a foto -> 204
    [HttpDelete("{id:int}")]
    public IActionResult Excluir(int id)
    {
        _negocio.Excluir(id, User.ObterId());
        return NoContent();
    }
}
