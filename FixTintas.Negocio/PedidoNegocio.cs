using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Repositorio;
using FixTintas.Repositorio.Interfaces;

namespace FixTintas.Negocio;

// ============================================================================
// Tudo o que envolve uma compra, no site e no app:
//   - conferir o carrinho (produto à venda, cor escolhida, estoque, cupom);
//   - calcular subtotal, descontos e total (Precificador);
//   - gravar o pedido (o estoque baixa sozinho no banco, pela trigger);
//   - cancelar e mudar o status (RegrasPedido).
//
// O PREÇO NUNCA VEM DO CLIENTE: o carrinho só tem produto, cor e quantidade.
// Os valores são sempre lidos do banco aqui.
// ============================================================================
public class PedidoNegocio
{
    private readonly IPedidoRepositorio _repositorio;
    private readonly IProdutoRepositorio _produtos;
    private readonly ICorRepositorio _cores;
    private readonly ICupomRepositorio _cupons;
    private readonly RegrasDaLoja _regras;

    // Uma linha do carrinho já conferida.
    private sealed record Linha(Produto Produto, Cor? Cor, int Quantidade);

    // O carrinho inteiro conferido, com os valores calculados.
    private sealed record Cotacao(List<Linha> Linhas, ResultadoPreco Preco, Cupom? Cupom);

    public PedidoNegocio(IPedidoRepositorio repositorio, IProdutoRepositorio produtos, ICorRepositorio cores,
                         ICupomRepositorio cupons, RegrasDaLoja regras)
    {
        _repositorio = repositorio;
        _produtos = produtos;
        _cores = cores;
        _cupons = cupons;
        _regras = regras;
    }

    // ================================================================ CARRINHO

    // "Quanto custa?": confere o carrinho e devolve preços e descontos. Não grava nada.
    public OrcamentoDto Orcar(CarrinhoDto carrinho) => ParaOrcamento(Cotar(carrinho));

    // Acrescenta um item (o mesmo produto na mesma cor soma a quantidade) e confere tudo.
    // Devolve o carrinho novo; o carrinho recebido não é alterado.
    public CarrinhoDto AdicionarItem(CarrinhoDto carrinho, ItemCarrinhoDto item)
    {
        if (item.Quantidade <= 0)
            throw new RegraDeNegocioException("A quantidade deve ser pelo menos 1.");

        var novo = Copiar(carrinho);
        var existente = Encontrar(novo, item.ProdutoId, item.CorId);
        if (existente is null)
            novo.Itens.Add(new ItemCarrinhoDto { ProdutoId = item.ProdutoId, CorId = NormalizarCor(item.CorId), Quantidade = item.Quantidade });
        else
            existente.Quantidade += item.Quantidade;

        ConferirItens(novo);
        return novo;
    }

    // Muda a quantidade de um item (0 = tirar do carrinho).
    public CarrinhoDto AlterarQuantidade(CarrinhoDto carrinho, int produtoId, int? corId, int quantidade)
    {
        var novo = Copiar(carrinho);
        var item = Encontrar(novo, produtoId, corId)
            ?? throw new NaoEncontradoException("Este item não está no carrinho.");

        if (quantidade <= 0)
        {
            novo.Itens.Remove(item);
            return novo;
        }

        item.Quantidade = quantidade;
        ConferirItens(novo);
        return novo;
    }

    public CarrinhoDto RemoverItem(CarrinhoDto carrinho, int produtoId, int? corId)
    {
        var novo = Copiar(carrinho);
        var item = Encontrar(novo, produtoId, corId);
        if (item is not null) novo.Itens.Remove(item);
        if (novo.Itens.Count == 0) novo.Cupom = null;
        return novo;
    }

    // Confere o cupom com o carrinho atual (lança a exceção com o motivo, se não valer).
    public CarrinhoDto AplicarCupom(CarrinhoDto carrinho, string? codigo)
    {
        var novo = Copiar(carrinho);
        novo.Cupom = Validacoes.TextoOpcional(codigo)?.ToUpperInvariant()
            ?? throw new RegraDeNegocioException("Digite o código do cupom.");

        Cotar(novo);
        return novo;
    }

