using System.Globalization;

namespace FixTintas.Modelos;

// Formatações em português usadas pelas telas, pelos DTOs e pelas mensagens do Negocio.
// O sistema LÊ números sempre com ponto (cultura invariável, ver Program.cs),
// mas MOSTRA tudo no formato brasileiro.
public static class Formatacao
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    // 1234.5 -> "R$ 1.234,50"
    public static string Moeda(decimal valor) => valor.ToString("C2", PtBr);

    // 0.9 -> "900 ml"   3.6 -> "3,6 L"   18 -> "18 L"
    public static string Volume(decimal litros) =>
        litros < 1m
            ? (litros * 1000m).ToString("0", PtBr) + " ml"
            : litros.ToString("0.##", PtBr) + " L";

    // 3.5 -> "3,5"
    public static string Numero(decimal valor) => valor.ToString("0.##", PtBr);

    public static string DataHora(DateTime data) => data.ToString("dd/MM/yyyy HH:mm", PtBr);

    public static string Data(DateTime data) => data.ToString("dd/MM/yyyy", PtBr);

    // Rótulo da opção que o cliente escolhe: volume da tinta, tamanho do acessório ou "Unidade".
    public static string RotuloDoProduto(decimal? volumeLitros, string? tamanho) =>
        volumeLitros is decimal litros ? Volume(litros)
        : string.IsNullOrWhiteSpace(tamanho) ? "Unidade"
        : tamanho;

    // Nome completo de um item, como aparece no carrinho e no pedido:
    // "Tintas Acrílicas Premium 3,6 L", "Rolo de Pintura 6\"", "Lixa Grossa".
    public static string DescricaoDoProduto(string nome, decimal? volumeLitros, string? tamanho) =>
        volumeLitros is null && string.IsNullOrWhiteSpace(tamanho)
            ? nome
            : $"{nome} {RotuloDoProduto(volumeLitros, tamanho)}";
}
