using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using LibreHardwareMonitor.Hardware;
namespace PCControl;
public sealed class SensorService : IDisposable
{
    readonly Computer computer = new();
    readonly Dictionary<string,Metric> catalog=[];
    readonly Dictionary<string,(long rx,long tx,long stamp)> net=[];
    readonly List<PerformanceCounter> disk=[];
    ulong idle,kernel,user;
    bool started;
    bool disposed;
    readonly List<string> lastUpdatedHardware=[];
    public string Diagnostic { get; private set; } = "";
    public IReadOnlyDictionary<string,Metric> Catalog=>catalog;
    public long ReadCount { get; private set; }
    public IReadOnlyList<string> LastUpdatedHardware=>lastUpdatedHardware;
    public SensorService()
    {
        foreach(var (id,name,unit) in new (string,string,string)[] {
            ("cpu.load","CPU · Uso","%"),("cpu.temp","CPU · Temperatura","°C"),("cpu.clock","CPU · Frecuencia","MHz"),("cpu.power","CPU · Potencia","W"),
            ("gpu.load","GPU · Uso","%"),("gpu.temp","GPU · Temperatura","°C"),("gpu.clock","GPU · Frecuencia","MHz"),("gpu.power","GPU · Potencia","W"),
            ("gpu.used","GPU · VRAM utilizada","GB"),("gpu.free","GPU · VRAM disponible","GB"),("ram.load","RAM · Uso","%"),("ram.used","RAM · Utilizada","GB"),("ram.free","RAM · Disponible","GB"),("ram.total","RAM · Total","GB"),
            ("net.down","Red · Bajada","MB/s"),("net.up","Red · Subida","MB/s"),("disk.read","Discos · Lectura total","MB/s"),("disk.write","Discos · Escritura total","MB/s"),("fps","Gaming · FPS presentados","FPS") })
            catalog[id]=new(id,name,unit,null);
    }
    public void Open()
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if(started)return;
        computer.IsCpuEnabled=true; computer.IsGpuEnabled=true; computer.IsStorageEnabled=true;
        // No motherboard, controller, service installation or automatic elevation.
        try { computer.Open(); started=true; } catch(Exception e) { Diagnostic="Sensores avanzados: "+e.Message; }
        if(string.IsNullOrEmpty(Diagnostic)) {
            if(!LibreHardwareMonitor.PawnIo.PawnIo.IsInstalled)
                Diagnostic="Temperatura de CPU: falta el controlador PawnIO. La aplicación no lo instala automáticamente.";
            else if(!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                Diagnostic="Sensores de CPU: PawnIO está instalado. Cierra PC Control y ábrelo con clic derecho → Ejecutar como administrador para acceder a temperatura, frecuencia y potencia.";
        }
        try { disk.Add(new("PhysicalDisk","Disk Read Bytes/sec","_Total")); disk.Add(new("PhysicalDisk","Disk Write Bytes/sec","_Total")); foreach(var d in disk) d.NextValue(); } catch { foreach(var d in disk) d.Dispose(); disk.Clear(); }
        GetSystemTimes(out idle,out kernel,out user);
    }
    public Dictionary<string,Metric> Read(HashSet<string>? only=null)
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        lastUpdatedHardware.Clear();
        ReadCount++;
        var result=catalog.ToDictionary(x=>x.Key,x=>x.Value with { Value=null });
        bool Needs(string prefix)=>only==null||only.Any(x=>x.StartsWith(prefix,StringComparison.Ordinal));
        void Put(string id,double? value) { if(result.TryGetValue(id,out var old)) result[id]=old with {Value=value}; }
        if(Needs("cpu.") && GetSystemTimes(out var i,out var k,out var u)) {
            var total=(k-kernel)+(u-user); Put("cpu.load",total==0?null:Math.Clamp(100d*(total-(i-idle))/total,0,100)); idle=i;kernel=k;user=u;
        }
        if(Needs("ram.")) { var m=new MemoryStatus(); m.Length=(uint)Marshal.SizeOf<MemoryStatus>(); if(GlobalMemoryStatusEx(ref m)) { Put("ram.load",m.Load); Put("ram.total",m.Total/1073741824d); Put("ram.free",m.Available/1073741824d); Put("ram.used",(m.Total-m.Available)/1073741824d); } }
        var primaryGpu=started?computer.Hardware.Where(h=>h.HardwareType.ToString().StartsWith("Gpu")).OrderBy(h=>h.HardwareType==HardwareType.GpuNvidia?0:h.HardwareType==HardwareType.GpuAmd?1:2).FirstOrDefault():null;
        if(started) foreach(var h in computer.Hardware) {
            bool cpu=h.HardwareType==HardwareType.Cpu;
            bool gpu=h.HardwareType.ToString().StartsWith("Gpu");
            // Windows already supplies CPU load. Do not access privileged CPU sensors
            // for a load-only HUD, or wake other GPUs for the primary GPU aliases.
            bool advancedCpu=only==null||only.Any(id=>id.StartsWith("cpu.",StringComparison.Ordinal)&&id!="cpu.load");
            if(cpu&&!advancedCpu&&!Needs(h.Identifier.ToString()))continue;
            if(gpu&&!(h==primaryGpu&&Needs("gpu."))&&!Needs(h.Identifier.ToString()))continue;
            if(!cpu&&!gpu&&!Needs("disk.")&&!Needs(h.Identifier.ToString())) continue;
            try { lastUpdatedHardware.Add(h.Identifier.ToString());ReadHardware(h,result); if(cpu) Map(h,"cpu",result); if(h==primaryGpu) Map(h,"gpu",result); } catch(Exception e) { Diagnostic=h.Name+": "+e.Message; }
        }
        if(Needs("net.")) {
            double down=0,up=0; bool measured=false; long now=Stopwatch.GetTimestamp();
            foreach(var n in NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up && n.NetworkInterfaceType!=NetworkInterfaceType.Loopback)) {
                try { var st=n.GetIPStatistics(); if(net.TryGetValue(n.Id,out var prev)) { var sec=(now-prev.stamp)/(double)Stopwatch.Frequency; if(sec>0 && st.BytesReceived>=prev.rx && st.BytesSent>=prev.tx) { down+=(st.BytesReceived-prev.rx)/sec/1e6;up+=(st.BytesSent-prev.tx)/sec/1e6;measured=true; } } net[n.Id]=(st.BytesReceived,st.BytesSent,now); }
                catch(NetworkInformationException) { }
            }
            Put("net.down",measured?down:null);Put("net.up",measured?up:null);
        }
        if(Needs("disk.")) {
            for(int j=0;j<disk.Count;j++) try { Put(j==0?"disk.read":"disk.write",disk[j].NextValue()/1e6); } catch { }
            foreach(var d in DriveInfo.GetDrives().Where(x=>x.DriveType==DriveType.Fixed)) try {
                if(!d.IsReady) continue;
                foreach(var (suffix,name,val,unit) in new[]{("total","Total",d.TotalSize/1073741824d,"GB"),("free","Libre",d.AvailableFreeSpace/1073741824d,"GB"),("used","Ocupado",(d.TotalSize-d.TotalFreeSpace)/1073741824d,"GB"),("load","Uso",100d*(d.TotalSize-d.TotalFreeSpace)/d.TotalSize,"%")}) {
                    var id="disk."+d.Name[..1]+"."+suffix; result[id]=new(id,$"Disco {d.Name} · {name}",unit,val);
                }
            } catch(IOException) { }
        }
        foreach(var kv in result) catalog[kv.Key]=kv.Value with {Value=null};
        return result;
    }
    static void ReadHardware(IHardware h,Dictionary<string,Metric> result)
    {
        h.Update();
        foreach(var s in h.Sensors) { var id=s.Identifier.ToString(); result[id]=new(id,h.Name+" · "+s.Name,Unit(s.SensorType),s.Value,"LibreHardwareMonitor"); }
        foreach(var child in h.SubHardware) ReadHardware(child,result);
    }
    static string Unit(SensorType t)=>t.ToString() switch {"Temperature"=>"°C","Load"=>"%","Clock"=>"MHz","Power"=>"W","Data"=>"GB","SmallData"=>"MB","Throughput"=>"B/s","Fan"=>"RPM","Voltage"=>"V",_=>""};
    static void Map(IHardware h,string prefix,Dictionary<string,Metric> r)
    {
        void Set(string suffix,SensorType type,Func<ISensor,bool>? match=null,double scale=1) {
            var s=h.Sensors.FirstOrDefault(s=>s.SensorType==type && s.Value is float value && float.IsFinite(value) && !(prefix=="cpu"&&(suffix=="clock"||suffix=="power"||suffix=="temp")&&value<=0) && (match==null||match(s)));
            if(s?.Value is float v && !(prefix=="cpu" && (suffix=="clock"||suffix=="power"||suffix=="temp") && v<=0) && (r[prefix+"."+suffix].Value==null || prefix=="gpu")) r[prefix+"."+suffix]=r[prefix+"."+suffix] with{Value=v*scale,Source=h.Name};
        }
        Set("load",SensorType.Load,s=>s.Name.Contains(prefix=="cpu"?"Total":"Core",StringComparison.OrdinalIgnoreCase));
        Set("temp",SensorType.Temperature,s=>s.Name.Contains(prefix=="cpu"?"Package":"Core",StringComparison.OrdinalIgnoreCase)||(prefix=="cpu"&&s.Name.Contains("Tctl",StringComparison.OrdinalIgnoreCase)));
        Set("clock",SensorType.Clock,s=>s.Name.Contains("Core",StringComparison.OrdinalIgnoreCase));
        Set("power",SensorType.Power,s=>s.Name.Contains(prefix=="cpu"?"Package":"Package",StringComparison.OrdinalIgnoreCase)||s.Name.Contains("GPU Power",StringComparison.OrdinalIgnoreCase));
        if(prefix=="gpu") { Set("used",SensorType.SmallData,s=>s.Name=="GPU Memory Used",1d/1024); Set("free",SensorType.SmallData,s=>s.Name=="GPU Memory Free",1d/1024); }
    }
    public string Adapters()=>string.Join("\n",NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.NetworkInterfaceType!=NetworkInterfaceType.Loopback).Select(n=>$"{n.Name} · {n.Description} · {n.OperationalStatus} · enlace {n.Speed/1e6:0} Mbps"));
    public void ResetBaselines() { GetSystemTimes(out idle,out kernel,out user); net.Clear(); foreach(var d in disk) try{d.NextValue();}catch{} }
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        try{if(started)computer.Close();}
        finally{started=false;foreach(var d in disk)d.Dispose();disk.Clear();net.Clear();lastUpdatedHardware.Clear();}
    }
    [DllImport("kernel32.dll")] static extern bool GetSystemTimes(out ulong idle,out ulong kernel,out ulong user);
    [StructLayout(LayoutKind.Sequential)] struct MemoryStatus { public uint Length,Load;public ulong Total,Available,PageTotal,PageAvailable,VirtualTotal,VirtualAvailable,Extended; }
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
}