    // Arruma um carrinho guardado (o site guarda o carrinho na sessão do navegador):
    // tira o que não está mais à venda, ajusta a quantidade ao estoque e solta o cupom
    // que deixou de valer. Os avisos explicam ao cliente o que mudou.
    public CarrinhoDto Sanear(CarrinhoDto carrinho, List<string> avisos)
    {
        var limpo = new CarrinhoDto { Cupom = carrinho.Cupom };
        if (carrinho.Itens.Count == 0)
        {
            limpo.Cupom = null;
            return limpo;
        }

        var produtos = _produtos.ObterPorIds(carrinho.Itens.Select(i => i.ProdutoId)).ToDictionary(p => p.Id);
        var cores = _cores.ObterPorIds(carrinho.Itens.Where(i => i.CorId is > 0).Select(i => i.CorId!.Value)).ToDictionary(c => c.Id);

        // 1) Tira o que não pode mais ser vendido.
        var validos = new List<(Produto Produto, int? CorId, int Quantidade)>();
        foreach (var item in carrinho.Itens)
        {
            if (!produtos.TryGetValue(item.ProdutoId, out var produto) || !ProdutoNegocio.EstaAVenda(produto))
            {
                avisos.Add("Um produto do seu carrinho não está mais à venda e foi retirado.");
                continue;
            }

            int? corId = null;
            if (produto.EhTinta)
            {
                if (item.CorId is not int idCor || !cores.TryGetValue(idCor, out var cor) || cor.Inativa)
                {
                    avisos.Add($"A cor escolhida para {Descricao(produto)} não está mais disponível; o item foi retirado.");
                    continue;
                }
                corId = idCor;
            }

            if (item.Quantidade > 0) validos.Add((produto, corId, item.Quantidade));
        }

        // 2) Junta repetidos e ajusta ao estoque (o estoque é do produto, somando as cores).
        var usadoPorProduto = new Dictionary<int, int>();
        foreach (var grupo in validos.GroupBy(v => (v.Produto.Id, v.CorId)))
        {
            var produto = grupo.First().Produto;
            var pedida = Math.Min(grupo.Sum(v => v.Quantidade), _regras.QuantidadeMaximaPorItem);
            var usado = usadoPorProduto.GetValueOrDefault(produto.Id);
            var quantidade = Math.Min(pedida, Math.Max(0, produto.Estoque - usado));

            if (quantidade <= 0)
            {
                avisos.Add($"{Descricao(produto)} esgotou e foi retirado do carrinho.");
                continue;
            }
            if (quantidade < pedida)
                avisos.Add($"Só temos {quantidade} unidade(s) de {Descricao(produto)}; a quantidade foi ajustada.");

            usadoPorProduto[produto.Id] = usado + quantidade;
            limpo.Itens.Add(new ItemCarrinhoDto { ProdutoId = produto.Id, CorId = grupo.Key.CorId, Quantidade = quantidade });
        }

        // 3) O cupom ainda vale para o carrinho que sobrou?
        if (limpo.Itens.Count == 0)
        {
            limpo.Cupom = null;
        }
        else if (limpo.Cupom is not null)
        {
            try
            {
                Cotar(limpo);
            }
            catch (NegocioException ex)
            {
                avisos.Add($"O cupom {limpo.Cupom} foi retirado: {ex.Message}");
                limpo.Cupom = null;
            }
        }

        return limpo;
    }

    // ================================================================= PEDIDOS

