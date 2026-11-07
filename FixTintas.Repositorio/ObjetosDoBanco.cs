using Dapper;
using FixTintas.Repositorio.Contexto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FixTintas.Repositorio;

// ============================================================================
// TRIGGERS E STORED PROCEDURES do banco FixTintas.
//
// O Entity Framework cria as tabelas sozinho, mas NÃO cria trigger nem procedure:
// esse SQL é escrito à mão (é o trecho "Escrito à mão" da migration do professor).
// Para não colar um SQL enorme dentro da migration, ele mora aqui, e a migration
// só chama, depois de gerada:
//
//     ObjetosDoBanco.Criar(migrationBuilder);     // ÚLTIMA linha do Up()
//     ObjetosDoBanco.Remover(migrationBuilder);   // PRIMEIRA linha do Down()
//
// (Esta classe está no namespace FixTintas.Repositorio, "pai" do namespace
// FixTintas.Repositorio.Migrations, por isso a migration enxerga ela sem "using".)
//
//   TR_Usuarios_DataAtualizacao   grava a data da última alteração do usuário
//   TR_Produtos_DataAtualizacao   grava a data da última alteração do produto
//   TR_PedidoItens_BaixaEstoque   ao gravar os itens de um pedido, baixa o estoque
//   sp_Produtos_Vitrine           busca da loja (texto, categoria, marca, preço, ofertas)
//   sp_Produtos_EstoqueBaixo      produtos no estoque mínimo ou zerados (painel)
//   sp_Pedidos_Cancelar           cancela o pedido, devolve o estoque e o uso do cupom
// ============================================================================
public static class ObjetosDoBanco
{
    // Nomes usados também no AppDbContext (HasTrigger) e nos repositórios (Dapper).
    public const string TriggerUsuarios = "TR_Usuarios_DataAtualizacao";
    public const string TriggerProdutos = "TR_Produtos_DataAtualizacao";
    public const string TriggerPedidoItens = "TR_PedidoItens_BaixaEstoque";
    public const string ProcedureVitrine = "sp_Produtos_Vitrine";
    public const string ProcedureEstoqueBaixo = "sp_Produtos_EstoqueBaixo";
    public const string ProcedureCancelarPedido = "sp_Pedidos_Cancelar";

    // Chamado no FIM do Up() da migration (as tabelas já precisam existir).
    public static void Criar(MigrationBuilder migrationBuilder)
    {
        // Cada Sql() é executado separado: CREATE TRIGGER e CREATE PROCEDURE
        // precisam ser o primeiro comando do lote no SQL Server.
        migrationBuilder.Sql(SqlTriggerUsuarios);
        migrationBuilder.Sql(SqlTriggerProdutos);
        migrationBuilder.Sql(SqlTriggerPedidoItens);
        migrationBuilder.Sql(SqlVitrine);
        migrationBuilder.Sql(SqlEstoqueBaixo);
        migrationBuilder.Sql(SqlCancelarPedido);
    }

    // Confere no banco se a migration criou tudo (o Program.cs avisa no console se faltar
    // algum: é o sinal de que a linha ObjetosDoBanco.Criar não foi colada na migration).
    public static List<string> Faltando(AppDbContext contexto)
    {
        var nomes = new[] { TriggerUsuarios, TriggerProdutos, TriggerPedidoItens, ProcedureVitrine, ProcedureEstoqueBaixo, ProcedureCancelarPedido };
        var existentes = contexto.Database.GetDbConnection()
            .Query<string>("SELECT name FROM sys.objects WHERE name IN @Nomes", new { Nomes = nomes })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return nomes.Where(n => !existentes.Contains(n)).ToList();
    }

    // Chamado no COMEÇO do Down() da migration (desfaz o que o Criar fez).
    public static void Remover(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Pedidos_Cancelar");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Produtos_EstoqueBaixo");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Produtos_Vitrine");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_PedidoItens_BaixaEstoque");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Produtos_DataAtualizacao");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Usuarios_DataAtualizacao");
    }

    // ---------------------------------------------------------------- triggers

    // Toda vez que um usuário é alterado, grava a data da alteração
    // (a mesma ideia da TR_Clientes_DataAtualizacao do exemplo do professor).
    private const string SqlTriggerUsuarios = @"
CREATE TRIGGER TR_Usuarios_DataAtualizacao
ON Usuarios
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE u
       SET DataAtualizacao = GETDATE()
      FROM Usuarios u
      JOIN inserted i ON i.Id = u.Id;
