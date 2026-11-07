using FixTintas.Modelos;

namespace FixTintas.Repositorio.Interfaces;

public interface ICupomRepositorio
{
    List<Cupom> Listar();
    Cupom? ObterPorId(int id);
    Cupom? ObterPorCodigo(string codigo);
    bool CodigoEmUso(string codigo, int? ignorarId = null);
    bool PossuiPedidos(int id);

    void Adicionar(Cupom cupom);
    void Atualizar(Cupom cupom);
    void Remover(Cupom cupom);
}
