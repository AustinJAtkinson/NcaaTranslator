using NcaaTranslator.Library;
using Photino.NET;

namespace NcaaTranslator.Desktop;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var appDir = AppContext.BaseDirectory;
        Settings.BaseDirectory = appDir;
        NameConverters.BaseDirectory = appDir;

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
            .Load("wwwroot/index.html");

        window.WaitForClose();
    }
}
