using NcaaTranslator.Library;
using Photino.NET;
using Photino.NET.Server;

namespace NcaaTranslator.Desktop;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var appDir = AppContext.BaseDirectory;
        Settings.BaseDirectory = appDir;
        NameConverters.BaseDirectory = appDir;

        // PhotinoServer resolves "wwwroot" against CWD. Pin to the exe dir so
        // `dotnet run` serves output wwwroot, not the project-tree copy.
        Directory.SetCurrentDirectory(appDir);

        PhotinoServer
            .CreateStaticFileServer(args, out var baseUrl)
            .RunAsync();

        var window = new PhotinoWindow()
            .SetTitle("NCAA Translator")
            .SetUseOsDefaultSize(false)
            .SetSize(1000, 600)
            .Center()
            .RegisterWebMessageReceivedHandler((sender, message) =>
            {
                var photino = (PhotinoWindow)sender!;
                photino.SendWebMessage(Bridge.Handle(message));
            })
            .Load($"{baseUrl}/index.html");

        window.WaitForClose();
    }
}
