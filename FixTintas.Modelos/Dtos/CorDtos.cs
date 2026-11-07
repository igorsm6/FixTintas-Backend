using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace FixTintas.Modelos.Dtos;

// Componentes de uma cor (0 a 255), usados pelo algoritmo de pintura do app.
public class RgbDto
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }

    // "#F8F8F5" -> (248, 248, 245). HEX inválido vira preto, sem derrubar nada.
    public static RgbDto DeHex(string? hex)
    {
        var limpo = (hex ?? string.Empty).TrimStart('#');
        if (limpo.Length != 6
            || !int.TryParse(limpo[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
            || !int.TryParse(limpo.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
            || !int.TryParse(limpo.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            return new RgbDto();

        return new RgbDto { R = r, G = g, B = b };
    }
}

// SAÍDA: uma cor. GET /api/cores devolve
//   { "id", "nome", "codigoHex", "rgb": { "r", "g", "b" }, "familia", "codigoPantone", "inativa" }
public class CorDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string CodigoHex { get; set; } = string.Empty;
    public RgbDto Rgb { get; set; } = new();
    public string? Familia { get; set; }
    public string? CodigoPantone { get; set; }
    public bool Inativa { get; set; }

    public static CorDto De(Cor c) => new()
    {
        Id = c.Id,
        Nome = c.Nome,
        CodigoHex = c.CodigoHex,
        Rgb = RgbDto.DeHex(c.CodigoHex),
        Familia = c.Familia,
        CodigoPantone = c.CodigoPantone,
        Inativa = c.Inativa
    };
}

// ENTRADA: cadastro e edição de cor.
public class CorSalvarDto
{
    [Display(Name = "Nome")]
    [Required(ErrorMessage = "Informe o nome da cor.")]
    [StringLength(80, ErrorMessage = "O nome pode ter no máximo 80 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "Código HEX")]
    [Required(ErrorMessage = "Informe o código HEX.")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "O código HEX deve estar no formato #RRGGBB.")]
    public string CodigoHex { get; set; } = "#FFFFFF";

    [Display(Name = "Família")]
    [StringLength(40, ErrorMessage = "A família pode ter no máximo 40 caracteres.")]
    public string? Familia { get; set; }

    [Display(Name = "Código Pantone")]
    [StringLength(40, ErrorMessage = "O código Pantone pode ter no máximo 40 caracteres.")]
    public string? CodigoPantone { get; set; }

    [Display(Name = "Inativa")]
    public bool Inativa { get; set; }

    public Cor ParaCor(int id = 0) => new()
    {
        Id = id,
        Nome = Nome,
        CodigoHex = CodigoHex,
        Familia = Familia,
        CodigoPantone = CodigoPantone,
        Inativa = Inativa
    };

    public static CorSalvarDto De(Cor c) => new()
    {
        Nome = c.Nome,
        CodigoHex = c.CodigoHex,
        Familia = c.Familia,
        CodigoPantone = c.CodigoPantone,
        Inativa = c.Inativa
    };
}
