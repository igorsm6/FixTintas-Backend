# Explicação do código do FixTintas

Este documento explica **o que foi feito, onde está cada coisa e por quê**. Ele
serve para o grupo entender o sistema e para a apresentação. Para **rodar**,
veja o [README.md](README.md); para a injeção de dependência explicada do
zero, o [INJECAO-DE-DEPENDENCIA.md](INJECAO-DE-DEPENDENCIA.md).

## Sumário

1. [A ideia geral](#1-a-ideia-geral)
2. [Do site HTML para o Razor](#2-do-site-html-para-o-razor)
3. [Das duas APIs para uma](#3-das-duas-apis-para-uma)
4. [Camada por camada](#4-camada-por-camada)
5. [Login: cookie no site, token no app](#5-login-cookie-no-site-token-no-app)
6. [O banco de dados](#6-o-banco-de-dados)
7. [Filme 1: uma compra pelo site](#7-filme-1-uma-compra-pelo-site)
8. [Filme 2: uma compra pelo app](#8-filme-2-uma-compra-pelo-app)
9. [Perguntas que o professor pode fazer](#9-perguntas-que-o-professor-pode-fazer)
10. [Glossário](#10-glossário)

---

## 1. A ideia geral

O FixTintas tem **dois "rostos"** e **um "cérebro"**:

```
Navegador  ──► FixTintas.Web  (site em Razor)  ──┐
                                                  ├──► Negocio ──► Repositorio ──► SQL Server
App Flutter ─► FixTintas.Api  (JSON)           ──┘    (regras)      (EF + Dapper)
```

- **FixTintas.Web** desenha as páginas no servidor (Razor) e devolve HTML
  pronto para o navegador. É a loja e o painel da equipe.
- **FixTintas.Api** devolve JSON para o aplicativo Flutter. É a **única API**.
- Os dois chamam o **mesmo Negocio**. Por isso uma regra (por exemplo, "cupom
  só vale acima de R$ 100") é escrita **uma vez** e vale no site e no app.
- Só o **Repositorio** fala com o banco. Web e Api nunca escrevem SQL nem
  criam conexão (é a regra do manual).

O banco é um só: o pedido feito no app aparece no painel do site; o produto
cadastrado no painel aparece no app; a cor favoritada no site aparece nas
favoritas do app.

## 2. Do site HTML para o Razor

O site antigo era HTML estático com JavaScript: os produtos estavam escritos à
mão no `script.js`, o carrinho vivia na memória do navegador e o login era de
mentira (`auth.js` no modo "mock"). No Razor, **o servidor** monta cada página
com os dados do banco. O visual é o mesmo: o `style2.css` foi copiado sem
alteração.

| Site antigo | No FixTintas.Web | O que mudou |
|---|---|---|
| `index.html` | `Views/Home/Index.cshtml` | as 4 ofertas vêm do banco (produtos com preço "De") |
| cabeçalho, menu lateral e rodapé repetidos em cada página | `Views/Shared/_Layout.cshtml` | escritos uma vez só; cada página entra no `@RenderBody()` |
| `produtos.html` | `Views/Produtos/Index.cshtml` | filtros na URL (`/Produtos?categoria=interior&ordem=menor`), busca pela procedure `sp_Produtos_Vitrine` |
| `busca.html` | a mesma página de produtos (`/Produtos?termo=verniz`) | uma página a menos para manter |
| lista de produtos no `script.js` | tabela `Produtos` (+ `DadosIniciais.cs`) | o painel cadastra, o site e o app enxergam |
| menu de categorias fixo no JS | `ViewComponents/MenuCategoriasViewComponent.cs` | categorias e marcas vêm do banco |
| janela "Adicionar ao carrinho" | `Views/Shared/Components/ModalProduto` + `wwwroot/js/site.js` | cores do banco; envia só produto, cor e quantidade |
| carrinho no JS (`carrinho = []`) | `Infra/CarrinhoDaSessao.cs` + `CarrinhoController` | guardado na sessão (no servidor); o preço é sempre recalculado |
| `auth.js` (login simulado) | `ContaController` + `Infra/LoginDoSite.cs` | login de verdade, senha com hash no banco |
| `admin.html` / `admin.js` / `admin.css` | `Areas/Painel/` + `wwwroot/css/painel.css` | uma página por tela, com as mesmas cores; permissões checadas no servidor |
| `style2.css` | `wwwroot/css/site.css` (igual) + `wwwroot/css/loja.css` (telas novas) | — |

**O que sobrou de JavaScript** (`wwwroot/js/site.js`) é só a parte visual:
carrossel, abrir/fechar menus, a janela do produto, as sugestões da busca e as
máscaras de CPF/telefone. Nenhuma regra e nenhum preço ficam no navegador:
mesmo que alguém altere o JavaScript, o servidor confere tudo de novo.

**Razor em uma frase:** um arquivo `.cshtml` é HTML com C# no meio. O `@`
liga o C#. Exemplo do card de produto (`Views/Shared/_CardProduto.cshtml`):

```cshtml
<h3>@Model.Titulo</h3>
<p class="preco">@Formatacao.Moeda(barata?.Preco ?? 0m)</p>
```

O servidor troca `@Model.Titulo` pelo nome do produto e entrega HTML puro.

**Tag helpers**: atributos `asp-...` que o Razor transforma em HTML certo.
`<a asp-controller="Produtos" asp-action="Index">` vira `<a href="/Produtos">`;
`<form asp-action="Adicionar" method="post">` ganha sozinho o token
anti-falsificação; `<input asp-for="Email">` pega o nome, o valor e as regras
de validação do DTO.

## 3. Das duas APIs para uma

O grupo tinha duas APIs: a do Flutter (fora do template, com o catálogo do app,
pedidos e a foto do simulador) e a "API do site" (que o `auth.js` chamaria).
O professor pediu **uma** API, a do app, **no template**. Ficou assim:

- **Tudo o que o app chamava continua no mesmo endereço**, com os mesmos campos
  JSON: `/api/auth`, `/api/perfil`, `/api/cores`, `/api/tipos-tinta`,
  `/api/marcas`, `/api/produtos`, `/api/calculos/tinta`, `/api/pedidos`,
  `/api/favoritos`, `/api/simulacoes`. O app não precisa mudar de endereço.
- **As regras da "API do site"** (login com perfis, senha temporária do
  funcionário, preço só pelo gerente, estoque, upload de imagem) foram para o
  **Negocio**. O site usa essas regras direto; o app usa pela API.
- **A administração** (produtos, estoque, pedidos, catálogo, cupons, equipe)
  ganhou rotas em `/api/admin/...`, protegidas por perfil, além das telas do
  painel no site.

## 4. Camada por camada

### 4.1 FixTintas.Modelos — os dados

**Entidades** (cada uma vira uma tabela):

| Classe | Tabela | O que guarda |
|---|---|---|
| `Usuario` | Usuarios | clientes, funcionários e gerentes (perfil, senha em hash, CPF, nascimento) |
| `Categoria` | Categorias | menu da loja em dois níveis (Interior › Tintas Acrílicas) |
| `Marca` | Marcas | Suvinil, Coral... |
| `TipoTinta` | TiposTinta | Acrílica Fosco, Esmalte Brilhante... com rendimento (m²/L), demãos e fator de contraste |
| `Cor` | Cores | nome, HEX, família, Pantone |
| `Produto` | Produtos | **um item vendável**: uma lata de um volume (tinta) ou um acessório |
| `Cupom` | Cupons | percentual ou valor fixo, mínimo, validade, limite de usos |
| `Pedido` / `PedidoItem` | Pedidos / PedidoItens | o pedido e uma "foto" de cada item (descrição, preço, cor) no momento da compra |
| `Favorito` | Favoritos | cor favorita de um usuário |
| `Simulacao` | Simulacoes | projeto do simulador do app (cor, tipo de tinta, foto) |

**Tinta ou acessório?** Um produto é tinta quando tem `TipoTintaId` (e então
precisa de marca e de volume, e o cliente escolhe a cor na compra). Sem tipo
de tinta, é acessório (rolo, lixa, escada), que pode ter um `Tamanho`.

**Por que uma lata por produto?** Porque cada volume tem preço, estoque e SKU
próprios. Na loja, as latas com o mesmo nome, marca, tipo e categoria viram
**um card só** com um botão por volume (`ProdutoNegocio.Agrupar`).

**DTOs** (pasta `Dtos`): o que entra e sai das telas e da API. A entidade
nunca sai: sai o DTO (por exemplo, o `Usuario` tem `SenhaHash`, o `PerfilDto`
não). As validações de formulário (`[Required]`, `[Range]`...) com as
mensagens em português ficam nos DTOs, e valem no site e na API.

`Formatacao.cs` mostra os valores no formato brasileiro (R$ 1.234,50; 3,6 L;
25/10/2026).

`RegrasDaSenha.cs` guarda a regra da senha (8 caracteres, com letras e
números) num lugar só: os DTOs usam no `[RegularExpression]` e o Negocio usa
em `Validacoes.ProblemaNaSenha`.

### 4.2 FixTintas.Repositorio — o banco

- `Contexto/AppDbContext.cs`: o mapa do banco para o Entity Framework — tamanho
  das colunas, casas decimais, chaves estrangeiras, índices únicos (e-mail,
  CPF, SKU, código do cupom...), regras **CHECK** (preço > 0, estoque ≥ 0,
  status válido...) e o `HasTrigger` das tabelas que têm trigger.
- `ObjetosDoBanco.cs`: o SQL das triggers e procedures (seção 6), que entra
  na migration.
- Um **repositório por entidade**, cada um com a sua interface em
  `Interfaces/`. Cadastro e consultas comuns com **EF Core**; as stored
  procedures com **Dapper**, usando a mesma conexão do EF
  (`_contexto.Database.GetDbConnection()`).
- `ArmazenamentoDeArquivos.cs`: grava as imagens enviadas (o Negocio não sabe
  em que pasta). Na Web, em `wwwroot/img/produtos` (públicas); na Api, em
  `App_Data` (as fotos do simulador, que só o dono vê).
- `Dados/DadosIniciais.cs`: o catálogo com que o banco nasce.
- `ConflitoDeGravacaoException`: quando o **banco** recusa (índice único,
  CHECK, duas pessoas gravando o mesmo registro), o repositório lança esta
  exceção e o Negocio troca por uma mensagem amigável.

### 4.3 FixTintas.Negocio — as regras

| Classe | Responsável por |
|---|---|
| `UsuarioNegocio` | login (`Autenticar`), cadastro de cliente, meus dados, troca de senha, funcionários (senha temporária, desativar), gerente inicial |
| `ProdutoNegocio` | vitrine (`Vitrine`, `Ofertas`, `Agrupar`), cadastro e edição pelo painel (preço só gerente, SKU automático, imagem), estoque, exclusão (vendido = desativado) |
| `PedidoNegocio` | carrinho (`AdicionarItem`, `AplicarCupom`, `Sanear`), orçamento (`Orcar`), fechar pedido (`Criar`), cancelar, status do painel |
| `Precificador` | a conta do preço: subtotal → desconto por volume (5% a partir de R$ 500) → cupom sobre o que sobrou |
| `RegrasPedido` | o caminho do status: Pendente → Pago → Enviado → Entregue (ou Cancelado) |
| `CalculoNegocio` + `CalculadoraTinta` | litros = área × demãos ÷ rendimento + 10% de perda; a combinação de latas mais barata (programação dinâmica, a "mochila") |
| `CategoriaNegocio`, `MarcaNegocio`, `TipoTintaNegocio`, `CorNegocio`, `CupomNegocio` | o catálogo: nomes únicos, slug das categorias, dois níveis, valores válidos |
| `FavoritoNegocio`, `SimulacaoNegocio` | favoritas e projetos do simulador (só o dono vê o projeto) |
| `Validacoes`, `ValidadorDeImagem`, `SenhaHasher` | CPF, e-mail, nome completo, senha forte; imagem de verdade (confere os bytes, não a extensão); senha em hash PBKDF2 |
| `RegraDeNegocioException` e irmãs | os erros com mensagem para o usuário (veja abaixo) |

**Os tipos de erro** — o Negocio lança, a tela mostra a mensagem, e a API
devolve o código HTTP certo:

| Exceção | Quando | Na API |
|---|---|---|
| `RegraDeNegocioException` | regra quebrada ("Este cupom expirou.") | 400 |
| `CredenciaisInvalidasException` | e-mail ou senha errados | 401 |
| `AcessoNegadoException` | sem permissão ("Só o gerente pode mudar o preço...") | 403 |
| `NaoEncontradoException` | não existe ou não é seu | 404 |
| `ConflitoException` | estoque acabou, e-mail repetido, alterado por outra pessoa | 409 |

### 4.4 FixTintas.Web — o site

- `Program.cs`: injeção de dependência (o mesmo bloco da Api), sessão (carrinho),
  login por cookie, limite de tentativas no login, proteção anti-falsificação
  em todo POST e o filtro que obriga a trocar a senha temporária.
- `Controllers/` (loja): `HomeController` (início), `ProdutosController`
  (vitrine), `CarrinhoController` (carrinho, cupom, finalizar), `PedidosController`
  (meus pedidos), `ContaController` (entrar, cadastrar, meus dados, senha),
  `CoresController` (catálogo de cores e favoritas) e `AppController` (página
  `/App` e o download do app Android em `/App/Baixar`).
- `Areas/Painel/`: o painel em `/Painel`, com `[Authorize(Roles = ...)]` em cada
  controller — funcionário e gerente em Início, Produtos, Estoque e Pedidos;
  só o gerente em Categorias, Marcas, Tipos de tinta, Cores, Cupons e
  Funcionários. Cada tela segue o molde do `ClientesController` do professor:
  `Index`, `Criar`, `Editar`, `Excluir` e a partial `_Formulario`.
- `ViewComponents/`: pedaços de página com dados próprios — o menu lateral, a
  janela do produto e a lista das sugestões da busca.
- `Infra/`: `LoginDoSite` (grava o cookie), `ValidacaoDoLogin` (a cada página,
  confere se a conta continua ativa), `CarrinhoDaSessao`,
  `ExigirTrocaDeSenhaFilter` e `AppAndroid` (onde está o `.apk`, versão e
  tamanho, lidos do `appsettings.json`).
- O app para Android aparece em `Views/App/Index.cshtml` (a página),
  `Views/Shared/_FaixaApp.cshtml` (a faixa da página inicial) e
  `Views/Shared/_JanelaApp.cshtml` (a janela que o `site.js` abre só para quem
  está num celular Android, uma vez).

### 4.5 FixTintas.Api — a API do app

- `Program.cs`: o mesmo bloco de injeção de dependência da Web, login por
  token (JWT), limite de tentativas (20 por minuto no login/cadastro), CORS
  (para o Flutter Web), JSON em camelCase, OpenAPI (`/openapi/v1.json`) e
  Scalar (`/scalar`).
- `Controllers/`: as rotas do app; `Controllers/Admin/`: as rotas de
  administração (`/api/admin/...`).
- `Seguranca/`: gera e confere o token; `ValidacaoDoToken` confere no banco, a
  cada chamada, se a conta continua ativa (desativou → próxima chamada dá 401);
  `BloquearSenhaTemporariaFilter` deixa quem tem senha temporária usar só o
  `/api/perfil` (onde troca a senha).
- `Erros/TratadorDeErros.cs`: transforma cada exceção do Negocio no código HTTP
  da tabela acima, sempre com `{ "mensagem": "..." }`.

## 5. Login: cookie no site, token no app

Os dois usam o **mesmo** `UsuarioNegocio.Autenticar` (e-mail + senha conferidos
contra o hash PBKDF2 do banco). Muda só **como o "estou logado" viaja**:

- **Site**: depois do login, o servidor grava um **cookie** criptografado
  (`FixTintas.Login`) com id, nome, perfil e se a senha é temporária. O navegador
  manda o cookie em toda página. Funciona porque o navegador e o servidor são
  o mesmo site.
- **App**: depois do login, a API devolve um **token JWT** assinado com a chave
  do `appsettings.json`. O app guarda o token e manda em toda chamada, no
  cabeçalho `Authorization: Bearer ...`.

**Perfis**: `Cliente` (compra), `Funcionario` (produtos, estoque e pedidos) e
`Gerente` (tudo, inclusive preço, exclusão, catálogo, cupons e equipe). A
permissão é conferida no **servidor**: `[Authorize(Roles = Perfis.Gerente)]`
nos controllers e, para o preço, também dentro do `ProdutoNegocio`. Esconder
botão na tela é só conforto.

**Senha temporária**: o funcionário criado pelo gerente (e o gerente inicial)
entra com `TrocarSenha = true`. No site, qualquer página leva para "Alterar
senha"; na API, as rotas respondem 403 até a troca.

## 6. O banco de dados

**Relacionamentos principais**: `Produto` → `Categoria` (que pode ter uma
`CategoriaPai`), `Marca` e `TipoTinta`; `Pedido` → `Usuario` e `Cupom`;
`PedidoItem` → `Pedido`, `Produto` e `Cor`; `Favorito` → `Usuario` + `Cor`;
`Simulacao` → `Usuario`, `Cor` e `TipoTinta`.

**As 3 triggers**:

| Trigger | Quando | O que faz |
|---|---|---|
| `TR_PedidoItens_BaixaEstoque` | itens de um pedido são gravados | diminui o estoque de cada produto. Com a regra `CK_Produtos_Estoque` (estoque ≥ 0), se duas pessoas comprarem a última lata no mesmo segundo, o banco recusa a segunda compra inteira |
| `TR_Produtos_DataAtualizacao` | produto alterado | grava a data da alteração |
| `TR_Usuarios_DataAtualizacao` | usuário alterado | grava a data da alteração |

**As 3 stored procedures** (chamadas pelo Dapper, com parâmetros):

| Procedure | Usada em | O que faz |
|---|---|---|
| `sp_Produtos_Vitrine` | vitrine do site e ofertas | busca por texto (sem diferenciar acento: "plastica" acha "Plásticas"), categoria ou subcategoria, marca, faixa de preço e só promoções; só produtos à venda |
| `sp_Produtos_EstoqueBaixo` | início do painel e `/api/admin/produtos/estoque-baixo` | o que está no estoque mínimo ou zerado |
| `sp_Pedidos_Cancelar` | cancelamento (cliente ou equipe) | numa transação: devolve o estoque, devolve o uso do cupom e muda o status |

**Proteções do próprio banco**: índices únicos (e-mail, CPF, SKU, código do
cupom, nome da marca, slug da categoria...) e regras CHECK (preço maior que
zero, preço "De" maior que o preço, estoque não negativo, status e perfil
válidos, quantidade maior que zero). O Negocio confere antes; o banco garante.

## 7. Filme 1: uma compra pelo site

1. A cliente abre `/Produtos?categoria=interior-tintas-acrilicas`. O
   `ProdutosController` pede ao `ProdutoNegocio.Vitrine`, que chama a
   `sp_Produtos_Vitrine` (pelo `ProdutoRepositorio`, com Dapper) e agrupa as
   latas em cards. A view `Produtos/Index.cshtml` desenha os cards.
2. Ela clica em "Adicionar ao Carrinho", escolhe a cor, o volume (3,6 L) e a
   quantidade. O `site.js` só preenche o formulário; o POST vai para
   `/Carrinho/Adicionar` com **produto, cor e quantidade** — sem preço.
3. O `CarrinhoController` chama `PedidoNegocio.AdicionarItem`, que confere se o
   produto está à venda, se a cor existe e se há estoque, e grava o carrinho na
   sessão (`CarrinhoDaSessao`).
4. Em `/Carrinho`, ela aplica o cupom `BEMVINDO10`. O `PedidoNegocio` confere
   validade, limite de usos e valor mínimo; o `Precificador` calcula subtotal,
   desconto por volume e desconto do cupom.
5. "Finalizar Compra": se não está logada, vai para `/Conta/Entrar` e volta ao
   carrinho depois. O `PedidoNegocio.Criar` confere **tudo de novo** (o estoque
   pode ter mudado), grava o pedido e soma o uso do cupom — tudo num
   `SaveChanges`.
6. No banco, a trigger `TR_PedidoItens_BaixaEstoque` baixa o estoque. Se não
   houver mais estoque, o banco recusa e ela vê "O estoque ou o cupom mudou...".
7. Ela é levada a `/Pedidos/Detalhes/1`, com o status **Pendente** e o botão
   de cancelar. O pedido já aparece no painel da equipe.

## 8. Filme 2: uma compra pelo app

1. O app chama `GET /api/produtos` e `GET /api/cores` (o mesmo
   `ProdutoNegocio`/`CorNegocio` do site) e mostra as latas.
2. A calculadora chama `POST /api/calculos/tinta` com as medidas da parede; o
   `CalculoNegocio` calcula os litros e sugere as latas mais baratas.
3. O carrinho do app chama `POST /api/pedidos/orcamento` (o mesmo
   `PedidoNegocio.Orcar` do site) para mostrar os valores.
4. `POST /api/auth/login` devolve o token; `POST /api/pedidos` (com o token)
   chama o mesmo `PedidoNegocio.Criar`. A partir daqui é igual ao filme 1,
   passos 5 a 7: mesma conferência, mesma trigger, mesmo pedido no painel.

## 9. Perguntas que o professor pode fazer

**Por que o site não chama a API?** Porque no Razor quem monta a página é o
servidor, que já tem acesso ao Negocio pela injeção de dependência. Chamar uma
API por HTTP de dentro do próprio servidor seria um caminho mais longo para
chegar ao mesmo lugar. A API existe para quem está **fora** do servidor: o app.

**Onde estão as regras de negócio?** Só no projeto Negocio. Os controllers da
Web e da Api recebem os dados, chamam o Negocio e mostram o resultado.

**Como o sistema evita SQL Injection?** O EF Core gera SQL parametrizado e, no
Dapper, os valores vão num objeto separado do comando
(`new { Termo = termo }`), nunca concatenados no texto do SQL.

**Como as senhas são guardadas?** Só o hash PBKDF2-SHA256 com sal aleatório e
100 mil repetições (`SenhaHasher.cs`). Nem o sistema sabe a senha.

**Onde está a regra da senha?** Em `RegrasDaSenha.cs` (Modelos): no mínimo 8
caracteres, com letras e números. O site confere no navegador, a API e o site
conferem no servidor (DTO e Negocio), e as caixas "A senha precisa ter:" do
site e do app mostram as mesmas regras enquanto a pessoa digita.

**Como o cliente baixa o app?** Pelo site: `/App/Baixar` entrega o arquivo
`FixTintas.apk` com o tipo de arquivo de aplicativo Android
(`application/vnd.android.package-archive`), então o celular oferece instalar
quando o download termina. Quem abre a loja num celular Android recebe o convite
numa janela; no computador e no iPhone, não.

**E se duas pessoas comprarem a última lata ao mesmo tempo?** O Negocio confere
o estoque, mas quem garante é o banco: a trigger baixa o estoque e a regra
`CK_Produtos_Estoque` não deixa ficar negativo, então a segunda compra é
recusada inteira.

**O cliente pode mudar o preço pelo navegador?** Não. O formulário só manda
produto, cor e quantidade; o preço é sempre lido do banco pelo Negocio.

**Por que DTO e não a entidade?** Para não expor campos internos (como o hash
da senha) e para validar exatamente o que a tela ou o app mandou.

**Onde está a injeção de dependência?** No bloco "Injeção de dependência" do
`Program.cs` da Web e da Api (iguais), explicado no
[INJECAO-DE-DEPENDENCIA.md](INJECAO-DE-DEPENDENCIA.md).

## 10. Glossário

| Termo | Em uma frase |
|---|---|
| Razor | HTML com C# no meio (`.cshtml`), montado no servidor |
| Controller | a classe que recebe a requisição, chama o Negocio e devolve a página ou o JSON |
| View | o arquivo `.cshtml` que desenha a página |
| Partial view | um pedaço de view reaproveitado (`_Formulario`, `_CardProduto`) |
| View component | um pedaço de página com o seu próprio código (o menu lateral) |
| Área | uma "seção" do site com controllers e views próprios (`/Painel`) |
| DTO | o objeto que entra ou sai pela tela/API |
| Migration | o arquivo que cria ou muda as tabelas do banco |
| Trigger | SQL que o banco roda sozinho quando uma tabela muda |
| Stored procedure | uma consulta guardada no banco, chamada pelo nome |
| JWT | o "crachá" assinado que o app manda em cada chamada |
| Cookie | o "crachá" que o navegador manda em cada página do site |
| Sessão | memória do servidor para cada visitante (o carrinho do site) |
| Anti-forgery token | o código escondido em cada formulário que impede outro site de enviar o formulário no seu nome |
