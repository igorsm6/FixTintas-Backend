namespace FixTintas.Modelos;

// Entidade: uma linha da tabela Categorias (o menu lateral do site).
// Categoria e subcategoria ficam na mesma tabela (autorrelacionamento):
//   Exterior   -> CategoriaPaiId = null           Slug = "exterior"
//   └ Vernizes -> CategoriaPaiId = Id do Exterior  Slug = "exterior-vernizes"
// O Slug é o "nome de URL": /Produtos?categoria=exterior-vernizes
public class Categoria
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    // null = categoria principal; preenchido = subcategoria.
    public int? CategoriaPaiId { get; set; }
    public Categoria? CategoriaPai { get; set; }

    public List<Categoria> Subcategorias { get; set; } = new();

    // Posição no menu (menor aparece primeiro).
    public int Ordem { get; set; }

    // Categoria desativada some do site junto com os produtos dela.
    public bool Inativa { get; set; }
}
