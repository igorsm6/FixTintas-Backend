using System.Text.Json;
using FixTintas.Modelos.Dtos;
using FixTintas.Negocio;

namespace FixTintas.Web.Infra;

// O carrinho conferido e com os valores calculados (o que as telas mostram).
public class ResumoCarrinho
{
    public OrcamentoDto? Orcamento { get; init; }
    public List<string> Avisos { get; init; } = new();

    public bool Vazio => Orcamento is null || Orcamento.Itens.Count == 0;
    public int QuantidadeItens => Orcamento?.QuantidadeItens ?? 0;
}

// ============================================================================
// CARRINHO DO SITE: fica na SESSÃO do navegador (no servidor), com o mesmo
// formato que o app manda para a Api (CarrinhoDto: produto, cor e quantidade).
// Preço não é guardado aqui: os valores são sempre calculados pelo PedidoNegocio.
//
// AddScoped no Program.cs: um objeto por requisição. O resumo é calculado uma
// vez só por página, mesmo que o cabeçalho e a página peçam os dois.
// ============================================================================
public class CarrinhoDaSessao
{
    private const string Chave = "carrinho";

    private readonly IHttpContextAccessor _http;
    private readonly PedidoNegocio _pedidos;
    private ResumoCarrinho? _resumo;

    public CarrinhoDaSessao(IHttpContextAccessor http, PedidoNegocio pedidos)
    {
        _http = http;
        _pedidos = pedidos;
    }

    private ISession Sessao => _http.HttpContext?.Session
        ?? throw new InvalidOperationException("Sessão indisponível (falta app.UseSession() no Program.cs?).");

    public CarrinhoDto Ler()
    {
        var json = Sessao.GetString(Chave);
        if (string.IsNullOrEmpty(json)) return new CarrinhoDto();

        try
        {
            return JsonSerializer.Deserialize<CarrinhoDto>(json) ?? new CarrinhoDto();
        }
        catch (JsonException)
        {
            return new CarrinhoDto();
        }
    }

    public void Gravar(CarrinhoDto carrinho)
    {
        Sessao.SetString(Chave, JsonSerializer.Serialize(carrinho));
        _resumo = null;
    }

    public void Limpar()
    {
        Sessao.Remove(Chave);
        _resumo = null;
    }

    // Confere o carrinho guardado (tira o que saiu de linha, ajusta ao estoque) e calcula os valores.
    public ResumoCarrinho Resumo()
    {
        if (_resumo is not null) return _resumo;

        var carrinho = Ler();
        var avisos = new List<string>();
        OrcamentoDto? orcamento = null;

        if (carrinho.Itens.Count > 0)
        {
            var conferido = _pedidos.Sanear(carrinho, avisos);
            if (avisos.Count > 0) Gravar(conferido);

            if (conferido.Itens.Count > 0)
            {
                try
                {
                    orcamento = _pedidos.Orcar(conferido);
                }
                catch (NegocioException ex)
                {
                    avisos.Add(ex.Message);
                }
            }
        }

        _resumo = new ResumoCarrinho { Orcamento = orcamento, Avisos = avisos };
        return _resumo;
    }
}
