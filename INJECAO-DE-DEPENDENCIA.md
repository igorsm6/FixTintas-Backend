# Injeção de dependência — o que é e como usar no PIM

O manual do PIM IV exige: *"as dependências entre as camadas devem ser
configuradas por injeção de dependência no arquivo Program.cs dos projetos Web
e Api"*. Este guia explica o que isso quer dizer, partindo do que você já sabe
de POO, e mostra exatamente o que escrever quando criar uma entidade nova.

Os exemplos usam a **Marca**, a entidade mais simples do FixTintas (é o mesmo
molde do Cliente do exemplo do professor).

## 1. O que você já sabe: uma classe que precisa de outra

Em POO você já viu composição: uma classe que *tem* outra como campo. No PIM,
cada camada precisa da camada de baixo para trabalhar:

- o `MarcasController` precisa de um `MarcaNegocio` para aplicar as regras;
- o `MarcaNegocio` precisa de um `MarcaRepositorio` para ler e gravar;
- o `MarcaRepositorio` precisa de um `AppDbContext` para falar com o banco;
- o `AppDbContext` precisa da connection string para saber qual banco abrir.

Cada uma dessas classes recebe a outra **no construtor**. Abra
`FixTintas.Negocio\MarcaNegocio.cs` e veja:

```csharp
public MarcaNegocio(IMarcaRepositorio repositorio)
{
    _repositorio = repositorio;
}
```

Isso você já conhece: um construtor com parâmetro, que guarda o valor num campo.
A pergunta é: **quem chama esse construtor, e de onde vem o repositório?**

## 2. O jeito sem injeção de dependência (e por que não serve)

Sem injeção de dependência, cada controller teria de montar a cadeia inteira
na mão, com `new`, de baixo para cima:

```csharp
// NÃO FAÇA ASSIM — é só para entender o problema
public IActionResult Index()
{
    var opcoes = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=FixTintasPIM;Trusted_Connection=True;TrustServerCertificate=True")
        .Options;
    var contexto    = new AppDbContext(opcoes);
    var repositorio = new MarcaRepositorio(contexto);
    var negocio     = new MarcaNegocio(repositorio);

    var marcas = negocio.Listar();
    ...
}
```

Isso funciona, mas cria três problemas:

1. **A connection string se espalha pelo código.** Ela apareceria em todo
   controller, de toda entidade, nos dois projetos. O manual proíbe: ela tem de
   ficar no `appsettings.json`. E no dia em que o servidor mudar de nome, você
   teria de caçar todas as cópias.
2. **A Web e a Api passam a mexer no Repositório.** O controller estaria
   criando um `MarcaRepositorio` e um `AppDbContext`, o que quebra a regra do
   manual: *"os projetos Web e Api não devem acessar o banco de dados nem a
   camada de Repositório diretamente"*.
3. **Ninguém fecha a conexão com o banco.** O contexto foi criado e nunca é
   descartado. Com muitos acessos, as conexões se acumulam até o SQL Server
   recusar novas.

## 3. A ideia: pedir em vez de criar

A injeção de dependência inverte quem faz o trabalho. Em vez de cada classe
**criar** o que precisa, ela só **pede** no construtor. Quem cria e entrega é o
ASP.NET Core, seguindo uma lista que você escreve **uma única vez** no
`Program.cs`.

Pense numa lanchonete. O atendente anota "um X-burguer" e não vai até a
cozinha fritar o hambúrguer. A cozinha tem a receita: um X-burguer leva pão,
hambúrguer e queijo; o hambúrguer leva carne e tempero. O atendente só pede, e
o lanche chega montado. No PIM, o controller é o atendente, o `Program.cs` é o
livro de receitas, e o ASP.NET Core é a cozinha.

O controller fica assim, sem nenhum `new` (este é o do painel da loja,
`FixTintas.Web\Areas\Painel\Controllers\MarcasController.cs`):

```csharp
public class MarcasController : Controller
{
    private readonly MarcaNegocio _negocio;

    public MarcasController(MarcaNegocio negocio)          // só pede
    {
        _negocio = negocio;
    }

    public IActionResult Index()
    {
        var marcas = _negocio.Listar(incluirInativas: true);  // e usa
        ...
    }
}
```

