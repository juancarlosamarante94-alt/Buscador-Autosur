# Buscador Autosur

Aplicacion de escritorio para Windows que permite:

- Buscar vehiculos por patente, modelo o motor.
- Buscar clientes por nombre, DNI o CUIT/CUIL.
- Leer agendas PDF, filtrar turnos Renault y excluir Nissan.
- Imprimir un turnero en A4 vertical con altura de fila ajustable.
- Exportar el turnero generado directamente a PDF.
- Copiar cualquier dato con doble clic.
- Buscar e instalar actualizaciones desde GitHub Releases.

## Privacidad

Las bases `clientes.dat` y `vehiculos.dat` no forman parte del repositorio ni de las actualizaciones. El instalador de actualización conserva las bases existentes en cada computadora. La primera instalación debe hacerse con el paquete completo entregado internamente.

## Publicar una actualización

1. Actualizar `UpdateManager.AppVersion` en `Buscador_VIN_Instalable/BuscadorVIN.cs`.
2. Subir los cambios a `main`.
3. El flujo de Windows compila la aplicación, ejecuta las pruebas del turnero y verifica la compilación del instalador.
4. Si todas las pruebas pasan, crea la etiqueta y la Release con `Buscador_Autosur_Actualizacion.zip`.

La aplicación consulta la Release más reciente cuando se presiona **Buscar actualizaciones**.

## Turnero actualizado — 1.2.9

Se trasladaron las mejoras del turnero de App repuestos 1.3.7. La grilla usa barras nativas y permite navegar con teclado, manteniendo fecha y hora fijas. La vista previa es independiente y se cierra desde el botón, la cruz o Esc. Incluye páginas y zoom. La altura mínima de turno impreso se puede ajustar de 28 a 60 mm, inicialmente 32 mm, tanto para imprimir como para exportar PDF.

El lector por encabezados incluye recuperación por posición de vehículo para agendas donde las horas quedan fuera de los límites del encabezado. Las pruebas usan un PDF sintético con siete Renault, dos Nissan, un horario vacío, cambios de escala y continuaciones entre páginas. También reproducen cero filas en el lector anterior y comprueban su recuperación. La verificación final con el PDF privado del usuario sigue pendiente; ese archivo no se publica en GitHub.

App taller mantiene Vehículos, Clientes y Turnero y su propio repositorio de actualizaciones. Sus rutas de instalación y bases locales se conservan.
