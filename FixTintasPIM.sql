IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE TABLE [Categorias] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(60) NOT NULL,
        [Slug] nvarchar(80) NOT NULL,
        [CategoriaPaiId] int NULL,
        [Ordem] int NOT NULL,
        [Inativa] bit NOT NULL,
        CONSTRAINT [PK_Categorias] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Categorias_Categorias_CategoriaPaiId] FOREIGN KEY ([CategoriaPaiId]) REFERENCES [Categorias] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE TABLE [Cores] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(80) NOT NULL,
        [CodigoHex] nvarchar(7) NOT NULL,
        [Familia] nvarchar(40) NULL,
        [CodigoPantone] nvarchar(40) NULL,
        [Inativa] bit NOT NULL,
        CONSTRAINT [PK_Cores] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE TABLE [Cupons] (
        [Id] int NOT NULL IDENTITY,
        [Codigo] nvarchar(40) NOT NULL,
        [Tipo] nvarchar(20) NOT NULL,
        [Valor] decimal(10,2) NOT NULL,
        [ValorMinimoPedido] decimal(10,2) NOT NULL,
        [ValidoAte] datetime2 NULL,
        [UsosMaximos] int NULL,
        [UsosRealizados] int NOT NULL,
        [Inativo] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Cupons] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Cupons_Tipo] CHECK ([Tipo] IN ('Percentual', 'ValorFixo')),
        CONSTRAINT [CK_Cupons_Usos] CHECK ([UsosRealizados] >= 0),
        CONSTRAINT [CK_Cupons_Valor] CHECK ([Valor] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE TABLE [Marcas] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(80) NOT NULL,
        [Descricao] nvarchar(300) NULL,
        [Inativa] bit NOT NULL,
        CONSTRAINT [PK_Marcas] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE TABLE [TiposTinta] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(60) NOT NULL,
        [Acabamento] nvarchar(20) NOT NULL,
        [Descricao] nvarchar(300) NULL,
        [RendimentoM2PorLitro] decimal(6,2) NOT NULL,
        [DemaosRecomendadas] int NOT NULL,
        [FatorContraste] decimal(4,2) NOT NULL,
        [Inativo] bit NOT NULL,
        CONSTRAINT [PK_TiposTinta] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TiposTinta_Demaos] CHECK ([DemaosRecomendadas] BETWEEN 1 AND 6),
        CONSTRAINT [CK_TiposTinta_Fator] CHECK ([FatorContraste] > 0),
        CONSTRAINT [CK_TiposTinta_Rendimento] CHECK ([RendimentoM2PorLitro] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE TABLE [Usuarios] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(120) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [SenhaHash] nvarchar(200) NOT NULL,
        [Perfil] nvarchar(20) NOT NULL,
        [Cpf] nchar(11) NULL,
        [Nascimento] date NULL,
        [Telefone] nvarchar(11) NULL,
        [Ativo] bit NOT NULL,
        [TrocarSenha] bit NOT NULL,
        [DataCadastro] datetime2 NOT NULL DEFAULT (GETDATE()),
        [DataAtualizacao] datetime2 NULL,
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Usuarios_Perfil] CHECK ([Perfil] IN ('Cliente', 'Funcionario', 'Gerente'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE TABLE [Produtos] (
        [Id] int NOT NULL IDENTITY,
        [Sku] nvarchar(40) NOT NULL,
        [Nome] nvarchar(150) NOT NULL,
        [Descricao] nvarchar(500) NULL,
        [ImagemUrl] nvarchar(300) NULL,
        [CategoriaId] int NOT NULL,
        [MarcaId] int NULL,
        [TipoTintaId] int NULL,
        [VolumeLitros] decimal(6,2) NULL,
        [Tamanho] nvarchar(30) NULL,
        [Preco] decimal(10,2) NOT NULL,
        [PrecoDe] decimal(10,2) NULL,
        [Estoque] int NOT NULL,
        [EstoqueMinimo] int NOT NULL,
        [Inativo] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [DataCadastro] datetime2 NOT NULL DEFAULT (GETDATE()),
        [DataAtualizacao] datetime2 NULL,
        CONSTRAINT [PK_Produtos] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Produtos_Estoque] CHECK ([Estoque] >= 0),
        CONSTRAINT [CK_Produtos_EstoqueMinimo] CHECK ([EstoqueMinimo] >= 0),
        CONSTRAINT [CK_Produtos_Preco] CHECK ([Preco] > 0),
        CONSTRAINT [CK_Produtos_PrecoDe] CHECK ([PrecoDe] IS NULL OR [PrecoDe] > [Preco]),
        CONSTRAINT [CK_Produtos_Tinta] CHECK ([TipoTintaId] IS NULL OR ([MarcaId] IS NOT NULL AND [VolumeLitros] IS NOT NULL)),
        CONSTRAINT [FK_Produtos_Categorias_CategoriaId] FOREIGN KEY ([CategoriaId]) REFERENCES [Categorias] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Produtos_Marcas_MarcaId] FOREIGN KEY ([MarcaId]) REFERENCES [Marcas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Produtos_TiposTinta_TipoTintaId] FOREIGN KEY ([TipoTintaId]) REFERENCES [TiposTinta] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE TABLE [Favoritos] (
        [UsuarioId] int NOT NULL,
        [CorId] int NOT NULL,
        [CriadoEm] datetime2 NOT NULL DEFAULT (GETDATE()),
        CONSTRAINT [PK_Favoritos] PRIMARY KEY ([UsuarioId], [CorId]),
        CONSTRAINT [FK_Favoritos_Cores_CorId] FOREIGN KEY ([CorId]) REFERENCES [Cores] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Favoritos_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE TABLE [Pedidos] (
        [Id] int NOT NULL IDENTITY,
        [UsuarioId] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Subtotal] decimal(12,2) NOT NULL,
        [DescontoVolume] decimal(12,2) NOT NULL,
        [DescontoCupom] decimal(12,2) NOT NULL,
        [Total] decimal(12,2) NOT NULL,
        [CupomId] int NULL,
        [CupomCodigo] nvarchar(40) NULL,
        [CriadoEm] datetime2 NOT NULL DEFAULT (GETDATE()),
        [AtualizadoEm] datetime2 NULL,
        CONSTRAINT [PK_Pedidos] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Pedidos_Status] CHECK ([Status] IN ('Pendente', 'Pago', 'Enviado', 'Entregue', 'Cancelado')),
        CONSTRAINT [CK_Pedidos_Total] CHECK ([Total] >= 0),
        CONSTRAINT [FK_Pedidos_Cupons_CupomId] FOREIGN KEY ([CupomId]) REFERENCES [Cupons] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pedidos_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE TABLE [Simulacoes] (
        [Id] int NOT NULL IDENTITY,
        [UsuarioId] int NOT NULL,
        [CorId] int NOT NULL,
        [TipoTintaId] int NOT NULL,
        [Nome] nvarchar(80) NULL,
        [AreaM2] decimal(10,2) NULL,
        [CaminhoImagem] nvarchar(300) NOT NULL,
        [CriadoEm] datetime2 NOT NULL DEFAULT (GETDATE()),
        CONSTRAINT [PK_Simulacoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Simulacoes_Cores_CorId] FOREIGN KEY ([CorId]) REFERENCES [Cores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Simulacoes_TiposTinta_TipoTintaId] FOREIGN KEY ([TipoTintaId]) REFERENCES [TiposTinta] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Simulacoes_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE TABLE [PedidoItens] (
        [Id] int NOT NULL IDENTITY,
        [PedidoId] int NOT NULL,
        [ProdutoId] int NOT NULL,
        [CorId] int NULL,
        [Quantidade] int NOT NULL,
        [PrecoUnitario] decimal(10,2) NOT NULL,
        [ProdutoDescricao] nvarchar(200) NOT NULL,
        [VolumeLitros] decimal(6,2) NULL,
        [CorNome] nvarchar(80) NULL,
        [CorHex] nvarchar(7) NULL,
        CONSTRAINT [PK_PedidoItens] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PedidoItens_Quantidade] CHECK ([Quantidade] > 0),
        CONSTRAINT [FK_PedidoItens_Cores_CorId] FOREIGN KEY ([CorId]) REFERENCES [Cores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PedidoItens_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PedidoItens_Produtos_ProdutoId] FOREIGN KEY ([ProdutoId]) REFERENCES [Produtos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Categorias_CategoriaPaiId_Nome] ON [Categorias] ([CategoriaPaiId], [Nome]) WHERE [CategoriaPaiId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categorias_Slug] ON [Categorias] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Cores_Nome] ON [Cores] ([Nome]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Cupons_Codigo] ON [Cupons] ([Codigo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Favoritos_CorId] ON [Favoritos] ([CorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Marcas_Nome] ON [Marcas] ([Nome]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_CorId] ON [PedidoItens] ([CorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_PedidoId] ON [PedidoItens] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE INDEX [IX_PedidoItens_ProdutoId] ON [PedidoItens] ([ProdutoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_CupomId] ON [Pedidos] ([CupomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_Status] ON [Pedidos] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_UsuarioId_CriadoEm] ON [Pedidos] ([UsuarioId], [CriadoEm]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Produtos_CategoriaId] ON [Produtos] ([CategoriaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Produtos_MarcaId] ON [Produtos] ([MarcaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Produtos_Sku] ON [Produtos] ([Sku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Produtos_TipoTintaId] ON [Produtos] ([TipoTintaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Simulacoes_CorId] ON [Simulacoes] ([CorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Simulacoes_TipoTintaId] ON [Simulacoes] ([TipoTintaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Simulacoes_UsuarioId_CriadoEm] ON [Simulacoes] ([UsuarioId], [CriadoEm]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TiposTinta_Nome_Acabamento] ON [TiposTinta] ([Nome], [Acabamento]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Usuarios_Cpf] ON [Usuarios] ([Cpf]) WHERE [Cpf] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_Email] ON [Usuarios] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN

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
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN

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
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN

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
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN

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
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN

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
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN

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
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026041448_Inicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261026041448_Inicial', N'9.0.22');
END;

COMMIT;
GO

