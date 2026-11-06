using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
namespace PCControl;
public sealed class MainWindow : Window
{
    readonly ConfigStore store;
    readonly Settings settings;
    readonly SensorService sensors=new();
    readonly PresentMonService fps=new();
    readonly AlertEngine alerts=new();
    readonly DispatcherTimer timer=new();
    readonly TabControl tabs=new();
    readonly Grid dashboard=new(),overlayEditor=new();
    readonly TextBlock status=Ui.Text("Iniciando sensores…",12),notice=Ui.Text("",13,"#FCBE76");
    readonly ComboBox profileSelector=new();
    readonly Button overlayButton;
    readonly ToggleButton gamingThemeButton;
    readonly GamingBackdrop backdrop=new(){IsHitTestVisible=false};
    readonly TextBlock brandTitle=Ui.Text("PC CONTROL",27,"#F2F5FF"),brandSubtitle=Ui.Text("TU EQUIPO, EN TIEMPO REAL",10,"#51E3D5");
    readonly Dictionary<string,List<double?>> history=[];
    readonly List<(Tile tile,TextBlock value,Sparkline graph)> views=[];
    readonly List<(Tile tile,TextBlock value,Sparkline graph)> gamingViews=[];
    Dictionary<string,Metric> latest=[];
    OverlayWindow? overlay;
    bool closing,busy,editing,overlayEditing=true,building;
    int columns=4;
    string? capturePath;
    int capturePid;
    bool captureRequested;
    Profile Current=>settings.Current;
    public MainWindow(ConfigStore? config=null)
    {
        store=config??new();
        settings=store.Load();Title="PC Control";Icon=BrandAssets.Logo;Width=1260;Height=840;MinWidth=850;MinHeight=600;
        Background=Ui.Brush("#101219");Foreground=Ui.Brush("#EEF1FA");FontFamily=new("Segoe UI");FontSize=14;
        overlayButton=Ui.Button("◉  Activar overlay",ToggleOverlay);
        gamingThemeButton=new ToggleButton{Content="Modo gaming",IsChecked=settings.GamingTheme,Style=(Style)FindResource("GamingThemeToggle"),ToolTip="Cambia el estilo visual conservando métricas y distribución"};
        gamingThemeButton.Checked+=(_,_)=>ToggleGamingTheme();gamingThemeButton.Unchecked+=(_,_)=>ToggleGamingTheme();
        var root=new DockPanel{Margin=new(28,18,28,14)};var shell=new Grid();shell.Children.Add(backdrop);shell.Children.Add(root);Content=shell;
        brandTitle.TextWrapping=TextWrapping.NoWrap;brandTitle.TextTrimming=TextTrimming.CharacterEllipsis;
        var brandText=new StackPanel();brandText.Children.Add(brandTitle);brandText.Children.Add(brandSubtitle);
        var brand=new DockPanel{VerticalAlignment=VerticalAlignment.Center};var logo=new Image{Source=BrandAssets.Logo,Width=48,Height=48,Margin=new(0,0,12,0),Stretch=Stretch.Uniform,ToolTip="PC Control"};DockPanel.SetDock(logo,Dock.Left);brand.Children.Add(logo);brand.Children.Add(brandText);
        var head=new DockPanel{Margin=new(0,0,0,16)};DockPanel.SetDock(head,Dock.Top);root.Children.Add(head);
        var right=Ui.Row(profileSelector,Ui.Button("Perfiles",ManageProfiles),overlayButton,gamingThemeButton);DockPanel.SetDock(right,Dock.Right);head.Children.Add(right);head.Children.Add(brand);
        var footer=new StackPanel();footer.Children.Add(notice);footer.Children.Add(status);DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);root.Children.Add(tabs);
        profileSelector.SelectionChanged+=(_,_)=>{if(building||profileSelector.SelectedIndex<0)return;settings.Active=profileSelector.SelectedIndex;alerts.Reset();history.Clear();Save();Rebuild();RegisterShortcut();overlay?.Configure(Current);};
        Loaded+=async(_,_)=> {busy=true;try{await Task.Run(sensors.Open);if(closing)return;latest=await Task.Run(()=>sensors.Read());if(closing)return;Rebuild();RegisterShortcut();notice.Text=store.Warning??sensors.Diagnostic;timer.Start();}catch(Exception e){notice.Text=e.Message;}finally{busy=false;}};
        timer.Interval=TimeSpan.FromMilliseconds(settings.Interval);timer.Tick+=async(_,_)=>await Tick();
        SourceInitialized+=(_,_)=>{var handle=new WindowInteropHelper(this).Handle;HwndSource.FromHwnd(handle).AddHook(WindowHook);int dark=1;DwmSetWindowAttribute(handle,20,ref dark,sizeof(int));};
        StateChanged+=(_,_)=>{sensorsReset=true;UpdateCapturePolicy();status.Text=WindowState==WindowState.Minimized&&overlay==null?"En pausa: ventana minimizada sin overlay":"Monitorización manual activa";};
        SizeChanged+=(_,_)=>{int n=ActualWidth<1050?3:4;if(n!=columns){columns=n;RenderCards(false);}};
        Closing+=(_,e)=>{if(closing)return;closing=true;timer.Stop();fps.Stop();overlay?.Close();overlay=null;UnregisterHotKey(new WindowInteropHelper(this).Handle,1);Save();};
        Closed+=async(_,_)=>{try{while(busy)await Task.Delay(25);await Task.Run(sensors.Dispose);}catch(Exception ex){Debug.WriteLine("Liberación de sensores: "+ex.Message);}finally{try{fps.Dispose();}finally{Application.Current.Shutdown();}}};
        Application.Current.ShutdownMode=ShutdownMode.OnExplicitShutdown;
        Rebuild();
        ApplyVisualTheme();
    }
    void ToggleGamingTheme(){settings.GamingTheme=gamingThemeButton.IsChecked==true;ApplyVisualTheme();Save();}
    void ApplyVisualTheme()
    {
        bool on=settings.GamingTheme;
        foreach(var (key,normal,gamer) in new[]{("Accent","#51E3D5","#C1FF38"),("SurfaceBackground","#191D2A","#111426"),("ButtonBackground","#272C40","#28213E"),("ButtonHover","#41496A","#54305C"),("InputBackground","#242A3D","#1F1934"),("BorderColor","#414860","#6A3570"),("TabBackground","#101219","#0B0B18")})Resources[key]=Ui.Brush(on?gamer:normal);
        Background=on?new LinearGradientBrush(Color.FromRgb(7,11,19),Color.FromRgb(29,9,35),35):Ui.Brush("#101219");
        brandTitle.Foreground=Ui.Brush(on?"#C1FF38":"#F2F5FF");brandTitle.FontFamily=new(on?"Bahnschrift":"Segoe UI");brandTitle.FontWeight=on?FontWeights.Bold:FontWeights.Normal;
        brandSubtitle.Foreground=Ui.Brush(on?"#FF63BF":"#51E3D5");
        backdrop.Enabled=on;backdrop.InvalidateVisual();
        RenderCards(false);RenderCards(true);
    }
    static string GamingColor(string metric)=>metric.StartsWith("cpu",StringComparison.OrdinalIgnoreCase)?"#C1FF38":metric.Contains("gpu",StringComparison.OrdinalIgnoreCase)?"#FF4BA8":metric.StartsWith("ram",StringComparison.OrdinalIgnoreCase)?"#38E7FF":metric.StartsWith("disk",StringComparison.OrdinalIgnoreCase)?"#FFC857":"#AB8AFF";
    bool sensorsReset;
    void Save(){try{store.Save(settings);}catch(Exception e){notice.Text="No se pudo guardar: "+e.Message;}}
    async Task Tick()
    {
        if(busy||closing)return;
        if(WindowState==WindowState.Minimized&&overlay==null)return;
        busy=true;
        try {
            HashSet<string>? needed=WindowState==WindowState.Minimized?Current.Overlay.Select(t=>t.Metric).ToHashSet():null;
            var data=await Task.Run(()=>{if(sensorsReset){sensors.ResetBaselines();sensorsReset=false;}return sensors.Read(needed);});
            if(closing)return; data["fps"]=sensors.Catalog["fps"] with {Value=fps.Fps,Source="PresentMon"};latest=data;
            foreach(var m in data.Values){if(needed!=null&&!needed.Contains(m.Id))continue;if(!history.TryGetValue(m.Id,out var list))history[m.Id]=list=[];list.Add(m.Value);int max=Math.Max(2,60000/settings.Interval);if(list.Count>max)list.RemoveRange(0,list.Count-max);}
            if(WindowState!=WindowState.Minimized){foreach(var (t,v,g) in tabs.SelectedIndex==1?gamingViews:views){v.Text=data.TryGetValue(t.Metric,out var m)?m.Display:"No disponible";g.Values=history.GetValueOrDefault(t.Metric)??[];g.InvalidateVisual();} }
            overlay?.Update(data);
            var selectedRules=Current.Alerts.Where(r=>needed==null||needed.Contains(r.Metric));
            foreach(var a in alerts.Evaluate(selectedRules,data,DateTime.UtcNow)){notice.Text=$"ALERTA · {data[a.Metric].Name}: {data[a.Metric].Display} (umbral {a.Threshold})";if(a.Channel.Contains("sonido"))System.Media.SystemSounds.Exclamation.Play();if(a.Channel=="Ventana")ShowAlert(notice.Text);}
            status.Text=$"●  {(WindowState==WindowState.Minimized?"Overlay · sensores seleccionados":"Monitorización activa")}   ·   {settings.Interval/1000d:0.#} s   ·   {DateTime.Now:HH:mm:ss}   ·   Sin historial   ·   {fps.Status}";
        }catch(Exception e){notice.Text="Lectura: "+e.Message;}finally{busy=false;}
    }
    void ShowAlert(string text) {var p=new StackPanel();p.Children.Add(Ui.Text(text,18,"#FCBE76"));var w=Ui.Dialog(this,"PC Control · Alerta",p);p.Children.Add(Ui.Button("Cerrar",w.Close));w.Show();}
    void Rebuild()
    {
        if(dashboard.Parent is Panel dp)dp.Children.Remove(dashboard);if(overlayEditor.Parent is Panel op)op.Children.Remove(overlayEditor);
        building=true;int selected=tabs.SelectedIndex<0?0:tabs.SelectedIndex;tabs.Items.Clear();views.Clear();
        profileSelector.Items.Clear();foreach(var p in settings.Profiles)profileSelector.Items.Add(p.Name);profileSelector.SelectedIndex=settings.Active;
        var panel=new StackPanel();panel.Children.Add(Ui.Text("Vista general",25,"#F2F5FF"));panel.Children.Add(Ui.Text("Sensores reales. Arrastra y redimensiona las tarjetas al editar. Los gráficos conservan hasta 60 s en memoria."));
        panel.Children.Add(Ui.Row(Ui.Button(editing?"✓  Terminar edición":"↔  Editar panel",()=>{editing=!editing;Rebuild();}),Ui.Button("+ Añadir tarjeta",()=>EditTile(false,null)),Ui.Button("Restablecer",()=>{if(MessageBox.Show(this,"¿Restablecer el panel del perfil actual?","PC Control",MessageBoxButton.YesNo)==MessageBoxResult.Yes){Current.Dashboard=Defaults.Panel();Save();Rebuild();}})));
        panel.Children.Add(dashboard);AddTab("Panel",panel);RenderCards(false);
        AddTab("Overlay y FPS",BuildGaming());RenderCards(true);
        AddTab("Alertas",BuildAlerts());AddTab("Ajustes y sensores",BuildSettings());
        tabs.SelectedIndex=Math.Clamp(selected,0,tabs.Items.Count-1);building=false;
    }
    void AddTab(string name,UIElement content){tabs.Items.Add(new TabItem{Header=name,Content=new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new(0,20,0,0)}});}
    void RenderCards(bool gaming)
    {
        var grid=gaming?overlayEditor:dashboard;
        // Detach before assigning the grid to a rebuilt tab.
        grid.Children.Clear();grid.RowDefinitions.Clear();grid.ColumnDefinitions.Clear();if(gaming)gamingViews.Clear();else views.Clear();
        var tiles=gaming?Current.Overlay:Current.Dashboard;var visible=tiles.Where(t=>gaming||!settings.HideUnavailable||!latest.TryGetValue(t.Metric,out var m)||m.Value!=null).ToList();
        int count=gaming?Current.OverlayColumns:columns;
        for(int i=0;i<count;i++)grid.ColumnDefinitions.Add(new());var placements=GridLayout.Pack(visible,count);int rows=placements.Count==0?0:placements.Max(p=>p.row+p.height);
        for(int i=0;i<rows;i++)grid.RowDefinitions.Add(new(){Height=new GridLength(gaming?145:190)});
        for(int i=0;i<visible.Count;i++){
            var t=visible[i];var pos=placements[i];bool edit=gaming?overlayEditing:editing;
            var card=new Grid();card.RowDefinitions.Add(new(){Height=GridLength.Auto});card.RowDefinitions.Add(new(){Height=GridLength.Auto});card.RowDefinitions.Add(new());
            bool skin=settings.GamingTheme;string accent=skin?GamingColor(t.Metric):t.Color;
            var title=Ui.Text(latest.GetValueOrDefault(t.Metric)?.Name??sensors.Catalog.GetValueOrDefault(t.Metric)?.Name??t.Metric,12,skin?"#D2D5E9":"#A7AEC5");title.TextTrimming=TextTrimming.CharacterEllipsis;title.TextWrapping=TextWrapping.NoWrap;title.FontFamily=new(skin?"Bahnschrift":"Segoe UI");
            var val=Ui.Text(latest.GetValueOrDefault(t.Metric)?.Display??"No disponible",gaming?21:32,accent);val.FontFamily=new(skin?"Consolas":"Segoe UI");val.FontWeight=skin?FontWeights.Bold:FontWeights.Normal;Grid.SetRow(val,1);card.Children.Add(title);card.Children.Add(val);
            var graph=new Sparkline{Line=Ui.Brush(accent),Gaming=skin,ChartStyle=skin?"Área":t.Style,Values=history.GetValueOrDefault(t.Metric)??[],Margin=new(0,4,0,0),Visibility=t.Graph?Visibility.Visible:Visibility.Collapsed};Grid.SetRow(graph,2);card.Children.Add(graph);(gaming?gamingViews:views).Add((t,val,graph));
            var border=new Border{Background=skin?new LinearGradientBrush(Color.FromRgb(16,22,36),Color.FromRgb(30,15,41),40):Ui.Brush("#1A1E2C"),BorderBrush=Ui.Brush(skin?accent:edit?"#596382":"#2D3348"),BorderThickness=skin?new(2,1,1,1):new(1),CornerRadius=new(skin?3:12),Padding=new(18,12,18,12),Margin=new(0,8,12,4),Child=card,AllowDrop=edit};
            if(edit){
                var actions=Ui.Row(Ui.Button("✎",()=>EditTile(gaming,t)),Ui.Button("×",()=>{tiles.Remove(t);Save();RenderCards(gaming);overlay?.Configure(Current);}));actions.HorizontalAlignment=HorizontalAlignment.Right;actions.VerticalAlignment=VerticalAlignment.Top;actions.RenderTransform=new ScaleTransform(.8,.8);Grid.SetRow(actions,0);card.Children.Add(actions);title.Margin=new(0,4,78,8);
                Point start=default;bool down=false;
                title.Cursor=Cursors.SizeAll;title.ToolTip="Arrastra para reordenar";
                title.MouseLeftButtonDown+=(_,e)=>{start=e.GetPosition(title);down=true;title.CaptureMouse();e.Handled=true;};title.MouseLeftButtonUp+=(_,_)=>{down=false;title.ReleaseMouseCapture();};
                title.MouseMove+=(_,e)=>{if(down&&e.LeftButton==MouseButtonState.Pressed&&(e.GetPosition(title)-start).Length>6){down=false;title.ReleaseMouseCapture();DragDrop.DoDragDrop(title,new DataObject("PCControl.Tile",t),DragDropEffects.Move);}};
                border.Drop+=(_,e)=>{if(e.Data.GetData("PCControl.Tile") is Tile from&&tiles.Contains(from)&&from!=t){int target=tiles.IndexOf(t);tiles.Remove(from);tiles.Insert(Math.Min(target,tiles.Count),from);Save();RenderCards(gaming);overlay?.Configure(Current);}e.Handled=true;};
                var thumb=new Thumb{Width=18,Height=18,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Cursor=Cursors.SizeNWSE,Background=Ui.Brush("#646F94")};Grid.SetRow(thumb,2);card.Children.Add(thumb);double dx=0,dy=0;thumb.DragDelta+=(_,e)=>{dx+=e.HorizontalChange;dy+=e.VerticalChange;};thumb.DragCompleted+=(_,_)=>{t.Width=Math.Clamp(t.Width+(int)Math.Round(dx/Math.Max(100,grid.ActualWidth/count)),1,count);t.Height=Math.Clamp(t.Height+(int)Math.Round(dy/(gaming?145:190)),1,3);Save();RenderCards(gaming);overlay?.Configure(Current);};
            }
            Grid.SetRow(border,pos.row);Grid.SetColumn(border,pos.col);Grid.SetColumnSpan(border,pos.width);Grid.SetRowSpan(border,pos.height);grid.Children.Add(border);
        }
    }
    void EditTile(bool gaming,Tile? tile)
    {
        var all=sensors.Catalog.Values.OrderBy(m=>m.Name).ToList();var stack=new StackPanel();stack.Children.Add(Ui.Text(tile==null?"Nueva tarjeta":"Editar tarjeta",23,"#FFFFFF"));
        var metric=new ComboBox{ItemsSource=all,DisplayMemberPath="Name",SelectedValuePath="Id",SelectedValue=tile?.Metric??"cpu.load",Width=410};stack.Children.Add(Ui.Text("Métrica"));stack.Children.Add(metric);
        var color=Ui.Input(tile?.Color??"#51E3D5");stack.Children.Add(Ui.Text("Color hexadecimal, por ejemplo #51E3D5"));stack.Children.Add(color);
        var palette=Ui.Row();foreach(var c in new[]{"#51E3D5","#B193FF","#FCBE76","#F078A7","#FFFFFF"}){var sw=Ui.Button("●",()=>color.Text=c);sw.Foreground=Ui.Brush(c);palette.Children.Add(sw);}stack.Children.Add(palette);
        var width=Ui.Select(["1","2","3","4"],(tile?.Width??1).ToString());var height=Ui.Select(["1","2","3"],(tile?.Height??1).ToString());stack.Children.Add(Ui.Text("Tamaño en celdas: ancho / alto"));stack.Children.Add(Ui.Row(width,height));
        var graph=new CheckBox{Content="Mostrar gráfico",IsChecked=tile?.Graph??true};stack.Children.Add(graph);var style=Ui.Select(["Línea","Área","Barras"],tile?.Style??"Línea");stack.Children.Add(style);
        var w=Ui.Dialog(this,"Tarjeta",stack);stack.Children.Add(Ui.Button("Guardar tarjeta",()=>{if(metric.SelectedValue is not string id)return;try{ColorConverter.ConvertFromString(color.Text);}catch{MessageBox.Show(w,"Introduce un color hexadecimal válido.");return;}var t=tile??new();t.Metric=id;t.Color=color.Text;t.Width=int.Parse((string)width.SelectedItem);t.Height=int.Parse((string)height.SelectedItem);t.Graph=graph.IsChecked==true;t.Style=(string)style.SelectedItem;if(tile==null)(gaming?Current.Overlay:Current.Dashboard).Add(t);Save();w.DialogResult=true;RenderCards(gaming);overlay?.Configure(Current);}));w.ShowDialog();
    }
    StackPanel BuildGaming()
    {
        var p=new StackPanel();p.Children.Add(Ui.Text("Tu HUD, a tu manera",25,"#FFFFFF"));
        p.Children.Add(Ui.Text("Activación manual. El overlay deja pasar los clics y permanece al minimizar. Usa un juego en ventana o sin bordes. La pantalla completa exclusiva puede ocultarlo; la compatibilidad con cada juego y antitrampas requiere una prueba real.",14));
        p.Children.Add(Ui.Row(Ui.Button("+ Añadir métrica",()=>EditTile(true,null)),Ui.Button("Apariencia, posición y atajo",ConfigureOverlay),Ui.Button("Restablecer overlay",()=>{Current.Overlay=Defaults.Overlay();Save();RenderCards(true);overlay?.Configure(Current);})));
        p.Children.Add(Ui.Text("EDITOR VISUAL · Arrastra el título para reordenar. Usa la esquina inferior derecha para cambiar el tamaño.",11,"#51E3D5"));p.Children.Add(overlayEditor);
        var box=new StackPanel();box.Children.Add(Ui.Text("Captura de FPS · Intel PresentMon",20,"#FFFFFF"));box.Children.Add(Ui.Text("Elige expresamente el proceso del juego. La captura no empieza al activar el overlay. Puede necesitar permisos de administrador; no se solicitarán ni se instalarán controladores automáticamente."));
        var processes=new ComboBox{MinWidth=400,DisplayMemberPath="Label",SelectedValuePath="Id"};
        void Refresh(){var items=new List<ProcessChoice>();foreach(var proc in Process.GetProcesses()){using(proc){try{if(proc.MainWindowHandle!=IntPtr.Zero&&proc.Id!=Environment.ProcessId)items.Add(new(proc.Id,$"{proc.ProcessName} · PID {proc.Id} · {proc.MainWindowTitle}"));}catch{}}}processes.ItemsSource=items.OrderBy(x=>x.Label).ToList();if(processes.Items.Count>0)processes.SelectedIndex=0;}
        Refresh();box.Children.Add(Ui.Row(processes,Ui.Button("Actualizar procesos",Refresh)));
        string bundled=Path.Combine(AppContext.BaseDirectory,"tools","PresentMon.exe");var path=Ui.Input(capturePath??bundled);box.Children.Add(Ui.Text("Ejecutable de PresentMon (incluido en la distribución)"));box.Children.Add(path);
        box.Children.Add(Ui.Row(Ui.Button("Iniciar FPS",()=>{if(processes.SelectedValue is not int pid){notice.Text="Selecciona un proceso.";return;}try{capturePid=pid;capturePath=path.Text;fps.Start(capturePath,capturePid);captureRequested=true;notice.Text=fps.Status;}catch(Exception e){captureRequested=false;notice.Text=e.Message;}}),Ui.Button("Detener FPS",()=>{captureRequested=false;fps.Stop();notice.Text=fps.Status;})));
        p.Children.Add(Ui.Box(box));return p;
    }
    sealed record ProcessChoice(int Id,string Label) { public override string ToString()=>Label; }
    void ConfigureOverlay()
    {
        var p=new StackPanel();p.Children.Add(Ui.Text("Configurar overlay",23,"#FFFFFF"));
        var x=Ui.Input(Current.OverlayX.ToString(CultureInfo.InvariantCulture));var y=Ui.Input(Current.OverlayY.ToString(CultureInfo.InvariantCulture));
        var width=Ui.Input(Current.OverlayWidth.ToString(CultureInfo.InvariantCulture));var font=Ui.Input(Current.OverlayFont.ToString(CultureInfo.InvariantCulture));
        var opacity=Ui.Input(Current.OverlayOpacity.ToString(CultureInfo.InvariantCulture));var color=Ui.Input(Current.OverlayColor);
        var hotkey=Ui.Input(Current.Hotkey);var cols=Ui.Select(["1","2","3","4"],Current.OverlayColumns.ToString());
        var bg=new CheckBox{Content="Mostrar fondo semitransparente",IsChecked=Current.OverlayBackground};
        foreach(var (label,control) in new (string,UIElement)[]{("Posición X / Y en el escritorio virtual",Ui.Row(x,y)),("Ancho (150–1200) / tamaño de letra (10–48)",Ui.Row(width,font)),("Opacidad del fondo (0.1–1)",opacity),("Color de acento",color),("Columnas de la rejilla",cols),("Atajo global (Ctrl+Shift+F10, Alt+F9…)",hotkey)}){p.Children.Add(Ui.Text(label));p.Children.Add(control);}p.Children.Add(bg);
        p.Children.Add(Ui.Text("Las coordenadas permiten otros monitores, incluidos valores negativos. La posición se limita al escritorio visible."));
        var w=Ui.Dialog(this,"Overlay",p,550);p.Children.Add(Ui.Button("Aplicar",()=>{
            if(!double.TryParse(x.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var vx)||!double.TryParse(y.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var vy)||!double.TryParse(width.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var vw)||!double.TryParse(font.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var vf)||!double.TryParse(opacity.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var vo)||!new[]{vx,vy,vw,vf,vo}.All(double.IsFinite)){MessageBox.Show(w,"Introduce números válidos (usa punto decimal).");return;}
            try{ColorConverter.ConvertFromString(color.Text);ParseHotkey(hotkey.Text);}catch{MessageBox.Show(w,"Revisa el color y el atajo. Debe incluir Ctrl, Alt o Shift y una tecla.");return;}
            Current.OverlayX=vx;Current.OverlayY=vy;Current.OverlayWidth=vw;Current.OverlayFont=vf;Current.OverlayOpacity=vo;Current.OverlayColor=color.Text;Current.OverlayColumns=int.Parse((string)cols.SelectedItem);Current.Hotkey=hotkey.Text;Current.OverlayBackground=bg.IsChecked==true;Save();RegisterShortcut();overlay?.Configure(Current);RenderCards(true);w.DialogResult=true;
        }));w.ShowDialog();
    }
    void ToggleOverlay()
    {
        if(overlay==null){overlay=new();overlay.Configure(Current);overlay.Show();overlay.Update(latest);overlayButton.Content="◉  Desactivar overlay";}
        else{overlay.Close();overlay=null;overlayButton.Content="◉  Activar overlay";}
        sensorsReset=true;UpdateCapturePolicy();
    }
    void UpdateCapturePolicy()
    {
        bool needs=WindowState!=WindowState.Minimized||overlay!=null&&Current.Overlay.Any(t=>t.Metric=="fps");
        if(!needs){fps.Stop();return;}
        if(captureRequested&&!fps.Running&&capturePath!=null)try{fps.Start(capturePath,capturePid);}catch(Exception e){captureRequested=false;notice.Text=e.Message;}
    }
    StackPanel BuildAlerts()
    {
        var p=new StackPanel();p.Children.Add(Ui.Text("Alertas bajo control",25,"#FFFFFF"));p.Children.Add(Ui.Text("Se avisa al superar el umbral. Histéresis del 3 % (mínimo 1 unidad) y al menos 60 segundos entre avisos de la misma regla. Al minimizar solo se evalúan las métricas del overlay."));
        p.Children.Add(Ui.Button("+ Crear alerta",()=>EditAlert(null)));
        foreach(var r in Current.Alerts.ToArray()){
            var content=new StackPanel();var active=new CheckBox{Content=sensors.Catalog.GetValueOrDefault(r.Metric)?.Name??r.Metric,IsChecked=r.Enabled};active.Checked+=(_,_)=>{r.Enabled=true;Save();};active.Unchecked+=(_,_)=>{r.Enabled=false;Save();};content.Children.Add(active);
            content.Children.Add(Ui.Text($"Mayor que {r.Threshold} · {r.Channel}"));content.Children.Add(Ui.Row(Ui.Button("Editar",()=>EditAlert(r)),Ui.Button("Eliminar",()=>{Current.Alerts.Remove(r);Save();Rebuild();})));p.Children.Add(Ui.Box(content));
        }
        return p;
    }
    void EditAlert(AlertRule? rule)
    {
        var p=new StackPanel();p.Children.Add(Ui.Text("Configurar alerta",23,"#FFFFFF"));var metric=new ComboBox{ItemsSource=sensors.Catalog.Values.OrderBy(x=>x.Name).ToList(),DisplayMemberPath="Name",SelectedValuePath="Id",SelectedValue=rule?.Metric??"cpu.temp"};p.Children.Add(metric);
        var threshold=Ui.Input((rule?.Threshold??85).ToString(CultureInfo.InvariantCulture));p.Children.Add(Ui.Text("Avisar al superar este valor (unidad del sensor)"));p.Children.Add(threshold);
        var channel=Ui.Select(["Panel","Panel y sonido","Ventana"],rule?.Channel??"Panel y sonido");p.Children.Add(channel);var enabled=new CheckBox{Content="Activada",IsChecked=rule?.Enabled??true};p.Children.Add(enabled);
        var w=Ui.Dialog(this,"Alerta",p);p.Children.Add(Ui.Button("Guardar",()=>{if(metric.SelectedValue is not string id||!double.TryParse(threshold.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var n)||!double.IsFinite(n)){MessageBox.Show(w,"Revisa el umbral.");return;}var r=rule??new();r.Metric=id;r.Threshold=n;r.Channel=(string)channel.SelectedItem;r.Enabled=enabled.IsChecked==true;if(rule==null)Current.Alerts.Add(r);alerts.Reset();Save();w.DialogResult=true;Rebuild();}));w.ShowDialog();
    }
    void ManageProfiles()
    {
        var p=new StackPanel();p.Children.Add(Ui.Text("Perfiles",23,"#FFFFFF"));p.Children.Add(Ui.Text("Cada perfil conserva panel, overlay, apariencia, atajo y alertas. El cambio es siempre manual."));
        var list=new ComboBox{ItemsSource=settings.Profiles.Select(x=>x.Name).ToList(),SelectedIndex=settings.Active};p.Children.Add(list);var w=Ui.Dialog(this,"Perfiles",p);
        void Complete(){Save();w.Close();Rebuild();overlay?.Configure(Current);RegisterShortcut();}
        p.Children.Add(Ui.Button("Crear perfil",()=>{var name=Ui.Ask(w,"Nombre del perfil", "Mi perfil");if(name==null)return;if(settings.Profiles.Any(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase))){MessageBox.Show(w,"Ese nombre ya existe.");return;}settings.Profiles.Add(new(){Name=name});settings.Active=settings.Profiles.Count-1;Complete();}));
        p.Children.Add(Ui.Button("Duplicar seleccionado",()=>{if(list.SelectedIndex<0)return;var source=settings.Profiles[list.SelectedIndex];var copy=JsonSerializer.Deserialize<Profile>(JsonSerializer.Serialize(source))!;copy.Name=source.Name+" copia "+DateTime.Now.ToString("HHmmss");foreach(var r in copy.Alerts)r.Id=Guid.NewGuid().ToString();settings.Profiles.Add(copy);settings.Active=settings.Profiles.Count-1;Complete();}));
        p.Children.Add(Ui.Button("Renombrar seleccionado",()=>{if(list.SelectedIndex<0)return;var original=settings.Profiles[list.SelectedIndex];var name=Ui.Ask(w,"Nombre del perfil",original.Name);if(name==null)return;if(settings.Profiles.Any(x=>x!=original&&x.Name.Equals(name,StringComparison.OrdinalIgnoreCase))){MessageBox.Show(w,"Ese nombre ya existe.");return;}original.Name=name;Complete();}));
        p.Children.Add(Ui.Button("Eliminar seleccionado",()=>{if(settings.Profiles.Count<=1){MessageBox.Show(w,"Debe quedar al menos un perfil.");return;}if(list.SelectedIndex>=0&&MessageBox.Show(w,"¿Eliminar este perfil?","PC Control",MessageBoxButton.YesNo)==MessageBoxResult.Yes){settings.Profiles.RemoveAt(list.SelectedIndex);settings.Active=Math.Min(settings.Active,settings.Profiles.Count-1);Complete();}}));w.ShowDialog();
    }
    StackPanel BuildSettings()
    {
        var p=new StackPanel();p.Children.Add(Ui.Text("Ajustes y disponibilidad",25,"#FFFFFF"));
        var controls=new StackPanel();controls.Children.Add(Ui.Text("Frecuencia de actualización",18,"#FFFFFF"));var interval=Ui.Select(["500","1000","1500","2000","3000","5000","10000"],settings.Interval.ToString());controls.Children.Add(Ui.Text("Milisegundos · recomendado: 1500–3000"));controls.Children.Add(interval);
        interval.SelectionChanged+=(_,_)=>{settings.Interval=int.Parse((string)interval.SelectedItem);timer.Interval=TimeSpan.FromMilliseconds(settings.Interval);Save();};
        var hide=new CheckBox{Content="Ocultar tarjetas sin sensor disponible",IsChecked=settings.HideUnavailable};hide.Checked+=(_,_)=>{settings.HideUnavailable=true;Save();RenderCards(false);};hide.Unchecked+=(_,_)=>{settings.HideUnavailable=false;Save();RenderCards(false);};controls.Children.Add(hide);p.Children.Add(Ui.Box(controls));
        p.Children.Add(Ui.Text("Privacidad y ciclo de vida",20,"#FFFFFF"));p.Children.Add(Ui.Text("Sin inicio con Windows, servicios propios, telemetría ni historial persistente. Al minimizar sin overlay se suspenden las lecturas. Con overlay solo se actualizan las familias necesarias. Cerrar la ventana termina la aplicación y la captura de FPS. La configuración se guarda en: "+store.Path));
        p.Children.Add(Ui.Text("Sensores avanzados",20,"#FFFFFF"));p.Children.Add(Ui.Text("LibreHardwareMonitor 0.9.6. Algunos sensores necesitan permisos o un controlador PawnIO ya instalado. PC Control no instala controladores ni reduce las protecciones de Windows. Si falta un sensor, se muestra «No disponible»; puedes elegir otro sensor detectado. En equipos con varias GPU, elige su sensor específico para evitar ambigüedad."));
        p.Children.Add(Ui.Text("Adaptadores de red",20,"#FFFFFF"));p.Children.Add(Ui.Text(sensors.Adapters()));
        p.Children.Add(Ui.Button("Actualizar lista de sensores",()=>Rebuild()));p.Children.Add(Ui.Text(sensors.Diagnostic,12,"#FCBE76"));
        var sensorList=new TextBox{Text=string.Join("\n",latest.Values.OrderBy(x=>x.Name).Select(m=>$"{m.Name}   {m.Display}   [{m.Source}]")),IsReadOnly=true,TextWrapping=TextWrapping.NoWrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Height=300};p.Children.Add(sensorList);
        return p;
    }
    static (uint modifiers,uint key) ParseHotkey(string value)
    {
        var parts=value.Split('+',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);uint mods=0;
        foreach(var p in parts.SkipLast(1))mods|=p.ToLowerInvariant() switch{"ctrl"=>2u,"alt"=>1u,"shift"=>4u,"win"=>8u,_=>throw new FormatException()};
        if(mods==0||parts.Length<2)throw new FormatException();var key=(Key)new KeyConverter().ConvertFromInvariantString(parts[^1])!;int vk=KeyInterop.VirtualKeyFromKey(key);if(vk==0)throw new FormatException();return(mods|0x4000,(uint)vk);
    }
    void RegisterShortcut()
    {
        var h=new WindowInteropHelper(this).Handle;if(h==IntPtr.Zero)return;UnregisterHotKey(h,1);
        try{var (m,k)=ParseHotkey(Current.Hotkey);if(!RegisterHotKey(h,1,m,k))notice.Text="El atajo está ocupado. Elige otro en Modo gaming.";}catch{notice.Text="El atajo no es válido. Cámbialo en Modo gaming.";}
    }
    IntPtr WindowHook(IntPtr h,int msg,IntPtr w,IntPtr l,ref bool handled){if(msg==0x312&&w.ToInt32()==1){ToggleOverlay();handled=true;}return IntPtr.Zero;}
    [DllImport("user32.dll")]static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
    [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr h,int id);
    [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr h,int attribute,ref int value,int size);
}
