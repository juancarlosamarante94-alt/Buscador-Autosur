# Buscador Autosur

Aplicacion de escritorio para Windows que permite:

- Buscar vehiculos por patente, modelo o motor.
- Buscar clientes por nombre, DNI o CUIT/CUIL.
- Leer agendas PDF, filtrar turnos Renault y excluir Nissan.
- Imprimir un turnero compacto en A4 vertical.
- Exportar el turnero generado directamente a PDF.
- Copiar cualquier dato con doble clic.
- Buscar e instalar actualizaciones desde GitHub Releases.

## Privacidad

Las bases `clientes.dat` y `vehiculos.dat` no forman parte del repositorio ni de las actualizaciones. El instalador de actualización conserva las bases existentes en cada computadora. La primera instalación debe hacerse con el paquete completo entregado internamente.

## Publicar una actualización

1. Actualizar `UpdateManager.AppVersion` en `Buscador_VIN_Instalable/BuscadorVIN.cs`.
2. Confirmar los cambios y crear una etiqueta con el mismo número, por ejemplo `v1.2.1`.
3. Publicar la etiqueta en GitHub.
4. El flujo automático crea una Release con el archivo `Buscador_Autosur_Actualizacion.zip`.

La aplicación consulta la Release más reciente cuando se presiona **Buscar actualizaciones**.