    // Fecha a compra: confere tudo de novo, grava o pedido e conta o uso do cupom.
    public PedidoDto Criar(int usuarioId, CarrinhoDto carrinho)
    {
        var cotacao = Cotar(carrinho);

        var pedido = new Pedido
        {
            UsuarioId = usuarioId,
            Status = StatusPedido.Pendente,
            Subtotal = cotacao.Preco.Subtotal,
            DescontoVolume = cotacao.Preco.DescontoVolume,
            DescontoCupom = cotacao.Preco.DescontoCupom,
            Total = cotacao.Preco.Total,
            CupomId = cotacao.Cupom?.Id,
            CupomCodigo = cotacao.Cupom?.Codigo,
            // "Foto" de cada item no momento da compra (se o produto mudar, o pedido não muda).
            Itens = cotacao.Linhas.Select(l => new PedidoItem
            {
                ProdutoId = l.Produto.Id,
                CorId = l.Cor?.Id,
                Quantidade = l.Quantidade,
                PrecoUnitario = l.Produto.Preco,
                ProdutoDescricao = Descricao(l.Produto),
                VolumeLitros = l.Produto.VolumeLitros,
                CorNome = l.Cor?.Nome,
                CorHex = l.Cor?.CodigoHex
            }).ToList()
        };

        // O estoque NÃO é baixado aqui: a trigger TR_PedidoItens_BaixaEstoque faz isso no banco.
        // O cupom foi lido por este mesmo contexto, então o +1 é gravado junto com o pedido.
        if (cotacao.Cupom is not null) cotacao.Cupom.UsosRealizados++;

        try
        {
            _repositorio.Adicionar(pedido);
        }
        catch (ConflitoDeGravacaoException)
        {
            // Outro cliente levou o estoque (ou o último uso do cupom) no mesmo instante.
            throw new ConflitoException("O estoque ou o cupom mudou enquanto o pedido era feito. Confira o carrinho e tente de novo.");
        }

        return ParaDto(pedido);
    }

    public List<PedidoResumoDto> ListarDoCliente(int usuarioId) =>
        _repositorio.ListarDoUsuario(usuarioId).Select(PedidoResumoDto.De).ToList();

    // Um cliente só enxerga os próprios pedidos (o de outro cliente é "não encontrado").
    public PedidoDto ObterDoCliente(int pedidoId, int usuarioId)
    {
        var pedido = _repositorio.ObterPorId(pedidoId);
        if (pedido is null || pedido.UsuarioId != usuarioId)
            throw new NaoEncontradoException("Pedido não encontrado.");
        return ParaDto(pedido);
    }

    // O cliente cancela enquanto o pedido está Pendente. O estoque e o uso do cupom voltam
    // (stored procedure sp_Pedidos_Cancelar).
    public PedidoDto CancelarPeloCliente(int pedidoId, int usuarioId)
    {
        var pedido = ObterDoCliente(pedidoId, usuarioId);

        if (!pedido.PodeCancelar)
            throw new ConflitoException("Este pedido não pode mais ser cancelado por aqui. Fale com a loja.");

        if (!_repositorio.Cancelar(pedidoId))
            throw new ConflitoException("O pedido mudou enquanto era cancelado. Atualize e tente de novo.");

        return ObterDoCliente(pedidoId, usuarioId);
    }

    // ------------------------------------------------------------------ equipe

    // status vazio = todos; status desconhecido ("Inventado") = erro, para não parecer que filtrou.
    public List<PedidoResumoDto> Listar(string? status = null)
    {
        StatusPedido? filtro = null;
        if (!string.IsNullOrWhiteSpace(status))
            filtro = RegrasPedido.Ler(status)
                ?? throw new RegraDeNegocioException("Status inválido. Use Pendente, Pago, Enviado, Entregue ou Cancelado.");

        return _repositorio.Listar(filtro).Select(PedidoResumoDto.De).ToList();
    }

    public PedidoDto Obter(int id) =>
        ParaDto(_repositorio.ObterPorId(id) ?? throw new NaoEncontradoException("Pedido não encontrado."));

    public Dictionary<StatusPedido, int> ContarPorStatus() => _repositorio.ContarPorStatus();

