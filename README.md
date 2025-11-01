# MongoDB Backup & Restore CLI (.NET)

[![Build y Test](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/actions/workflows/build-test.yml/badge.svg)](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/actions/workflows/build-test.yml)
[![Validación de PR](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/actions/workflows/pr-validation.yml/badge.svg)](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/actions/workflows/pr-validation.yml)
[![Release](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/actions/workflows/release.yml/badge.svg)](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/actions/workflows/release.yml)

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
- **Políticas de retención y limpieza automática de backups** ✓
- **Cifrado AES-256 de backups** ✓
- **Logs estructurados y niveles de verbosidad** ✓
- **Integración CI/CD con GitHub Actions** ✓
- **Distribución como .NET global tool** ✓

## Requisitos
- .NET SDK 8.0 o superior
- Docker Desktop (para escenarios con contenedores locales)
- MongoDB Database Tools (mongodump y mongorestore) disponibles en PATH:
  - Descarga: https://www.mongodb.com/try/download/database-tools
- Acceso al host/puerto de MongoDB en escenarios remotos
- (Opcional) GitHub CLI para automatizar tareas con el repo

## Instalación

### Opción 1: Como Herramienta Global de .NET (Recomendado)

La forma más sencilla de instalar y usar la CLI es como herramienta global de .NET:

```bash
# Instalar desde NuGet
dotnet tool install --global MongoBackupRestore.Cli

# Verificar la instalación
mongodb-br --version
mongodb-br --help
```

**Actualizar a una nueva versión:**
```bash
dotnet tool update --global MongoBackupRestore.Cli
```

**Desinstalar:**
```bash
dotnet tool uninstall --global MongoBackupRestore.Cli
```

### Opción 2: Desde Código Fuente

Si prefieres compilar desde el código fuente:

```bash
git clone https://github.com/JoseRGWeb/mongodb-backup-restore-cli.git
cd mongodb-backup-restore-cli
dotnet build
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- [comando] [opciones]
```

**O empaquetar e instalar localmente:**
```bash
# Generar el paquete
dotnet pack src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj --configuration Release --output ./nupkg

# Instalar desde el paquete local
dotnet tool install --global --add-source ./nupkg MongoBackupRestore.Cli
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

- **Backup con retención de 7 días** (elimina automáticamente backups más antiguos):
```bash
mongodb-br backup --db MyDatabase --out ./backups/2025-11-01 --retention-days 7
```

- **Backup con retención y compresión**:
```bash
mongodb-br backup --db MyDatabase --out ./backups/2025-11-01 --compress zip --retention-days 30
```

- **Backup con retención usando variable de entorno**:
```bash
export MONGO_RETENTION_DAYS=14
mongodb-br backup --db MyDatabase --out ./backups/2025-11-01
```

- **Backup con cifrado AES-256**:
```bash
mongodb-br backup --db MyDatabase --out ./backups/2025-11-01 --encrypt --encryption-key "MiClaveSegura123456"
```

- **Backup con cifrado usando variable de entorno**:
```bash
export MONGO_ENCRYPTION_KEY="MiClaveSegura123456"
mongodb-br backup --db MyDatabase --out ./backups/2025-11-01 --encrypt
```

- **Backup comprimido y cifrado**:
```bash
mongodb-br backup --db MyDatabase --out ./backups/2025-11-01 --compress zip --encrypt --encryption-key "MiClaveSegura123456"
```
> **Nota**: Se recomienda siempre combinar cifrado con compresión para reducir el tamaño antes de cifrar.

- **Backup completo con todas las opciones**:
```bash
mongodb-br backup --db MyDatabase --out ./backups/2025-11-01 \
  --compress zip --encrypt --encryption-key "MiClaveSegura123456" \
  --retention-days 30 --verbose
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

- **Restaurar desde backup cifrado**:
```bash
mongodb-br restore --db MyDatabase --from ./backups/2025-11-01_20251101_120000.zip.encrypted \
  --encryption-key "MiClaveSegura123456"
```
> **Nota**: El backup cifrado se detecta automáticamente por la extensión .encrypted

