# MongoDB Backup & Restore CLI (.NET)

CLI en .NET para realizar copias de seguridad (backup) y restauraciones (restore) de bases de datos MongoDB en:
- Instancia local de MongoDB en Windows.
- Contenedor Docker local (Windows).
- Contenedor Docker remoto o instancia remota accesible por red.

Este proyecto sigue buenas prácticas de repositorios, versionado semántico y convenciones de contribución para facilitar su mantenimiento y escalabilidad.

## Características (MVP y roadmap)
- Backup y restore de una base de datos MongoDB.
- Modos de origen/destino:
  - Local (mongodump/mongorestore contra localhost).
  - Docker local (docker exec en el contenedor de MongoDB).
  - **Detección automática de contenedores Docker con MongoDB** ✓
  - Remoto (conexión por host:puerto o, en roadmap, Docker remoto).
- **Validación automática de binarios MongoDB dentro de contenedores** ✓
- Soporte de autenticación (usuario/contraseña y authSource).
- **Compresión de backups (ZIP/TAR.GZ)** ✓
- Directorio de salida configurable.
- Políticas de retención [roadmap].
- Cifrado de backups [roadmap].
- Logs estructurados y niveles de verbosidad.
- Integración CI/CD con GitHub Actions [roadmap].
- Distribución como .NET global tool [roadmap].

## Requisitos
- .NET SDK 8.0 o superior
- Docker Desktop (para escenarios con contenedores locales)
- MongoDB Database Tools (mongodump y mongorestore) disponibles en PATH:
  - Descarga: https://www.mongodb.com/try/download/database-tools
- Acceso al host/puerto de MongoDB en escenarios remotos
- (Opcional) GitHub CLI para automatizar tareas con el repo

## Instalación (desde código fuente)
```bash
git clone https://github.com/JoseRGWeb/mongodb-backup-restore-cli.git
cd mongodb-backup-restore-cli
dotnet build
```

En el roadmap se publicará como herramienta global:
```bash
# futuro
dotnet tool install -g mongodb-br
```

## Uso
La CLI expondrá dos comandos principales: `backup` y `restore`. Los parámetros pueden pasarse por línea de comandos o por variables de entorno.

### Ejemplos de backup
- MongoDB local (Windows):
```bash
mongodb-br backup --db MyDatabase --host localhost --port 27017 --out ./backups/2025-11-01
```

- Docker local (contenedor llamado "mongo"):
```bash
mongodb-br backup --db MyDatabase --in-docker --container-name mongo --out ./backups/2025-11-01
```

- Docker local con auto-detección (detecta automáticamente el contenedor MongoDB):
```bash
mongodb-br backup --db MyDatabase --in-docker --out ./backups/2025-11-01
```
> **Nota**: Si existe un único contenedor con MongoDB en ejecución, se detectará automáticamente. Si hay múltiples contenedores, debe especificar `--container-name`.

- Host remoto (MongoDB expuesto por red):
```bash
mongodb-br backup --db MyDatabase --host mongo.example.com --port 27017 \
  --user myuser --password "mypassword" --auth-db admin \
  --out ./backups/2025-11-01
```

- **Backup con compresión ZIP**:
```bash
mongodb-br backup --db MyDatabase --out ./backups/2025-11-01 --compress zip
```

- **Backup con compresión TAR.GZ**:
```bash
mongodb-br backup --db MyDatabase --out ./backups/2025-11-01 --compress targz
```

- **Backup con compresión usando variable de entorno**:
```bash
export MONGO_COMPRESSION=zip
mongodb-br backup --db MyDatabase --out ./backups/2025-11-01
```

### Ejemplos de restore
- Restaurar a MongoDB local:
```bash
mongodb-br restore --db MyDatabase --host localhost --port 27017 --from ./backups/2025-11-01
```

- Restaurar a contenedor Docker local:
```bash
mongodb-br restore --db MyDatabase --in-docker --container-name mongo --from ./backups/2025-11-01
```

- Restaurar a contenedor Docker con auto-detección:
```bash
mongodb-br restore --db MyDatabase --in-docker --from ./backups/2025-11-01
```
> **Nota**: Si existe un único contenedor con MongoDB en ejecución, se detectará automáticamente.

- Restaurar a host remoto:
```bash
mongodb-br restore --db MyDatabase --host mongo.example.com --port 27017 \
  --user myuser --password "mypassword" --auth-db admin \
  --from ./backups/2025-11-01
```

- **Restaurar desde backup comprimido (auto-detección de formato)**:
```bash
mongodb-br restore --db MyDatabase --from ./backups/2025-11-01_20251101_120000.zip
```
> **Nota**: El formato de compresión se detecta automáticamente por la extensión del archivo (.zip o .tar.gz)

- **Restaurar especificando formato de compresión**:
```bash
mongodb-br restore --db MyDatabase --from ./backups/backup.tar.gz --compress targz
```

