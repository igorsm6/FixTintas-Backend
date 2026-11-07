using FixTintas.Modelos;

namespace FixTintas.Repositorio.Interfaces;

public interface ISimulacaoRepositorio
{
    // Projetos do cliente (com a cor e o tipo de tinta), do mais recente para o mais antigo.
    List<Simulacao> ListarDoUsuario(int usuarioId);

    // Só devolve se o projeto for deste usuário (um cliente não vê o projeto de outro).
    Simulacao? ObterDoUsuario(int id, int usuarioId);

    void Adicionar(Simulacao simulacao);
    void Remover(Simulacao simulacao);
}
