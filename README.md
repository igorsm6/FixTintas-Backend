# FixTintas — loja de tintas (PIM IV)

Solution no **template do professor** (a estrutura do *Manual do PIM IV, Etapa 4*):
cinco projetos, referências no sentido certo, Entity Framework Core + Dapper,
triggers e stored procedures, injeção de dependência no `Program.cs`.

Tudo o que o grupo tinha foi juntado aqui, num sistema só:

- o **site** (as páginas HTML/CSS/JS) virou o projeto **FixTintas.Web**, em
  **Razor**, com o mesmo visual — a loja e o painel do funcionário/gerente;
- a **API do Flutter** virou o projeto **FixTintas.Api**, a **única API** do
  sistema, agora com tudo o que o app precisa (catálogo, login, carrinho,
  pedidos, favoritos, calculadora e os projetos do simulador de parede) e
  também as rotas de administração;
- a "API do site" deixou de existir: as regras dela foram para o **Negocio**,
  que o site e a API usam juntos.

## Como as peças se encaixam

```
Navegador  ──► FixTintas.Web  (Razor: loja + painel)  ──┐
                                                         ├──► FixTintas.Negocio ──► FixTintas.Repositorio ──► SQL Server
App Flutter ─► FixTintas.Api  (JSON: a única API)     ──┘          (regras)          (EF Core + Dapper)
```

- **Site em Razor não chama API.** No Razor, quem monta a página é o servidor:
  o controller da Web pede o `ProdutoNegocio` (injeção de dependência), pega os
  dados e entrega para a view. Por isso o site não precisa de uma API própria.
- **A API existe para o app.** O celular não roda C#; ele conversa com o
  servidor por HTTP/JSON. É a FixTintas.Api.
- **As regras ficam num lugar só.** O preço do carrinho, o cupom, o desconto
  por volume, o estoque, o cadastro, a senha... estão no **Negocio**. O site e
  o app chamam as mesmas classes, então as regras são **iguais** nos dois, e o
  banco é um só: um pedido feito pelo app aparece no painel do site, um
  produto cadastrado no painel aparece no app.

Sentido das referências, como o manual manda (Web e Api nunca tocam no banco):

```
Web ──┐
      ├──► Negocio ──► Repositorio ──► Modelos
Api ──┘
```

## O que tem em cada projeto

```
FixTintas.sln
│
├── FixTintas.Modelos       entidades (Produto, Pedido, Cor...), DTOs (Dtos/), Formatacao (R$, litros)
│                           e RegrasDaSenha (a regra da senha, num lugar só)
│
├── FixTintas.Repositorio   o único que fala com o SQL Server
│   ├── Contexto/AppDbContext.cs   tabelas, regras CHECK, índices únicos e HasTrigger
│   ├── ObjetosDoBanco.cs          o SQL das 3 triggers e das 3 stored procedures
│   ├── Interfaces/                um contrato por repositório
│   ├── *Repositorio.cs            EF Core no cadastro; Dapper nas procedures
│   ├── ArmazenamentoDeArquivos.cs onde as imagens enviadas são gravadas
│   └── Dados/DadosIniciais.cs     o catálogo com que o banco nasce
│
├── FixTintas.Negocio       TODAS as regras da loja (usadas pela Web e pela Api)
│   ├── ProdutoNegocio, PedidoNegocio, UsuarioNegocio, CupomNegocio...
│   ├── Precificador.cs            subtotal → desconto por volume → cupom
│   ├── CalculadoraTinta.cs        litros e a melhor combinação de latas
│   └── RegraDeNegocioException.cs os erros com mensagem para o usuário
│
├── FixTintas.Web           o site em ASP.NET Core MVC (Razor)
│   ├── Controllers/               loja: Home, Produtos, Carrinho, Pedidos, Conta, Cores, App (download do app)
│   ├── Areas/Painel/              painel da equipe: produtos, estoque, pedidos, catálogo, cupons, funcionários
│   ├── ViewComponents/            menu lateral, janela do produto, sugestões da busca
│   ├── Infra/                     login por cookie, carrinho na sessão, troca de senha obrigatória, AppAndroid
│   ├── Aplicativo/                o arquivo FixTintas.apk que o site oferece para download
│   ├── Views/                     as telas (o visual do site original)
│   └── wwwroot/                   css/site.css (= style2.css original), loja.css, painel.css, js/
│
└── FixTintas.Api           a API do app Flutter
    ├── Controllers/               auth, perfil, catálogo, cálculos, pedidos, favoritos, simulações
    ├── Controllers/Admin/         /api/admin/... (funcionário e gerente)
    ├── Seguranca/                 login por token (JWT), limite de tentativas
    ├── Erros/TratadorDeErros.cs   toda exceção vira { "mensagem": "..." } com o código HTTP certo
    └── FixTintas.Api.http         todas as chamadas do app, prontas para testar
```

