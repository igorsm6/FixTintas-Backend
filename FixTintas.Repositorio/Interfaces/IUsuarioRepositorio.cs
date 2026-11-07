using FixTintas.Modelos;

namespace FixTintas.Repositorio.Interfaces;

// Contrato do repositório de usuários (clientes, funcionários e gerentes).
// O Negocio conhece só esta interface, nunca a classe concreta.
public interface IUsuarioRepositorio
{
    Usuario? ObterPorId(int id);
    Usuario? ObterPorEmail(string email);
    bool EmailEmUso(string email, int? ignorarId = null);
    bool CpfEmUso(string cpf, int? ignorarId = null);

    // Funcionários e gerentes (tela "Funcionários" do painel).
    List<Usuario> ListarEquipe();
    bool ExisteGerenteAtivo();
    int ContarClientes();

    void Adicionar(Usuario usuario);
    void Atualizar(Usuario usuario);
}
