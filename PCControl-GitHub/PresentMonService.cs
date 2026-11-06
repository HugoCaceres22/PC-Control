using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
namespace PCControl;
public sealed class PresentMonService : IDisposable
{
    Process? process;
    IntPtr job;
    string? executablePath;
    readonly string sessionName="PCControl-"+Environment.ProcessId;
    readonly object gate=new();
    readonly Queue<(long time,double ms)> frames=[];
    string status="FPS desactivados. Selecciona un proceso y pulsa Iniciar FPS.";
    public string Status {get=>process is {HasExited:true}?process.ExitCode==6?"FPS no disponibles: PresentMon requiere permiso ETW (administrador o grupo Performance Log Users). No se han cambiado permisos.":$"PresentMon detenido (código {process.ExitCode}). {status}":status;private set=>status=value;}
    public bool Running=>process is {HasExited:false};
    public void Start(string executable,int pid)
    {
        Stop(); if(!File.Exists(executable)) throw new FileNotFoundException("No se encuentra PresentMon.",executable);
        using(var target=Process.GetProcessById(pid)){if(target.HasExited)throw new InvalidOperationException("El proceso seleccionado ya ha terminado.");}
        var info=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var a in new[]{"--process_id",pid.ToString(),"--output_stdout","--v1_metrics","--no_console_stats","--no_track_input","--session_name",sessionName,"--terminate_on_proc_exit"}) info.ArgumentList.Add(a);
        process=new(){StartInfo=info,EnableRaisingEvents=true};
        int index=-1;
        process.OutputDataReceived+=(_,e)=> { if(e.Data==null)return; var cells=Csv(e.Data); int header=Array.IndexOf(cells,"MsBetweenPresents"); if(header>=0){index=header;return;} if(index>=0 && cells.Length>index && double.TryParse(cells[index],NumberStyles.Float,CultureInfo.InvariantCulture,out var ms) && ms>0 && ms<10000) lock(gate){frames.Enqueue((Stopwatch.GetTimestamp(),ms));while(frames.Count>4096)frames.Dequeue();} };
        process.ErrorDataReceived+=(_,e)=>{if(!string.IsNullOrWhiteSpace(e.Data))Status="PresentMon: "+e.Data;};
        // Suspend creation until assigned to a kill-on-close job is not available through Process;
        // fail closed if assignment fails, never leave a detached collector.
        process.Start();
        executablePath=executable;
        job=CreateJobObject(IntPtr.Zero,null);
        var limit=new JobExtended();limit.Basic.LimitFlags=0x2000;
        int size=Marshal.SizeOf<JobExtended>(); var p=Marshal.AllocHGlobal(size);
        try { Marshal.StructureToPtr(limit,p,false); if(job==IntPtr.Zero||!SetInformationJobObject(job,9,p,(uint)size)||!AssignProcessToJobObject(job,process.Handle)) { Stop();throw new InvalidOperationException("No se pudo proteger el cierre de PresentMon; captura cancelada."); } } finally{Marshal.FreeHGlobal(p);}
        Status="Capturando proceso "+pid+" · sin archivos de historial";
        process.BeginOutputReadLine();process.BeginErrorReadLine();
    }
    public double? Fps {get { lock(gate) { long cutoff=Stopwatch.GetTimestamp()-Stopwatch.Frequency*2;while(frames.TryPeek(out var f)&&f.time<cutoff)frames.Dequeue();return Running&&frames.Count>0?1000/frames.Average(x=>x.ms):null; } } }
    public static string[] Csv(string line)
    {
        var result=new List<string>();var field=new System.Text.StringBuilder();bool quoted=false;
        for(int i=0;i<line.Length;i++){char c=line[i];if(c=='"'){if(quoted && i+1<line.Length && line[i+1]=='"'){field.Append('"');i++;}else quoted=!quoted;}else if(c==','&&!quoted){result.Add(field.ToString());field.Clear();}else field.Append(c);}result.Add(field.ToString());return result.ToArray();
    }
    public void Stop()
    {
        if(executablePath!=null){
            // Stop our own ETW session explicitly before terminating the reader.
            try{var stop=new ProcessStartInfo(executablePath){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var a in new[]{"--session_name",sessionName,"--terminate_existing_session"})stop.ArgumentList.Add(a);using var cleanup=Process.Start(stop);if(cleanup!=null&&!cleanup.WaitForExit(3000)){cleanup.Kill(true);cleanup.WaitForExit(1000);}}catch{}finally{executablePath=null;}
        }
        if(process!=null){try{if(!process.HasExited){process.Kill(true);process.WaitForExit(3000);}}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){/* Closing the owning job below is the fallback termination path. */}finally{process.Dispose();process=null;}}
        if(job!=IntPtr.Zero){CloseHandle(job);job=IntPtr.Zero;}lock(gate)frames.Clear();Status="FPS detenidos";
    }
    public void Dispose()=>Stop();
    [StructLayout(LayoutKind.Sequential)] struct JobBasic {public long PerProcess,PerJob;public uint LimitFlags;public UIntPtr Min,Max;public uint Active;public UIntPtr Affinity;public uint Priority,Scheduling;}
    [StructLayout(LayoutKind.Sequential)] struct IoCounters {public ulong A,B,C,D,E,F;}
    [StructLayout(LayoutKind.Sequential)] struct JobExtended {public JobBasic Basic;public IoCounters Io;public UIntPtr ProcessMemory,JobMemory,PeakProcess,PeakJob;}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr CreateJobObject(IntPtr attributes,string? name);
    [DllImport("kernel32.dll")] static extern bool SetInformationJobObject(IntPtr job,int type,IntPtr data,uint length);
    [DllImport("kernel32.dll")] static extern bool AssignProcessToJobObject(IntPtr job,IntPtr process);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
}
