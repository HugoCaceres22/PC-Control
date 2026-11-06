<p align="center">
  <img src="assets/pc-control.png" alt="Logo de PC Control" width="125">
</p>
<h1 align="center">PC Control</h1>
<p align="center"><strong>Tu equipo, en tiempo real.</strong><br>Monitorización local para Windows, con panel editable y overlay gaming.</p>
<p align="center">
  <img alt="Windows 11 x64" src="https://img.shields.io/badge/Windows-11%20x64-0078D4?style=flat-square">
  <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8-512BD4?style=flat-square">
  <img alt="Interfaz WPF" src="https://img.shields.io/badge/Interfaz-WPF-B193FF?style=flat-square">
  <img alt="Licencia del código propio: MIT" src="https://img.shields.io/badge/C%C3%B3digo%20propio-MIT-51E3D5?style=flat-square">
</p>

<p align="center">
  <a href="#qué-hace">Funciones</a> ·
  <a href="#instalación-y-uso">Instalación</a> ·
  <a href="#compilar-desde-el-código">Compilar</a> ·
  <a href="#repositorios-y-créditos">Repositorios utilizados</a> ·
  <a href="#licencia">Licencia</a>
</p>

---

**PC Control** es una aplicación de escritorio escrita en C# y WPF para consultar el rendimiento del ordenador. Funciona en local: no necesita una cuenta, una API de pago ni un servidor externo.

El usuario decide cuándo abrirla, qué sensores mostrar y cuándo activar el overlay. La aplicación no se añade al inicio de Windows ni mantiene un recolector oculto cuando se cierra.

> **Estado del proyecto:** aplicación compilable y ejecutable. Panel, perfiles, alertas y ciclo de vida cuentan con pruebas. La captura de FPS y la compatibilidad del overlay con videojuegos concretos siguen pendientes de validación real. Consulta [PRUEBAS.md](PRUEBAS.md).

## Qué hace

| Función | Descripción |
|---|---|
| **Monitorización** | Uso de CPU, RAM, GPU, discos y red; temperaturas, frecuencias, potencia y VRAM cuando el hardware permite leerlas. |
| **Panel editable** | Añadir y quitar tarjetas, arrastrarlas, redimensionarlas y elegir sensor, color y tipo de gráfico. |
| **Modo gaming ON/OFF** | Cambia la apariencia de la ventana conservando métricas y distribución. |
| **Overlay manual** | Superposición configurable con posición, tamaño, columnas, colores, fondo y atajo global. Deja pasar los clics. |
| **Perfiles** | Crear, duplicar, renombrar, editar y eliminar configuraciones; el cambio es manual. |
| **Alertas** | Umbrales por sensor, aviso en panel, sonido o ventana y control de repetición. |
| **Privacidad** | Preferencias guardadas localmente y gráficos temporales en memoria; sin telemetría ni historial de rendimiento en disco. |

Si falta un sensor, la interfaz muestra **No disponible**. No se sustituyen lecturas ausentes por datos simulados.

## Instalación y uso

### Aplicación preparada para Windows

Cuando el repositorio publique una versión en **Releases**, descarga `PCControl-win-x64.zip`, extrae **todo** su contenido y abre `PCControl.exe`. No necesita Visual Studio ni instalar .NET, porque la publicación incluye el runtime. Conserva las DLL, `tools` y `licenses` junto al ejecutable.

Esta carpeta contiene el código fuente. No basta con descargarla y hacer doble clic en un archivo `.cs`: para obtener el ejecutable hay que compilar o descargar una publicación preparada.

### Temperatura de CPU y permisos

Algunos sensores avanzados requieren el controlador **PawnIO** y permisos de administrador:

