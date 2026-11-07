using FixTintas.Modelos;
using FixTintas.Repositorio;
using FixTintas.Repositorio.Interfaces;

namespace FixTintas.Negocio;

// ============================================================================
// Regras dos tipos de tinta (Acrílica Fosco, Esmalte Sintético Brilhante...).
// Os números daqui alimentam a calculadora e o simulador do app:
//   RendimentoM2PorLitro  quantos m² um litro pinta em uma demão
//   DemaosRecomendadas    quantas demãos a calculadora usa por padrão
//   FatorContraste        quanto da sombra da foto o simulador mantém
//                         (fosco 0,80 · acetinado 0,95 · brilhante 1,10)
// ============================================================================
public class TipoTintaNegocio
{
    private readonly ITipoTintaRepositorio _repositorio;

    public TipoTintaNegocio(ITipoTintaRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public List<TipoTinta> Listar(bool incluirInativos = false) => _repositorio.Listar(incluirInativos);

    public TipoTinta? ObterPorId(int id) => _repositorio.ObterPorId(id);

    public TipoTinta Cadastrar(TipoTinta tipo)
    {
        Normalizar(tipo);
        Validar(tipo, ignorarId: null);

        try
        {
            _repositorio.Adicionar(tipo);
        }
        catch (ConflitoDeGravacaoException)
        {
            throw new ConflitoException("Já existe este tipo de tinta com este acabamento.");
        }
        return tipo;
    }

    public void Atualizar(TipoTinta tipo)
    {
        var existente = _repositorio.ObterPorId(tipo.Id)
            ?? throw new NaoEncontradoException("Tipo de tinta não encontrado.");

        Normalizar(tipo);
        Validar(tipo, ignorarId: tipo.Id);

        existente.Nome = tipo.Nome;
        existente.Acabamento = tipo.Acabamento;
        existente.Descricao = tipo.Descricao;
        existente.RendimentoM2PorLitro = tipo.RendimentoM2PorLitro;
        existente.DemaosRecomendadas = tipo.DemaosRecomendadas;
        existente.FatorContraste = tipo.FatorContraste;
        existente.Inativo = tipo.Inativo;

        try
        {
            _repositorio.Atualizar(existente);
        }
        catch (ConflitoDeGravacaoException)
        {
            throw new ConflitoException("Já existe este tipo de tinta com este acabamento.");
        }
    }

    // Tipo usado por produtos ou projetos do app não é apagado: é desativado.
    public ResultadoExclusao Remover(int id)
    {
        var tipo = _repositorio.ObterPorId(id)
            ?? throw new NaoEncontradoException("Tipo de tinta não encontrado.");

        if (_repositorio.EmUso(id))
        {
            tipo.Inativo = true;
            _repositorio.Atualizar(tipo);
            return ResultadoExclusao.Desativado;
        }

        _repositorio.Remover(tipo);
        return ResultadoExclusao.Excluido;
    }

    private static void Normalizar(TipoTinta tipo)
    {
        tipo.Nome = Validacoes.NormalizarTexto(tipo.Nome);
        tipo.Acabamento = Acabamentos.Normalizar(tipo.Acabamento) ?? string.Empty;
        tipo.Descricao = Validacoes.TextoOpcional(tipo.Descricao);
    }

    private void Validar(TipoTinta tipo, int? ignorarId)
    {
        if (tipo.Nome.Length < 2)
            throw new RegraDeNegocioException("Informe o nome do tipo de tinta.");
        if (tipo.Nome.Length > 60)
            throw new RegraDeNegocioException("O nome pode ter no máximo 60 caracteres.");
        if (tipo.Acabamento.Length == 0)
            throw new RegraDeNegocioException("Escolha o acabamento: Fosco, Acetinado ou Brilhante.");
        if (tipo.Descricao?.Length > 300)
            throw new RegraDeNegocioException("A descrição pode ter no máximo 300 caracteres.");
        if (Validacoes.TemCaracterePerigoso(tipo.Nome) || Validacoes.TemCaracterePerigoso(tipo.Descricao))
            throw new RegraDeNegocioException("Os textos não podem ter os caracteres < ou >.");
        if (tipo.RendimentoM2PorLitro is <= 0 or > 50)
            throw new RegraDeNegocioException("O rendimento deve ser maior que 0 e até 50 m² por litro.");
        if (tipo.DemaosRecomendadas is < 1 or > 6)
            throw new RegraDeNegocioException("As demãos recomendadas devem ser de 1 a 6.");
        if (tipo.FatorContraste is < 0.5m or > 1.5m)
            throw new RegraDeNegocioException("O fator de contraste deve ficar entre 0,5 e 1,5.");

        if (_repositorio.NomeEmUso(tipo.Nome, tipo.Acabamento, ignorarId))
            throw new ConflitoException("Já existe este tipo de tinta com este acabamento.");
    }
}
