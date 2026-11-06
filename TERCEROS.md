# Avisos y fuentes de terceros

La licencia [MIT](LICENSE) de PC Control se aplica al código propio del proyecto. No cambia las licencias, copyright ni derechos sobre los componentes siguientes, que se utilizan sin modificar.

## Componentes principales

| Componente | Versión | Licencia / aviso | Fuente correspondiente |
|---|---|---|---|
| LibreHardwareMonitorLib | 0.9.6 | [MPL-2.0](licenses/LibreHardwareMonitor.txt) y [avisos internos](licenses/LibreHardwareMonitor-THIRD-PARTY-NOTICES.txt) | [Código del paquete publicado](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/tree/3d331e3370efb858411f19511373eff65a218701) |
| PawnIO.Modules, incorporado por LHM | 0.1.6 | [LGPL-2.1](licenses/PawnIO.Modules-LGPL-2.1.txt) | [Fuentes de la versión 0.1.6](https://github.com/namazso/PawnIO.Modules/tree/0.1.6) |
| PresentMon | 2.3.1 | [MIT; copyright Intel Corporation](licenses/PresentMon.txt) | [Código de la versión 2.3.1](https://github.com/GameTechDev/PresentMon/tree/v2.3.1) |
| DiskInfoToolkit | 1.1.2 | MPL-2.0; copyright Florian K. | [Fuente exacta](https://github.com/Blacktempel/DiskInfoToolkit/tree/25319eae5781e75bcf141e844ceab2afe94d40ea) |
| RAMSPDToolkit-NDD | 1.4.2 | MPL-2.0; copyright Florian K. | [Fuente exacta](https://github.com/Blacktempel/RAMSPDToolkit/tree/3b47b960e0830fef344624ad5e389675d5f0a1ce) |
| BlackSharp.Core | 1.0.7 | MPL-2.0; copyright Florian K. | [Fuente exacta](https://github.com/Blacktempel/BlackSharp/tree/c70b735c6cec123ee8a046ac4a0bc6c606f52cf0) |
| HidSharp | 2.6.4 | [Apache-2.0; copyright James F. Bellinger](licenses/HidSharp.txt) | [Proyecto oficial](https://software.seekye.com/hidsharp) · [NuGet](https://www.nuget.org/packages/HidSharp/2.6.4) |
| Mono.Posix.NETStandard | 1.0.0 | [Avisos de Mono](licenses/Mono.Posix.NETStandard.txt) | [Repositorio oficial](https://github.com/mono/mono) · [NuGet](https://www.nuget.org/packages/Mono.Posix.NETStandard/1.0.0) |
| .NET Runtime y bibliotecas Microsoft | Versiones en DEPENDENCIAS.md | MIT y avisos de terceros | [.NET Runtime](https://github.com/dotnet/runtime) |
| WPF | Runtime de la publicación .NET 8 | [Licencia](licenses/wpf-LICENSE.txt) y [avisos](licenses/wpf-THIRD-PARTY-NOTICES.txt) | [Repositorio WPF](https://github.com/dotnet/wpf) |

El mismo [texto MPL-2.0](licenses/LibreHardwareMonitor.txt) corresponde a DiskInfoToolkit, RAMSPDToolkit y BlackSharp. Los avisos íntegros de LibreHardwareMonitor también mencionan componentes del proyecto original que pueden no formar parte de la biblioteca o de la publicación Windows; se conservan completos.

## PawnIO: controlador externo

El instalador y el controlador PawnIO **no se distribuyen en esta carpeta ni en la publicación de PC Control**. Quien necesite sensores protegidos puede instalar la edición oficial firmada desde [pawnio.eu](https://pawnio.eu/).

El código del controlador está en [namazso/PawnIO](https://github.com/namazso/PawnIO), bajo GPL-2.0-or-later con la excepción de interoperabilidad descrita por sus autores. Los módulos PawnIO de LibreHardwareMonitor son un componente distinto y están sujetos a LGPL-2.1. No debe confundirse la licencia de ambos.

## Binario de PresentMon

Se conserva el ejecutable oficial sin modificar para que una compilación del proyecto incluya la captura FPS. El archivo es pequeño y está versionado expresamente; no es un resultado propio de compilación.

- Ruta: `tools/PresentMon.exe`.
- [Descarga oficial de la versión 2.3.1](https://github.com/GameTechDev/PresentMon/releases/download/v2.3.1/PresentMon-2.3.1-x64.exe).
- SHA-256: `364E5D98D4D134BD54DD25C22ED2CA2F4883F8BC3ED6502BEE0C151E3436D30C`.
- Firma Authenticode comprobada en el equipo de preparación: Intel Corporation, válida.

Los scripts verifican ese hash antes de publicar. No sustituyas el archivo por otra versión sin revisar integración, licencia y suma de comprobación.

## Redistribuir una aplicación compilada

La carpeta del repositorio contiene principalmente código propio, recursos y avisos, no las DLL de LibreHardwareMonitor ni sus dependencias NuGet. Al compilar se restauran esas bibliotecas, por lo que una publicación ejecutable requiere conservar también las condiciones de terceros.

El script `prepare-release.ps1` prepara dos descargas complementarias:

1. `PCControl-win-x64.zip`: publicación Windows con los textos de licencia y este aviso.
2. `PCControl-fuentes-terceros.zip`: archivos de las fuentes fijadas de las bibliotecas MPL y de los módulos LGPL, junto a su índice de origen.

**Publica los dos archivos juntos en la misma Release.** No basta con subir solamente el ejecutable o borrar las licencias. Para estas bibliotecas se facilita acceso al código correspondiente bajo sus licencias originales. El repositorio de PC Control mantiene su propio código y las instrucciones necesarias para reconstruir la aplicación; no impone restricciones a la sustitución o modificación de esas bibliotecas conforme a sus términos.

La copia de fuentes de LibreHardwareMonitor excluye únicamente su instalador PawnIO ajeno, que PC Control no distribuye; conserva el resto del árbol, los módulos y sus licencias. El índice `third-party-sources.json` contiene las revisiones utilizadas. Si actualizas una dependencia, actualiza ese índice, los avisos y la publicación de fuentes correspondiente.

Conservar MIT en PC Control no permite quitar avisos de terceros, cambiar su autoría ni relicenciar sus archivos. Si modificas código de una biblioteca cubierta por MPL/LGPL, documenta y ofrece también las modificaciones bajo las condiciones aplicables.
