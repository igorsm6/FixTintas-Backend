using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FixTintas.Modelos;

namespace FixTintas.Negocio;

// Regras de validação usadas por mais de uma classe de negócio.
// Valem para o site e para o app, porque os dois passam por aqui: o navegador e o
// celular podem conferir antes (para avisar mais rápido), mas quem decide é o Negocio.
public static class Validacoes
{
    private static readonly Regex RegexEmail = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private static readonly Regex RegexEspacos = new(@"\s+", RegexOptions.Compiled);

    // "123.456.789-09" -> "12345678909"
    public static string SomenteDigitos(string? texto) =>
        new string((texto ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

    // Tira espaços das pontas e deixa em minúsculas: "Ana@Exemplo.com " -> "ana@exemplo.com"
    public static string NormalizarEmail(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    // Tira espaços das pontas e junta espaços repetidos: "  Ana   Souza " -> "Ana Souza"
    public static string NormalizarTexto(string? texto) =>
        RegexEspacos.Replace((texto ?? string.Empty).Trim(), " ");

    // Texto opcional: vazio vira null.
    public static string? TextoOpcional(string? texto)
    {
        var limpo = NormalizarTexto(texto);
        return limpo.Length == 0 ? null : limpo;
    }

    public static bool EmailValido(string email) =>
        email.Length <= 150 && RegexEmail.IsMatch(email);

    // Pelo menos duas palavras ("nome e sobrenome").
    public static bool NomeCompleto(string nome) =>
        nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2;

    // < e > não são aceitos em textos que aparecem nas páginas: assim ninguém grava
    // um "<script>" no nome de um produto para rodar código no navegador dos clientes.
    public static bool TemCaracterePerigoso(string? texto) =>
        texto is not null && (texto.Contains('<') || texto.Contains('>'));

    // CPF com os dois dígitos verificadores corretos.
    public static bool CpfValido(string cpf)
    {
        if (cpf.Length != 11 || !cpf.All(char.IsAsciiDigit)) return false;

        // 000.000.000-00, 111.111.111-11... passam na conta, mas não são CPFs reais.
        if (cpf.Distinct().Count() == 1) return false;

        for (var posicao = 9; posicao < 11; posicao++)
        {
            var soma = 0;
            for (var i = 0; i < posicao; i++)
                soma += (cpf[i] - '0') * (posicao + 1 - i);

            var digito = soma * 10 % 11 % 10;
            if (digito != cpf[posicao] - '0') return false;
        }
        return true;
    }

    // null = senha boa; texto = o problema. No mínimo 8 caracteres, com letras e números.
    // A regra em si fica em FixTintas.Modelos/RegrasDaSenha.cs (a mesma dos DTOs).
    public static string? ProblemaNaSenha(string? senha)
    {
        if (!RegrasDaSenha.Atende(senha))
            return RegrasDaSenha.Mensagem;

        if (senha!.Length > RegrasDaSenha.TamanhoMaximo)
            return RegrasDaSenha.MensagemTamanhoMaximo;

        return null;
    }

    // Idade completa na data de hoje.
    public static int Idade(DateOnly nascimento, DateOnly hoje)
    {
        var idade = hoje.Year - nascimento.Year;
        if (nascimento > hoje.AddYears(-idade)) idade--;
        return idade;
    }

    // "Plásticos de Proteção" -> "Plasticos de Protecao"
    public static string RemoverAcentos(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(decomposto.Length);
        foreach (var letra in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark)
                resultado.Append(letra);
        }
        return resultado.ToString().Normalize(NormalizationForm.FormC);
    }

    // Nome que vai na URL: "Plásticos de Proteção" -> "plasticos-de-protecao"
    public static string SlugDe(string? texto)
    {
        var minusculo = RemoverAcentos(texto ?? string.Empty).ToLowerInvariant();
        var resultado = new StringBuilder(minusculo.Length);
        foreach (var letra in minusculo)
        {
            if (char.IsAsciiLetterOrDigit(letra))
                resultado.Append(letra);
            else if (resultado.Length > 0 && resultado[^1] != '-')
                resultado.Append('-');
        }
        return resultado.ToString().Trim('-');
    }

    // "#abc", "AABBCC" ou "#aabbcc" -> "#AABBCC". Devolve null se não for uma cor válida.
    public static string? NormalizarHex(string? hex)
    {
        var limpo = (hex ?? string.Empty).Trim().TrimStart('#');
        if (limpo.Length == 3) limpo = string.Concat(limpo.Select(c => $"{c}{c}"));
        if (limpo.Length != 6 || !limpo.All(char.IsAsciiHexDigit)) return null;
        return "#" + limpo.ToUpperInvariant();
    }
}
