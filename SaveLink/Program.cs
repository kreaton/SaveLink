using Velopack;

namespace SaveLink;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Must run first: handles Velopack's install/update/uninstall hooks and may exit the process.
        VelopackApp.Build().Run();

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