O `MarcasController` da Api (`FixTintas.Api\Controllers\MarcasController.cs`)
pede exatamente o mesmo `MarcaNegocio`. É assim que o site e o app usam as
**mesmas regras**: os dois pedem o mesmo Negocio.

## 4. A lista de montagem, linha por linha

No `Program.cs` da Web e no da Api existe este bloco (igual nos dois; aqui só o
começo):

```csharp
builder.Services.AddDbContext<AppDbContext>(opcoes => opcoes.UseSqlServer(connectionString));

builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<UsuarioNegocio>();

builder.Services.AddScoped<IMarcaRepositorio, MarcaRepositorio>();
builder.Services.AddScoped<MarcaNegocio>();
...
```

Leia cada linha como uma frase:

| Linha | Em português |
|---|---|
| `AddDbContext<AppDbContext>(... UseSqlServer(connectionString))` | Quando alguém pedir um `AppDbContext`, crie um que use SQL Server com a connection string do `appsettings.json`. |
| `AddScoped<IMarcaRepositorio, MarcaRepositorio>()` | Quando alguém pedir um `IMarcaRepositorio` (a interface), entregue um `MarcaRepositorio` (a classe que implementa a interface). |
| `AddScoped<MarcaNegocio>()` | Quando alguém pedir um `MarcaNegocio`, crie um. |

A segunda linha é polimorfismo, que você já viu em POO: o `MarcaNegocio`
declara que precisa de *algo que cumpra o contrato* `IMarcaRepositorio`, e o
`Program.cs` decide qual classe concreta cumpre esse contrato. Se um dia o
grupo trocar o Entity Framework por Dapper, escreve outra classe que implementa
a mesma interface e muda só esta linha; o `MarcaNegocio` nem fica sabendo.

**Por que dois `Program.cs`?** Porque a Web e a Api são dois programas
separados: cada um sobe sozinho e tem a sua própria lista de montagem. Por isso
o bloco é igual nos dois, e toda linha nova vai para os dois.

**O que é `AddScoped`?** Diz *quanto tempo* o objeto vive. *Scoped* significa
um objeto novo para cada requisição (cada clique na Web, cada chamada da Api),
jogado fora quando a resposta sai. O `AddDbContext` funciona do mesmo jeito, e
é ao jogar o contexto fora que a conexão com o banco é fechada — o problema 3
da seção 2 se resolve sozinho. Existem também `AddSingleton` (um objeto só para
o sistema inteiro) e `AddTransient` (um novo a cada pedido). No FixTintas,
repositórios e classes de negócio são sempre `AddScoped`; só duas coisas são
`AddSingleton`, porque não mudam enquanto o sistema roda: as `RegrasDaLoja`
(lidas da seção `"Loja"` do `appsettings.json`) e o `IArmazenamentoDeArquivos`
(a pasta onde as imagens são gravadas).

## 5. O que acontece quando o gerente abre a tela de marcas

1. O navegador pede `/Painel/Marcas`.
2. O ASP.NET Core vê que precisa criar um `MarcasController` e olha o
   construtor dele: ele pede um `MarcaNegocio`.
3. Procura `MarcaNegocio` na lista do `Program.cs`. Acha. Olha o construtor:
   ele pede um `IMarcaRepositorio`.
4. Procura `IMarcaRepositorio` na lista. A lista diz: entregue um
   `MarcaRepositorio`. Olha o construtor dele: pede um `AppDbContext`.
5. Procura `AppDbContext`. A lista diz como criar, com a connection string.
6. Agora monta de baixo para cima: contexto → repositório → negócio →
   controller. E chama `Index()`.
7. A resposta sai, e tudo o que foi criado para esta requisição é descartado.

Você não escreve nada disso. Escreve só os construtores e as linhas do
`Program.cs`.

Um negócio pode pedir **vários** repositórios. O `PedidoNegocio` precisa
conferir produtos, cores e cupons para fechar um pedido, então pede todos:

```csharp
public PedidoNegocio(IPedidoRepositorio repositorio, IProdutoRepositorio produtos, ICorRepositorio cores,
                     ICupomRepositorio cupons, RegrasDaLoja regras)
```

