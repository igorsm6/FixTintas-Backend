using FixTintas.Repositorio.Interfaces;

namespace FixTintas.Repositorio;

// Guarda arquivos numa pasta do disco (a "raiz", definida no Program.cs).
public class ArmazenamentoDeArquivos : IArmazenamentoDeArquivos
{
    private readonly string _raiz;

    public ArmazenamentoDeArquivos(string raiz)
    {
        _raiz = Path.GetFullPath(raiz);
        Directory.CreateDirectory(_raiz);
    }

    public string Salvar(string pasta, string extensao, byte[] conteudo)
    {
        var pastaRelativa = pasta.Replace('\\', '/').Trim('/');
        var destino = Path.Combine(_raiz, pastaRelativa);
        Directory.CreateDirectory(destino);

        // O nome do arquivo é sempre gerado aqui: nunca usamos o nome enviado pelo usuário.
        var nome = $"{Guid.NewGuid():N}{extensao}";
        using (var arquivo = new FileStream(Path.Combine(destino, nome), FileMode.CreateNew))
        {
            arquivo.Write(conteudo, 0, conteudo.Length);
        }

        return $"{pastaRelativa}/{nome}";
    }

    public string? CaminhoFisico(string caminhoRelativo)
    {
        if (string.IsNullOrWhiteSpace(caminhoRelativo)) return null;

        var relativo = caminhoRelativo.Replace('\\', '/').TrimStart('/');
        var completo = Path.GetFullPath(Path.Combine(_raiz, relativo));
        var raizComBarra = _raiz.EndsWith(Path.DirectorySeparatorChar) ? _raiz : _raiz + Path.DirectorySeparatorChar;

        // Impede caminhos como "../../Windows/arquivo": só vale o que estiver dentro da raiz.
        if (!completo.StartsWith(raizComBarra, StringComparison.OrdinalIgnoreCase)) return null;

        return File.Exists(completo) ? completo : null;
    }

    public void Excluir(string? caminhoRelativo)
    {
        if (string.IsNullOrWhiteSpace(caminhoRelativo)) return;

        var caminho = CaminhoFisico(caminhoRelativo);
        if (caminho is null) return;

        try
        {
            File.Delete(caminho);
        }
        catch (IOException)
        {
            // Arquivo em uso: sobra um arquivo solto, sem afetar o sistema.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