    // Painel: Pendente -> Pago -> Enviado -> Entregue (ou Cancelado no caminho).
    public PedidoDto AlterarStatus(int id, string? novoStatus)
    {
        var novo = RegrasPedido.Ler(novoStatus)
            ?? throw new RegraDeNegocioException("Status inválido. Use Pendente, Pago, Enviado, Entregue ou Cancelado.");

        var pedido = _repositorio.ObterPorId(id)
            ?? throw new NaoEncontradoException("Pedido não encontrado.");

        if (pedido.Status == novo) return ParaDto(pedido);

        if (!RegrasPedido.TransicaoValida(pedido.Status, novo))
            throw new RegraDeNegocioException($"Um pedido \"{pedido.Status}\" não pode passar para \"{novo}\".");

        // Cancelar devolve o estoque e o cupom (procedure); os outros status só mudam o texto.
        var alterou = novo == StatusPedido.Cancelado
            ? _repositorio.Cancelar(id)
            : _repositorio.AtualizarStatus(id, pedido.Status, novo);

        if (!alterou)
            throw new ConflitoException("O pedido foi alterado por outra pessoa. Atualize a página e tente de novo.");

        return Obter(id);
    }

    // ============================================================== CONVERSÕES

    // Pedido -> DTO, com as regras que as telas precisam (pode cancelar? próximos status?).
    public static PedidoDto ParaDto(Pedido pedido) => new()
    {
        Id = pedido.Id,
        CriadoEm = pedido.CriadoEm,
        AtualizadoEm = pedido.AtualizadoEm,
        Status = pedido.Status.ToString(),
        Itens = pedido.Itens.OrderBy(i => i.Id).Select(LinhaPedidoDto.De).ToList(),
        Subtotal = pedido.Subtotal,
        DescontoVolume = pedido.DescontoVolume,
        DescontoCupom = pedido.DescontoCupom,
        Total = pedido.Total,
        Cupom = pedido.CupomCodigo,
        PodeCancelar = RegrasPedido.ClientePodeCancelar(pedido.Status),
        ClienteId = pedido.UsuarioId,
        ClienteNome = pedido.Usuario?.Nome,
        ClienteEmail = pedido.Usuario?.Email,
        ProximosStatus = RegrasPedido.Proximos(pedido.Status).Select(s => s.ToString()).ToList()
    };

    // ================================================================= REGRAS

    // Confere o carrinho inteiro e calcula os valores. Lança a exceção com o motivo.
    private Cotacao Cotar(CarrinhoDto carrinho)
    {
        var itens = carrinho.Itens ?? new List<ItemCarrinhoDto>();
        if (itens.Count == 0)
            throw new RegraDeNegocioException("O carrinho está vazio.");
        if (itens.Count > 50)
            throw new RegraDeNegocioException("O carrinho aceita até 50 itens diferentes.");
        if (itens.Any(i => i.Quantidade <= 0))
            throw new RegraDeNegocioException("A quantidade de cada item deve ser pelo menos 1.");

        var produtos = _produtos.ObterPorIds(itens.Select(i => i.ProdutoId)).ToDictionary(p => p.Id);
        var cores = _cores.ObterPorIds(itens.Where(i => i.CorId is > 0).Select(i => i.CorId!.Value)).ToDictionary(c => c.Id);

        // Cada item: o produto está à venda? se é tinta, a cor foi escolhida e existe?
        var conferidos = new List<Linha>();
        foreach (var item in itens)
        {
            if (!produtos.TryGetValue(item.ProdutoId, out var produto) || !ProdutoNegocio.EstaAVenda(produto))
                throw new RegraDeNegocioException("Um dos produtos do carrinho não está mais disponível.");

            Cor? cor = null;
            if (produto.EhTinta)
            {
                if (item.CorId is not int corId || corId <= 0)
                    throw new RegraDeNegocioException($"Escolha a cor de {Descricao(produto)}.");

                if (!cores.TryGetValue(corId, out cor) || cor.Inativa)
                    throw new RegraDeNegocioException("Uma das cores do carrinho não está mais disponível.");
            }

            conferidos.Add(new Linha(produto, cor, item.Quantidade));
        }

        // O mesmo produto na mesma cor, repetido, vira uma linha só.
        var linhas = conferidos
            .GroupBy(l => (l.Produto.Id, CorId: l.Cor?.Id ?? 0))
            .Select(g => new Linha(g.First().Produto, g.First().Cor, g.Sum(l => l.Quantidade)))
            .ToList();

        if (linhas.Any(l => l.Quantidade > _regras.QuantidadeMaximaPorItem))
            throw new RegraDeNegocioException($"A quantidade máxima por item é {_regras.QuantidadeMaximaPorItem}.");

        // O estoque é do produto: soma as quantidades de todas as cores do mesmo produto.
        foreach (var grupo in linhas.GroupBy(l => l.Produto.Id))
        {
            var produto = grupo.First().Produto;
            var pedida = grupo.Sum(l => l.Quantidade);
            if (pedida > produto.Estoque)
            {
                throw new ConflitoException(produto.Estoque <= 0
                    ? $"{Descricao(produto)} está sem estoque."
                    : $"Estoque insuficiente de {Descricao(produto)}: restam {produto.Estoque}.");
            }
        }

        var subtotal = Precificador.Arredondar(linhas.Sum(l => l.Produto.Preco * l.Quantidade));

        Cupom? cupom = null;
        var codigo = Validacoes.TextoOpcional(carrinho.Cupom)?.ToUpperInvariant();
        if (codigo is not null)
        {
            cupom = _cupons.ObterPorCodigo(codigo)
                ?? throw new RegraDeNegocioException("Cupom não encontrado.");

            var baseCupom = subtotal - Precificador.DescontoVolume(subtotal, _regras);
            var erro = Precificador.ValidarCupom(cupom, baseCupom, DateTime.Now);
            if (erro is not null) throw new RegraDeNegocioException(erro);
        }

        return new Cotacao(linhas, Precificador.Calcular(subtotal, _regras, cupom), cupom);
    }

