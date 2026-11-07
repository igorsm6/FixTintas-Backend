using System.Security.Cryptography;

namespace FixTintas.Negocio;

// ============================================================================
// SENHAHASHER — como guardar senhas do jeito certo
//
// NUNCA se guarda a senha em texto: se o banco vazar, vazam todas as senhas.
// Guardamos um HASH: o resultado de uma conta matemática que não tem volta.
//   - PBKDF2: algoritmo feito para senhas, propositalmente lento (100 mil
//     repetições), para dificultar quem tenta adivinhar milhões de senhas.
//   - SAL: um valor aleatório diferente para cada usuário. Duas pessoas com a
//     mesma senha ficam com hashes diferentes no banco.
// Tudo isso já vem no .NET (System.Security.Cryptography), sem pacote extra.
//
// Formato guardado na coluna SenhaHash:
//   PBKDF2-SHA256$100000$<sal em base64>$<hash em base64>
// ============================================================================
public static class SenhaHasher
{
    private const string Algoritmo = "PBKDF2-SHA256";
    private const int Iteracoes = 100_000;
    private const int TamanhoSal = 16;   // bytes
    private const int TamanhoHash = 32;  // bytes

    // Cadastro / troca de senha: transforma "minhasenha1" no texto que vai para o banco.
    public static string Gerar(string senha)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanhoSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, sal, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
        return string.Join('$', Algoritmo, Iteracoes.ToString(), Convert.ToBase64String(sal), Convert.ToBase64String(hash));
    }

    // Login: refaz a conta com a senha digitada e compara com o hash guardado.
    public static bool Verificar(string senha, string hashGuardado)
    {
        var partes = hashGuardado.Split('$');
        if (partes.Length != 4 || partes[0] != Algoritmo || !int.TryParse(partes[1], out var iteracoes))
            return false;

        byte[] sal;
        byte[] esperado;
        try
        {
            sal = Convert.FromBase64String(partes[2]);
            esperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, sal, iteracoes, HashAlgorithmName.SHA256, esperado.Length);

        // FixedTimeEquals compara sempre no mesmo tempo, acertando ou errando
        // (evita o "ataque de tempo", que descobre a senha medindo a demora da resposta).
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }

    // Senha temporária para funcionário novo (ou que esqueceu a senha): 10 caracteres,
    // com letras e números, sem os que se confundem (0/O, 1/l/I).
    // O funcionário é obrigado a trocar no primeiro acesso.
    public static string GerarSenhaTemporaria()
    {
        const string letras = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digitos = "23456789";

        var senha = new char[10];
        for (var i = 0; i < senha.Length; i++)
        {
            var conjunto = i % 3 == 2 ? digitos : letras;
            senha[i] = conjunto[RandomNumberGenerator.GetInt32(conjunto.Length)];
        }
        return new string(senha);
    }
}