Os DTOs ficam no Modelos porque Web e Api usam os mesmos: a tela de cadastro do
site e o `POST /api/auth/registrar` recebem o mesmo `CadastroClienteDto`, com as
mesmas validações e mensagens. A entidade nunca sai para a tela nem para o app;
o que sai é sempre o DTO.

Dois textos de apoio, na mesma pasta:

- [EXPLICACAO-DO-CODIGO.md](EXPLICACAO-DO-CODIGO.md): o que cada parte faz, de
  onde veio cada arquivo do site antigo, o caminho de uma compra pelo site e
  pelo app, e as perguntas que o professor pode fazer;
- [INJECAO-DE-DEPENDENCIA.md](INJECAO-DE-DEPENDENCIA.md): como o `Program.cs`
  monta controller → negócio → repositório → banco, explicado do zero.

## Como rodar pela primeira vez

Precisa do **Visual Studio 2022** (com a carga de trabalho *ASP.NET e
desenvolvimento Web*) ou do **.NET 9 SDK**, e de um **SQL Server**. A Web e a
Api têm `RollForward` no `.csproj`: com só o .NET 10 instalado, rodam nele sem
mudar nada.

### 0. O SQL Server

A connection string fica no `appsettings.json` da Web **e** no da Api (chave
`Padrao`):

```json
"Padrao": "Server=(localdb)\\MSSQLLocalDB;Database=FixTintasPIM;Trusted_Connection=True;TrustServerCertificate=True"
```

`(localdb)\MSSQLLocalDB` é o SQL Server que vem junto com o Visual Studio (o
mesmo que as APIs antigas do grupo usavam). Se na sua máquina for outro, troque
o `Server=` nos **dois** arquivos:

| Você usa | No `appsettings.json` escreva |
|---|---|
| LocalDB (vem com o Visual Studio) | `Server=(localdb)\\MSSQLLocalDB;` (já vem assim) |
| SQL Server, instância padrão (MSSQLSERVER) | `Server=localhost;` |
| SQL Server Express (SQLEXPRESS) | `Server=localhost\\SQLEXPRESS;` |

Dentro do JSON a barra invertida é escrita **duas vezes**. O banco se chama
`FixTintasPIM` para não misturar com o banco `FixTintas` da API antiga (que
tem outras tabelas). O antigo pode ser apagado pelo SSMS quando quiserem.

### 1. Criar o banco (a migration)

O banco nasce de uma migration, que vocês geram na máquina de vocês. São três
passos, no terminal, na pasta onde está o `FixTintas.sln`:

**a) Gerar a migration**

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add Inicial --project FixTintas.Repositorio --startup-project FixTintas.Api
```

(O primeiro comando é uma vez por máquina; se disser que já está instalado,
siga.) Isso cria a pasta `FixTintas.Repositorio/Migrations` com o arquivo
`..._Inicial.cs`, que cria todas as tabelas.

**b) Colar as triggers e as procedures na migration**

O Entity Framework cria as tabelas, mas **não** cria trigger nem procedure (é o
"escrito à mão" da migration do exemplo do professor). O SQL delas já está
pronto no `FixTintas.Repositorio/ObjetosDoBanco.cs`; a migration só precisa
chamá-lo. Abra o `..._Inicial.cs` e acrescente **duas linhas**:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ... tudo o que o EF gerou (CreateTable, CreateIndex...) fica como está ...

    ObjetosDoBanco.Criar(migrationBuilder);      // <- ÚLTIMA linha do Up()
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    ObjetosDoBanco.Remover(migrationBuilder);    // <- PRIMEIRA linha do Down()

    // ... tudo o que o EF gerou (DropTable...) fica como está ...
}
```

