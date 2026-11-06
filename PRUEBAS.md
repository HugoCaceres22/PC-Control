# Pruebas y límites de PC Control

Este documento distingue las comprobaciones realizadas de las que siguen pendientes. Los resultados de sensores dependen del equipo y de los permisos.

## Validación de la carpeta pública · 6 de noviembre de 2026

Se copió esta entrega a un directorio de comprobación independiente y se ejecutó su propio `build.ps1 -Test` con SDK .NET 8: restauración desde NuGet, compilación sin errores/advertencias, 16 pruebas de lógica correctas y publicación autónoma win-x64 generada. También se ejecutó `prepare-release.ps1 -SkipBuild`: los dos ZIP y sus sumas se prepararon correctamente. Se comprobó la presencia de LICENSE, los avisos de LibreHardwareMonitor y LGPL, las fuentes de las cinco bibliotecas indicadas y la ausencia del instalador/controlador PawnIO en la publicación.

## Comprobado en el entorno de desarrollo

- Compilación Release y publicación autónoma Windows x64 sin errores ni advertencias.
- Suite de lógica: 16 comprobaciones de configuración, perfiles independientes, validación, recuperación de JSON, alertas y parser CSV.
- Distribución de 400 tarjetas en rejillas de 1 a 4 columnas sin solapamientos.
- Suite con hardware real: 24 comprobaciones en el equipo usado, incluyendo CPU/RAM, discos, selección de hardware y cierre idempotente. El número puede variar según los sensores presentes.
- Integración WPF: arranque, métricas reales, alerta RAM en panel, perfiles, tarjetas, guardado y recarga, overlay, pausa y reanudación.
- Tema gaming ON/OFF: posiciones, tamaños, métricas y estilos originales conservados; preferencia guardada; estado independiente del overlay y de la captura FPS.
- Gestos de edición comprobados: reordenación por título, cambio de ancho con tirador y guardado desde el diálogo.
- Logo y recursos cargados en ventanas y ejecutable. ICO con resoluciones de 16 a 256 píxeles.
- Al minimizar sin overlay, el contador de lecturas permaneció constante; al restaurar, reanudó. Con overlay de RAM se omitieron CPU y otras familias no solicitadas.
- Tras cerrar las pruebas, no quedaron procesos recolectores propios ni sesiones ETW PCControl.
- Con PawnIO instalado y ejecución manual como administrador se obtuvieron temperatura, frecuencia y potencia de CPU reales. Sin elevar se indica el permiso que falta.

## Rendimiento: alcance de la medición

Una muestra breve de una publicación anterior a la incorporación del logo registró aproximadamente 0,53 % del total de CPU y 167 MiB de memoria residente, con actualización de 1500 ms. Es una referencia local, no una garantía para otras configuraciones. No se ha realizado una prueba prolongada de varias horas ni una medición independiente del consumo propio de GPU.

## Pendiente o condicionado

- Captura de FPS efectiva y sostenida en videojuegos reales. PresentMon detectó la falta de permisos ETW sin elevación y pudo iniciar una sesión elevada, pero no se verificaron FPS reales de un juego.
- Overlay con juegos, antitrampas y pantalla completa exclusiva concretos.
- Revisión visual directa de la última apariencia gaming: falló la captura del escritorio. Sus estados y controles sí se comprobaron por integración.
- Verificación auditiva de los avisos sonoros y todas las combinaciones de ventanas de alerta.
- Cambios de monitores/DPI, suspensión durante captura y desconexión física de hardware.
- Limpieza ETW tras un fallo forzado durante una captura exitosa de un videojuego.

## Ejecutar pruebas

```powershell
# Solo lógica/configuración, apropiado para comprobaciones de compilación
.\build.ps1 -Test

# Hardware real (sensores avanzados opcionales)
dotnet run --project tests/PCControl.Tests.csproj -c Release -- --hardware

# Interfaz y ciclo de vida con configuración temporal aislada
dotnet run --project tests/PCControl.Tests.csproj -c Release -- --ui

# Exigir temperatura CPU real: terminal elevada manualmente
dotnet run --project tests/PCControl.Tests.csproj -c Release -- --hardware --require-cpu-temperature
```

La prueba de interfaz abre ventanas y captura eventos del propio proceso de prueba; no busca videojuegos ni utiliza las preferencias personales. Las pruebas hardware muestran lecturas en su salida de consola. El programa de uso normal no guarda ese historial.
