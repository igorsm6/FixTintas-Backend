using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Repositorio.Interfaces;

namespace FixTintas.Negocio;

// ============================================================================
// CALCULADORA DE TINTA (usada pelo app): recebe as medidas da parede, calcula os
// litros pelo rendimento do tipo de tinta e sugere, para cada linha de tinta à
// venda, a combinação de latas mais barata que cobre o necessário.
// ============================================================================
public class CalculoNegocio
{
    private readonly ITipoTintaRepositorio _tipos;
    private readonly IProdutoRepositorio _produtos;
    private readonly RegrasDaLoja _regras;

    public CalculoNegocio(ITipoTintaRepositorio tipos, IProdutoRepositorio produtos, RegrasDaLoja regras)
    {
        _tipos = tipos;
        _produtos = produtos;
        _regras = regras;
    }

    public CalculoTintaResultadoDto CalcularTinta(CalculoTintaEntradaDto entrada)
    {
        // 1) Área: altura × largura, ou a área informada.
        decimal areaBruta;
        if (entrada.AlturaM is decimal altura && entrada.LarguraM is decimal largura)
            areaBruta = altura * largura;
        else if (entrada.AreaM2 is decimal area)
            areaBruta = area;
        else
            throw new RegraDeNegocioException("Informe a altura e a largura da parede, ou a área em m².");

        areaBruta = Math.Round(areaBruta, 2, MidpointRounding.AwayFromZero);
        var descontada = Math.Round(Math.Max(0m, entrada.AreaDescontarM2), 2, MidpointRounding.AwayFromZero);

        if (areaBruta <= 0)
            throw new RegraDeNegocioException("A área da parede deve ser maior que zero.");
        if (descontada >= areaBruta)
            throw new RegraDeNegocioException("A área a descontar (portas e janelas) precisa ser menor que a área da parede.");

        var areaLiquida = areaBruta - descontada;
        if (areaLiquida > _regras.AreaMaximaM2)
            throw new RegraDeNegocioException($"A área máxima aceita no cálculo é {Formatacao.Numero(_regras.AreaMaximaM2)} m².");

        // 2) Tipo de tinta: rendimento e demãos.
        var tipo = _tipos.ObterPorId(entrada.TipoTintaId);
        if (tipo is null || tipo.Inativo)
            throw new NaoEncontradoException("Tipo de tinta não encontrado.");
        if (tipo.RendimentoM2PorLitro <= 0)
            throw new RegraDeNegocioException("Este tipo de tinta ainda não tem o rendimento cadastrado.");

        var demaos = entrada.Demaos ?? tipo.DemaosRecomendadas;
        if (demaos is < 1 or > 6)
            throw new RegraDeNegocioException("Informe de 1 a 6 demãos.");

        var litros = CalculadoraTinta.CalcularLitros(areaLiquida, demaos, tipo.RendimentoM2PorLitro, _regras.MargemPerdaPercentual);

        // 3) Latas à venda deste tipo (e da marca escolhida, se houver), com estoque.
        var latas = _produtos.ListarLatasDoTipo(tipo.Id, entrada.MarcaId is > 0 ? entrada.MarcaId : null);

        // Cada LINHA de tinta (mesmo nome, marca e categoria) é uma opção: não mistura
        // latas de linhas diferentes (por exemplo, uma de interior com outra de fachada).
        var opcoes = new List<OpcaoCompraDto>();
        foreach (var linha in latas.GroupBy(p => (p.MarcaId, p.CategoriaId, Nome: p.Nome.Trim().ToUpperInvariant())))
        {
            var produtos = linha.ToList();
            var combinacao = CalculadoraTinta.MelhorCombinacao(
                produtos.Select(p => new CalculadoraTinta.Lata(p.Id, p.VolumeLitros ?? 0m, p.Preco, p.Estoque)).ToList(),
                litros);
            if (combinacao is null) continue; // o estoque desta linha não chega

            var porId = produtos.ToDictionary(p => p.Id);
            var itens = combinacao.Select(c =>
            {
                var p = porId[c.Lata.ProdutoId];
                return new ItemOpcaoDto
                {
                    ProdutoId = p.Id,
                    Sku = p.Sku,
                    Descricao = Formatacao.DescricaoDoProduto(p.Nome, p.VolumeLitros, null),
                    VolumeLitros = p.VolumeLitros ?? 0m,
                    Quantidade = c.Quantidade,
                    PrecoUnitario = p.Preco,
                    Subtotal = p.Preco * c.Quantidade
                };
            }).ToList();

            var comprados = itens.Sum(i => i.VolumeLitros * i.Quantidade);
            var primeiro = produtos[0];
            opcoes.Add(new OpcaoCompraDto
            {
                MarcaId = primeiro.MarcaId ?? 0,
                MarcaNome = NomeDaOpcao(primeiro.Marca?.Nome ?? string.Empty, primeiro.Nome),
                Itens = itens,
                LitrosComprados = comprados,
                SobraLitros = comprados - litros,
                Total = itens.Sum(i => i.Subtotal)
            });
        }

        opcoes = opcoes.OrderBy(o => o.Total).ThenBy(o => o.SobraLitros).ToList();

        return new CalculoTintaResultadoDto
        {
            AreaBrutaM2 = areaBruta,
            AreaDescontadaM2 = descontada,
            AreaLiquidaM2 = areaLiquida,
            Demaos = demaos,
            RendimentoM2PorLitro = tipo.RendimentoM2PorLitro,
            MargemPerdaPercentual = _regras.MargemPerdaPercentual,
            LitrosNecessarios = litros,
            TipoTintaId = tipo.Id,
            TipoTintaNome = $"{tipo.Nome} {tipo.Acabamento}",
            Aviso = opcoes.Count == 0 ? "Nenhuma marca tem estoque suficiente deste tipo de tinta no momento." : null,
            Opcoes = opcoes
        };
    }

    // "Suvinil Acrílica Fosco" já tem a marca no nome; "Tintas Acrílicas Básico" vira
    // "FixTintas · Tintas Acrílicas Básico".
    private static string NomeDaOpcao(string marca, string linha) =>
        marca.Length == 0 || linha.Contains(marca, StringComparison.OrdinalIgnoreCase) ? linha : $"{marca} · {linha}";
}