Não precisa de `using`: a migration está no namespace
`FixTintas.Repositorio.Migrations`, que enxerga o `FixTintas.Repositorio`.
Se esquecer este passo, o site e a Api avisam no console, ao subir:
*"Faltam no banco: TR_..., sp_..."*.

**c) Criar o banco**

```bash
dotnet ef database update --project FixTintas.Repositorio --startup-project FixTintas.Api
```

No fim aparece `Done.`.

**Pelo Visual Studio** (em vez do terminal): botão direito em `FixTintas.Api` ›
*Definir como Projeto de Inicialização*; menu *Ferramentas › Gerenciador de
Pacotes do NuGet › Console do Gerenciador de Pacotes*; em *Projeto padrão*
escolha `FixTintas.Repositorio`; digite `Add-Migration Inicial`, cole as duas
linhas do passo **b** e depois digite `Update-Database`.

Para conferir no SSMS: banco `FixTintasPIM` › *Programação › Procedimentos
Armazenados* (as 3 procedures) e *Tabelas › dbo.Produtos › Gatilhos* (as
triggers ficam dentro das tabelas Usuarios, Produtos e PedidoItens).

Na primeira vez que a Web ou a Api sobem, o banco vazio recebe o **catálogo
inicial** (`DadosIniciais.cs`): 85 produtos, 24 cores, 10 tipos de tinta, 5
marcas, as categorias do menu e os cupons `BEMVINDO10` e `PINTE50`.

### 2. As imagens do site

As imagens do site original não estão no projeto (elas estavam só no GitHub
Pages). Copie estes arquivos do site antigo para
`FixTintas.Web/wwwroot/img/`:

| Arquivo | Onde aparece |
|---|---|
| `image.png`, `image22.png`, `image3.png` | carrossel do topo |
| `tinta_eucatex.png`, `tintaa.png`, `tinta_lukscolor.png`, `tinta_coral_3.png` | as 4 ofertas da página inicial |
| `whatsapp_3767236.png` | ícone do telefone, em "Contato" |

Sem elas o site funciona: o carrossel mostra um fundo azul com o nome da loja
e os cards mostram o nome da categoria no lugar da foto. As fotos dos outros
produtos são enviadas pelo painel (*Produtos › Editar › Imagem*) e ficam em
`wwwroot/img/produtos/`.

### 3. Rodar o site e a API juntos

**Pelo Visual Studio**: botão direito na solution › *Configurar Projetos de
Inicialização* › *Vários projetos de inicialização*, marque *Iniciar* em
`FixTintas.Web` e em `FixTintas.Api`, e **F5**. Na primeira vez, aceite o
certificado de desenvolvimento.

- Site: `https://localhost:7241` (ou `http://localhost:5157`)
- API: `http://localhost:5000` — a tela de teste da API abre em `/scalar`

**Pelo terminal**, em duas janelas:

```bash
dotnet run --project FixTintas.Web
dotnet run --project FixTintas.Api
```

### 4. Primeiro acesso

| Quem | E-mail | Senha |
|---|---|---|
| Gerente | `gerente@fixtintas.com` | `Gerente@123` (o sistema obriga a trocar no primeiro acesso) |

O gerente é criado sozinho na primeira vez (seção `GerenteInicial` do
`appsettings.json`). Entrando no site com ele, o menu do usuário mostra
**Painel da equipe**. Os funcionários são criados pelo gerente, no painel, em
*Funcionários*: o sistema gera uma senha temporária, mostrada uma única vez, e
o funcionário troca no primeiro acesso. Clientes se cadastram sozinhos, pelo
site ou pelo app.

## O que o sistema faz

**Loja (site)** — o visual do site original, agora com dados do banco:
- vitrine com o menu lateral de categorias, subcategorias e marcas; busca com
  sugestões (acha "plastica" em "Plásticas"); filtros de preço e ordem — tudo
  pela stored procedure `sp_Produtos_Vitrine`;
- as latas da mesma tinta viram um card só, com um botão por volume
  (900 ml, 3,6 L, 18 L); a janela do produto pede a cor (as mesmas do app);
