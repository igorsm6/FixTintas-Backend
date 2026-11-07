using System.ComponentModel.DataAnnotations;

namespace FixTintas.Modelos.Dtos;

// SAÍDA: um tipo de tinta. GET /api/tipos-tinta devolve
//   { "id", "nome", "acabamento", "descricao", "rendimentoM2PorLitro",
//     "demaosRecomendadas", "fatorContraste", "inativo" }
public class TipoTintaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Acabamento { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal RendimentoM2PorLitro { get; set; }
    public int DemaosRecomendadas { get; set; }
    public decimal FatorContraste { get; set; }
    public bool Inativo { get; set; }

    // "Acrílica Fosco"
    public string NomeCompleto => $"{Nome} {Acabamento}";

    public static TipoTintaDto De(TipoTinta t) => new()
    {
        Id = t.Id,
        Nome = t.Nome,
        Acabamento = t.Acabamento,
        Descricao = t.Descricao,
        RendimentoM2PorLitro = t.RendimentoM2PorLitro,
        DemaosRecomendadas = t.DemaosRecomendadas,
        FatorContraste = t.FatorContraste,
        Inativo = t.Inativo
    };
}

// ENTRADA: cadastro e edição de tipo de tinta.
public class TipoTintaSalvarDto
{
    [Display(Name = "Nome")]
    [Required(ErrorMessage = "Informe o nome do tipo de tinta.")]
    [StringLength(80, ErrorMessage = "O nome pode ter no máximo 80 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "Acabamento")]
    [Required(ErrorMessage = "Escolha o acabamento.")]
    [StringLength(30)]
    public string Acabamento { get; set; } = Acabamentos.Fosco;

    [Display(Name = "Descrição")]
    [StringLength(300, ErrorMessage = "A descrição pode ter no máximo 300 caracteres.")]
    public string? Descricao { get; set; }

    [Display(Name = "Rendimento (m² por litro, 1 demão)")]
    [Required(ErrorMessage = "Informe o rendimento.")]
    [Range(1, 50, ErrorMessage = "O rendimento deve estar entre 1 e 50 m² por litro.")]
    public decimal RendimentoM2PorLitro { get; set; } = 10m;

    [Display(Name = "Demãos recomendadas")]
    [Required(ErrorMessage = "Informe as demãos recomendadas.")]
    [Range(1, 6, ErrorMessage = "As demãos recomendadas devem ser de 1 a 6.")]
    public int DemaosRecomendadas { get; set; } = 2;

    [Display(Name = "Fator de contraste (simulação)")]
    [Required(ErrorMessage = "Informe o fator de contraste.")]
    [Range(0.5, 1.5, ErrorMessage = "O fator de contraste deve estar entre 0,5 e 1,5.")]
    public decimal FatorContraste { get; set; } = 0.95m;

    [Display(Name = "Inativo")]
    public bool Inativo { get; set; }

    public TipoTinta ParaTipoTinta(int id = 0) => new()
    {
        Id = id,
        Nome = Nome,
        Acabamento = Acabamento,
        Descricao = Descricao,
        RendimentoM2PorLitro = RendimentoM2PorLitro,
        DemaosRecomendadas = DemaosRecomendadas,
        FatorContraste = FatorContraste,
        Inativo = Inativo
    };

    public static TipoTintaSalvarDto De(TipoTinta t) => new()
    {
        Nome = t.Nome,
        Acabamento = t.Acabamento,
        Descricao = t.Descricao,
        RendimentoM2PorLitro = t.RendimentoM2PorLitro,
        DemaosRecomendadas = t.DemaosRecomendadas,
        FatorContraste = t.FatorContraste,
        Inativo = t.Inativo
    };
}
