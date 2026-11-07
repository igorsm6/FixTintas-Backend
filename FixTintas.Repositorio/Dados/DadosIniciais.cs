using FixTintas.Modelos;
using FixTintas.Repositorio.Contexto;
using Microsoft.EntityFrameworkCore;

namespace FixTintas.Repositorio.Dados;

// ============================================================================
// DADOS INICIAIS: o catálogo com que o banco nasce, para o site e o app já
// terem o que mostrar. Junta o que antes estava escrito à mão no site
// (script.js e as ofertas da index.html) com o catálogo do app Flutter
// (cores, tipos de tinta e cupons).
//
// Roda na subida da Web e da Api, mas só faz alguma coisa com o banco VAZIO
// (sem nenhuma categoria). Depois disso, tudo é mantido pelo painel.
// Os preços, estoques e cupons são exemplos; mude à vontade pelo painel.
// ============================================================================
public static class DadosIniciais
{
    public static void Popular(AppDbContext db)
    {
        using var transacao = db.Database.BeginTransaction();

        // Trava do SQL Server: se a Web e a Api subirem juntas, só uma popula;
        // a outra espera aqui e, quando continuar, encontra tudo pronto.
        db.Database.ExecuteSqlRaw(
            "EXEC sp_getapplock @Resource = N'FixTintas_DadosIniciais', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 60000;");

        if (db.Categorias.Any())
        {
            transacao.Commit();
            return;
        }

        // ---------------------------------------------------------------- marcas
        var suvinil = new Marca { Nome = "Suvinil", Descricao = "Tintas imobiliárias de alta cobertura." };
        var coral = new Marca { Nome = "Coral", Descricao = "Linha completa para interiores e exteriores." };
        var lukscolor = new Marca { Nome = "Lukscolor", Descricao = "Qualidade com ótimo rendimento." };
        var eucatex = new Marca { Nome = "Eucatex", Descricao = "Tintas e vernizes para todos os ambientes." };
        var fixtintas = new Marca { Nome = "FixTintas", Descricao = "Linha própria da loja." };
        db.Marcas.AddRange(suvinil, coral, lukscolor, eucatex, fixtintas);

        // ---------------------------------------------------------- tipos de tinta
        // Rendimento (m² por litro, em uma demão), demãos recomendadas e o fator de
        // contraste usado pelo simulador do app (fosco 0,80 · acetinado 0,95 · brilhante 1,10).
        var acrilicaFosco = Tipo("Acrílica", Acabamentos.Fosco, "Para paredes internas e externas. Sem brilho, disfarça pequenas imperfeições.", 10m, 2);
        var acrilicaAcetinado = Tipo("Acrílica", Acabamentos.Acetinado, "Leve brilho e toque macio. Fácil de limpar.", 10m, 2);
        var latexPva = Tipo("Látex PVA", Acabamentos.Fosco, "Opção econômica para paredes internas.", 8m, 3);
        var esmalteBrilhante = Tipo("Esmalte Sintético", Acabamentos.Brilhante, "Para madeira e metal. Acabamento brilhante e resistente.", 11m, 2);
        var esmalteAcetinado = Tipo("Esmalte Sintético", Acabamentos.Acetinado, "Para portas e móveis, com brilho suave.", 11m, 2);
        var verniz = Tipo("Verniz", Acabamentos.Brilhante, "Protege e realça os veios da madeira.", 12m, 3);
        var piso = Tipo("Tinta para Piso", Acabamentos.Fosco, "Alta resistência ao tráfego de pessoas e carros.", 8m, 2);
        var antimofo = Tipo("Antimofo", Acabamentos.Fosco, "Protege ambientes úmidos contra mofo e bolor.", 9m, 2);
        var antiferrugem = Tipo("Antiferrugem", Acabamentos.Brilhante, "Protege metais contra a corrosão.", 9m, 2);
        var spray = Tipo("Tinta Spray", Acabamentos.Brilhante, "Secagem rápida para pequenos retoques e artesanato.", 3m, 2);
        db.TiposTinta.AddRange(acrilicaFosco, acrilicaAcetinado, latexPva, esmalteBrilhante, esmalteAcetinado,
                               verniz, piso, antimofo, antiferrugem, spray);

        // ------------------------------------------------------------------ cores
        // As cores do app Flutter, agrupadas por família, mais o preto.
        db.Cores.AddRange(
            Cor("Branco Neve", "#F8F8F5", "Branco"),
            Cor("Gelo", "#E9EDEF", "Branco"),
            Cor("Pérola", "#EAE0C8", "Branco"),
            Cor("Areia", "#E3D5B8", "Neutro"),
            Cor("Bege Camurça", "#D2B48C", "Neutro"),
            Cor("Marrom Café", "#5C4033", "Neutro"),
            Cor("Cinza Concreto", "#9A9A9A", "Cinza"),
            Cor("Cinza Chumbo", "#4A4F54", "Cinza"),
            Cor("Grafite", "#2F3337", "Cinza"),
            Cor("Preto", "#1C1C1C", "Cinza"),
            Cor("Azul Céu", "#A8D4F0", "Azul"),
            Cor("Azul Oceano", "#1F5F8B", "Azul"),
            Cor("Azul Petróleo", "#1B4D5C", "Azul"),
            Cor("Verde Menta", "#B8E0CE", "Verde"),
            Cor("Verde Oliva", "#6B7B3A", "Verde"),
            Cor("Verde Floresta", "#2E5E3E", "Verde"),
            Cor("Amarelo Sol", "#F5D547", "Amarelo"),
            Cor("Mostarda", "#D4A017", "Amarelo"),
            Cor("Laranja Terra", "#D9772B", "Laranja"),
            Cor("Terracota", "#B5543A", "Laranja"),
            Cor("Vermelho Cereja", "#A8232F", "Vermelho"),
            Cor("Rosa Antigo", "#D8A7A7", "Rosa"),
            Cor("Lilás Suave", "#C9B6E0", "Roxo"),
            Cor("Uva", "#5B3A74", "Roxo"));

        // ------------------------------------------------------------- categorias
        // As mesmas do menu lateral do site. A antiga categoria "Marcas" virou
        // filtro (as marcas agora são uma tabela).
        var ordem = 0;
        Categoria Principal(string nome, string slug) => new() { Nome = nome, Slug = slug, Ordem = ++ordem };

        var interior = Principal("Interior", "interior");
        var exterior = Principal("Exterior", "exterior");
        var moveis = Principal("Móveis", "moveis");
        var especial = Principal("Especial", "especial");
        var ferramentas = Principal("Ferramentas", "ferramentas");
        var acessorios = Principal("Acessórios", "acessorios");

        var intPlasticas = Sub(interior, "Tintas Plásticas", "tintas-plasticas");
        var intAcrilicas = Sub(interior, "Tintas Acrílicas", "tintas-acrilicas");
        var intMassas = Sub(interior, "Massas Corridas", "massas-corridas");
        var intLatex = Sub(interior, "Látex", "latex");

        var extAcrilicas = Sub(exterior, "Tintas Acrílicas", "tintas-acrilicas");
        var extVernizes = Sub(exterior, "Vernizes", "vernizes");
        var extPiso = Sub(exterior, "Tintas para Piso", "tintas-piso");
        var extLatex = Sub(exterior, "Látex", "latex");

        var movEsmaltes = Sub(moveis, "Esmaltes", "esmaltes");
        var movVernizes = Sub(moveis, "Vernizes", "vernizes");
        var movSpray = Sub(moveis, "Tintas Spray", "tintas-spray");

        var espAntimofo = Sub(especial, "Antimofo", "antimofo");
        var espAntiferrugem = Sub(especial, "Antiferrugem", "antiferrugem");
        var espMadeira = Sub(especial, "Tintas para Madeira", "tintas-madeira");

        var ferRolos = Sub(ferramentas, "Rolos", "rolos");
        var ferPinceis = Sub(ferramentas, "Pincéis", "pinceis");
        var ferEscadas = Sub(ferramentas, "Escadas", "escadas");
        var ferBandejas = Sub(ferramentas, "Bandejas", "bandejas");

        var aceLixas = Sub(acessorios, "Lixas", "lixas");
        var aceFitas = Sub(acessorios, "Fitas Crepe", "fitas-crepe");
        var acePlasticos = Sub(acessorios, "Plásticos de Proteção", "plasticos-protecao");
        var aceLuvas = Sub(acessorios, "Luvas", "luvas");
        var aceMascaras = Sub(acessorios, "Máscaras", "mascaras");

        db.Categorias.AddRange(interior, exterior, moveis, especial, ferramentas, acessorios);

        // --------------------------------------------------------------- produtos
        var produtos = new List<Produto>();

        // Linhas de tinta do site: cada uma vira as latas de 900 ml, 3,6 L e 18 L.
        // O preço do site é o do galão de 3,6 L; as outras latas são proporcionais.
        produtos.AddRange(Linha("Tintas Plásticas Básico", intPlasticas, latexPva, fixtintas, "Pacote essencial para tintas plásticas.", 49.90m));
        produtos.AddRange(Linha("Tintas Plásticas Premium", intPlasticas, latexPva, fixtintas, "Solução completa para acabamento superior.", 79.90m));
        produtos.AddRange(Linha("Tintas Acrílicas Básico", intAcrilicas, acrilicaFosco, fixtintas, "Tinta acrílica para interiores.", 49.90m));
        produtos.AddRange(Linha("Tintas Acrílicas Premium", intAcrilicas, acrilicaAcetinado, fixtintas, "Tinta acrílica premium para interiores.", 79.90m));
        produtos.AddRange(Linha("Latex Básico", intLatex, latexPva, fixtintas, "Tinta látex para interiores.", 49.90m));
        produtos.AddRange(Linha("Latex Premium", intLatex, latexPva, fixtintas, "Tinta látex premium.", 79.90m, estoque: 6));
        produtos.AddRange(Linha("Tintas Acrílicas Exterior", extAcrilicas, acrilicaFosco, fixtintas, "Tinta acrílica para fachadas.", 59.90m));
        produtos.AddRange(Linha("Vernizes Exterior", extVernizes, verniz, fixtintas, "Verniz de alta proteção.", 69.90m));
        produtos.AddRange(Linha("Tintas Piso Básico", extPiso, piso, fixtintas, "Tinta para pisos externos.", 59.90m));
        produtos.AddRange(Linha("Tintas Piso Profissional", extPiso, piso, fixtintas, "Tinta profissional para pisos.", 99.90m));
        produtos.AddRange(Linha("Esmalte Sintético", movEsmaltes, esmalteBrilhante, fixtintas, "Esmalte para móveis.", 79.90m));
        produtos.AddRange(Linha("Esmalte Premium", movEsmaltes, esmalteAcetinado, fixtintas, "Esmalte premium para móveis.", 99.90m));
        produtos.AddRange(Linha("Verniz Madeira", movVernizes, verniz, fixtintas, "Verniz protetor para madeira.", 59.90m));
        produtos.AddRange(Linha("Antimofo Premium", espAntimofo, antimofo, fixtintas, "Tinta antimofo para ambientes úmidos.", 89.90m));
        produtos.AddRange(Linha("Antiferrugem Pro", espAntiferrugem, antiferrugem, fixtintas, "Proteção contra ferrugem.", 79.90m));
        produtos.AddRange(Linha("Tinta para Madeira", espMadeira, esmalteBrilhante, fixtintas, "Tinta especial para madeira.", 69.90m, estoque: 4));

        // A antiga categoria "Marcas" do site: as mesmas tintas, agora com marca de verdade.
        produtos.AddRange(Linha("Suvinil Acrílica Fosco", intAcrilicas, acrilicaFosco, suvinil, "Tinta Suvinil de alta cobertura.", 89.90m));
        produtos.AddRange(Linha("Coral Acrílica Premium", intAcrilicas, acrilicaAcetinado, coral, "Tinta Coral premium, fácil de limpar.", 84.90m));
        produtos.AddRange(Linha("Lukscolor Acrílica Fosco", extAcrilicas, acrilicaFosco, lukscolor, "Tinta Lukscolor para fachadas.", 89.90m));
        produtos.AddRange(Linha("Eucatex Látex PVA", intLatex, latexPva, eucatex, "Tinta Eucatex econômica para interiores.", 54.90m));

        // Spray: tinta (o cliente escolhe a cor), em lata de 400 ml.
        produtos.Add(Lata("Tinta Spray Básico", movSpray, spray, fixtintas, "Spray para acabamentos rápidos.", 0.4m, 29.90m, null, 50));

        // As 4 ofertas da página inicial (index.html), com preço "De" e imagem.
        produtos.Add(Lata("Tinta Eucatex Interna", intAcrilicas, acrilicaFosco, eucatex, "Tinta de alta qualidade para interiores.",
                          18m, 249.90m, 264.99m, 14, "/img/tinta_eucatex.png"));
        produtos.Add(Lata("Tinta Suvinil Externa", extAcrilicas, acrilicaFosco, suvinil, "Tinta de alta qualidade para fachadas.",
                          3.6m, 54.90m, 69.90m, 3, "/img/tintaa.png"));
        produtos.Add(Lata("Tinta Lukscolor", extLatex, latexPva, lukscolor, "Tinta para ambientes externos.",
                          1m, 19.90m, 24.90m, 8, "/img/tinta_lukscolor.png", minimo: 10));
        produtos.Add(Lata("Tinta Coral Econômica", intAcrilicas, acrilicaFosco, coral, "Tinta acrílica linha econômica.",
                          3.6m, 49.90m, 59.90m, 20, "/img/tinta_coral_3.png"));

        // Massa corrida não leva cor: é vendida como item, com o tamanho da embalagem.
        produtos.Add(Item("Massas Corridas Básico", intMassas, fixtintas, "Massa corrida para nivelamento.", 39.90m, 30, "3,6 L"));
        produtos.Add(Item("Massas Corridas Básico", intMassas, fixtintas, "Massa corrida para nivelamento.", 129.90m, 12, "18 L"));
        produtos.Add(Item("Massas Corridas Premium", intMassas, fixtintas, "Massa corrida premium.", 69.90m, 20, "3,6 L"));
        produtos.Add(Item("Massas Corridas Premium", intMassas, fixtintas, "Massa corrida premium.", 219.90m, 8, "18 L"));

        // Ferramentas: os tamanhos que o site oferecia no modal viram opções do mesmo card.
        foreach (var tamanho in new[] { "1\"", "2\"", "6\"" })
            produtos.Add(Item("Rolo de Pintura", ferRolos, null, "Rolo para pintura de paredes.", 19.90m, 40, tamanho, minimo: 10));
        foreach (var tamanho in new[] { "3/4\"", "1\"", "2\"" })
            produtos.Add(Item("Pincel Profissional", ferPinceis, null, "Pincel de alta qualidade.", 14.90m, 60, tamanho, minimo: 10));
        produtos.Add(Item("Escada Dobrável", ferEscadas, null, "Escada dobrável de alumínio.", 129.90m, 6, "3 degraus", minimo: 3));
        produtos.Add(Item("Escada Dobrável", ferEscadas, null, "Escada dobrável de alumínio.", 169.90m, 6, "5 degraus", minimo: 3));
        produtos.Add(Item("Escada Dobrável", ferEscadas, null, "Escada dobrável de alumínio.", 199.90m, 5, "7 degraus", minimo: 3));
        produtos.Add(Item("Escada Dobrável", ferEscadas, null, "Escada dobrável de alumínio.", 289.90m, 2, "10 degraus", minimo: 3));
        produtos.Add(Item("Bandeja para Tinta", ferBandejas, null, "Bandeja para aplicação de tinta.", 12.90m, 45, null, minimo: 10));

        // Acessórios.
        produtos.Add(Item("Lixa Grossa", aceLixas, null, "Lixa para preparação de superfícies.", 9.90m, 100, null, minimo: 20));
        produtos.Add(Item("Fita Crepe 50m", aceFitas, null, "Fita crepe para proteção durante pintura.", 7.90m, 80, null, minimo: 20));
        produtos.Add(Item("Plástico Proteção 4x5m", acePlasticos, null, "Plástico para proteção de móveis.", 15.90m, 50, null, minimo: 10));
        produtos.Add(Item("Luvas de Proteção", aceLuvas, null, "Luvas para proteção durante pintura.", 12.90m, 70, null, minimo: 15));
        produtos.Add(Item("Máscara Respiratória", aceMascaras, null, "Máscara para proteção ao pintar.", 19.90m, 0, null, minimo: 10));

        // Código (SKU) de cada produto: FT0001, FT0002...
        for (var i = 0; i < produtos.Count; i++)
            produtos[i].Sku = $"FT{i + 1:0000}";

        db.Produtos.AddRange(produtos);

        // ------------------------------------------------------------------ cupons
        db.Cupons.AddRange(
            new Cupom { Codigo = "BEMVINDO10", Tipo = TipoDesconto.Percentual, Valor = 10m, ValorMinimoPedido = 100m },
            new Cupom { Codigo = "PINTE50", Tipo = TipoDesconto.ValorFixo, Valor = 50m, ValorMinimoPedido = 300m });

        // Um SaveChanges só: o EF grava primeiro as marcas, tipos e categorias e
        // usa os Ids gerados nos produtos.
        db.SaveChanges();
        transacao.Commit();
    }

