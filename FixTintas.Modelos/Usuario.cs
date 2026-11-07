namespace FixTintas.Modelos;

// Entidade: uma linha da tabela Usuarios.
// Clientes, funcionários e gerentes ficam na mesma tabela; o que muda é o Perfil.
// É a mesma conta no site e no aplicativo.
//
// A entidade nunca sai para a tela nem para o app: o que sai é sempre um DTO
// (pasta Dtos). Assim a coluna SenhaHash nunca aparece num JSON.
public class Usuario
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    // Guardado sempre em minúsculas (o Negocio normaliza antes de salvar).
    public string Email { get; set; } = string.Empty;

    // NUNCA a senha em texto: só o hash gerado pelo SenhaHasher (camada Negocio).
    public string SenhaHash { get; set; } = string.Empty;

    // "Cliente", "Funcionario" ou "Gerente" (ver Perfis.cs).
    public string Perfil { get; set; } = Perfis.Cliente;

    // Dados pessoais: obrigatórios no cadastro pelo site, opcionais pelo app.
    public string? Cpf { get; set; }          // só os 11 dígitos
    public DateOnly? Nascimento { get; set; } // só a data, sem hora
    public string? Telefone { get; set; }     // só os dígitos, com DDD

    // false = conta desativada pelo gerente: não entra mais no site nem no app.
    public bool Ativo { get; set; } = true;

    // true = senha temporária criada pelo gerente; precisa trocar no próximo acesso.
    public bool TrocarSenha { get; set; }

    // Preenchida pelo banco (valor padrão GETDATE()).
    public DateTime DataCadastro { get; set; }

    // Preenchida pela trigger TR_Usuarios_DataAtualizacao, não pelo C#.
    public DateTime? DataAtualizacao { get; set; }

    // Navegações do Entity Framework (Usuario 1 --- N ...).
    public List<Pedido> Pedidos { get; set; } = new();
    public List<Favorito> Favoritos { get; set; } = new();
    public List<Simulacao> Simulacoes { get; set; } = new();
}
