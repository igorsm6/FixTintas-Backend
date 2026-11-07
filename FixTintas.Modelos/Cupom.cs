namespace FixTintas.Modelos;

// Entidade: uma linha da tabela Cupons (cupom de desconto). O código fica em maiúsculas.
public class Cupom
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public TipoDesconto Tipo { get; set; } = TipoDesconto.Percentual;

    // Porcentagem (Percentual) ou reais (ValorFixo).
    public decimal Valor { get; set; }

    // Valor mínimo do pedido para o cupom valer (0 = sem mínimo).
    public decimal ValorMinimoPedido { get; set; }

    // Último dia em que o cupom vale (null = sem validade).
    public DateTime? ValidoAte { get; set; }

    // Quantas vezes pode ser usado no total (null = sem limite).
    public int? UsosMaximos { get; set; }

    public int UsosRealizados { get; set; }

    public bool Inativo { get; set; }

    // Impede que duas compras usem o último uso do cupom ao mesmo tempo.
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
