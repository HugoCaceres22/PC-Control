using PCControl;
using System.Diagnostics;
using System.Text.Json;
using System.IO;
int count=0;
void Check(bool value,string name){if(!value)throw new Exception("FALLO: "+name);Console.WriteLine("OK "+name);count++;}
var root=Path.Combine(Path.GetTempPath(),"PCControlTests-"+Guid.NewGuid());Directory.CreateDirectory(root);
try {
var store=new ConfigStore(Path.Combine(root,"settings.json"));var s=store.Load();Check(s.Profiles.Count==3,"perfiles iniciales");
s.Current.Dashboard[0].Metric="ram.total";s.Current.Dashboard[0].Width=3;s.Current.OverlayX=-1920;s.Current.Hotkey="Alt+F9";s.Current.Alerts.Add(new(){Threshold=91});store.Save(s);
var loaded=store.Load();Check(loaded.Current.Dashboard[0].Metric=="ram.total"&&loaded.Current.Dashboard[0].Width==3&&loaded.Current.OverlayX==-1920&&loaded.Current.Hotkey=="Alt+F9"&&loaded.Current.Alerts[0].Threshold==91,"persistencia completa de perfil");
Check(!File.Exists(store.Path+".tmp"),"guardado atómico sin temporal restante");
var copy=JsonSerializer.Deserialize<Profile>(JsonSerializer.Serialize(loaded.Current))!;copy.Name="copia";copy.Dashboard[0].Metric="gpu.load";Check(loaded.Current.Dashboard[0].Metric=="ram.total","duplicado independiente");
s.Interval=1;s.Current.Dashboard[0].Width=99;ConfigStore.Validate(s);Check(s.Interval==500&&s.Current.Dashboard[0].Width==4,"validación de límites");
File.WriteAllText(store.Path,"{corrupto");Check(store.Load().Profiles.Count==3&&store.Warning!=null&&File.Exists(store.Path+".invalid"),"recuperación de JSON dañado");
var rng=new Random(42);for(int columns=1;columns<=4;columns++){var tiles=Enumerable.Range(0,100).Select(_=>new Tile{Width=rng.Next(1,5),Height=rng.Next(1,4)}).ToList();var places=GridLayout.Pack(tiles,columns);var used=new HashSet<(int,int)>();foreach(var p in places){if(p.col<0||p.col+p.width>columns)throw new Exception("Rejilla fuera de columnas");for(int r=p.row;r<p.row+p.height;r++)for(int c=p.col;c<p.col+p.width;c++)if(!used.Add((r,c)))throw new Exception("Solapamiento");} }Check(true,"400 tarjetas sin solapamiento en 1–4 columnas");
var engine=new AlertEngine();var rule=new AlertRule{Metric="cpu.temp",Threshold=85};var metrics=new Dictionary<string,Metric>{{"cpu.temp",new("cpu.temp","CPU","°C",90)}};var now=DateTime.UtcNow;
Check(engine.Evaluate([rule],metrics,now).Count==1,"alerta al cruzar umbral");Check(engine.Evaluate([rule],metrics,now.AddSeconds(70)).Count==0,"sin avisos repetidos mientras persiste");
metrics["cpu.temp"]=metrics["cpu.temp"] with{Value=84};engine.Evaluate([rule],metrics,now.AddSeconds(71));metrics["cpu.temp"]=metrics["cpu.temp"] with{Value=90};Check(engine.Evaluate([rule],metrics,now.AddSeconds(72)).Count==0,"histéresis evita oscilación");
metrics["cpu.temp"]=metrics["cpu.temp"] with{Value=80};engine.Evaluate([rule],metrics,now.AddSeconds(73));metrics["cpu.temp"]=metrics["cpu.temp"] with{Value=90};Check(engine.Evaluate([rule],metrics,now.AddSeconds(74)).Count==1,"rearme tras recuperación");
rule.Enabled=false;Check(engine.Evaluate([rule],metrics,now.AddSeconds(140)).Count==0,"alerta desactivada");rule.Enabled=true;metrics["cpu.temp"]=metrics["cpu.temp"] with{Value=null};Check(engine.Evaluate([rule],metrics,now.AddSeconds(200)).Count==0,"sensor ausente no genera aviso");
Check(PresentMonService.Csv("\"game, test.exe\",42,\"a\"\"b\",16.7").SequenceEqual(new[]{"game, test.exe","42","a\"b","16.7"}),"parser CSV con comillas");
using(var pm=new PresentMonService()){Check(pm.Fps==null&&!pm.Running,"FPS sin captura es no disponible");pm.Stop();pm.Stop();Check(true,"detención idempotente");}
if(args.Contains("--hardware")) {
using var sensor=new SensorService();sensor.Open();sensor.Read();Thread.Sleep(1200);var values=sensor.Read();
Check(values["ram.total"].Value>0&&values["ram.used"].Value>0&&values["ram.free"].Value>0,"RAM real disponible");Check(values["cpu.load"].Value is >=0 and <=100,"CPU real dentro de rango");Check(values.Keys.Any(x=>x.StartsWith("disk.")&&x.EndsWith(".total")),"capacidad de discos reales");
if(args.Contains("--require-cpu-temperature"))Check(values["cpu.temp"].Value is >0 and <150,"temperatura CPU real obligatoria");
sensor.Read(new(){"cpu.load"});Check(sensor.LastUpdatedHardware.Count==0,"overlay CPU uso no lee sensores privilegiados");
sensor.Read(new(){"gpu.load"});Check(sensor.LastUpdatedHardware.Count<=1&&sensor.LastUpdatedHardware.All(x=>x.StartsWith("/gpu-")),"overlay GPU solo actualiza la GPU elegida");
var rawGpu=values.Values.FirstOrDefault(m=>m.Id.StartsWith("/gpu-")&&m.Value!=null);
if(rawGpu!=null){sensor.Read(new(){rawGpu.Id});Check(sensor.LastUpdatedHardware.Count==1&&rawGpu.Id.StartsWith(sensor.LastUpdatedHardware[0]+"/"),"sensor GPU explícito actualiza su hardware");}
var sw=Stopwatch.StartNew();for(int i=0;i<5;i++)sensor.Read(new(){"ram.used"});sw.Stop();Console.WriteLine("5 lecturas selectivas RAM: "+sw.Elapsed.TotalMilliseconds.ToString("0.0")+" ms");Check(sw.Elapsed.TotalSeconds<5,"lectura selectiva sin bloqueo prolongado");
Console.WriteLine("SENSORES REALES:");foreach(var m in values.Values)Console.WriteLine($"{m.Id}: {m.Display} [{m.Source}]");Console.WriteLine("Diagnóstico: "+sensor.Diagnostic);
sensor.Dispose();sensor.Dispose();bool readAfterDispose=false;try{sensor.Read();}catch(ObjectDisposedException){readAfterDispose=true;}Check(readAfterDispose,"cierre idempotente impide nuevas lecturas");
}
Console.WriteLine($"PRUEBAS CORRECTAS: {count}");
if(args.Contains("--ui")) UiIntegration.Run(root);
}finally{Directory.Delete(root,true);}

