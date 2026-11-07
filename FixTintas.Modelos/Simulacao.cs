namespace FixTintas.Modelos;

// Entidade: uma linha da tabela Simulacoes ("Meus projetos" do app).
// A pintura da foto acontece no celular; aqui fica a foto já pintada, a cor
// e o tipo de tinta escolhidos. A foto é privada: só o dono baixa.
public class Simulacao
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int CorId { get; set; }
    public Cor? Cor { get; set; }

    public int TipoTintaId { get; set; }
    public TipoTinta? TipoTinta { get; set; }

    // Nome dado pelo cliente (ex.: "Sala de estar"). Opcional.
    public string? Nome { get; set; }

    // Área da parede em m², se o cliente informou. Opcional.
    public decimal? AreaM2 { get; set; }

    // Caminho do arquivo na pasta privada da Api (App_Data/simulacoes/...).
    public string CaminhoImagem { get; set; } = string.Empty;

    // Preenchida pelo banco (valor padrão GETDATE()).
    public DateTime CriadoEm { get; set; }
}
