using System.ComponentModel.DataAnnotations.Schema;

namespace FixTintas.Modelos;

// Entidade: uma linha da tabela Produtos. É o item que se coloca no carrinho.
//
// Existem dois tipos de produto na mesma tabela:
//   TINTA     -> uma lata de uma MARCA, de um TIPO DE TINTA, em um VOLUME.
//                Ex.: "Suvinil Acrílica Fosco 3,6 L". A cor é escolhida na compra.
//                Tem TipoTintaId, MarcaId e VolumeLitros preenchidos.
//   ACESSÓRIO -> ferramentas e acessórios (rolo, pincel, lixa...). Sem cor.
//                TipoTintaId e VolumeLitros ficam vazios; pode ter um Tamanho ("23 cm").
// Cada volume é um produto separado, com preço e estoque próprios
// (a lata de 18 L não custa 5 vezes a de 3,6 L).
public class Produto
{
    public int Id { get; set; }

    // Código único do produto no estoque.
    public string Sku { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    // "/img/produtos/abc.jpg" (enviada pelo painel) ou "/img/tinta_coral_3.png".
    public string? ImagemUrl { get; set; }

    // Categoria ou subcategoria do menu do site.
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    // Obrigatória para tinta; opcional para acessório.
    public int? MarcaId { get; set; }
    public Marca? Marca { get; set; }

    // Preenchido = é tinta (exige cor no pedido).
    public int? TipoTintaId { get; set; }
    public TipoTinta? TipoTinta { get; set; }

    // Volume da lata em litros (só tinta). Múltiplo de 0,1.
    public decimal? VolumeLitros { get; set; }

    // Tamanho de acessório: "23 cm", "5 degraus", "400 ml"... (opcional).
    public string? Tamanho { get; set; }

    // decimal (e não double): dinheiro não pode ter erro de arredondamento.
    public decimal Preco { get; set; }

    // Preço "De" das ofertas (riscado no site). null = sem oferta.
    public decimal? PrecoDe { get; set; }

    // Diminui sozinho quando sai um pedido (trigger TR_PedidoItens_BaixaEstoque).
    public int Estoque { get; set; }

    // Abaixo (ou igual) a este número, o painel avisa "estoque baixo".
    public int EstoqueMinimo { get; set; } = 5;

    // Produto inativo some do site e do app (continua no painel).
    public bool Inativo { get; set; }

    // Controle de concorrência: o SQL Server muda este valor a cada alteração da linha.
    // Se duas pessoas alterarem o mesmo produto ao mesmo tempo, a segunda é avisada.
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Preenchida pelo banco (valor padrão GETDATE()).
    public DateTime DataCadastro { get; set; }

    // Preenchida pela trigger TR_Produtos_DataAtualizacao.
    public DateTime? DataAtualizacao { get; set; }

    // Não vira coluna ([NotMapped]): é só um atalho para "este produto é tinta?".
    [NotMapped]
    public bool EhTinta => TipoTintaId.HasValue;
}
