# TK-5 · Clasificación y trazabilidad fotográfica

Aplicación HTML de ejecución en navegador para organizar fotografías del tanque TK-5, asociarlas a planos de fondo y soporte de membrana, documentar observaciones y generar informes PowerPoint.

## Protección de la información
Este repositorio es **público** y debe contener **únicamente el código genérico de la aplicación**. **No incorporar** fotografías del tanque, planos de Chevron, archivos JSON de inspección, observaciones, documentos contractuales ni rutas confidenciales.

Las fotografías y los planos se seleccionan desde el navegador; la aplicación no debe enviarlos a servidores. El registro JSON se conserva localmente y se puede exportar manualmente.

## Estructura esperada
- `index.html`: interfaz y lógica de la aplicación.
- `pptxgen.min.js`: biblioteca cliente para exportar PPTX, sujeta a su licencia.
- `LEEME.txt`: instrucciones de operación, sin información confidencial.

## Publicación
En GitHub: **Settings → Pages → Build and deployment → Deploy from a branch → main → /(root) → Save**.

La dirección prevista será:
https://ferlozada.github.io/TK5-Inspeccion-Fotografica/

Una vez publicada la aplicación, las actualizaciones del código confirmadas en la rama `main` estarán disponibles desde el mismo enlace. En Chrome, `Ctrl + F5` recarga la versión desplegada. La publicación de GitHub Pages puede demorar y debe finalizar antes de recargar.

## Advertencia sobre almacenamiento
`file://` y `https://` usan orígenes de almacenamiento distintos. Antes de migrar, guardar desde la aplicación local un respaldo JSON del trabajo existente. Tras publicar, usar **Abrir proyecto** para restaurarlo y volver a seleccionar fotos y planos.

## Estado
Repositorio inicial configurado. Pendiente de cargar los tres archivos de la aplicación y habilitar GitHub Pages.