- **Restaurar desde backup cifrado usando variable de entorno**:
```bash
export MONGO_ENCRYPTION_KEY="MiClaveSegura123456"
mongodb-br restore --db MyDatabase --from ./backups/2025-11-01_20251101_120000.zip.encrypted
```

- **Restaurar desde backup comprimido y cifrado**:
```bash
mongodb-br restore --db MyDatabase --from ./backups/backup.zip.encrypted \
  --encryption-key "MiClaveSegura123456"
```
> **Nota**: La herramienta descifrará automáticamente primero y luego descomprimirá el backup antes de restaurar.

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
- `--retention-days` / `-r` **Número de días para retener backups**. Los backups más antiguos que este período serán eliminados automáticamente después de crear un nuevo backup. Se puede configurar también con la variable de entorno `MONGO_RETENTION_DAYS`.
- `--encrypt` / `-e` **Habilitar cifrado AES-256** para proteger el backup (solo para `backup`).
- `--encryption-key` / `-k` **Clave de cifrado** para cifrar o descifrar el backup (mínimo 16 caracteres). Se puede configurar con la variable de entorno `MONGO_ENCRYPTION_KEY`.
- `--drop` Eliminar la base de datos antes de restaurar (solo para `restore`).
- `--verbose` / `-v` Habilitar modo verbose (muestra logs de nivel DEBUG para depuración detallada).
- `--log-file` Ruta del archivo donde guardar los logs. También se puede usar la variable de entorno `MONGO_LOG_FILE`.

## Logging y Verbosidad

La aplicación proporciona un sistema de logging estructurado con diferentes niveles de detalle:

### Niveles de Log
- **Information** (por defecto): Muestra información general de operaciones
- **Debug**: Muestra información detallada para depuración (activado con `--verbose`)
- **Warning**: Muestra advertencias que no impiden la operación
- **Error**: Muestra errores que impiden completar la operación

### Configuración de Logging

**Modo Verbose (Debug)**:
```bash
# Usando la opción --verbose
mongodb-br backup --db MyDatabase --out ./backups --verbose

# Usando la variable de entorno MONGO_LOG_LEVEL
export MONGO_LOG_LEVEL=debug
mongodb-br backup --db MyDatabase --out ./backups
```

**Guardar logs en archivo**:
```bash
# Usando la opción --log-file
mongodb-br backup --db MyDatabase --out ./backups --log-file /var/log/mongodb-backup.log

# Usando la variable de entorno MONGO_LOG_FILE
export MONGO_LOG_FILE=/var/log/mongodb-backup.log
mongodb-br backup --db MyDatabase --out ./backups
```

**Combinar verbose y archivo de log**:
```bash
mongodb-br backup --db MyDatabase --out ./backups --verbose --log-file ./logs/backup.log
```

### Variables de Entorno para Logging
- `MONGO_LOG_LEVEL`: Nivel de log (`trace`, `debug`, `info`, `warning`, `error`, `critical`)
- `MONGO_LOG_FILE`: Ruta del archivo de log

> **Nota**: La opción `--verbose` tiene prioridad sobre `MONGO_LOG_LEVEL`. Cuando se usa `--verbose`, el nivel se establece automáticamente en `debug`.

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
- `MONGO_RETENTION_DAYS` - Número de días para retener backups (elimina automáticamente backups antiguos)
- `MONGO_ENCRYPTION_KEY` - Clave de cifrado para cifrar/descifrar backups (mínimo 16 caracteres)
- `MONGO_LOG_LEVEL` - Nivel de log (trace, debug, info, warning, error, critical)
- `MONGO_LOG_FILE` - Ruta del archivo donde guardar los logs
- `DOCKER_CONTEXT` (para escenarios de Docker remoto en roadmap)
- `MONGOBR_OUT_DIR` (directorio por defecto de backups)

## Retención y Limpieza Automática de Backups

