using System.IO;
using System.Text.Json;
namespace PCControl;
public sealed class Tile
{
    public string Metric { get; set; } = "cpu.load";
    public string Color { get; set; } = "#51E3D5";
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
    public bool Graph { get; set; } = true;
    public string Style { get; set; } = "Línea";
}
public sealed class AlertRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Metric { get; set; } = "cpu.temp";
    public double Threshold { get; set; } = 85;
    public bool Enabled { get; set; } = true;
    public string Channel { get; set; } = "Panel y sonido";
}
public sealed class Profile
{
    public string Name { get; set; } = "Predeterminado";
    public List<Tile> Dashboard { get; set; } = Defaults.Panel();
    public List<Tile> Overlay { get; set; } = Defaults.Overlay();
    public List<AlertRule> Alerts { get; set; } = [];
    public double OverlayX { get; set; } = 24;
    public double OverlayY { get; set; } = 24;
    public double OverlayWidth { get; set; } = 330;
    public double OverlayFont { get; set; } = 16;
    public double OverlayOpacity { get; set; } = .8;
    public bool OverlayBackground { get; set; } = true;
    public string OverlayColor { get; set; } = "#51E3D5";
    public string Hotkey { get; set; } = "Ctrl+Shift+F10";
    public int OverlayColumns { get; set; } = 2;
}
public sealed class Settings
{
    public int Interval { get; set; } = 1500;
    public bool HideUnavailable { get; set; }
    public bool GamingTheme { get; set; }
    public int Active { get; set; }
    public List<Profile> Profiles { get; set; } = [new(), new() { Name = "Gaming", Dashboard = Defaults.Overlay() }, new() { Name = "Minimalista", Dashboard = [new() {Metric="cpu.load"},new() {Metric="ram.load"}] }];
    [System.Text.Json.Serialization.JsonIgnore] public Profile Current => Profiles[Math.Clamp(Active,0,Profiles.Count-1)];
}
public static class Defaults
{
    public static List<Tile> Panel() => [new(){Metric="cpu.load",Width=2},new(){Metric="gpu.load",Color="#B193FF",Width=2},new(){Metric="ram.load",Color="#FCBE76"},new(){Metric="cpu.temp"},new(){Metric="gpu.temp",Color="#B193FF"},new(){Metric="ram.used"},new(){Metric="net.down",Width=2},new(){Metric="net.up",Width=2}];
    public static List<Tile> Overlay() => [new(){Metric="fps"},new(){Metric="cpu.load"},new(){Metric="gpu.load"},new(){Metric="ram.used"}];
}
public sealed class ConfigStore
{
    public string Path { get; }
    public string? Warning { get; private set; }
    public ConfigStore(string? path=null) => Path=path??System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PCControl","settings.json");
    public Settings Load()
    {
        try { if(!File.Exists(Path)) return new(); var s=JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path))??new(); Validate(s); return s; }
        catch(Exception e) { Warning="No se pudo cargar la configuración; se recuperaron valores iniciales. "+e.Message; try{if(File.Exists(Path)) File.Copy(Path,Path+".invalid",true);}catch{Warning+=" No se pudo crear copia del archivo dañado.";} return new(); }
    }
    public void Save(Settings s)
    {
        Validate(s); Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path+".tmp",JsonSerializer.Serialize(s,new JsonSerializerOptions{WriteIndented=true}));
        File.Move(Path+".tmp",Path,true);
    }
    public static void Validate(Settings s)
    {
        s.Interval=Math.Clamp(s.Interval,500,10000);
        if(s.Profiles==null||s.Profiles.Count==0) s.Profiles=[new()];
        s.Active=Math.Clamp(s.Active,0,s.Profiles.Count-1);
        foreach(var p in s.Profiles) { p.Dashboard??=[]; p.Overlay??=[]; p.Alerts??=[]; p.OverlayColumns=Math.Clamp(p.OverlayColumns,1,4); p.OverlayWidth=Math.Clamp(p.OverlayWidth,150,1200); p.OverlayFont=Math.Clamp(p.OverlayFont,10,48); p.OverlayOpacity=Math.Clamp(p.OverlayOpacity,.1,1); foreach(var t in p.Dashboard.Concat(p.Overlay)) { t.Width=Math.Clamp(t.Width,1,4); t.Height=Math.Clamp(t.Height,1,3); } }
    }
}
public sealed class AlertEngine
{
    readonly Dictionary<string,(bool active,DateTime last)> state=[];
    public List<AlertRule> Evaluate(IEnumerable<AlertRule> rules,IReadOnlyDictionary<string,Metric> metrics,DateTime now)
    {
        var result=new List<AlertRule>();
        foreach(var r in rules) {
            state.TryGetValue(r.Id,out var st);
            if(!r.Enabled||!metrics.TryGetValue(r.Metric,out var m)||m.Value==null) { state.Remove(r.Id); continue; }
            if(m.Value>r.Threshold) { if(!st.active && (now-st.last).TotalSeconds>=60) { result.Add(r); state[r.Id]=(true,now); } }
            else if(m.Value<r.Threshold-Math.Max(1,Math.Abs(r.Threshold)*.03)) state[r.Id]=(false,st.last);
        }
        return result;
    }
    public void Reset()=>state.Clear();
}
public sealed record Metric(string Id,string Name,string Unit,double? Value,string Source="Windows")
{
    public override string ToString()=>Name;
    public string Display => Value is double v && double.IsFinite(v) ? $"{v:0.#} {Unit}" : "No disponible";
}
