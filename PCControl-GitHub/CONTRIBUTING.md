# Contribuir a PC Control

Gracias por ayudar a mejorar la aplicación.

## Informar de un fallo

Incluye la versión de Windows, versión de PC Control o commit, si se ejecutó como administrador, modelo del componente afectado y pasos para reproducirlo. Indica lo que esperabas y lo que ocurrió. Para un sensor ausente, aclara si PawnIO está instalado.

Las capturas son útiles, pero oculta nombres de usuario, rutas personales, direcciones de red y datos ajenos al problema. No publiques contraseñas, tokens ni archivos de configuración privados.

## Proponer cambios

1. Explica el problema que resuelve el cambio y limita su alcance.
2. Compila con el SDK .NET 8 en Windows.
3. Ejecuta las pruebas apropiadas y distingue lo verificado de lo pendiente.
4. Documenta cualquier dependencia nueva y su licencia.

## Decisiones que deben conservarse

- Monitorización manual, sin autoarranque ni servicios recolectores permanentes.
- Sin telemetría ni historial de métricas guardado en disco.
- Detener lecturas al minimizar sin overlay y finalizar auxiliares al cerrar.
- Mostrar sensores ausentes con honestidad, sin valores de ejemplo en producción.
- No instalar controladores ni elevar permisos automáticamente.
- No inyectar código en juegos ni eludir sistemas antitrampas.

No ejecutes pruebas de hardware, interfaz o captura FPS en equipos ajenos sin autorización. No afirmes compatibilidad con un juego que no hayas probado.

## Licencias

Las contribuciones al código propio se reciben bajo MIT, salvo que se indique expresamente otra licencia compatible. Los avisos de terceros y las obligaciones de sus licencias deben mantenerse. Consulta [TERCEROS.md](TERCEROS.md).