    // Confere só os itens (sem o cupom): usado ao montar o carrinho.
    private void ConferirItens(CarrinhoDto carrinho) =>
        Cotar(new CarrinhoDto { Itens = carrinho.Itens });

    private static OrcamentoDto ParaOrcamento(Cotacao cotacao) => new()
    {
        Itens = cotacao.Linhas.Select(l => new LinhaPedidoDto
        {
            ProdutoId = l.Produto.Id,
            ProdutoDescricao = Descricao(l.Produto),
            VolumeLitros = l.Produto.VolumeLitros ?? 0m,
            CorId = l.Cor?.Id ?? 0,
            CorNome = l.Cor?.Nome ?? string.Empty,
            CorHex = l.Cor?.CodigoHex ?? string.Empty,
            Quantidade = l.Quantidade,
            PrecoUnitario = l.Produto.Preco,
            Total = l.Produto.Preco * l.Quantidade,
            ImagemUrl = l.Produto.ImagemUrl,
            EhTinta = l.Produto.EhTinta
        }).ToList(),
        LitrosTotais = cotacao.Linhas.Sum(l => (l.Produto.VolumeLitros ?? 0m) * l.Quantidade),
        Subtotal = cotacao.Preco.Subtotal,
        DescontoVolume = cotacao.Preco.DescontoVolume,
        DescontoCupom = cotacao.Preco.DescontoCupom,
        Total = cotacao.Preco.Total,
        Cupom = cotacao.Cupom?.Codigo,
        QuantidadeItens = cotacao.Linhas.Sum(l => l.Quantidade)
    };

    // "Tintas Acrílicas Premium 3,6 L", "Rolo de Pintura 6\"", "Lixa Grossa"
    private static string Descricao(Produto produto) =>
        Formatacao.DescricaoDoProduto(produto.Nome, produto.VolumeLitros, produto.Tamanho);

    private static int? NormalizarCor(int? corId) => corId is > 0 ? corId : null;

    private static ItemCarrinhoDto? Encontrar(CarrinhoDto carrinho, int produtoId, int? corId)
    {
        var cor = NormalizarCor(corId);
        return carrinho.Itens.FirstOrDefault(i => i.ProdutoId == produtoId && NormalizarCor(i.CorId) == cor);
    }

    private static CarrinhoDto Copiar(CarrinhoDto carrinho) => new()
    {
        Cupom = carrinho.Cupom,
        Itens = (carrinho.Itens ?? new List<ItemCarrinhoDto>())
            .Select(i => new ItemCarrinhoDto { ProdutoId = i.ProdutoId, CorId = NormalizarCor(i.CorId), Quantidade = i.Quantidade })
            .ToList()
    };
}