### Opciones principales
- `--db` Nombre de la base de datos (obligatorio).
- `--host` Host de MongoDB (por defecto: `localhost`).
- `--port` Puerto de MongoDB (por defecto: `27017`).
- `--user`, `--password` Credenciales (si aplica).
- `--auth-db` Base de autenticación (p. ej., `admin`).
- `--uri` Cadena de conexión completa (alternativa a host/port/user/password).
- `--in-docker` Indica que el origen/destino es un contenedor local.
- `--container-name` Nombre del contenedor (con `--in-docker`). Si no se especifica, se intentará detectar automáticamente.
- `--out` Ruta de salida del backup (para `backup`).
- `--from` Ruta de origen del backup (para `restore`).
- `--compress` Formato de compresión: `none`, `zip`, `targz` (por defecto: `none`). También se puede configurar con la variable de entorno `MONGO_COMPRESSION`.
- `--drop` Eliminar la base de datos antes de restaurar (solo para `restore`).
- `--encrypt` Habilitar cifrado [roadmap].
- `--retention-days` Días a conservar [roadmap].
- `--verbose` Aumenta la verbosidad de logs.

## Modo Docker

### Detección Automática de Contenedores
Cuando se usa `--in-docker` sin especificar `--container-name`, la herramienta intentará detectar automáticamente contenedores Docker que ejecutan MongoDB:

1. Busca contenedores basados en la imagen oficial de MongoDB
2. Busca contenedores con el puerto 27017 expuesto
3. Valida que los binarios `mongodump`/`mongorestore` estén disponibles en el contenedor

Si se encuentra **un único contenedor**, se usa automáticamente. Si hay **múltiples contenedores** o **ninguno**, se muestra un error indicando que debe especificar `--container-name`.

### Validación de Binarios
La herramienta valida automáticamente que:
- El contenedor Docker existe y está en ejecución
- Los binarios necesarios (`mongodump` para backup, `mongorestore` para restore) están disponibles dentro del contenedor
- El contenedor tiene acceso a MongoDB

Esto asegura que las operaciones fallen rápidamente con mensajes claros si faltan dependencias.

### Variables de entorno soportadas
- `MONGO_URI`
- `MONGO_HOST`, `MONGO_PORT`
- `MONGO_USER`, `MONGO_PASSWORD`, `MONGO_AUTH_DB`
- `MONGO_COMPRESSION` - Formato de compresión para backups (none, zip, targz)
- `DOCKER_CONTEXT` (para escenarios de Docker remoto en roadmap)
- `MONGOBR_OUT_DIR` (directorio por defecto de backups)

## Arquitectura (propuesta)
- `MongoBackupRestore.Core`: abstracciones y servicios para orquestar `mongodump`/`mongorestore` de forma cross-platform, compresión, logging y validaciones.
- `MongoBackupRestore.Cli`: interfaz de línea de comandos usando `System.CommandLine`, mapeo de opciones/variables de entorno y UX.
- `MongoBackupRestore.Tests`: pruebas unitarias/funcionales (con fixtures de Docker para pruebas locales).

Patrones clave:
- Inversión de dependencias para proveedores de origen/destino (local, docker, remoto).
- Adaptadores para ejecutar procesos externos (`mongodump`, `mongorestore`, `docker`) con manejo robusto de errores y timeouts.
- Logs estructurados (Serilog/ILogger) y códigos de salida consistentes.

Estructura sugerida:
```
/src
  /MongoBackupRestore.Core
  /MongoBackupRestore.Cli
/tests
  /MongoBackupRestore.Tests
/.github
  /workflows/ci.yml      # build + test (roadmap)
.editorconfig
.gitattributes
.gitignore
LICENSE
README.md
```

## Calidad, estilo y entregas
- Versionado: SemVer (MAJOR.MINOR.PATCH).
- Commits: Conventional Commits (feat, fix, chore, docs, etc.).
- Código: análisis estático y `dotnet format`.
- CI (roadmap): Build, test y publicación de artefactos con GitHub Actions.
- Lanzamientos: changelog automatizado (roadmap).

## Seguridad
- No subas credenciales al repositorio.
- Usa variables de entorno o gestores de secretos (p. ej., GitHub Actions Secrets).
- Reporta vulnerabilidades abriendo un “Security Advisory” o contactando por el canal indicado.

## Cómo contribuir
1. Crea un fork y rama desde `main`.
2. Agrega o ajusta pruebas.
3. Ejecuta `dotnet build` y `dotnet test`.
4. Sigue Conventional Commits en tus mensajes.
5. Abre un PR con una descripción clara del cambio.

Guía de contribución y Código de Conducta se añadirán en el roadmap.

## Roadmap (issues a crear)
- CLI: comandos `backup` y `restore` con opciones principales.
- Soporte de `--uri` y autenticación.
- Modo Docker local (`docker exec`) con detección de binarios.
- Compresión de backups (zip/tar.gz).
- Retención de backups por días y limpieza segura.
- Cifrado AES opcional de backups.
- Logs estructurados y `--verbose`.
- CI GitHub Actions (build + test).
- Publicación como .NET global tool.
- Documentación de ejemplos end-to-end.

## Licencia
MIT. Ver [LICENSE](./LICENSE).
