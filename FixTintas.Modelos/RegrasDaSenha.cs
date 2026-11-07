using System.Text.RegularExpressions;

namespace FixTintas.Modelos;

// A REGRA DA SENHA, num lugar só: no mínimo 8 caracteres, com pelo menos uma letra
// e pelo menos um número (e no máximo 100 caracteres).
//
// Quem usa esta classe:
//   - Os DTOs (CadastroClienteDto, AlterarSenhaDto e FuncionarioCriarDto), no atributo
//     [RegularExpression(RegrasDaSenha.Padrao, ...)]. Com isso:
//       * a Api responde 400 com a mensagem antes mesmo de chegar no controller;
//       * a documentação da Api (/scalar) mostra a regra no campo da senha;
//       * o site confere no navegador (jQuery Validation) antes de enviar o formulário.
//   - O Negocio (Validacoes.ProblemaNaSenha), que confere de novo antes de salvar.
//
// As caixas "A senha precisa ter:" mostram as mesmas três regras enquanto a pessoa digita:
//   - no site:  FixTintas.Web/wwwroot/js/site.js (função iniciarRegrasSenha)
//   - no app:   lib/widgets/regras_senha.dart (projeto Flutter)
// Mudou a regra aqui? Mude também nesses dois arquivos.
public static class RegrasDaSenha
{
    public const int TamanhoMinimo = 8;
    public const int TamanhoMaximo = 100;

    // Letras de A a Z, maiúsculas e minúsculas, com ou sem acento (À até ÿ, sem os sinais × e ÷).
    public const string Letras = "A-Za-zÀ-ÖØ-öø-ÿ";

    // O mesmo texto serve para o .NET (servidor) e para o JavaScript (navegador):
    //   (?=.*[letras])  tem pelo menos uma letra
    //   (?=.*[0-9])     tem pelo menos um número
    //   .{8,}           tem 8 caracteres ou mais (o máximo de 100 fica com o [StringLength])
    public const string Padrao = "^(?=.*[" + Letras + "])(?=.*[0-9]).{8,}$";

    public const string Mensagem = "A senha deve ter no mínimo 8 caracteres, com letras e números.";
    public const string MensagemTamanhoMaximo = "A senha pode ter no máximo 100 caracteres.";

    // Texto curto que aparece na documentação da Api, ao lado do campo da senha.
    public const string Descricao = "Mínimo de 8 caracteres, com pelo menos uma letra e um número.";

    private static readonly Regex Expressao = new(Padrao, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    // true = a senha cumpre as três regras (tamanho mínimo, letra e número).
    // Confere do mesmo jeito que o [RegularExpression]: o padrão tem que cobrir a senha inteira.
    public static bool Atende(string? senha)
    {
        if (senha is null) return false;
        var resultado = Expressao.Match(senha);
        return resultado.Success && resultado.Index == 0 && resultado.Length == senha.Length;
    }
}
