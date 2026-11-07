using FixTintas.Modelos;
using FixTintas.Repositorio;
using FixTintas.Repositorio.Interfaces;

namespace FixTintas.Negocio;

// Regras do catálogo de cores (usado pelo site, pelo app e pelos favoritos).
// O código da cor é guardado sempre no formato "#RRGGBB", em maiúsculas.
public class CorNegocio
{
    private readonly ICorRepositorio _repositorio;

    public CorNegocio(ICorRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public List<Cor> Listar(string? familia = null, string? busca = null, bool incluirInativas = false) =>
        _repositorio.Listar(Validacoes.TextoOpcional(familia), Validacoes.TextoOpcional(busca), incluirInativas);

    public List<string> ListarFamilias() => _repositorio.ListarFamilias();

    public Cor? ObterPorId(int id) => _repositorio.ObterPorId(id);

    // Só cores que estão à venda (o app e o site não mostram as inativas).
    public Cor? ObterAtiva(int id)
    {
        var cor = _repositorio.ObterPorId(id);
        return cor is { Inativa: false } ? cor : null;
    }

    public Cor Cadastrar(Cor cor)
    {
        Normalizar(cor);
        Validar(cor, ignorarId: null);

        try
        {
            _repositorio.Adicionar(cor);
        }
        catch (ConflitoDeGravacaoException)
        {
            throw new ConflitoException("Já existe uma cor com este nome.");
        }
        return cor;
    }

    public void Atualizar(Cor cor)
    {
        var existente = _repositorio.ObterPorId(cor.Id)
            ?? throw new NaoEncontradoException("Cor não encontrada.");

        Normalizar(cor);
        Validar(cor, ignorarId: cor.Id);

        existente.Nome = cor.Nome;
        existente.CodigoHex = cor.CodigoHex;
        existente.Familia = cor.Familia;
        existente.CodigoPantone = cor.CodigoPantone;
        existente.Inativa = cor.Inativa;

        try
        {
            _repositorio.Atualizar(existente);
        }
        catch (ConflitoDeGravacaoException)
        {
            throw new ConflitoException("Já existe uma cor com este nome.");
        }
    }

    // Cor que já foi vendida (ou está num projeto do app) não é apagada: é desativada.
    public ResultadoExclusao Remover(int id)
    {
        var cor = _repositorio.ObterPorId(id)
            ?? throw new NaoEncontradoException("Cor não encontrada.");

        if (_repositorio.EmUso(id))
        {
            cor.Inativa = true;
            _repositorio.Atualizar(cor);
            return ResultadoExclusao.Desativado;
        }

        _repositorio.Remover(cor);
        return ResultadoExclusao.Excluido;
    }

    private static void Normalizar(Cor cor)
    {
        cor.Nome = Validacoes.NormalizarTexto(cor.Nome);
        cor.CodigoHex = Validacoes.NormalizarHex(cor.CodigoHex) ?? string.Empty;
        cor.Familia = Validacoes.TextoOpcional(cor.Familia);
        cor.CodigoPantone = Validacoes.TextoOpcional(cor.CodigoPantone);
    }

    private void Validar(Cor cor, int? ignorarId)
    {
        if (cor.Nome.Length < 2)
            throw new RegraDeNegocioException("Informe o nome da cor.");
        if (cor.Nome.Length > 80)
            throw new RegraDeNegocioException("O nome pode ter no máximo 80 caracteres.");
        if (cor.CodigoHex.Length == 0)
            throw new RegraDeNegocioException("Informe o código da cor no formato #RRGGBB (ex.: #1F5F8B).");
        if (cor.Familia?.Length > 40 || cor.CodigoPantone?.Length > 40)
            throw new RegraDeNegocioException("A família e o código Pantone podem ter no máximo 40 caracteres.");
        if (Validacoes.TemCaracterePerigoso(cor.Nome) || Validacoes.TemCaracterePerigoso(cor.Familia)
            || Validacoes.TemCaracterePerigoso(cor.CodigoPantone))
            throw new RegraDeNegocioException("Os textos não podem ter os caracteres < ou >.");

        if (_repositorio.NomeEmUso(cor.Nome, ignorarId))
            throw new ConflitoException("Já existe uma cor com este nome.");
    }
}
