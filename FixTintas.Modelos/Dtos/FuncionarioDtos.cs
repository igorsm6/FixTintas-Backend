using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FixTintas.Modelos.Dtos;

// ============================================================================
// DTOs DA EQUIPE (tela "Funcionários" do painel e /api/admin/funcionarios).
// Só o gerente usa.
// ============================================================================

public class FuncionarioDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public bool Ativo { get; set; }

    // true = ainda não trocou a senha temporária.
    public bool TrocarSenha { get; set; }

    public DateTime DataCadastro { get; set; }

    public static FuncionarioDto De(Usuario u) => new()
    {
        Id = u.Id,
        Nome = u.Nome,
        Email = u.Email,
        Perfil = u.Perfil,
        Ativo = u.Ativo,
        TrocarSenha = u.TrocarSenha,
        DataCadastro = u.DataCadastro
    };
}

// ENTRADA: novo funcionário (nasce com senha temporária e precisa trocar no 1º acesso).
public class FuncionarioCriarDto
{
    [Display(Name = "Nome completo")]
    [Required(ErrorMessage = "Informe o nome completo.")]
    [StringLength(120, ErrorMessage = "O nome pode ter no máximo 120 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "E-mail")]
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150, ErrorMessage = "O e-mail pode ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    // "Funcionario" ou "Gerente".
    [Display(Name = "Perfil")]
    [Required(ErrorMessage = "Escolha o perfil.")]
    public string Perfil { get; set; } = Perfis.Funcionario;

    // Opcional: vazio = o sistema gera uma senha temporária e mostra ao gerente.
    // Se o gerente digitar uma, ela segue a mesma regra das outras senhas (RegrasDaSenha.cs).
    [Display(Name = "Senha temporária (opcional)")]
    [Description("Opcional. " + RegrasDaSenha.Descricao + " Vazio = o sistema gera uma.")]
    [StringLength(RegrasDaSenha.TamanhoMaximo, ErrorMessage = RegrasDaSenha.MensagemTamanhoMaximo)]
    [RegularExpression(RegrasDaSenha.Padrao, ErrorMessage = RegrasDaSenha.Mensagem)]
    public string? SenhaTemporaria { get; set; }
}

// SAÍDA do cadastro de funcionário e da redefinição de senha: o gerente precisa ver a
// senha temporária UMA vez, para passar ao funcionário (ela não fica guardada em texto).
public class FuncionarioCriadoDto
{
    public FuncionarioDto Funcionario { get; set; } = new();
    public string SenhaTemporaria { get; set; } = string.Empty;
}

// ENTRADA de PATCH /api/admin/funcionarios/{id}   { "ativo": false }
// bool? para dar erro se o campo não vier, em vez de virar false sozinho.
public class FuncionarioAtivoDto
{
    [Required(ErrorMessage = "Informe \"ativo\": true ou false.")]
    public bool? Ativo { get; set; }
}
