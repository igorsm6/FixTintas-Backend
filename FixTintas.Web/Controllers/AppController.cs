using FixTintas.Web.Infra;
using Microsoft.AspNetCore.Mvc;

namespace FixTintas.Web.Controllers;

// O app FixTintas para Android (o app Flutter) no site:
//   GET /App          página "Baixe o app": o que o app faz e como instalar
//   GET /App/Baixar   entrega o arquivo .apk (ou leva ao link externo, se configurado)
// Onde fica o arquivo e como trocar a versão: Infra/AppAndroid.cs.
public class AppController : Controller
{
    private readonly AppAndroid _app;

    public AppController(AppAndroid app)
    {
        _app = app;
    }

    // GET /App
    [HttpGet]
    public IActionResult Index()
    {
        return View(_app);
    }

    // GET /App/Baixar  (HEAD também: alguns gerenciadores de download perguntam o tamanho antes)
    [HttpGet, HttpHead]
    public IActionResult Baixar()
    {
        if (_app.TemLinkExterno)
            return Redirect(_app.LinkExterno!);

        if (!_app.ArquivoExiste)
        {
            TempData["Erro"] = "O aplicativo ainda não está disponível para download. Tente de novo mais tarde.";
            return RedirectToAction(nameof(Index));
        }

        // Com o tipo "aplicativo Android" e o nome FixTintas.apk, o celular oferece instalar
        // quando o download termina. enableRangeProcessing: se a internet cair no meio,
        // o navegador continua o download de onde parou.
        return PhysicalFile(_app.CaminhoDoArquivo, AppAndroid.TipoDoArquivo, AppAndroid.NomeDoArquivoBaixado, enableRangeProcessing: true);
    }
}
