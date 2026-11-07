using FixTintas.Modelos;
using FixTintas.Repositorio;
using FixTintas.Repositorio.Interfaces;

namespace FixTintas.Negocio;

// Regras das marcas (Suvinil, Coral...). O nome não se repete.
public class MarcaNegocio
{
    private readonly IMarcaRepositorio _repositorio;

    public MarcaNegocio(IMarcaRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public List<Marca> Listar(bool incluirInativas = false) => _repositorio.Listar(incluirInativas);

    public Marca? ObterPorId(int id) => _repositorio.ObterPorId(id);

    public Marca Cadastrar(Marca marca)
    {
        Normalizar(marca);
        Validar(marca, ignorarId: null);

        try
        {
            _repositorio.Adicionar(marca);
        }
        catch (ConflitoDeGravacaoException)
        {
            throw new ConflitoException("Já existe uma marca com este nome.");
        }
        return marca;
    }

    public void Atualizar(Marca marca)
    {
        var existente = _repositorio.ObterPorId(marca.Id)
            ?? throw new NaoEncontradoException("Marca não encontrada.");

        Normalizar(marca);
        Validar(marca, ignorarId: marca.Id);

        existente.Nome = marca.Nome;
        existente.Descricao = marca.Descricao;
        existente.Inativa = marca.Inativa;

        try
        {
            _repositorio.Atualizar(existente);
        }
        catch (ConflitoDeGravacaoException)
        {
            throw new ConflitoException("Já existe uma marca com este nome.");
        }
    }

    // Marca com produtos não é apagada: é desativada (some do site e do app).
    public ResultadoExclusao Remover(int id)
    {
        var marca = _repositorio.ObterPorId(id)
            ?? throw new NaoEncontradoException("Marca não encontrada.");

        if (_repositorio.PossuiProdutos(id))
        {
            marca.Inativa = true;
            _repositorio.Atualizar(marca);
            return ResultadoExclusao.Desativado;
        }

        _repositorio.Remover(marca);
        return ResultadoExclusao.Excluido;
    }

    private static void Normalizar(Marca marca)
    {
        marca.Nome = Validacoes.NormalizarTexto(marca.Nome);
        marca.Descricao = Validacoes.TextoOpcional(marca.Descricao);
    }

    private void Validar(Marca marca, int? ignorarId)
    {
        if (marca.Nome.Length < 2)
            throw new RegraDeNegocioException("Informe o nome da marca.");
        if (marca.Nome.Length > 80)
            throw new RegraDeNegocioException("O nome pode ter no máximo 80 caracteres.");
        if (marca.Descricao?.Length > 300)
            throw new RegraDeNegocioException("A descrição pode ter no máximo 300 caracteres.");
        if (Validacoes.TemCaracterePerigoso(marca.Nome) || Validacoes.TemCaracterePerigoso(marca.Descricao))
            throw new RegraDeNegocioException("Os textos não podem ter os caracteres < ou >.");

        if (_repositorio.NomeEmUso(marca.Nome, ignorarId))
            throw new ConflitoException("Já existe uma marca com este nome.");
    }
}
