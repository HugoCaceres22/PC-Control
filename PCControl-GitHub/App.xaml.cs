using System.Windows;
namespace PCControl;
public partial class App : Application
{
    private System.Threading.Mutex? singleton;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        singleton = new(true, "Local\\PCControl.Desktop", out bool fresh);
        if (!fresh) { MessageBox.Show("PC Control ya está abierto."); Shutdown(); return; }
        DispatcherUnhandledException += (_, a) => { MessageBox.Show(a.Exception.Message, "PC Control · Error"); a.Handled = true; };
        new MainWindow().Show();
    }
    protected override void OnExit(ExitEventArgs e) { singleton?.Dispose(); base.OnExit(e); }
}
