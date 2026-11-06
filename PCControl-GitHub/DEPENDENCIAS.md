# Inventario de dependencias

Las dependencias directas se fijan en PCControl.csproj. NuGet resuelve las demás al compilar. No se incluyen sus DLL en el repositorio: se restauran desde nuget.org y se copian a la publicación según corresponda a Windows x64.

| Dependencia | Versión utilizada | Tipo | Proyecto / fuente |
|---|---|---|---|
| LibreHardwareMonitorLib | 0.9.6 | Directa, MPL-2.0 y avisos internos | [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) |
| System.Diagnostics.PerformanceCounter | 8.0.1 | Directa, MIT | [.NET Runtime](https://github.com/dotnet/runtime) |
| DiskInfoToolkit | 1.1.2 | Transitiva, MPL-2.0 | [DiskInfoToolkit](https://github.com/Blacktempel/DiskInfoToolkit) |
| RAMSPDToolkit-NDD | 1.4.2 | Transitiva, MPL-2.0 | [RAMSPDToolkit](https://github.com/Blacktempel/RAMSPDToolkit) |
| BlackSharp.Core | 1.0.7 | Transitiva, MPL-2.0 | [BlackSharp](https://github.com/Blacktempel/BlackSharp) |
| HidSharp | 2.6.4 | Transitiva, Apache-2.0 | [Proyecto oficial](https://software.seekye.com/hidsharp) |
| Mono.Posix.NETStandard | 1.0.0 | Transitiva, avisos Mono | [Mono](https://github.com/mono/mono) |
| System.Management | 10.0.2 | Transitiva, MIT | [.NET](https://github.com/dotnet/dotnet) |
| System.IO.Ports | 10.0.3 | Transitiva, MIT | [.NET](https://github.com/dotnet/dotnet) |
| System.Threading.AccessControl | 10.0.3 | Transitiva, MIT | [.NET](https://github.com/dotnet/dotnet) |
| System.CodeDom | 10.0.2 | Transitiva, MIT | [.NET](https://github.com/dotnet/dotnet) |
| System.Configuration.ConfigurationManager | 8.0.1 | Transitiva, MIT | [.NET Runtime](https://github.com/dotnet/runtime) |
| System.Diagnostics.EventLog | 8.0.1 | Transitiva, MIT | [.NET Runtime](https://github.com/dotnet/runtime) |
| System.Security.Cryptography.ProtectedData | 8.0.0 | Transitiva, MIT | [.NET Runtime](https://github.com/dotnet/runtime) |

La restauración puede incluir paquetes de resolución runtime para distintas plataformas. Eso no significa que se incluyan todos en la publicación Windows. El inventario efectivo de cada compilación se puede consultar con `dotnet list PCControl.csproj package --include-transitive`.

## Fuera de NuGet

- **PresentMon 2.3.1:** ejecutable oficial en tools/PresentMon.exe, MIT, con hash verificado por build.ps1.
- **PawnIO.Modules 0.1.6:** módulos incorporados por LibreHardwareMonitor, LGPL-2.1; fuentes fijadas en third-party-sources.json.
- **PawnIO:** controlador externo opcional para determinados sensores. Su instalador no se incluye ni se ejecuta desde la aplicación.
- **.NET/WPF:** runtime incluido en publicaciones autónomas; licencias y avisos en licenses/.

Fuentes exactas y reglas para publicar los binarios: [TERCEROS.md](TERCEROS.md).
