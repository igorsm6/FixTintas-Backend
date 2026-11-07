using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Repositorio.Interfaces;

namespace FixTintas.Negocio;

// ============================================================================
// "Meus projetos" do app: a foto da parede já pintada pelo simulador, com a cor
// e o tipo de tinta escolhidos. A foto é PRIVADA: fica na pasta App_Data da Api
// (que não é publicada como arquivo) e só o dono consegue baixar.
// ============================================================================
public class SimulacaoNegocio
{
    private const string Pasta = "simulacoes";

    private readonly ISimulacaoRepositorio _repositorio;
    private readonly ICorRepositorio _cores;
    private readonly ITipoTintaRepositorio _tipos;
    private readonly IArmazenamentoDeArquivos _arquivos;

    public SimulacaoNegocio(ISimulacaoRepositorio repositorio, ICorRepositorio cores, ITipoTintaRepositorio tipos,
                            IArmazenamentoDeArquivos arquivos)
    {
        _repositorio = repositorio;
        _cores = cores;
        _tipos = tipos;
        _arquivos = arquivos;
    }

    public List<Simulacao> Listar(int usuarioId) => _repositorio.ListarDoUsuario(usuarioId);

    public Simulacao Criar(int usuarioId, NovaSimulacaoDto dto)
    {
        // Confere os primeiros bytes: só aceita JPEG ou PNG de verdade, até 10 MB.
        var extensao = ValidadorDeImagem.Validar(dto.Imagem, ValidadorDeImagem.TamanhoMaximoSimulacao, aceitarWebp: false);

        var cor = _cores.ObterPorId(dto.CorId);
        if (cor is null || cor.Inativa)
            throw new RegraDeNegocioException("Cor não encontrada.");

        var tipo = _tipos.ObterPorId(dto.TipoTintaId);
        if (tipo is null || tipo.Inativo)
            throw new RegraDeNegocioException("Tipo de tinta não encontrado.");

        var nome = Validacoes.TextoOpcional(dto.Nome);
        if (nome?.Length > 80)
            throw new RegraDeNegocioException("O nome do projeto pode ter no máximo 80 caracteres.");
        if (Validacoes.TemCaracterePerigoso(nome))
            throw new RegraDeNegocioException("O nome não pode ter os caracteres < ou >.");

        if (dto.AreaM2 is decimal area && (area <= 0 || area > 100_000))
            throw new RegraDeNegocioException("A área deve ser maior que zero.");

        // O nome do arquivo é gerado pelo sistema; o nome enviado pelo celular nunca é usado.
        var caminho = _arquivos.Salvar(Pasta, extensao, dto.Imagem);

        var simulacao = new Simulacao
        {
            UsuarioId = usuarioId,
            CorId = cor.Id,
            TipoTintaId = tipo.Id,
            Nome = nome,
            AreaM2 = dto.AreaM2 is decimal a ? Math.Round(a, 2, MidpointRounding.AwayFromZero) : null,
            CaminhoImagem = caminho
        };

        try
        {
            _repositorio.Adicionar(simulacao);
        }
        catch
        {
            // Não deixa imagem sobrando se o banco falhar.
            _arquivos.Excluir(caminho);
            throw;
        }

        simulacao.Cor = cor;
        simulacao.TipoTinta = tipo;
        return simulacao;
    }

    // A foto do projeto. Para quem não é o dono, o projeto "não existe".
    public ArquivoImagemDto ObterImagem(int id, int usuarioId)
    {
        var simulacao = _repositorio.ObterDoUsuario(id, usuarioId)
            ?? throw new NaoEncontradoException("Projeto não encontrado.");

        var caminho = _arquivos.CaminhoFisico(simulacao.CaminhoImagem)
            ?? throw new NaoEncontradoException("A foto deste projeto não foi encontrada.");

        return new ArquivoImagemDto { CaminhoFisico = caminho, TipoConteudo = ValidadorDeImagem.TipoDeConteudo(caminho) };
    }

    public void Excluir(int id, int usuarioId)
    {
        var simulacao = _repositorio.ObterDoUsuario(id, usuarioId)
            ?? throw new NaoEncontradoException("Projeto não encontrado.");

        var caminho = simulacao.CaminhoImagem;
        _repositorio.Remover(simulacao);
        _arquivos.Excluir(caminho);
    }
}