- carrinho guardado na sessão; o preço é sempre recalculado pelo servidor;
- desconto automático de 5% a partir de R$ 500,00 e cupons de desconto;
- cadastro com nome completo, CPF válido, maior de 18 anos e senha com letras e
  números; "Meus pedidos" (cancela enquanto está Pendente), "Meus dados",
  "Alterar senha" e "Cores favoritas" (as mesmas favoritas do app);
- embaixo do campo de senha (cadastro e "Alterar senha"), a caixa **"A senha
  precisa ter:"** marca em verde cada regra cumprida enquanto a pessoa digita;
- o app para Android: página `/App`, faixa na página inicial, link no menu e no
  rodapé, e uma janela que convida a baixar quem abre a loja num celular Android
  (veja "O app para Android no site").

**Painel da equipe (site, `/Painel`)**:
- **funcionário**: cadastra e edita produtos (com upload de imagem), mexe no
  estoque e acompanha os pedidos (Pendente → Pago → Enviado → Entregue, ou
  Cancelado, que devolve o estoque);
- **gerente**: tudo isso e mais preços, exclusão de produtos, categorias,
  marcas, tipos de tinta, cores, cupons e funcionários.

Produto, marca, cor ou categoria que já foi usado não é apagado: é
**desativado** (some da loja e do app), para os pedidos antigos continuarem
certos.

**No banco**:
- `TR_PedidoItens_BaixaEstoque` baixa o estoque quando um pedido é gravado — e,
  com a regra `CK_Produtos_Estoque`, o banco recusa o pedido se o estoque
  acabou no mesmo instante;
- `TR_Usuarios_DataAtualizacao` e `TR_Produtos_DataAtualizacao` gravam a data
  da última alteração;
- `sp_Produtos_Vitrine` (busca da loja), `sp_Produtos_EstoqueBaixo` (painel) e
  `sp_Pedidos_Cancelar` (devolve estoque e cupom numa transação), chamadas pelo
  Dapper com parâmetros (sem risco de SQL Injection).

## Regras da senha

No mínimo **8 caracteres, com pelo menos uma letra e um número** (e no máximo 100).
A regra está num lugar só, `FixTintas.Modelos/RegrasDaSenha.cs`, e vale igual
no site, no painel e na API:

- os DTOs de cadastro, de troca de senha e de funcionário usam a regra no
  `[RegularExpression]`: o site avisa no navegador antes de enviar, a API
  responde 400 com a mensagem e a documentação da API (`/scalar`) mostra a regra
  no campo `senha`;
- o Negocio (`Validacoes.ProblemaNaSenha`) confere de novo antes de salvar;
- as caixas "A senha precisa ter:" repetem as mesmas três regras: no site em
  `wwwroot/js/site.js` (função `iniciarRegrasSenha`) e no app em
  `lib/widgets/regras_senha.dart`. Mudou a regra? Mude nos três lugares.

O login não aplica a regra: quem já tem conta continua entrando normalmente.

## O app para Android no site

O site oferece o app Flutter (arquivo `.apk`) para download:

- **`/App`**: página com o que o app faz, o botão de download e o passo a passo
  para instalar (link "App" no menu de cima e no rodapé);
- **faixa na página inicial** ("Teste as cores na sua parede antes de comprar");
- **janela para celulares Android**: quem abre a loja (início, produtos ou
  cores) num celular Android vê um convite para baixar o app, uma vez. Quem
  fecha não vê de novo por 7 dias; quem baixa, por 30 dias. No computador e no
  iPhone a janela não aparece (no iPhone o site avisa que o app é só para Android).

**Onde fica o arquivo**: `FixTintas.Web/Aplicativo/FixTintas.apk`. Sem ele, o
site mostra "Download disponível em breve". Para publicar uma versão nova do
app, é só trocar o arquivo (mesmo nome), sem parar o site. Em vez do arquivo,
dá para usar um link (Google Drive, GitHub...) em `AppAndroid:LinkExterno` no
`appsettings.json` do site.

**Gerar o `.apk`** (na pasta do projeto Flutter):

```bash
flutter build apk --release --dart-define=API_URL=https://ENDERECO-DA-API
```

O arquivo sai em `build\app\outputs\flutter-apk\app-release.apk`: copie para
`FixTintas.Web/Aplicativo/` com o nome `FixTintas.apk`. O `API_URL` é o endereço
da API que o app vai usar; no celular de verdade ele precisa ser um endereço
que o celular alcance (a API publicada, ou o IP do computador na mesma rede).

