using FixTintas.Modelos;
using FixTintas.Repositorio;
using FixTintas.Repositorio.Interfaces;

namespace FixTintas.Negocio;

// Cores favoritas do cliente (o coração na página de cores do site e no app).
// Marcar duas vezes ou desmarcar o que não estava marcado não dá erro.
public class FavoritoNegocio
{
    private readonly IFavoritoRepositorio _repositorio;
    private readonly ICorRepositorio _cores;

    public FavoritoNegocio(IFavoritoRepositorio repositorio, ICorRepositorio cores)
    {
        _repositorio = repositorio;
        _cores = cores;
    }

    public List<Cor> Listar(int usuarioId) => _repositorio.ListarCores(usuarioId);

    public HashSet<int> IdsDasCores(int usuarioId) => _repositorio.IdsDasCores(usuarioId).ToHashSet();

    public void Adicionar(int usuarioId, int corId)
    {
        var cor = _cores.ObterPorId(corId);
        if (cor is null || cor.Inativa)
            throw new NaoEncontradoException("Cor não encontrada.");

        if (_repositorio.Existe(usuarioId, corId)) return;

        try
        {
            _repositorio.Adicionar(new Favorito { UsuarioId = usuarioId, CorId = corId });
        }
        catch (ConflitoDeGravacaoException)
        {
            // Dois cliques seguidos: o primeiro já gravou. Nada a fazer.
        }
    }

    public void Remover(int usuarioId, int corId) => _repositorio.Remover(usuarioId, corId);
}
