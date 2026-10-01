using System.Globalization;
using System.Windows;
using System.Windows.Threading;

namespace Biblioteca.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var cultura = new CultureInfo("es-PE");
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        Thread.CurrentThread.CurrentCulture = cultura;
        Thread.CurrentThread.CurrentUICulture = cultura;

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        base.OnStartup(e);
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Dialogos.Error(e.Exception);
        e.Handled = true;
    }
}
