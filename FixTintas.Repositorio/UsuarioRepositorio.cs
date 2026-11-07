using FixTintas.Modelos;
using FixTintas.Repositorio.Contexto;
using FixTintas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixTintas.Repositorio;

// Única classe que conversa com a tabela Usuarios (Entity Framework Core).
public class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly AppDbContext _contexto;

    // INJEÇÃO DE DEPENDÊNCIA: o contexto do banco chega pronto, já com a
    // connection string do appsettings.json (linha AddDbContext do Program.cs).
    public UsuarioRepositorio(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public Usuario? ObterPorId(int id) =>
        _contexto.Usuarios.FirstOrDefault(u => u.Id == id);

    public Usuario? ObterPorEmail(string email) =>
        _contexto.Usuarios.FirstOrDefault(u => u.Email == email);

    public bool EmailEmUso(string email, int? ignorarId = null) =>
        _contexto.Usuarios.Any(u => u.Email == email && u.Id != ignorarId);

    public bool CpfEmUso(string cpf, int? ignorarId = null) =>
        _contexto.Usuarios.Any(u => u.Cpf == cpf && u.Id != ignorarId);

    public List<Usuario> ListarEquipe() =>
        _contexto.Usuarios.AsNoTracking()
            .Where(u => u.Perfil == Perfis.Funcionario || u.Perfil == Perfis.Gerente)
            .OrderBy(u => u.Nome)
            .ToList();

    public bool ExisteGerenteAtivo() =>
        _contexto.Usuarios.Any(u => u.Perfil == Perfis.Gerente && u.Ativo);

    public int ContarClientes() =>
        _contexto.Usuarios.Count(u => u.Perfil == Perfis.Cliente);

    public void Adicionar(Usuario usuario)
    {
        _contexto.Usuarios.Add(usuario);
        _contexto.Gravar();
    }

    public void Atualizar(Usuario usuario)
    {
        // Se o usuário veio do banco nesta mesma requisição (ObterPorId), o EF já
        // acompanha o que mudou e só grava. Se veio "de fora", o Update avisa o EF.
        if (_contexto.Entry(usuario).State == EntityState.Detached)
            _contexto.Usuarios.Update(usuario);
        _contexto.Gravar();
    }
}
