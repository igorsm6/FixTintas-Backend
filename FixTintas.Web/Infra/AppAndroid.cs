using FixTintas.Modelos;

namespace FixTintas.Web.Infra;

// O APLICATIVO PARA ANDROID (o app Flutter) que o site oferece para download.
//
// Onde o site mostra o app:
//   - /App                         página "Baixe o app" (o que o app faz e como instalar)
//   - /App/Baixar                  o download do arquivo .apk
//   - Views/Shared/_JanelaApp      janela que aparece para quem abre o site num celular Android
//   - Views/Shared/_FaixaApp       faixa "Teste as cores na sua parede" da página inicial
//   - rodapé e página de cores     link "Baixe o app"
//
// Configuração: seção "AppAndroid" do appsettings.json.
//   "Arquivo"       onde fica o .apk, a partir da pasta do projeto Web (padrão: Aplicativo/FixTintas.apk).
//                   Para publicar uma versão nova do app, é só trocar esse arquivo (nem precisa parar o site).
//   "LinkExterno"   opcional: se preenchido (Google Drive, GitHub...), o botão leva para esse link.
//   "Versao"        só para mostrar na página.
//   "AndroidMinimo" só para mostrar na página (o app pede Android 7.0 ou mais novo).
//
// AddSingleton no Program.cs: um objeto só para o site inteiro. O arquivo é conferido
// a cada pedido, então o botão aparece assim que o .apk é colocado na pasta.
public class AppAndroid
{
    public const string NomeDoArquivoBaixado = "FixTintas.apk";

    // Tipo de arquivo de aplicativo Android: com ele o celular entende que é para instalar.
    public const string TipoDoArquivo = "application/vnd.android.package-archive";

    public string CaminhoDoArquivo { get; }
    public string? LinkExterno { get; }
    public string Versao { get; }
    public string AndroidMinimo { get; }

    // Em desenvolvimento, a página /App explica para a equipe onde colocar o .apk.
    public bool AmbienteDeDesenvolvimento { get; }

    public AppAndroid(IConfiguration configuracao, IWebHostEnvironment ambiente)
    {
        var secao = configuracao.GetSection("AppAndroid");

        var arquivo = secao["Arquivo"];
        if (string.IsNullOrWhiteSpace(arquivo)) arquivo = Path.Combine("Aplicativo", NomeDoArquivoBaixado);
        CaminhoDoArquivo = Path.GetFullPath(Path.Combine(ambiente.ContentRootPath, arquivo));

        var link = secao["LinkExterno"];
        LinkExterno = string.IsNullOrWhiteSpace(link) ? null : link.Trim();

        Versao = string.IsNullOrWhiteSpace(secao["Versao"]) ? "1.0.0" : secao["Versao"]!.Trim();
        AndroidMinimo = string.IsNullOrWhiteSpace(secao["AndroidMinimo"]) ? "7.0" : secao["AndroidMinimo"]!.Trim();
        AmbienteDeDesenvolvimento = ambiente.IsDevelopment();
    }

    public bool TemLinkExterno => LinkExterno is not null;

    public bool ArquivoExiste => File.Exists(CaminhoDoArquivo);

    // Dá para baixar? (link externo configurado ou o .apk na pasta)
    public bool Disponivel => TemLinkExterno || ArquivoExiste;

    // "56 MB" (só quando o arquivo está na pasta; com link externo o tamanho não é conhecido).
    public string? Tamanho
    {
        get
        {
            if (TemLinkExterno || !ArquivoExiste) return null;
            var megabytes = new FileInfo(CaminhoDoArquivo).Length / 1024m / 1024m;
            return Formatacao.Numero(megabytes < 10 ? Math.Round(megabytes, 1) : Math.Round(megabytes)) + " MB";
        }
    }

    // "Grátis · Versão 1.0.0 · 56 MB · Android 7.0 ou superior"
    public string Resumo(bool comVersao = true)
    {
        var partes = new List<string> { "Grátis" };
        if (comVersao) partes.Add($"Versão {Versao}");
        if (Tamanho is not null) partes.Add(Tamanho);
        partes.Add($"Android {AndroidMinimo} ou superior");
        return string.Join(" · ", partes);
    }
}