END";

    // Toda vez que um produto é alterado (inclusive o estoque), grava a data.
    private const string SqlTriggerProdutos = @"
CREATE TRIGGER TR_Produtos_DataAtualizacao
ON Produtos
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE p
       SET DataAtualizacao = GETDATE()
      FROM Produtos p
      JOIN inserted i ON i.Id = p.Id;
END";

    // Quando os itens de um pedido são gravados, o estoque diminui sozinho.
    // A tabela especial "inserted" tem as linhas que acabaram de entrar, e o SUM
    // junta o mesmo produto que aparece em mais de um item (ex.: duas cores).
    // Se o estoque ficasse negativo, a regra CK_Produtos_Estoque (Estoque >= 0)
    // barra o UPDATE e o pedido inteiro é desfeito: o banco protege o estoque
    // mesmo que duas pessoas comprem a última lata no mesmo segundo.
    private const string SqlTriggerPedidoItens = @"
CREATE TRIGGER TR_PedidoItens_BaixaEstoque
ON PedidoItens
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE p
       SET p.Estoque = p.Estoque - x.Total
      FROM Produtos p
      JOIN (SELECT ProdutoId, SUM(Quantidade) AS Total
              FROM inserted
             GROUP BY ProdutoId) x ON x.ProdutoId = p.Id;
END";

    // ---------------------------------------------------------- procedures

    // Busca da loja (páginas Produtos e Início). Todos os parâmetros são opcionais:
    //   @Termo             texto procurado no nome, descrição, categoria, marca e tipo
    //   @Categoria         slug da categoria ou da subcategoria ('exterior', 'exterior-vernizes')
    //   @MarcaId           só uma marca
    //   @PrecoMin/@PrecoMax faixa de preço
    //   @SomentePromocoes  1 = só produtos com preço "De" (ofertas da página inicial)
    // COLLATE Latin1_General_CI_AI = ignora maiúsculas E acentos: "plastica" acha "Plástica".
    // Os REPLACE fazem os caracteres especiais do LIKE ([ % _) valerem como texto comum.
    // ATENÇÃO: os nomes das colunas do SELECT são os nomes das propriedades da classe
    // ProdutoVitrineLinha (Modelos/Dtos/VitrineDtos.cs). O Dapper liga um ao outro pelo nome.
    private const string SqlVitrine = @"
CREATE PROCEDURE sp_Produtos_Vitrine
    @Termo            NVARCHAR(100) = NULL,
    @Categoria        NVARCHAR(80)  = NULL,
    @MarcaId          INT           = NULL,
    @PrecoMin         DECIMAL(10,2) = NULL,
    @PrecoMax         DECIMAL(10,2) = NULL,
    @SomentePromocoes BIT           = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Padrao NVARCHAR(320) = NULL;
    IF @Termo IS NOT NULL
        SET @Padrao = '%' + REPLACE(REPLACE(REPLACE(@Termo, '[', '[[]'), '%', '[%]'), '_', '[_]') + '%';

    SELECT p.Id, p.Sku, p.Nome, p.Descricao, p.ImagemUrl,
           p.CategoriaId, c.Nome AS CategoriaNome, c.Slug AS CategoriaSlug,
           pai.Nome AS CategoriaPaiNome, pai.Slug AS CategoriaPaiSlug,
           p.MarcaId, m.Nome AS MarcaNome,
           p.TipoTintaId, t.Nome AS TipoTintaNome, t.Acabamento,
           p.VolumeLitros, p.Tamanho, p.Preco, p.PrecoDe, p.Estoque, p.EstoqueMinimo
      FROM Produtos p
      JOIN Categorias c        ON c.Id = p.CategoriaId
      LEFT JOIN Categorias pai ON pai.Id = c.CategoriaPaiId
      LEFT JOIN Marcas m       ON m.Id = p.MarcaId
      LEFT JOIN TiposTinta t   ON t.Id = p.TipoTintaId
     WHERE p.Inativo = 0
       AND c.Inativa = 0
       AND (pai.Id IS NULL OR pai.Inativa = 0)
       AND (m.Id IS NULL OR m.Inativa = 0)
       AND (t.Id IS NULL OR t.Inativo = 0)
       AND (@Categoria IS NULL OR c.Slug = @Categoria OR pai.Slug = @Categoria)
       AND (@MarcaId   IS NULL OR p.MarcaId = @MarcaId)
       AND (@PrecoMin  IS NULL OR p.Preco >= @PrecoMin)
       AND (@PrecoMax  IS NULL OR p.Preco <= @PrecoMax)
       AND (@SomentePromocoes = 0 OR p.PrecoDe IS NOT NULL)
       AND (@Padrao IS NULL
            OR p.Nome      COLLATE Latin1_General_CI_AI LIKE @Padrao
            OR p.Descricao COLLATE Latin1_General_CI_AI LIKE @Padrao
            OR c.Nome      COLLATE Latin1_General_CI_AI LIKE @Padrao
            OR pai.Nome    COLLATE Latin1_General_CI_AI LIKE @Padrao
            OR m.Nome      COLLATE Latin1_General_CI_AI LIKE @Padrao
            OR t.Nome      COLLATE Latin1_General_CI_AI LIKE @Padrao)
     ORDER BY p.Nome, p.VolumeLitros, p.Preco;
