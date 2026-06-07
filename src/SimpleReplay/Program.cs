using System.Windows.Forms;
using Velopack;

namespace SimpleReplay;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Must be the first call in Main for Velopack to handle install/update hooks
        VelopackApp.Build().Run();

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.Run(new TrayApplicationContext());
    }
}
