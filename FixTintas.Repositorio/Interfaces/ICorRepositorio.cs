using FixTintas.Modelos;

namespace FixTintas.Repositorio.Interfaces;

public interface ICorRepositorio
{
    // Filtros opcionais: família (ex.: "Azul") e parte do nome.
    List<Cor> Listar(string? familia, string? busca, bool incluirInativas);
    List<string> ListarFamilias();
    Cor? ObterPorId(int id);
    List<Cor> ObterPorIds(IEnumerable<int> ids);
    bool NomeEmUso(string nome, int? ignorarId = null);

    // true = a cor já foi vendida ou está num projeto do app.
    bool EmUso(int id);

    void Adicionar(Cor cor);
    void Atualizar(Cor cor);
    void Remover(Cor cor);
}
