# Abrir PowerPoint automáticamente — r52

Windows 10/11 con Microsoft PowerPoint y .NET Framework 4.8. No requiere administrador ni Python. GitHub Pages por sí solo no puede abrir programas del PC.

1. [Descarga el proyecto ZIP](https://github.com/ferlozada/TK5-Inspeccion-Fotografica/archive/refs/heads/main.zip) y extrae su contenido.
2. En `windows-helper`, haz doble clic en **Instalar.cmd**, una sola vez. Se compila el helper desde el código incluido, se configura el inicio de sesión y se abre la web vinculada. Windows puede pedir autorización para ejecutar un archivo descargado; comprueba su procedencia. El instalador no está firmado.
3. En la web, abre tu proyecto existente y pulsa **Exportar PowerPoint**. Si Chrome/Edge pide acceso a aplicaciones o red local, permítelo para este sitio. Al terminar, el helper guarda el PPTX e inicia Microsoft PowerPoint. El aviso dice «apertura solicitada»: no confirma que PowerPoint haya terminado de cargarlo.

Si el helper está detenido, falta PowerPoint, se bloquea el permiso o falla la conexión, la web descarga el archivo generado sin volver a generar las diapositivas. Ábrelo desde las descargas. No reintenta la entrega automáticamente; si se perdió la respuesta del helper, también puede existir una copia local.

Las presentaciones quedan en `%LOCALAPPDATA%\TK5PowerPoint\Presentaciones`, con nombres únicos. El helper nunca recibe JSON, rutas de origen, fotos individuales ni planos; solo recibe el informe final. No modifica tu proyecto. Guarda el JSON mediante **Guardar proyecto**, como antes.

Para otro navegador/perfil, vuelve a ejecutar **Instalar.cmd**: conserva la clave y vincula el navegador predeterminado. Para detener el inicio automático, ejecuta `Desinstalar.ps1` con PowerShell; conserva las presentaciones. La instalación vive en `%LOCALAPPDATA%\TK5PowerPoint`. Puedes iniciar `TK5PowerPoint.exe` allí sin reiniciar Windows.

## Seguridad y límites

- Loopback IPv4 exclusivamente, puerto 18765; verifica Host y Origin exactos. Sin firewall, puertos externos ni privilegios administrativos.
- Token aleatorio de 256 bits, privado al usuario de Windows; se entrega mediante fragmento URL, se retira de la barra y se conserva en el almacenamiento del navegador. No lo compartas. Para revocarlo, desinstala y reinstala.
- CORS solo `https://ferlozada.github.io`. Los orígenes web no distinguen rutas: otros proyectos del mismo propietario comparten origen y almacenamiento. La seguridad también depende de la integridad de las páginas/scripts de ese propietario y de las bibliotecas que carga la web. No es aislamiento frente a malware local ni frente a una página comprometida del mismo origen.
- Solo POST autenticado, tamaño máximo 256 MiB, tiempos de recepción limitados; valida ZIP, XML, tipos de presentación, nombres de entradas y límites de expansión. Rechaza macros declaradas, VBA, objetos embebidos y relaciones externas. No extrae ZIP ni acepta nombres/rutas enviados por la web. No sustituye antivirus ni valida cada detalle del formato Office.
- Cada archivo se crea exclusivamente sin sobrescribir. Se prefiere POWERPNT.EXE registrado; como alternativa se usa automatización COM de PowerPoint. No ejecuta comandos recibidos por HTTP.
- El navegador puede bloquear loopback por política empresarial; la descarga sigue disponible. No cambies opciones de seguridad globales. Referencia: [permisos locales de Chrome](https://developer.chrome.com/docs/modern-web-guidance/guides/security/local-network-access).

## Verificación técnica

`tests/test_helper.py` compila y prueba el servidor con apertura desactivada y datos sintéticos en la raíz externa indicada. `tests/test_web.cjs` comprueba entrega/fallback de la integración. La apertura visual real necesita PowerPoint instalado y un navegador con permiso local. Ninguna prueba debe usar los datos originales de inspección.
