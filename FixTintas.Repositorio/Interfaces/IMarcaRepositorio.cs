using FixTintas.Modelos;

namespace FixTintas.Repositorio.Interfaces;

public interface IMarcaRepositorio
{
    List<Marca> Listar(bool incluirInativas);
    Marca? ObterPorId(int id);
    bool NomeEmUso(string nome, int? ignorarId = null);
    bool PossuiProdutos(int id);

    void Adicionar(Marca marca);
    void Atualizar(Marca marca);
    void Remover(Marca marca);
}
