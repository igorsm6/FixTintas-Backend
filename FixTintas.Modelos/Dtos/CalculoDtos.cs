using System.ComponentModel.DataAnnotations;

namespace FixTintas.Modelos.Dtos;

// ============================================================================
// DTOs DA CALCULADORA DE TINTA (POST /api/calculos/tinta, usada pelo app).
// ============================================================================

// ENTRADA: altura e largura da parede OU a área em m².
// "AreaDescontarM2" = portas, janelas e o que não vai ser pintado.
public class CalculoTintaEntradaDto
{
    [Range(0.1, 1000, ErrorMessage = "A altura deve estar entre 0,1 e 1000 metros.")]
    public decimal? AlturaM { get; set; }

    [Range(0.1, 1000, ErrorMessage = "A largura deve estar entre 0,1 e 1000 metros.")]
    public decimal? LarguraM { get; set; }

    [Range(0.1, 100000, ErrorMessage = "A área deve ser maior que zero.")]
    public decimal? AreaM2 { get; set; }

    [Range(0, 100000, ErrorMessage = "A área a descontar não pode ser negativa.")]
    public decimal AreaDescontarM2 { get; set; }

    // Vazio = usa as demãos recomendadas do tipo de tinta.
    [Range(1, 6, ErrorMessage = "Informe de 1 a 6 demãos.")]
    public int? Demaos { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Escolha um tipo de tinta.")]
    public int TipoTintaId { get; set; }

    // Opcional: limita as sugestões a uma marca.
    public int? MarcaId { get; set; }
}

// Uma lata dentro de uma sugestão de compra.
public class ItemOpcaoDto
{
    public int ProdutoId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal VolumeLitros { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

// Uma forma de comprar a quantidade necessária: a mais barata de uma marca.
public class OpcaoCompraDto
{
    public int MarcaId { get; set; }
    public string MarcaNome { get; set; } = string.Empty;
    public List<ItemOpcaoDto> Itens { get; set; } = new();
    public decimal LitrosComprados { get; set; }
    public decimal SobraLitros { get; set; }
    public decimal Total { get; set; }
}

// SAÍDA: o resultado do cálculo.
public class CalculoTintaResultadoDto
{
    public decimal AreaBrutaM2 { get; set; }
    public decimal AreaDescontadaM2 { get; set; }
    public decimal AreaLiquidaM2 { get; set; }
    public int Demaos { get; set; }
    public decimal RendimentoM2PorLitro { get; set; }
    public decimal MargemPerdaPercentual { get; set; }
    public decimal LitrosNecessarios { get; set; }
    public int TipoTintaId { get; set; }
    public string TipoTintaNome { get; set; } = string.Empty;
    public string? Aviso { get; set; }
    public List<OpcaoCompraDto> Opcoes { get; set; } = new();
}
