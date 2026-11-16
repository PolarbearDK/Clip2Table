using Clip2Table.Core.Settings;

namespace Clip2Table;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\Clip2Table.SingleInstance", out var createdNew);
        if (!createdNew)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        using var context = new TrayApplicationContext(new SettingsStore(SettingsStore.DefaultPath));
        Application.Run(context);
        GC.KeepAlive(mutex);
    }
}
