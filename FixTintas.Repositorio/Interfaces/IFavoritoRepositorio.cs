using FixTintas.Modelos;

namespace FixTintas.Repositorio.Interfaces;

public interface IFavoritoRepositorio
{
    // As cores favoritas do cliente (só as que continuam ativas), da mais recente para a mais antiga.
    List<Cor> ListarCores(int usuarioId);
    List<int> IdsDasCores(int usuarioId);
    bool Existe(int usuarioId, int corId);

    void Adicionar(Favorito favorito);
    void Remover(int usuarioId, int corId);
}
