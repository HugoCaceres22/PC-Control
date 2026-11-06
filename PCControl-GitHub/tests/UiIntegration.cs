using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Text.Json;
using System.Windows.Interop;
using System.Windows.Threading;
using PCControl;
public static class UiIntegration
{
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o)!;
    static void Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,args);
    public static void Run(string root)
    {
        Exception? failure=null;var thread=new Thread(()=>{
            var app=new Application();app.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("/PCControl;component/Theme.xaml",UriKind.Relative)});var config=new ConfigStore(Path.Combine(root,"ui.json"));var w=new MainWindow(config);
            app.DispatcherUnhandledException+=(_,e)=>{failure=e.Exception;e.Handled=true;w.Close();};
            w.Show();
            _=w.Dispatcher.InvokeAsync(async()=>{
                try{
                    for(int wait=0;wait<200&&!Field<Dictionary<string,Metric>>(w,"latest").ContainsKey("ram.total");wait++)await Task.Delay(100);
                    if(!Field<Dictionary<string,Metric>>(w,"latest").ContainsKey("ram.total"))throw new Exception("Los sensores no se iniciaron: "+Field<TextBlock>(w,"notice").Text);
                    void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("UI OK "+text);}
                    var sensors=Field<SensorService>(w,"sensors");var settings=Field<Settings>(w,"settings");var tabs=Field<TabControl>(w,"tabs");
                    Check(tabs.Items.Count==4,"cuatro pantallas creadas");Check(Field<Grid>(w,"dashboard").Children.Count==Defaults.Panel().Count,"rejilla inicial completa");
                    Check(Field<Dictionary<string,Metric>>(w,"latest")["ram.total"].Value>0,"métricas reales en ventana");
                    var themeSwitch=Field<ToggleButton>(w,"gamingThemeButton");
                    var originalProfile=JsonSerializer.Serialize(settings.Current);
                    var originalLayout=Field<Grid>(w,"dashboard").Children.Cast<UIElement>().Select(x=>(Grid.GetRow(x),Grid.GetColumn(x),Grid.GetRowSpan(x),Grid.GetColumnSpan(x))).ToArray();
                    var originalHistory=Field<Dictionary<string,List<double?>>>(w,"history");
                    themeSwitch.IsChecked=true;
                    Check(settings.GamingTheme&&JsonSerializer.Serialize(settings.Current)==originalProfile,"tema gaming conserva métricas, tamaño, colores y estilos guardados");
                    Check(originalLayout.SequenceEqual(Field<Grid>(w,"dashboard").Children.Cast<UIElement>().Select(x=>(Grid.GetRow(x),Grid.GetColumn(x),Grid.GetRowSpan(x),Grid.GetColumnSpan(x)))) ,"tema gaming conserva posición exacta de tarjetas");
                    Check(Field<List<(Tile tile,TextBlock value,Sparkline graph)>>(w,"views").All(x=>x.graph.Gaming)&&ReferenceEquals(originalHistory,Field<Dictionary<string,List<double?>>>(w,"history")),"gráficos gaming sin perder curvas temporales");
                    Check(config.Load().GamingTheme,"interruptor ON se guarda y recarga");
                    themeSwitch.IsChecked=false;
                    Check(!settings.GamingTheme&&Field<List<(Tile tile,TextBlock value,Sparkline graph)>>(w,"views").All(x=>!x.graph.Gaming&&x.graph.ChartStyle==x.tile.Style),"OFF recupera estilos personalizados");
                    Check(Field<OverlayWindow?>(w,"overlay")==null&&!Field<PresentMonService>(w,"fps").Running,"cambio visual mantiene estado de overlay y captura");
                    settings.Current.Alerts.Add(new(){Metric="ram.load",Threshold=0,Channel="Panel"});await Task.Delay(1800);Check(Field<TextBlock>(w,"notice").Text.Contains("ALERTA"),"alerta real visible en panel");settings.Current.Alerts.Clear();
                    var combo=Field<ComboBox>(w,"profileSelector");combo.SelectedIndex=2;Check(settings.Current.Name=="Minimalista"&&Field<Grid>(w,"dashboard").Children.Count==2,"cambio de perfil actualiza tarjetas");combo.SelectedIndex=0;
                    int initial=settings.Current.Dashboard.Count;settings.Current.Dashboard.Add(new(){Metric="ram.total",Width=3,Height=2});Call(w,"RenderCards",false);Check(Field<Grid>(w,"dashboard").Children.Count==initial+1,"añadir tarjeta y tamaño múltiple");settings.Current.Dashboard.RemoveAt(initial);Call(w,"RenderCards",false);
                    Call(w,"Rebuild");Call(w,"Rebuild");Check(tabs.Items.Count==4,"reconstrucción repetida sin error de parent");
                    Call(w,"ToggleOverlay");var overlay=Field<OverlayWindow>(w,"overlay");Check(overlay.IsVisible&&overlay.Topmost&&!overlay.ShowInTaskbar,"overlay visible superior sin barra de tareas");
                    settings.Current.Overlay=[new(){Metric="ram.used"}];overlay.Configure(settings.Current);w.WindowState=WindowState.Minimized;await Task.Delay(3500);
                    Check(sensors.ReadCount>1&&Field<Dictionary<string,Metric>>(w,"latest")["ram.used"].Value>0,"minimizado con overlay lee RAM");Check(Field<Dictionary<string,Metric>>(w,"latest")["cpu.load"].Value==null,"minimizado omite CPU no seleccionada");
                    Call(w,"ToggleOverlay");await Task.Delay(1000);long reads=sensors.ReadCount;await Task.Delay(3500);Check(sensors.ReadCount==reads,"minimizado sin overlay suspende sensores");
                    w.WindowState=WindowState.Normal;await Task.Delay(2000);Check(sensors.ReadCount>reads,"restaurar reanuda sensores");
                    var pm=Field<PresentMonService>(w,"fps");string exe=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../tools/PresentMon.exe"));
                    try{pm.Start(exe,Environment.ProcessId);await Task.Delay(4000);Console.WriteLine("PRESENTMON REAL: running="+pm.Running+" fps="+pm.Fps+" status="+pm.Status);pm.Stop();Check(!pm.Running&&pm.Fps==null,"PresentMon detenido sin métricas residuales");}catch(Exception e){Console.WriteLine("PRESENTMON LIMITACIÓN: "+e.Message);}
                    Call(w,"ToggleOverlay");Call(w,"Save");var roundtrip=config.Load();Check(roundtrip.Current.Overlay.Count==1,"configuración UI guardada y recargada");
                    w.Close();await Task.Delay(200);Check(!overlay.IsVisible&&!pm.Running,"cierre de ventana cierra overlay y FPS");
                }catch(Exception e){failure=e;Console.WriteLine("UI ERROR "+e);w.Close();}
            });
            app.Run();
        });thread.SetApartmentState(ApartmentState.STA);thread.Start();if(!thread.Join(TimeSpan.FromSeconds(50)))throw new Exception("Timeout integración WPF");if(failure!=null)throw failure;Console.WriteLine("INTEGRACIÓN WPF CORRECTA");
    }
}

