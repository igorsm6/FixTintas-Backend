namespace FixTintas.Modelos.Dtos;

// ============================================================================
// DTOs DOS PROJETOS DO APP ("Meus projetos": foto pintada + cor + tipo de tinta).
// ============================================================================

// SAÍDA: um projeto salvo. A imagem é privada: o app baixa com o token em "imagemUrl".
public class SimulacaoDto
{
    public int Id { get; set; }
    public DateTime CriadoEm { get; set; }
    public string? Nome { get; set; }
    public decimal? AreaM2 { get; set; }
    public int CorId { get; set; }
    public string CorNome { get; set; } = string.Empty;
    public string CorHex { get; set; } = string.Empty;
    public int TipoTintaId { get; set; }
    public string TipoTintaNome { get; set; } = string.Empty;
    public string ImagemUrl { get; set; } = string.Empty;

    // Precisa da Cor e do TipoTinta carregados (Include).
    public static SimulacaoDto De(Simulacao s) => new()
    {
        Id = s.Id,
        CriadoEm = s.CriadoEm,
        Nome = s.Nome,
        AreaM2 = s.AreaM2,
        CorId = s.CorId,
        CorNome = s.Cor?.Nome ?? string.Empty,
        CorHex = s.Cor?.CodigoHex ?? string.Empty,
        TipoTintaId = s.TipoTintaId,
        TipoTintaNome = s.TipoTinta?.Nome ?? string.Empty,
        ImagemUrl = $"/api/simulacoes/{s.Id}/imagem"
    };
}

// ENTRADA: o que o controller da Api monta a partir do formulário multipart do app
// (corId, tipoTintaId, nome, areaM2 e o arquivo "imagem").
public class NovaSimulacaoDto
{
    public int CorId { get; set; }
    public int TipoTintaId { get; set; }
    public string? Nome { get; set; }
    public decimal? AreaM2 { get; set; }
    public byte[] Imagem { get; set; } = Array.Empty<byte>();
}

// SAÍDA do Negocio para o controller devolver o arquivo da foto.
public class ArquivoImagemDto
{
    public string CaminhoFisico { get; set; } = string.Empty;
    public string TipoConteudo { get; set; } = "image/jpeg";
}
