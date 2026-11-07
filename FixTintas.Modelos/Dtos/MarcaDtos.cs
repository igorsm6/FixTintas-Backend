using System.ComponentModel.DataAnnotations;

namespace FixTintas.Modelos.Dtos;

// SAÍDA: uma marca. GET /api/marcas devolve { "id", "nome", "descricao", "inativa" }.
public class MarcaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Inativa { get; set; }

    public static MarcaDto De(Marca m) => new()
    {
        Id = m.Id,
        Nome = m.Nome,
        Descricao = m.Descricao,
        Inativa = m.Inativa
    };
}

// ENTRADA: cadastro e edição de marca.
public class MarcaSalvarDto
{
    [Display(Name = "Nome")]
    [Required(ErrorMessage = "Informe o nome da marca.")]
    [StringLength(80, ErrorMessage = "O nome pode ter no máximo 80 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "Descrição")]
    [StringLength(300, ErrorMessage = "A descrição pode ter no máximo 300 caracteres.")]
    public string? Descricao { get; set; }

    [Display(Name = "Inativa (não aparece no site nem no app)")]
    public bool Inativa { get; set; }

    public Marca ParaMarca(int id = 0) => new()
    {
        Id = id,
        Nome = Nome,
        Descricao = Descricao,
        Inativa = Inativa
    };

    public static MarcaSalvarDto De(Marca m) => new()
    {
        Nome = m.Nome,
        Descricao = m.Descricao,
        Inativa = m.Inativa
    };
}