END";

    // Produtos que precisam de reposição: estoque no mínimo ou abaixo (inclui zerados).
    // Mesmas colunas da sp_Produtos_Vitrine (o Dapper usa a mesma classe).
    private const string SqlEstoqueBaixo = @"
CREATE PROCEDURE sp_Produtos_EstoqueBaixo
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Id, p.Sku, p.Nome, p.Descricao, p.ImagemUrl,
           p.CategoriaId, c.Nome AS CategoriaNome, c.Slug AS CategoriaSlug,
           pai.Nome AS CategoriaPaiNome, pai.Slug AS CategoriaPaiSlug,
           p.MarcaId, m.Nome AS MarcaNome,
           p.TipoTintaId, t.Nome AS TipoTintaNome, t.Acabamento,
           p.VolumeLitros, p.Tamanho, p.Preco, p.PrecoDe, p.Estoque, p.EstoqueMinimo
      FROM Produtos p
      JOIN Categorias c        ON c.Id = p.CategoriaId
      LEFT JOIN Categorias pai ON pai.Id = c.CategoriaPaiId
      LEFT JOIN Marcas m       ON m.Id = p.MarcaId
      LEFT JOIN TiposTinta t   ON t.Id = p.TipoTintaId
     WHERE p.Inativo = 0
       AND p.Estoque <= p.EstoqueMinimo
     ORDER BY p.Estoque ASC, p.Nome ASC;
END";

    // Cancela um pedido: devolve ao estoque o que ele baixou, libera o uso do cupom
    // e muda o status. Tudo numa TRANSAÇÃO: ou acontece tudo, ou nada.
    //   XACT_ABORT ON     = qualquer erro desfaz tudo automaticamente.
    //   UPDLOCK, HOLDLOCK = trava o pedido: dois cancelamentos ao mesmo tempo não
    //                       devolvem o estoque duas vezes.
    // Devolve uma linha com a coluna Cancelado (1 = cancelou; 0 = já estava
    // cancelado ou entregue, nada foi feito).
    private const string SqlCancelarPedido = @"
CREATE PROCEDURE sp_Pedidos_Cancelar
    @PedidoId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Cancelado BIT = 0;
    DECLARE @CupomId INT = NULL;

    BEGIN TRANSACTION;

    SELECT @CupomId = CupomId
      FROM Pedidos WITH (UPDLOCK, HOLDLOCK)
     WHERE Id = @PedidoId
       AND Status NOT IN ('Cancelado', 'Entregue');

    IF @@ROWCOUNT = 1
    BEGIN
        UPDATE p
           SET p.Estoque = p.Estoque + x.Total
          FROM Produtos p
          JOIN (SELECT ProdutoId, SUM(Quantidade) AS Total
                  FROM PedidoItens
                 WHERE PedidoId = @PedidoId
                 GROUP BY ProdutoId) x ON x.ProdutoId = p.Id;

        IF @CupomId IS NOT NULL
            UPDATE Cupons
               SET UsosRealizados = UsosRealizados - 1
             WHERE Id = @CupomId
               AND UsosRealizados > 0;

        UPDATE Pedidos
           SET Status = 'Cancelado',
               AtualizadoEm = GETDATE()
         WHERE Id = @PedidoId;

        SET @Cancelado = 1;
    END

    COMMIT TRANSACTION;

    SELECT @Cancelado AS Cancelado;
END";
}