La funcionalidad de retención permite mantener automáticamente solo los backups recientes, eliminando aquellos que superen el período de retención configurado.

### Características

- **Retención por días**: Configura cuántos días deseas mantener los backups
- **Limpieza automática**: Después de cada backup exitoso, se eliminan automáticamente los backups antiguos
- **Soporte múltiples formatos**: Identifica y elimina tanto directorios de backup como archivos comprimidos (.zip, .tar.gz, .tgz)
- **Logs detallados**: Registra cada acción de limpieza incluyendo:
  - Backups identificados como antiguos
  - Fecha de creación de cada backup
  - Tamaño de los archivos eliminados
  - Espacio total liberado
- **Protección inteligente**: Ignora directorios ocultos (comenzando con `.`) y temporales (comenzando con `temp`)

### Uso

La retención se activa añadiendo el parámetro `--retention-days` al comando de backup:

```bash
# Retener backups de los últimos 7 días
mongodb-br backup --db MyDatabase --out ./backups/backup-20251101 --retention-days 7

# Combinar con compresión para optimizar espacio
mongodb-br backup --db MyDatabase --out ./backups/backup-20251101 --compress zip --retention-days 30

# Usar variable de entorno
export MONGO_RETENTION_DAYS=14
mongodb-br backup --db MyDatabase --out ./backups/backup-20251101
```

### Funcionamiento

1. El backup se ejecuta normalmente
2. Si el backup es exitoso y se especificó `--retention-days`:
   - Se escanea el directorio padre del backup
   - Se identifican todos los backups (directorios y archivos comprimidos)
   - Se calcula la fecha límite: `fecha_actual - días_retención`
   - Los backups con fecha de creación anterior a la fecha límite son eliminados
   - Se registran logs detallados de cada acción

### Ejemplo de Logs

```
Iniciando limpieza de backups antiguos. Directorio: ./backups, Retención: 7 días, Modo: REAL
Fecha límite de retención: 2025-10-25 14:13:44
Total de backups encontrados: 5
Backup antiguo identificado: backup-20251015.zip, Fecha: 2025-10-15 10:30:00, Tamaño: 15728640 bytes
✓ Backup eliminado: backup-20251015.zip
Backup antiguo identificado: backup-20251020, Fecha: 2025-10-20 08:45:00, Tamaño: 20971520 bytes
✓ Backup eliminado: backup-20251020
Backup retenido: backup-20251028.zip, Fecha: 2025-10-28 12:00:00
Backup retenido: backup-20251030.tar.gz, Fecha: 2025-10-30 16:30:00
Limpieza de backups completada. Eliminados: 2, Retenidos: 3, Espacio liberado: 35.00 MB
```

## Cifrado de Backups (AES-256)

La herramienta incluye cifrado AES-256 opcional para proteger backups sensibles. Esta funcionalidad utiliza **AES-256-CBC** con **HMAC-SHA256** para garantizar tanto la confidencialidad como la integridad de los datos.

### Características de Seguridad

- **Cifrado AES-256-CBC**: Estándar de cifrado avanzado con clave de 256 bits
- **Autenticación HMAC-SHA256**: Verifica la integridad del backup y previene manipulaciones
- **IV aleatorio**: Cada cifrado genera un Vector de Inicialización único para máxima seguridad
- **Derivación de claves PBKDF2**: Las claves se derivan usando PBKDF2 con 100,000 iteraciones
- **Limpieza de memoria**: Las claves se eliminan de la memoria después de su uso
- **Detección automática**: Los backups cifrados se identifican por extensión `.encrypted` y encabezado

### Uso Básico

**Cifrar un backup:**
```bash
mongodb-br backup --db MiBaseDatos --out ./backups/backup-20251101 \
  --encrypt --encryption-key "MiClaveSegura123456789"
```

**Descifrar y restaurar:**
```bash
mongodb-br restore --db MiBaseDatos \
  --from ./backups/backup-20251101_20251101_143000.encrypted \
  --encryption-key "MiClaveSegura123456789"
```