Como todos já estão na lista, o ASP.NET entrega todos.

## 6. Receita: criando a entidade `Fornecedor`

Sempre que criar uma entidade nova, os construtores seguem o mesmo molde da
Marca:

```csharp
// FixTintas.Repositorio\FornecedorRepositorio.cs
public class FornecedorRepositorio : IFornecedorRepositorio
{
    private readonly AppDbContext _contexto;

    public FornecedorRepositorio(AppDbContext contexto)        // pede o banco
    {
        _contexto = contexto;
    }
    ...
}

// FixTintas.Negocio\FornecedorNegocio.cs
public class FornecedorNegocio
{
    private readonly IFornecedorRepositorio _repositorio;

    public FornecedorNegocio(IFornecedorRepositorio repositorio)  // pede o repositório
    {
        _repositorio = repositorio;
    }
    ...
}

// FixTintas.Web\Areas\Painel\Controllers\FornecedoresController.cs  (e, se o app precisar, na Api)
public class FornecedoresController : Controller
{
    private readonly FornecedorNegocio _negocio;

    public FornecedoresController(FornecedorNegocio negocio)      // pede o negócio
    {
        _negocio = negocio;
    }
    ...
}
```

E acrescente **duas linhas em cada `Program.cs`** (o da Web e o da Api), junto
das outras:

```csharp
builder.Services.AddScoped<IFornecedorRepositorio, FornecedorRepositorio>();
builder.Services.AddScoped<FornecedorNegocio>();
```

O `AddDbContext` não se repete: o mesmo `AppDbContext` serve a todas as
entidades. O que muda nele é ganhar o `DbSet<Fornecedor>` (e, depois, uma
migration nova: veja o README).

## 7. Os erros que você vai ver

**Esqueceu de registrar a classe de negócio** (a linha `AddScoped<FornecedorNegocio>()`).
O sistema sobe, mas a tela ou o endpoint devolve erro 500 com:

```
InvalidOperationException: Unable to resolve service for type
'FixTintas.Negocio.MarcaNegocio' while attempting to activate
'FixTintas.Web.Areas.Painel.Controllers.MarcasController'.
```

Leia assim: *"não consegui criar o MarcasController, porque ele pediu um
MarcaNegocio e ninguém me ensinou a criar um"*. Correção: a linha que falta
no `Program.cs` — e confira se ela está nos **dois**.

**Esqueceu de registrar o repositório** (a linha
`AddScoped<IFornecedorRepositorio, FornecedorRepositorio>()`). Aqui o sistema
nem chega a subir: o `dotnet run` ou o F5 para na hora com:

```
System.AggregateException: Some services are not able to be constructed
(... Unable to resolve service for type 'FixTintas.Repositorio.Interfaces.IMarcaRepositorio'
while attempting to activate 'FixTintas.Negocio.MarcaNegocio'.)
```

O ASP.NET confere a lista ao iniciar e percebe que o negócio pede algo que não
está nela. Correção: a linha do repositório nos dois `Program.cs`.

**Escreveu `new MarcaNegocio()` no controller.** Não compila: o construtor
exige um repositório. E se você tentar resolver criando o repositório também,
volta ao problema da seção 2. Peça no construtor do controller.

**Registrou a classe no lugar da interface** (`AddScoped<MarcaRepositorio>()`,
sem a interface). O `MarcaNegocio` pede `IMarcaRepositorio`, que não está na
lista, e dá o mesmo erro do repositório esquecido. A linha precisa dizer as
duas coisas: `AddScoped<IMarcaRepositorio, MarcaRepositorio>()`.

## 8. Resumo para colar no caderno

- Toda classe **pede** o que precisa no construtor e **nunca** dá `new` em
  controller, negócio, repositório ou contexto.
- O `Program.cs` é a lista de montagem. Cada entidade nova = **duas linhas**
  (repositório e negócio), nos **dois** `Program.cs`.
- Interface no repositório: a linha diz `<Interface, Classe>`.
- Erro com *Unable to resolve service for type 'X'* = falta registrar `X` no
  `Program.cs`.
