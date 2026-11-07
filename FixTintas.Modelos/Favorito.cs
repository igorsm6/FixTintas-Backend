namespace FixTintas.Modelos;

// Entidade: uma linha da tabela Favoritos (uma cor favoritada por um cliente).
// A chave é o par (UsuarioId, CorId): a mesma cor não entra duas vezes.
// O cliente favorita no site e vê no app, e vice-versa.
public class Favorito
{
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int CorId { get; set; }
    public Cor? Cor { get; set; }

    // Preenchida pelo banco (valor padrão GETDATE()).
    public DateTime CriadoEm { get; set; }
}