### Mejores Prácticas

1. **Longitud de clave**: Use claves de al menos 16 caracteres (se recomienda 32 o más)
2. **Complejidad**: Combine letras mayúsculas, minúsculas, números y caracteres especiales
3. **Gestión de claves**: 
   - No incluya claves en scripts de control de versiones
   - Use variables de entorno o gestores de secretos (AWS Secrets Manager, Azure Key Vault, HashiCorp Vault)
   - Rote las claves periódicamente
4. **Combinación con compresión**: Comprima primero, luego cifre para mejor rendimiento
   ```bash
   mongodb-br backup --db MiBaseDatos --out ./backups/backup \
     --compress zip --encrypt --encryption-key "$MONGO_ENCRYPTION_KEY"
   ```
5. **Backup de claves**: Almacene las claves de cifrado de forma segura y separada de los backups

### Funcionamiento

1. **Durante el backup:**
   - Si se especifica `--encrypt`, el backup se cifra después de generarse
   - Si hay compresión, primero se comprime y luego se cifra
   - Se genera un archivo con extensión `.encrypted`
   - Estructura del archivo cifrado:
     ```
     [Encabezado "MONGOBR-AES256"] (14 bytes)
     [IV - Vector de Inicialización] (16 bytes)
     [HMAC-SHA256] (32 bytes)
     [Datos cifrados] (variable)
     ```

2. **Durante el restore:**
   - La herramienta detecta automáticamente si el backup está cifrado
   - Valida el encabezado y verifica el HMAC antes de descifrar
   - Si el HMAC no coincide (clave incorrecta o archivo corrupto), se rechaza el restore
   - Descifra el backup a un archivo temporal
   - Si está comprimido, lo descomprime
   - Finalmente ejecuta el restore

### Ejemplo Completo

**Workflow recomendado para backups seguros:**

```bash
# 1. Configurar clave de cifrado de forma segura (solo una vez)
export MONGO_ENCRYPTION_KEY="$(openssl rand -base64 32)"
echo "Guarde esta clave en un gestor de secretos: $MONGO_ENCRYPTION_KEY"

# 2. Realizar backup cifrado y comprimido con retención
mongodb-br backup --db ProductionDB \
  --out ./backups/prod-backup-$(date +%Y%m%d) \
  --compress zip \
  --encrypt \
  --retention-days 30 \
  --verbose

# Resultado: ./backups/prod-backup-20251101/prod-backup-20251101_143052.zip.encrypted

# 3. Restaurar en caso necesario
mongodb-br restore --db ProductionDB \
  --from ./backups/prod-backup-20251101/prod-backup-20251101_143052.zip.encrypted \
  --verbose
```

### Validación y Errores

La herramienta proporciona mensajes claros en caso de problemas:

- **Clave demasiado corta**: "La clave de cifrado debe tener al menos 16 caracteres"
- **Clave incorrecta al descifrar**: "Error al descifrar: la clave de cifrado es incorrecta o el archivo está corrupto"
- **Archivo cifrado sin clave**: "El backup está cifrado pero no se proporcionó la clave de cifrado"
- **Archivo corrupto**: "Error criptográfico al descifrar el backup"

### Consideraciones de Rendimiento

- El cifrado añade ~5-10% de overhead al tiempo de backup/restore
- Los archivos cifrados son ligeramente más grandes debido a padding y metadatos
- Se recomienda comprimir antes de cifrar para optimizar el tamaño
- Use claves desde variables de entorno para evitar escribirlas en logs

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
- CI/CD: Build, test y publicación de artefactos con GitHub Actions ✓
- Lanzamientos: changelog automatizado (roadmap).

## Seguridad
- No subas credenciales al repositorio.
- Usa variables de entorno o gestores de secretos (p. ej., GitHub Actions Secrets).
- Reporta vulnerabilidades abriendo un “Security Advisory” o contactando por el canal indicado.

## Cómo contribuir

