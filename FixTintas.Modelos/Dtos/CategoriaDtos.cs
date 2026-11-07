using System.ComponentModel.DataAnnotations;

namespace FixTintas.Modelos.Dtos;

// SAÍDA: uma categoria (com as subcategorias, quando carregadas).
// Usada no menu do site, no painel e em GET /api/categorias.
public class CategoriaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? CategoriaPaiId { get; set; }
    public string? CategoriaPaiNome { get; set; }
    public int Ordem { get; set; }
    public bool Inativa { get; set; }
    public List<CategoriaDto> Subcategorias { get; set; } = new();

    // "Exterior › Vernizes" (ou só "Exterior", se for principal).
    public string NomeCompleto => CategoriaPaiNome is null ? Nome : $"{CategoriaPaiNome} › {Nome}";

    public static CategoriaDto De(Categoria c) => new()
    {
        Id = c.Id,
        Nome = c.Nome,
        Slug = c.Slug,
        CategoriaPaiId = c.CategoriaPaiId,
        CategoriaPaiNome = c.CategoriaPai?.Nome,
        Ordem = c.Ordem,
        Inativa = c.Inativa,
        Subcategorias = c.Subcategorias
            .OrderBy(s => s.Ordem).ThenBy(s => s.Nome)
            .Select(s => new CategoriaDto
            {
                Id = s.Id,
                Nome = s.Nome,
                Slug = s.Slug,
                CategoriaPaiId = c.Id,
                CategoriaPaiNome = c.Nome,
                Ordem = s.Ordem,
                Inativa = s.Inativa
            })
            .ToList()
    };
}

// ENTRADA: cadastro e edição de categoria (painel e /api/admin/categorias).
public class CategoriaSalvarDto
{
    [Display(Name = "Nome")]
    [Required(ErrorMessage = "Informe o nome da categoria.")]
    [StringLength(80, ErrorMessage = "O nome pode ter no máximo 80 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    // Opcional: se ficar vazio, é gerado a partir do nome ("Tintas Piso" -> "exterior-tintas-piso").
    [Display(Name = "Endereço (slug)")]
    [StringLength(80, ErrorMessage = "O slug pode ter no máximo 80 caracteres.")]
    public string? Slug { get; set; }

    // Vazio = categoria principal.
    [Display(Name = "Categoria principal")]
    public int? CategoriaPaiId { get; set; }

    [Display(Name = "Ordem no menu")]
    [Required(ErrorMessage = "Informe a ordem no menu.")]
    [Range(0, 999, ErrorMessage = "A ordem deve ser de 0 a 999.")]
    public int Ordem { get; set; }

    [Display(Name = "Inativa (não aparece no site)")]
    public bool Inativa { get; set; }

    public Categoria ParaCategoria(int id = 0) => new()
    {
        Id = id,
        Nome = Nome,
        Slug = Slug ?? string.Empty,
        CategoriaPaiId = CategoriaPaiId,
        Ordem = Ordem,
        Inativa = Inativa
    };

    public static CategoriaSalvarDto De(Categoria c) => new()
    {
        Nome = c.Nome,
        Slug = c.Slug,
        CategoriaPaiId = c.CategoriaPaiId,
        Ordem = c.Ordem,
        Inativa = c.Inativa
    };
}