    // ------------------------------------------------------------------ ajudantes

    private static TipoTinta Tipo(string nome, string acabamento, string descricao, decimal rendimento, int demaos) => new()
    {
        Nome = nome,
        Acabamento = acabamento,
        Descricao = descricao,
        RendimentoM2PorLitro = rendimento,
        DemaosRecomendadas = demaos,
        FatorContraste = acabamento switch
        {
            Acabamentos.Fosco => 0.80m,
            Acabamentos.Brilhante => 1.10m,
            _ => 0.95m
        }
    };

    private static Cor Cor(string nome, string hex, string familia) =>
        new() { Nome = nome, CodigoHex = hex, Familia = familia };

    private static Categoria Sub(Categoria pai, string nome, string slug)
    {
        var sub = new Categoria
        {
            Nome = nome,
            Slug = $"{pai.Slug}-{slug}",
            CategoriaPai = pai,
            Ordem = pai.Subcategorias.Count + 1
        };
        pai.Subcategorias.Add(sub);
        return sub;
    }

    // As três latas de uma linha de tinta (900 ml, 3,6 L e 18 L).
    private static IEnumerable<Produto> Linha(string nome, Categoria categoria, TipoTinta tipo, Marca marca,
                                              string descricao, decimal precoGalao, int estoque = 30)
    {
        yield return Lata(nome, categoria, tipo, marca, descricao, 0.9m, Math.Round(precoGalao * 0.30m, 0) - 0.10m, null, estoque);
        yield return Lata(nome, categoria, tipo, marca, descricao, 3.6m, precoGalao, null, estoque);
        yield return Lata(nome, categoria, tipo, marca, descricao, 18m, Math.Round(precoGalao * 4.30m, 0) - 0.10m, null, Math.Max(estoque / 2, 1));
    }

    private static Produto Lata(string nome, Categoria categoria, TipoTinta tipo, Marca marca, string descricao,
                                decimal volume, decimal preco, decimal? precoDe, int estoque,
                                string? imagem = null, int minimo = 5) => new()
    {
        Nome = nome,
        Descricao = descricao,
        ImagemUrl = imagem,
        Categoria = categoria,
        Marca = marca,
        TipoTinta = tipo,
        VolumeLitros = volume,
        Preco = preco,
        PrecoDe = precoDe,
        Estoque = estoque,
        EstoqueMinimo = minimo
    };

    private static Produto Item(string nome, Categoria categoria, Marca? marca, string descricao,
                                decimal preco, int estoque, string? tamanho, int minimo = 5) => new()
    {
        Nome = nome,
        Descricao = descricao,
        Categoria = categoria,
        Marca = marca,
        Tamanho = tamanho,
        Preco = preco,
        Estoque = estoque,
        EstoqueMinimo = minimo
    };
}
