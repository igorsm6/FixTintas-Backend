namespace FixTintas.Modelos;

// Os três perfis de acesso do sistema (site, painel e aplicativo usam os mesmos):
//   Cliente     -> quem se cadastra pelo site ou pelo app para comprar
//   Funcionario -> equipe da loja: cadastra produtos, cuida do estoque e dos pedidos
//   Gerente     -> tudo do funcionário + preços, exclusões, catálogo, cupons e equipe
//
// São "const" para poderem ser usados dentro de atributos, por exemplo
// [Authorize(Roles = Perfis.Gerente)] nos controllers da Web e da Api.
public static class Perfis
{
    public const string Cliente = "Cliente";
    public const string Funcionario = "Funcionario";
    public const string Gerente = "Gerente";

    // Os dois perfis da equipe, no formato do [Authorize(Roles = ...)]:
    // separados por vírgula = "qualquer um destes".
    public const string Equipe = Funcionario + "," + Gerente;

    public static readonly IReadOnlyList<string> Todos = new[] { Cliente, Funcionario, Gerente };

    public static bool EhEquipe(string? perfil) => perfil == Funcionario || perfil == Gerente;

    // Texto para mostrar na tela (com acento).
    public static string Rotulo(string? perfil) => perfil switch
    {
        Funcionario => "Funcionário",
        Gerente => "Gerente",
        _ => "Cliente"
    };
}