**Testar no celular** (celular e computador na mesma rede Wi-Fi):
1. no Visual Studio, rode o **FixTintas.Web** com o perfil **http-rede** (na
   setinha ao lado do botão de rodar); na primeira vez, permita o acesso no aviso
   do Firewall do Windows;
2. descubra o IP do computador (`ipconfig`, linha "Endereço IPv4");
3. no celular, abra `http://IP-DO-COMPUTADOR:5157`: a janela "Baixe o app" aparece.

## A API do app

Todas as respostas são JSON em camelCase. Erros sempre vêm como
`{ "mensagem": "..." }` (e, na validação, também `"erros"`), com o código HTTP
certo: 400 regra/validação, 401 sem login, 403 sem permissão, 404 não achou,
409 conflito, 429 muitas tentativas.

| Verbo | Endereço | Login | O que faz |
|---|---|---|---|
| POST | `/api/auth/registrar` | — | cadastra cliente (nome, e-mail, senha) → 201 |
| POST | `/api/auth/login` | — | devolve `token`, `nome`, `perfil`, `trocarSenha` |
| GET / PUT | `/api/perfil` | cliente | ver / alterar nome e telefone |
| POST | `/api/perfil/alterar-senha` | cliente | troca a senha → 204 |
| GET | `/api/cores`, `/api/tipos-tinta`, `/api/marcas`, `/api/categorias` | — | catálogo |
| GET | `/api/produtos` (`?marcaId=&tipoTintaId=`) | — | latas de tinta à venda |
| POST | `/api/calculos/tinta` | — | litros e as melhores combinações de latas |
| POST | `/api/pedidos/orcamento` | — | preços, descontos e total do carrinho |
| POST / GET | `/api/pedidos` | cliente | fecha o pedido (201) / meus pedidos |
| GET | `/api/pedidos/{id}` | cliente | um pedido |
| POST | `/api/pedidos/{id}/cancelar` | cliente | cancela (só Pendente) |
| GET / PUT / DELETE | `/api/favoritos` · `/api/favoritos/{corId}` | cliente | cores favoritas |
| GET / POST / DELETE | `/api/simulacoes` · `/api/simulacoes/{id}` | cliente | projetos do simulador (foto em multipart) |
| GET | `/api/simulacoes/{id}/imagem` | cliente | a foto do projeto (só para o dono) |
| … | `/api/admin/produtos`, `/api/admin/pedidos` | funcionário ou gerente | produtos, estoque e pedidos |
| … | `/api/admin/categorias`, `marcas`, `tipos-tinta`, `cores`, `cupons`, `funcionarios` | gerente | catálogo, cupons e equipe |

Para testar: com a Api rodando, abra `http://localhost:5000/scalar` (tela com
todos os endpoints, botão *Test Request*), ou abra o
`FixTintas.Api/FixTintas.Api.http` no Visual Studio e clique em *Enviar
solicitação* em cima de cada chamada.

### Para quem cuida do app Flutter

O app funciona com esta API sem mudar os endereços: porta **5000**, os mesmos
caminhos e os mesmos campos JSON que ele já usava. O que mudou:

- os perfis agora são `Cliente`, `Funcionario` e `Gerente` (não existe mais
  `Admin`). O app só usa isso para mostrar o cartão de administrador no perfil
  (`auth.ehAdmin`, em `auth_provider.dart`, que aceita os três nomes);
- a senha precisa ter **8 caracteres, com letras e números**: o cadastro e o
  "Alterar senha" do app mostram a caixa "A senha precisa ter:"
  (`lib/widgets/regras_senha.dart`) e conferem antes de enviar; a API confere
  de novo e devolve a mensagem;
- o `nome` do produto não traz mais o volume ("Tintas Acrílicas Premium"); o
  volume vem no `volumeLitros`;
- pedidos feitos no site podem ter acessórios (rolo, lixa...), que não têm cor:
  essas linhas vêm com `corId: 0`, `corNome: ""`, `corHex: ""` e
  `volumeLitros: 0`;
