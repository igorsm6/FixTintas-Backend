using FixTintas.Modelos;

namespace FixTintas.Repositorio.Interfaces;

public interface ITipoTintaRepositorio
{
    List<TipoTinta> Listar(bool incluirInativos);
    TipoTinta? ObterPorId(int id);
    bool NomeEmUso(string nome, string acabamento, int? ignorarId = null);

    // true = existe produto ou projeto do app usando este tipo.
    bool EmUso(int id);

    void Adicionar(TipoTinta tipo);
    void Atualizar(TipoTinta tipo);
    void Remover(TipoTinta tipo);
}
