using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FixTintas.Modelos.Dtos;

// ============================================================================
// DTOs DA CONTA DO CLIENTE: login, cadastro, perfil e senha.
// Os mesmos DTOs servem a tela da Web e o JSON da Api (o app Flutter).
// No JSON os nomes saem em camelCase: "Nome" vira "nome", "ExpiraEm" vira "expiraEm".
// ============================================================================

// ENTRADA do login (tela "Entrar" do site e POST /api/auth/login do app).
public class LoginDto
{
    [Display(Name = "E-mail")]
    [Required(ErrorMessage = "Informe o e-mail.")]
    [StringLength(150, ErrorMessage = "O e-mail pode ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Senha")]
    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(100, ErrorMessage = "A senha pode ter no máximo 100 caracteres.")]
    public string Senha { get; set; } = string.Empty;
}

// SAÍDA do login do app: o token e quem é o usuário.
//   { "token", "expiraEm", "id", "nome", "email", "perfil", "trocarSenha" }
public class LoginRespostaDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public bool TrocarSenha { get; set; }
}

// ENTRADA do cadastro de cliente.
//   - O site manda tudo (CPF, nascimento, telefone e a confirmação da senha são
//     obrigatórios na Web: o Negocio confere com exigirDadosPessoais = true).
//   - O app manda só nome, e-mail e senha; os outros campos são opcionais,
//     mas se vierem são validados do mesmo jeito.
public class CadastroClienteDto
{
    [Display(Name = "Nome completo")]
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(120, ErrorMessage = "O nome pode ter no máximo 120 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "E-mail")]
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150, ErrorMessage = "O e-mail pode ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    // A regra da senha (8 caracteres, letras e números) está em RegrasDaSenha.cs.
    [Display(Name = "Senha")]
    [Description(RegrasDaSenha.Descricao)]
    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(RegrasDaSenha.TamanhoMaximo, ErrorMessage = RegrasDaSenha.MensagemTamanhoMaximo)]
    [RegularExpression(RegrasDaSenha.Padrao, ErrorMessage = RegrasDaSenha.Mensagem)]
    public string Senha { get; set; } = string.Empty;

    [Display(Name = "Confirmar senha")]
    [StringLength(100)]
    public string? ConfirmarSenha { get; set; }

    [Display(Name = "CPF")]
    [StringLength(14, ErrorMessage = "CPF inválido.")]
    public string? Cpf { get; set; }

    [Display(Name = "Data de nascimento")]
    public DateOnly? Nascimento { get; set; }

    [Display(Name = "Telefone")]
    [StringLength(20, ErrorMessage = "Telefone inválido.")]
    public string? Telefone { get; set; }
}

// SAÍDA: os dados da conta (tela "Meus dados" e GET /api/perfil do app).
//   { "id", "nome", "email", "telefone", "perfil", "criadoEm", "cpf", "nascimento", "trocarSenha" }
public class PerfilDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string Perfil { get; set; } = Perfis.Cliente;
    public DateTime CriadoEm { get; set; }
    public string? Cpf { get; set; }
    public DateOnly? Nascimento { get; set; }
    public bool TrocarSenha { get; set; }

    public static PerfilDto De(Usuario u) => new()
    {
        Id = u.Id,
        Nome = u.Nome,
        Email = u.Email,
        Telefone = u.Telefone,
        Perfil = u.Perfil,
        CriadoEm = u.DataCadastro,
        Cpf = u.Cpf,
        Nascimento = u.Nascimento,
        TrocarSenha = u.TrocarSenha
    };
}

// ENTRADA: alterar os próprios dados (PUT /api/perfil e tela "Meus dados").
public class AtualizarPerfilDto
{
    [Display(Name = "Nome completo")]
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(120, ErrorMessage = "O nome pode ter no máximo 120 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "Telefone")]
    [StringLength(20, ErrorMessage = "Telefone inválido.")]
    public string? Telefone { get; set; }

    public static AtualizarPerfilDto De(Usuario u) => new() { Nome = u.Nome, Telefone = u.Telefone };
}

// ENTRADA: trocar a própria senha (POST /api/perfil/alterar-senha e tela "Alterar senha").
// O app não manda a confirmação; o site manda (e o Negocio confere quando vem).
public class AlterarSenhaDto
{
    [Display(Name = "Senha atual")]
    [Required(ErrorMessage = "Informe a senha atual.")]
    [StringLength(100)]
    public string SenhaAtual { get; set; } = string.Empty;

    [Display(Name = "Nova senha")]
    [Description(RegrasDaSenha.Descricao)]
    [Required(ErrorMessage = "Informe a nova senha.")]
    [StringLength(RegrasDaSenha.TamanhoMaximo, ErrorMessage = RegrasDaSenha.MensagemTamanhoMaximo)]
    [RegularExpression(RegrasDaSenha.Padrao, ErrorMessage = RegrasDaSenha.Mensagem)]
    public string NovaSenha { get; set; } = string.Empty;

    [Display(Name = "Confirmar nova senha")]
    [StringLength(100)]
    public string? ConfirmarNovaSenha { get; set; }
}