- celular de verdade na mesma rede: rode a Api com o perfil **http-rede** (no
  Visual Studio, na setinha ao lado do botão de rodar) e o app com
  `flutter run --dart-define=API_URL=http://IP-DO-SEU-PC:5000`.

## Três cuidados que o manual cobra (e onde estão)

- **Tabela com trigger precisa de `HasTrigger`** — está no `AppDbContext`
  (Usuarios, Produtos e PedidoItens). Sem isso, o EF Core dá erro ao gravar.
- **Trigger e procedure são escritas à mão na migration** — o SQL está no
  `ObjetosDoBanco.cs` e entra na migration pelas duas linhas do passo 1b. Para
  ter o script SQL completo do banco (útil no documento):
  `dotnet ef migrations script --project FixTintas.Repositorio --startup-project FixTintas.Api -o banco.sql`.
- **Consulta sempre parametrizada** — no Dapper, os valores vão num objeto
  separado (`new { Termo = termo }`), nunca concatenados no SQL.

## Mudou uma entidade? Nova migration

Acrescentou uma propriedade ou uma entidade nova (veja a receita na seção 6 do
[INJECAO-DE-DEPENDENCIA.md](INJECAO-DE-DEPENDENCIA.md)):

```bash
dotnet ef migrations add NomeDaMudanca --project FixTintas.Repositorio --startup-project FixTintas.Api
dotnet ef database update --project FixTintas.Repositorio --startup-project FixTintas.Api
```

Só a migration `Inicial` leva as duas linhas do `ObjetosDoBanco`. Se um dia
mudar o SQL de uma procedure, crie uma migration com o `ALTER PROCEDURE` à mão.

## Se algo deu errado

| Aparece | Causa | Correção |
|---|---|---|
| `error: 26 - Error Locating Server/Instance Specified` | O `Server=` do `appsettings.json` não é o seu SQL Server. | Corrija nos **dois** `appsettings.json` (seção 0). |
| `Cannot open database "FixTintasPIM"` ou, no console, *"Dados iniciais não carregados"* | O banco ainda não foi criado. | Passo 1 (migration e `database update`). |
| No console: *"Faltam no banco: TR_..., sp_..."* / na loja: `Could not find stored procedure 'sp_Produtos_Vitrine'` | A migration foi aplicada sem as duas linhas do `ObjetosDoBanco`. | Desfaça com `dotnet ef database update 0 --project FixTintas.Repositorio --startup-project FixTintas.Api`, cole as linhas (passo 1b) e rode o `database update` de novo. |
| `There is already an object named 'Categorias' in the database` | O banco já tinha tabelas de outra versão. | Use outro nome no `Database=` ou apague o banco antigo no SSMS e rode o `database update`. |
| `The model for context 'AppDbContext' has pending changes` | Uma entidade mudou e não existe migration para a mudança. | `dotnet ef migrations add ...` e depois o `database update`. |
| `Unable to resolve service for type '...Negocio'` | A classe não foi registrada no `Program.cs`. | Acrescente o `AddScoped` nos **dois** `Program.cs` ([INJECAO-DE-DEPENDENCIA.md](INJECAO-DE-DEPENDENCIA.md), seção 7). |
| O app diz *"Não foi possível conectar ao servidor"* | A Api não está rodando ou o endereço não é o do app. | Rode a Api (porta 5000). No emulador Android o app usa `10.0.2.2:5000`; no celular, veja "Para quem cuida do app Flutter". |
| A página `/App` diz *"Download disponível em breve"* | O arquivo do app não está na pasta. | Copie o `.apk` para `FixTintas.Web/Aplicativo/FixTintas.apk` e atualize a página (rodando no Visual Studio, a página mostra o caminho completo). |
| O celular não abre o site pelo IP do computador | O site está rodando só para o próprio computador, ou o Firewall bloqueou. | Rode o Web com o perfil **http-rede**, permita no Firewall do Windows (redes privadas) e confira se o celular está na mesma rede Wi-Fi. |
| No celular: *"App não instalado"* ao atualizar | O `.apk` novo foi gerado em outro computador (assinatura diferente). | Desinstale o app antigo do celular e instale o novo. |
| Erro de compilação: arquivo em uso por outro processo | O sistema ainda está rodando. | Pare (**Shift+F5** no Visual Studio, **Ctrl+C** no terminal) e compile de novo. |