1. Consulta e instala, si lo necesitas, la **edición oficial firmada** desde [pawnio.eu](https://pawnio.eu/).
2. Abre `PCControl.exe` con clic derecho → **Ejecutar como administrador**.
3. Revisa los sensores disponibles en **Ajustes y sensores**.

PC Control no instala controladores automáticamente ni cambia los permisos o las protecciones de Windows. El controlador puede permanecer instalado y cargado; las solicitudes de lectura de PC Control terminan al cerrar la aplicación. Otras aplicaciones independientes pueden utilizar el mismo controlador.

### Controles principales

- **Panel → Editar panel:** arrastra el título de una tarjeta para moverla y su esquina inferior derecha para cambiar tamaño. El lápiz abre sus opciones.
- **Modo gaming:** interruptor visual arriba a la derecha. Su preferencia se conserva entre sesiones.
- **Activar overlay:** muestra u oculta la superposición. El atajo inicial es **Ctrl + Shift + F10**; se puede cambiar.
- **Overlay y FPS:** configura la superposición y selecciona expresamente el proceso que deseas capturar. Los FPS no comienzan automáticamente.
- **Perfiles y Alertas:** gestionan distribuciones y avisos desde la interfaz.

Las preferencias se guardan en `%LOCALAPPDATA%\PCControl\settings.json`. Ese archivo contiene configuración, no lecturas de rendimiento. La aplicación no está firmada con un certificado comercial.

## Privacidad y ciclo de vida

| Estado | Lecturas de PC Control |
|---|---|
| Aplicación cerrada | Ningún proceso recolector propio activo. |
| Ventana abierta | Actualización periódica; intervalo configurable de 500 a 10000 ms. |
| Minimizada sin overlay | Lecturas periódicas suspendidas y captura FPS detenida. |
| Minimizada con overlay | Actualiza las familias de sensores necesarias para las métricas seleccionadas. |
| Cierre | Termina el overlay, detiene la captura propia y libera los sensores. |

Las curvas conservan hasta 60 segundos en RAM. No hay base de datos, cuentas, analítica de uso ni guardado automático de CSV. Los perfiles no cambian al detectar juegos y el overlay siempre comienza apagado.

## Compilar desde el código

**Requisitos:** Windows 11 x64 y [SDK de .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0). Los paquetes se restauran desde NuGet.

Desde PowerShell, situado en esta carpeta:

```powershell
# Compilar y crear una publicación autónoma
.\build.ps1

# Además, ejecutar las comprobaciones de lógica y configuración
.\build.ps1 -Test
```

El resultado queda en `dist/PCControl/PCControl.exe`. El script comprueba el hash del PresentMon incluido antes de empaquetarlo.

También puedes compilar directamente:

```powershell
dotnet restore PCControl.csproj --configfile NuGet.Config
dotnet build PCControl.csproj -c Release --no-restore
dotnet publish PCControl.csproj -c Release -r win-x64 --self-contained true -p:RestoreConfigFile=NuGet.Config -o dist/PCControl
```

### Pruebas opcionales

```powershell
# Hardware real; los sensores avanzados pueden no estar disponibles
dotnet run --project tests/PCControl.Tests.csproj -c Release -- --hardware

# Integración WPF: abre ventanas y usa configuración temporal aislada
dotnet run --project tests/PCControl.Tests.csproj -c Release -- --ui

# Temperatura CPU obligatoria: ejecutar manualmente como administrador
dotnet run --project tests/PCControl.Tests.csproj -c Release -- --hardware --require-cpu-temperature
```

Las pruebas generales admiten sensores ausentes. La última orden falla si no se obtiene una temperatura CPU válida. Detalles y límites de la evidencia: [PRUEBAS.md](PRUEBAS.md).

## Repositorios y créditos

PC Control utiliza estos proyectos. **El crédito y las licencias de sus autores se conservan; no forman parte del código propio bajo MIT.**

| Proyecto | Para qué se utiliza | Acceso directo | Licencia principal |
|---|---|---|---|
| **LibreHardwareMonitor** | Lectura de sensores de CPU, GPU y almacenamiento. | [Repositorio](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) · [Versión 0.9.6](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/tree/v0.9.6) | MPL-2.0; incluye componentes con otros términos. |
| **Intel PresentMon** | Captura externa de eventos de presentación para los FPS, sin inyección en juegos. | [Repositorio](https://github.com/GameTechDev/PresentMon) · [Versión 2.3.1](https://github.com/GameTechDev/PresentMon/tree/v2.3.1) | MIT. |
| **PawnIO** | Controlador externo que permite acceder a sensores protegidos. No se incluye su instalador. | [Repositorio](https://github.com/namazso/PawnIO) · [Descarga oficial](https://pawnio.eu/) | GPL-2.0-or-later con excepción de interoperabilidad; véase su licencia. |
| **PawnIO.Modules** | Módulos que LibreHardwareMonitor incorpora para acceder al hardware. | [Repositorio](https://github.com/namazso/PawnIO.Modules) · [Versión 0.1.6](https://github.com/namazso/PawnIO.Modules/releases/tag/0.1.6) | LGPL-2.1. |
| **DiskInfoToolkit** | Dependencia de LibreHardwareMonitor para información de discos. | [Repositorio](https://github.com/Blacktempel/DiskInfoToolkit) | MPL-2.0. |
| **RAMSPDToolkit** | Dependencia de LibreHardwareMonitor relacionada con información SPD. | [Repositorio](https://github.com/Blacktempel/RAMSPDToolkit) | MPL-2.0. |
| **BlackSharp** | Componentes compartidos utilizados por las bibliotecas anteriores. | [Repositorio](https://github.com/Blacktempel/BlackSharp) | MPL-2.0. |
| **HidSharp** | Dependencia para acceso a dispositivos HID. | [Proyecto oficial](https://software.seekye.com/hidsharp) · [Paquete NuGet](https://www.nuget.org/packages/HidSharp/2.6.4) | Apache-2.0. |
| **.NET y WPF** | Runtime, bibliotecas y ventanas nativas de la aplicación. | [.NET Runtime](https://github.com/dotnet/runtime) · [WPF](https://github.com/dotnet/wpf) | MIT y avisos de terceros. |
| **Mono.Posix.NETStandard** | Dependencia transitiva declarada por LibreHardwareMonitor. | [Repositorio Mono](https://github.com/mono/mono) · [Paquete NuGet](https://www.nuget.org/packages/Mono.Posix.NETStandard/1.0.0) | Avisos incluidos en `licenses`. |

Las versiones, fuentes correspondientes, hashes y condiciones de redistribución se detallan en [TERCEROS.md](TERCEROS.md). El inventario de paquetes está en [DEPENDENCIAS.md](DEPENDENCIAS.md) y los textos de licencia en [licenses/](licenses/).

## Limitaciones conocidas

- **Sensores:** su disponibilidad depende del equipo, del controlador y de los permisos. La potencia de algunas GPU puede no exponerse.
- **FPS:** requiere permisos ETW. Se probó el inicio de la captura y la gestión de errores, pero no se ha verificado una captura completa en un videojuego real. Los FPS calculados son de presentaciones, no necesariamente de fotogramas mostrados; varias swapchains pueden afectar a la media.
- **Overlay:** pensado para ventana y ventana sin bordes. La pantalla completa exclusiva puede ocultarlo; no se promete compatibilidad con antitrampas o juegos concretos y no se eluden sus restricciones.
- **Interfaz:** el interruptor gaming cuenta con pruebas funcionales; la revisión visual directa de su último aspecto quedó pendiente por un fallo de captura del escritorio.
- **Unidades:** memoria rotulada GB representa GiB; red MB/s utiliza unidades decimales.

## Estructura

```text
PCControl/
├── App.xaml / App.xaml.cs          Inicio de la aplicación
├── MainWindow.cs                  Panel, editores y configuraciones
├── Theme.xaml / Ui.cs              Estilos y controles
├── Models.cs                      Perfiles, preferencias y alertas
├── Sensors.cs                     Proveedores de métricas
├── OverlayWindow.cs               Ventana de superposición
├── PresentMonService.cs            Captura FPS y cierre del auxiliar
├── BrandAssets.cs / assets/        Logo e icono
├── tests/                         Código de las pruebas
├── tools/PresentMon.exe            Binario oficial, sin modificar
├── licenses/                      Licencias y avisos de terceros
├── build.ps1                      Compilación y publicación
├── prepare-release.ps1            ZIP de Windows y fuentes de terceros
└── LICENSE                        MIT para el código propio
```

`bin`, `obj`, `.packages` y `dist` se generan localmente y están excluidos de Git. Las dependencias NuGet no se suben como DLL al repositorio.

## Contribuir

Puedes proponer mejoras o informar de fallos con los pasos para reproducirlos. Evita adjuntar datos personales o configuraciones privadas. Consulta [CONTRIBUTING.md](CONTRIBUTING.md).

## Licencia

El código propio de **PC Control** se publica bajo [MIT](LICENSE). Permite utilizarlo, modificarlo y redistribuirlo conservando el aviso de licencia.

Las bibliotecas, módulos y herramientas de terceros mantienen **sus propias licencias y autorías**. La licencia MIT de este repositorio no sustituye MPL, LGPL, Apache u otras condiciones indicadas en [TERCEROS.md](TERCEROS.md). Para redistribuir una aplicación compilada, sigue el procedimiento de ese documento y conserva los avisos y el acceso a las fuentes correspondientes.
