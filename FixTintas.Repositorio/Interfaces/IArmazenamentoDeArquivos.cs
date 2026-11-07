namespace FixTintas.Repositorio.Interfaces;

// Onde os arquivos enviados (imagens) são guardados no disco.
// É um "repositório de arquivos": o Negocio pede para salvar e recebe o caminho,
// sem saber em que pasta do computador o arquivo foi parar.
//   Web: pasta wwwroot (imagens dos produtos, que o site mostra).
//   Api: pasta App_Data (fotos dos projetos do app, que são privadas).
public interface IArmazenamentoDeArquivos
{
    // Salva com um nome novo (gerado aqui) e devolve o caminho relativo,
    // por exemplo "img/produtos/3f2a...c1.jpg".
    string Salvar(string pasta, string extensao, byte[] conteudo);

    // Caminho completo no disco de um arquivo que EXISTE. Devolve null se o arquivo
    // não existe ou se o caminho tentar sair da pasta permitida ("..").
    string? CaminhoFisico(string caminhoRelativo);

    // Apaga o arquivo, se existir. Nunca dá erro.
    void Excluir(string? caminhoRelativo);
}
