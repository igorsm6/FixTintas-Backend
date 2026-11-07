using System.ComponentModel.DataAnnotations;

namespace FixTintas.Modelos.Dtos;

// SAÍDA: um cupom (painel e /api/admin/cupons).
public class CupomDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;

    // "Percentual" ou "ValorFixo".
    public string Tipo { get; set; } = string.Empty;

    public decimal Valor { get; set; }
    public decimal ValorMinimoPedido { get; set; }
    public DateTime? ValidoAte { get; set; }
    public int? UsosMaximos { get; set; }
    public int UsosRealizados { get; set; }
    public bool Inativo { get; set; }

    // "10%" ou "R$ 50,00"
    public string ValorTexto => Tipo == nameof(TipoDesconto.Percentual)
        ? Formatacao.Numero(Valor) + "%"
        : Formatacao.Moeda(Valor);

    public static CupomDto De(Cupom c) => new()
    {
        Id = c.Id,
        Codigo = c.Codigo,
        Tipo = c.Tipo.ToString(),
        Valor = c.Valor,
        ValorMinimoPedido = c.ValorMinimoPedido,
        ValidoAte = c.ValidoAte,
        UsosMaximos = c.UsosMaximos,
        UsosRealizados = c.UsosRealizados,
        Inativo = c.Inativo
    };
}

// ENTRADA: cadastro e edição de cupom. O Negocio converte o Tipo (texto) e
// confere as regras (percentual até 100%, código único...).
public class CupomSalvarDto
{
    [Display(Name = "Código")]
    [Required(ErrorMessage = "Informe o código do cupom.")]
    [StringLength(40, ErrorMessage = "O código pode ter no máximo 40 caracteres.")]
    [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Use só letras, números, hífen e sublinhado no código.")]
    public string Codigo { get; set; } = string.Empty;

    // "Percentual" ou "ValorFixo".
    [Display(Name = "Tipo de desconto")]
    [Required(ErrorMessage = "Informe o tipo do desconto.")]
    public string Tipo { get; set; } = nameof(TipoDesconto.Percentual);

    [Display(Name = "Valor (% ou R$)")]
    [Required(ErrorMessage = "Informe o valor do desconto.")]
    [Range(0.01, 100000, ErrorMessage = "Informe um valor válido.")]
    public decimal Valor { get; set; }

    [Display(Name = "Valor mínimo do pedido (R$)")]
    [Required(ErrorMessage = "Informe o valor mínimo (0 = sem mínimo).")]
    [Range(0, 1000000, ErrorMessage = "Informe um valor mínimo válido.")]
    public decimal ValorMinimoPedido { get; set; }

    [Display(Name = "Válido até (último dia)")]
    public DateTime? ValidoAte { get; set; }

    [Display(Name = "Limite de usos")]
    [Range(1, int.MaxValue, ErrorMessage = "O limite de usos deve ser de pelo menos 1.")]
    public int? UsosMaximos { get; set; }

    [Display(Name = "Inativo")]
    public bool Inativo { get; set; }

    public static CupomSalvarDto De(Cupom c) => new()
    {
        Codigo = c.Codigo,
        Tipo = c.Tipo.ToString(),
        Valor = c.Valor,
        ValorMinimoPedido = c.ValorMinimoPedido,
        ValidoAte = c.ValidoAte,
        UsosMaximos = c.UsosMaximos,
        Inativo = c.Inativo
    };
}