¡Valoramos y agradecemos todas las contribuciones! Para contribuir al proyecto:

1. Lee nuestra **[Guía de Contribución](CONTRIBUTING.md)** completa que incluye:
   - Convenciones de Conventional Commits
   - Proceso de Pull Request y revisión
   - Estructura de ramas y releases
   - Guías de estilo de código
   - Testing y calidad
2. Asegúrate de seguir nuestro **[Código de Conducta](CODE_OF_CONDUCT.md)**
3. Crea un fork y rama desde `main`
4. Agrega o ajusta pruebas para tus cambios
5. Ejecuta `dotnet build` y `dotnet test`
6. Sigue Conventional Commits en tus mensajes
7. Abre un PR con una descripción clara del cambio

Para más detalles, consulta [CONTRIBUTING.md](CONTRIBUTING.md).

## Documentación Completa

### 📚 Guías de Uso
- **[Ejemplos End-to-End](./EJEMPLOS_END_TO_END.md)** - Casos de uso completos en diferentes escenarios:
  - Backup local en Windows
  - Backup y restore en Docker
  - Backup remoto con autenticación
  - Backup con compresión y cifrado
  - Backup automatizado con retención
  - Migración entre ambientes
  - Recuperación ante desastres
  - Y más...

- **[Variables de Entorno](./VARIABLES_ENTORNO.md)** - Guía completa de configuración:
  - Variables de conexión (`MONGO_HOST`, `MONGO_PORT`, `MONGO_URI`)
  - Variables de autenticación (`MONGO_USER`, `MONGO_PASSWORD`)
  - Variables de compresión y seguridad (`MONGO_COMPRESSION`, `MONGO_ENCRYPTION_KEY`)
  - Variables de logging (`MONGO_LOG_LEVEL`, `MONGO_LOG_FILE`)
  - Ejemplos de configuración por ambiente

- **[Integración CI/CD](./INTEGRACION_CI_CD.md)** - Integración con plataformas CI/CD:
  - GitHub Actions (workflows completos)
  - GitLab CI/CD (pipelines multi-ambiente)
  - Azure DevOps (YAML pipelines)
  - Jenkins (declarativo y script)
  - CircleCI
  - Mejores prácticas y notificaciones

- **[Logs y Debugging](./LOGS_Y_DEBUGGING.md)** - Guía de logs y depuración:
  - Sistema de logging estructurado
  - Niveles de log (trace, debug, info, warning, error, critical)
  - Análisis y filtrado de logs
  - Debugging avanzado
  - Errores comunes y soluciones

### 🔧 Documentación Técnica
- **[Guía de Instalación](./INSTALACION.md)** - Instalación detallada y solución de problemas
- **[Modo Docker](./DOCKER_MODE.md)** - Uso avanzado con Docker y auto-detección
- **[Seguridad](./SEGURIDAD.md)** - Mejores prácticas de seguridad

### 🤝 Contribución y Comunidad
- **[Guía de Contribución](./CONTRIBUTING.md)** - Cómo contribuir al proyecto:
  - Convenciones de Conventional Commits
  - Proceso de Pull Request y revisión
  - Estructura de ramas y releases
  - Guías de estilo y testing
- **[Código de Conducta](./CODE_OF_CONDUCT.md)** - Normas de comportamiento para la comunidad

## Roadmap
- CLI: comandos `backup` y `restore` con opciones principales. ✓
- Soporte de `--uri` y autenticación. ✓
- Modo Docker local (`docker exec`) con detección de binarios. ✓
- Compresión de backups (zip/tar.gz). ✓
- Retención de backups por días y limpieza segura. ✓
- Cifrado AES-256 opcional de backups. ✓
- Logs estructurados y `--verbose`. ✓
- CI/CD con GitHub Actions (build, test y releases). ✓
- **Publicación como .NET global tool**. ✓
- **Documentación completa con ejemplos end-to-end**. ✓

## Licencia
MIT. Ver [LICENSE](./LICENSE).
