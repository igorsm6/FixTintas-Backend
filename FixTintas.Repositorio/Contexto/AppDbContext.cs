using FixTintas.Modelos;
using Microsoft.EntityFrameworkCore;

namespace FixTintas.Repositorio.Contexto;

// ============================================================================
// O DbContext é a "porta" do Entity Framework Core para o banco FixTintas.
// Cada DbSet vira uma tabela; o OnModelCreating diz como cada coluna é criada
// (tamanho, obrigatória ou não, índices únicos, chaves estrangeiras e regras CHECK).
//
// A migration é GERADA a partir deste arquivo pelo comando
//     dotnet ef migrations add Inicial --project FixTintas.Repositorio --startup-project FixTintas.Api
// Mudou alguma coisa aqui? Gere uma migration nova.
// ============================================================================
public class AppDbContext : DbContext
{
    // As "options" trazem qual banco usar (SQL Server) e a connection string.
    // Elas são montadas pela linha AddDbContext do Program.cs e entregues aqui.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<TipoTinta> TiposTinta => Set<TipoTinta>();
    public DbSet<Cor> Cores => Set<Cor>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Cupom> Cupons => Set<Cupom>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItens => Set<PedidoItem>();
    public DbSet<Favorito> Favoritos => Set<Favorito>();
    public DbSet<Simulacao> Simulacoes => Set<Simulacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------------------------------------------------------------- Usuarios
        // Clientes, funcionários e gerentes ficam na mesma tabela; o que muda é o Perfil.
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios", t =>
            {
                // OBRIGATÓRIO a partir do EF Core 7 em tabela que tem trigger
                // (sem esta linha, todo INSERT/UPDATE nesta tabela dá erro).
                t.HasTrigger(ObjetosDoBanco.TriggerUsuarios);
                t.HasCheckConstraint("CK_Usuarios_Perfil", "[Perfil] IN ('Cliente', 'Funcionario', 'Gerente')");
            });

            e.HasKey(u => u.Id);
            e.Property(u => u.Nome).HasMaxLength(120).IsRequired();
            e.Property(u => u.Email).HasMaxLength(150).IsRequired();
            e.Property(u => u.SenhaHash).HasMaxLength(200).IsRequired();
            e.Property(u => u.Perfil).HasMaxLength(20).IsRequired();
            e.Property(u => u.Cpf).HasMaxLength(11).IsFixedLength();
            e.Property(u => u.Telefone).HasMaxLength(11);
            e.Property(u => u.DataCadastro).HasDefaultValueSql("GETDATE()");

            // O banco também barra e-mail e CPF repetidos (o CPF é opcional no app).
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Cpf).IsUnique().HasFilter("[Cpf] IS NOT NULL");
        });

        // -------------------------------------------------------------- Categorias
        // Dois níveis: categoria (Interior) e subcategoria (Interior › Tintas Acrílicas).
        modelBuilder.Entity<Categoria>(e =>
        {
            e.ToTable("Categorias");
            e.HasKey(c => c.Id);
            e.Property(c => c.Nome).HasMaxLength(60).IsRequired();
            e.Property(c => c.Slug).HasMaxLength(80).IsRequired();

            e.HasOne(c => c.CategoriaPai)
                .WithMany(c => c.Subcategorias)
                .HasForeignKey(c => c.CategoriaPaiId)
                .OnDelete(DeleteBehavior.Restrict);

            // O "slug" é o nome usado na URL (/Produtos?categoria=exterior-vernizes).
            e.HasIndex(c => c.Slug).IsUnique();
            e.HasIndex(c => new { c.CategoriaPaiId, c.Nome }).IsUnique();
        });

        // ------------------------------------------------------------------ Marcas
        modelBuilder.Entity<Marca>(e =>
        {
            e.ToTable("Marcas");
            e.HasKey(m => m.Id);
            e.Property(m => m.Nome).HasMaxLength(80).IsRequired();
            e.Property(m => m.Descricao).HasMaxLength(300);
            e.HasIndex(m => m.Nome).IsUnique();
        });

        // ------------------------------------------------------------- TiposTinta
        // Ex.: "Acrílica" + "Fosco". Guarda os números técnicos usados na calculadora
        // (rendimento e demãos) e no simulador do app (fator de contraste).
        modelBuilder.Entity<TipoTinta>(e =>
        {
            e.ToTable("TiposTinta", t =>
            {
                t.HasCheckConstraint("CK_TiposTinta_Rendimento", "[RendimentoM2PorLitro] > 0");
                t.HasCheckConstraint("CK_TiposTinta_Demaos", "[DemaosRecomendadas] BETWEEN 1 AND 6");
                t.HasCheckConstraint("CK_TiposTinta_Fator", "[FatorContraste] > 0");
            });
            e.HasKey(t => t.Id);
            e.Property(t => t.Nome).HasMaxLength(60).IsRequired();
            e.Property(t => t.Acabamento).HasMaxLength(20).IsRequired();
            e.Property(t => t.Descricao).HasMaxLength(300);
            e.Property(t => t.RendimentoM2PorLitro).HasPrecision(6, 2);
            e.Property(t => t.FatorContraste).HasPrecision(4, 2);
            e.HasIndex(t => new { t.Nome, t.Acabamento }).IsUnique();
        });

        // ------------------------------------------------------------------- Cores
        modelBuilder.Entity<Cor>(e =>
        {
            e.ToTable("Cores");
            e.HasKey(c => c.Id);
            e.Property(c => c.Nome).HasMaxLength(80).IsRequired();
            e.Property(c => c.CodigoHex).HasMaxLength(7).IsRequired();
            e.Property(c => c.Familia).HasMaxLength(40);
            e.Property(c => c.CodigoPantone).HasMaxLength(40);
            e.HasIndex(c => c.Nome).IsUnique();
        });

        // ---------------------------------------------------------------- Produtos
        // Cada linha é um item que se vende: uma lata de tinta (com tipo, marca e volume)
        // ou um acessório (rolo, lixa...). O estoque é do produto.
        modelBuilder.Entity<Produto>(e =>
        {
            e.ToTable("Produtos", t =>
            {
                t.HasTrigger(ObjetosDoBanco.TriggerProdutos);

                // Regras que o próprio banco garante, mesmo que alguém grave direto pelo SSMS:
                t.HasCheckConstraint("CK_Produtos_Preco", "[Preco] > 0");
                t.HasCheckConstraint("CK_Produtos_PrecoDe", "[PrecoDe] IS NULL OR [PrecoDe] > [Preco]");
                // O estoque nunca fica negativo: se dois clientes comprarem a última lata
                // no mesmo segundo, o segundo pedido é desfeito pelo banco.
                t.HasCheckConstraint("CK_Produtos_Estoque", "[Estoque] >= 0");
                t.HasCheckConstraint("CK_Produtos_EstoqueMinimo", "[EstoqueMinimo] >= 0");
                // Tinta (tem tipo) precisa de marca e de volume.
                t.HasCheckConstraint("CK_Produtos_Tinta", "[TipoTintaId] IS NULL OR ([MarcaId] IS NOT NULL AND [VolumeLitros] IS NOT NULL)");
            });

            e.HasKey(p => p.Id);
            e.Property(p => p.Sku).HasMaxLength(40).IsRequired();
            e.Property(p => p.Nome).HasMaxLength(150).IsRequired();
            e.Property(p => p.Descricao).HasMaxLength(500);
            e.Property(p => p.ImagemUrl).HasMaxLength(300);
            e.Property(p => p.Tamanho).HasMaxLength(30);
            e.Property(p => p.VolumeLitros).HasPrecision(6, 2);
            e.Property(p => p.Preco).HasPrecision(10, 2);
            e.Property(p => p.PrecoDe).HasPrecision(10, 2);
            e.Property(p => p.DataCadastro).HasDefaultValueSql("GETDATE()");

            // RowVersion: o SQL Server muda este número a cada alteração da linha.
            // Se duas pessoas editarem o mesmo produto ao mesmo tempo, a segunda recebe
            // um aviso em vez de apagar sem saber o que a primeira gravou.
            e.Property(p => p.RowVersion).IsRowVersion();

            e.HasOne(p => p.Categoria).WithMany().HasForeignKey(p => p.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Marca).WithMany().HasForeignKey(p => p.MarcaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.TipoTinta).WithMany().HasForeignKey(p => p.TipoTintaId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(p => p.Sku).IsUnique();
        });

        // ------------------------------------------------------------------ Cupons
        modelBuilder.Entity<Cupom>(e =>
        {
            e.ToTable("Cupons", t =>
            {
                t.HasCheckConstraint("CK_Cupons_Tipo", "[Tipo] IN ('Percentual', 'ValorFixo')");
                t.HasCheckConstraint("CK_Cupons_Valor", "[Valor] > 0");
                t.HasCheckConstraint("CK_Cupons_Usos", "[UsosRealizados] >= 0");
            });
            e.HasKey(c => c.Id);
            e.Property(c => c.Codigo).HasMaxLength(40).IsRequired();
            // O enum é gravado como texto ("Percentual"), mais fácil de ler no SSMS.
            e.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Valor).HasPrecision(10, 2);
            e.Property(c => c.ValorMinimoPedido).HasPrecision(10, 2);
            e.Property(c => c.RowVersion).IsRowVersion();
            e.HasIndex(c => c.Codigo).IsUnique();
        });

        // ----------------------------------------------------------------- Pedidos
        modelBuilder.Entity<Pedido>(e =>
        {
            e.ToTable("Pedidos", t =>
            {
                t.HasCheckConstraint("CK_Pedidos_Status", "[Status] IN ('Pendente', 'Pago', 'Enviado', 'Entregue', 'Cancelado')");
                t.HasCheckConstraint("CK_Pedidos_Total", "[Total] >= 0");
            });
            e.HasKey(p => p.Id);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Subtotal).HasPrecision(12, 2);
            e.Property(p => p.DescontoVolume).HasPrecision(12, 2);
            e.Property(p => p.DescontoCupom).HasPrecision(12, 2);
            e.Property(p => p.Total).HasPrecision(12, 2);
            e.Property(p => p.CupomCodigo).HasMaxLength(40);
            e.Property(p => p.CriadoEm).HasDefaultValueSql("GETDATE()");

            e.HasOne(p => p.Usuario).WithMany(u => u.Pedidos).HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Cupom).WithMany().HasForeignKey(p => p.CupomId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(p => new { p.UsuarioId, p.CriadoEm });
            e.HasIndex(p => p.Status);
        });

        // ------------------------------------------------------------- PedidoItens
        // Guarda uma "foto" do produto no momento da compra (descrição, preço, cor):
        // se o produto mudar depois, o pedido antigo continua igual.
        modelBuilder.Entity<PedidoItem>(e =>
        {
            e.ToTable("PedidoItens", t =>
            {
                // A trigger desta tabela baixa o estoque dos produtos.
                t.HasTrigger(ObjetosDoBanco.TriggerPedidoItens);
                t.HasCheckConstraint("CK_PedidoItens_Quantidade", "[Quantidade] > 0");
            });
            e.HasKey(i => i.Id);
            e.Property(i => i.PrecoUnitario).HasPrecision(10, 2);
            e.Property(i => i.ProdutoDescricao).HasMaxLength(200).IsRequired();
            e.Property(i => i.VolumeLitros).HasPrecision(6, 2);
            e.Property(i => i.CorNome).HasMaxLength(80);
            e.Property(i => i.CorHex).HasMaxLength(7);

            e.HasOne(i => i.Pedido).WithMany(p => p.Itens).HasForeignKey(i => i.PedidoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Produto).WithMany().HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.Cor).WithMany().HasForeignKey(i => i.CorId).OnDelete(DeleteBehavior.Restrict);
        });

        // --------------------------------------------------------------- Favoritos
        // Cores favoritas do cliente. A chave é o par (usuário, cor): não repete.
        modelBuilder.Entity<Favorito>(e =>
        {
            e.ToTable("Favoritos");
            e.HasKey(f => new { f.UsuarioId, f.CorId });
            e.Property(f => f.CriadoEm).HasDefaultValueSql("GETDATE()");
            e.HasOne(f => f.Usuario).WithMany(u => u.Favoritos).HasForeignKey(f => f.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.Cor).WithMany().HasForeignKey(f => f.CorId).OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------------------------------------------------- Simulacoes
        // Projetos salvos pelo app: a foto da parede já pintada, a cor e o tipo de tinta.
        modelBuilder.Entity<Simulacao>(e =>
        {
            e.ToTable("Simulacoes");
            e.HasKey(s => s.Id);
            e.Property(s => s.Nome).HasMaxLength(80);
            e.Property(s => s.AreaM2).HasPrecision(10, 2);
            e.Property(s => s.CaminhoImagem).HasMaxLength(300).IsRequired();
            e.Property(s => s.CriadoEm).HasDefaultValueSql("GETDATE()");

            e.HasOne(s => s.Usuario).WithMany(u => u.Simulacoes).HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Cor).WithMany().HasForeignKey(s => s.CorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.TipoTinta).WithMany().HasForeignKey(s => s.TipoTintaId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(s => new { s.UsuarioId, s.CriadoEm });
        });
    }
}
