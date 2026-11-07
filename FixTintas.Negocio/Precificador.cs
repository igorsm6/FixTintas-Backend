using FixTintas.Modelos;

namespace FixTintas.Negocio;

// Resultado das contas de um carrinho.
public record ResultadoPreco(decimal Subtotal, decimal DescontoVolume, decimal DescontoCupom, decimal Total);

// ============================================================================
// PRECIFICADOR — as contas de preço, desconto e cupom. Sem banco: só números.
//
// Ordem: subtotal -> desconto por volume -> cupom sobre o que sobrou -> total.
// O desconto por volume e o cupom acumulam.
//   Ex.: subtotal R$ 600,00 -> volume 5% = R$ 30,00 -> sobra R$ 570,00
//        cupom BEMVINDO10 (10%) = R$ 57,00 -> total R$ 513,00
// ============================================================================
public static class Precificador
{
    public static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    public static decimal DescontoVolume(decimal subtotal, RegrasDaLoja regras)
    {
        if (regras.DescontoVolumePercentual <= 0 || subtotal < regras.DescontoVolumeAcimaDe) return 0m;
        return Arredondar(subtotal * regras.DescontoVolumePercentual / 100m);
    }

    // Devolve a mensagem de erro se o cupom não puder ser usado, ou null se estiver tudo certo.
    // "ValidoAte" é o último dia de validade: o cupom vale até 23h59 desse dia.
    public static string? ValidarCupom(Cupom cupom, decimal baseCalculo, DateTime agora)
    {
        if (cupom.Inativo) return "Este cupom não está mais ativo.";
        if (cupom.ValidoAte is DateTime validade && agora >= validade.Date.AddDays(1)) return "Este cupom expirou.";
        if (cupom.UsosMaximos is int maximo && cupom.UsosRealizados >= maximo) return "Este cupom esgotou.";
        if (baseCalculo < cupom.ValorMinimoPedido)
            return $"Este cupom vale para pedidos a partir de {Formatacao.Moeda(cupom.ValorMinimoPedido)}.";
        return null;
    }

    public static decimal DescontoCupom(Cupom cupom, decimal baseCalculo) => cupom.Tipo switch
    {
        TipoDesconto.Percentual => Arredondar(baseCalculo * Math.Min(cupom.Valor, 100m) / 100m),
        TipoDesconto.ValorFixo => Math.Min(cupom.Valor, baseCalculo),
        _ => 0m
    };

    // O cupom precisa ter sido validado antes (veja ValidarCupom).
    public static ResultadoPreco Calcular(decimal subtotal, RegrasDaLoja regras, Cupom? cupom)
    {
        subtotal = Arredondar(subtotal);
        var volume = DescontoVolume(subtotal, regras);
        var baseCupom = subtotal - volume;
        var desconto = cupom is null ? 0m : DescontoCupom(cupom, baseCupom);
        var total = Math.Max(0m, baseCupom - desconto);
        return new ResultadoPreco(subtotal, volume, desconto, Arredondar(total));
    }
}
