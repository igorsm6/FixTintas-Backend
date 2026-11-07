namespace FixTintas.Negocio;

// Confere se um arquivo enviado é mesmo uma imagem, olhando os primeiros bytes
// (a "assinatura" do formato). Não confia na extensão nem no nome do arquivo:
// alguém poderia renomear "virus.exe" para "foto.jpg".
public static class ValidadorDeImagem
{
    public const int TamanhoMaximoProduto = 2 * 1024 * 1024;     // 2 MB (imagem de produto)
    public const int TamanhoMaximoSimulacao = 10 * 1024 * 1024;  // 10 MB (foto do app)

    // Devolve ".jpg", ".png" ou ".webp", ou null se não for nenhum desses.
    public static string? DetectarExtensao(byte[] conteudo)
    {
        // JPEG: FF D8 FF
        if (conteudo.Length >= 3 && conteudo[0] == 0xFF && conteudo[1] == 0xD8 && conteudo[2] == 0xFF)
            return ".jpg";

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (conteudo.Length >= 8
            && conteudo[0] == 0x89 && conteudo[1] == 0x50 && conteudo[2] == 0x4E && conteudo[3] == 0x47
            && conteudo[4] == 0x0D && conteudo[5] == 0x0A && conteudo[6] == 0x1A && conteudo[7] == 0x0A)
            return ".png";

        // WEBP: "RIFF" ???? "WEBP"
        if (conteudo.Length >= 12
            && conteudo[0] == 'R' && conteudo[1] == 'I' && conteudo[2] == 'F' && conteudo[3] == 'F'
            && conteudo[8] == 'W' && conteudo[9] == 'E' && conteudo[10] == 'B' && conteudo[11] == 'P')
            return ".webp";

        return null;
    }

    // Valida tamanho e formato e devolve a extensão. Lança RegraDeNegocioException com a mensagem.
    public static string Validar(byte[]? conteudo, int tamanhoMaximo, bool aceitarWebp)
    {
        if (conteudo is null || conteudo.Length == 0)
            throw new RegraDeNegocioException("Escolha uma imagem.");

        if (conteudo.Length > tamanhoMaximo)
            throw new RegraDeNegocioException($"A imagem deve ter até {tamanhoMaximo / (1024 * 1024)} MB.");

        var extensao = DetectarExtensao(conteudo);
        if (extensao is null || (extensao == ".webp" && !aceitarWebp))
            throw new RegraDeNegocioException(aceitarWebp ? "Envie uma imagem JPG, PNG ou WEBP." : "Envie uma imagem JPEG ou PNG.");

        return extensao;
    }

    public static string TipoDeConteudo(string caminho) =>
        Path.GetExtension(caminho).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
}
