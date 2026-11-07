namespace FixTintas.Negocio;

// ============================================================================
// Números que a loja pode mudar sem mexer no código: ficam na seção "Loja" do
// appsettings.json (da Web E da Api — mudou num, mude no outro):
//
//   "Loja": {
//     "DescontoVolumeAcimaDe": 500,
//     "DescontoVolumePercentual": 5,
//     "MargemPerdaPercentual": 10,
//     "AreaMaximaM2": 1000,
//     "QuantidadeMaximaPorItem": 100
//   }
//
// O Program.cs lê a seção e registra um objeto só para o sistema inteiro
// (AddSingleton). As classes de negócio pedem RegrasDaLoja no construtor.
// ============================================================================
public class RegrasDaLoja
{
    // Pedidos com subtotal a partir deste valor ganham o desconto por volume.
    public decimal DescontoVolumeAcimaDe { get; set; } = 500m;

    // Porcentagem do desconto por volume (5 = 5%). Use 0 para desligar.
    public decimal DescontoVolumePercentual { get; set; } = 5m;

    // Folga somada ao cálculo de litros para cobrir perdas e retoques (10 = 10%).
    public decimal MargemPerdaPercentual { get; set; } = 10m;

    // Maior área aceita na calculadora de tinta, em m².
    public decimal AreaMaximaM2 { get; set; } = 1000m;

    // Maior quantidade de um mesmo item num pedido.
    public int QuantidadeMaximaPorItem { get; set; } = 100;
}
