# Guía de Instalación de MongoDB Backup & Restore CLI

Esta guía proporciona instrucciones detalladas para instalar y configurar la herramienta CLI de MongoDB Backup & Restore en diferentes escenarios.

## Tabla de Contenidos

- [Requisitos del Sistema](#requisitos-del-sistema)
- [Instalación como Herramienta Global](#instalación-como-herramienta-global)
- [Instalación desde Código Fuente](#instalación-desde-código-fuente)
- [Instalación Local en un Proyecto](#instalación-local-en-un-proyecto)
- [Verificación de la Instalación](#verificación-de-la-instalación)
- [Actualización de la Herramienta](#actualización-de-la-herramienta)
- [Desinstalación](#desinstalación)
- [Solución de Problemas](#solución-de-problemas)

## Requisitos del Sistema

Antes de instalar la herramienta, asegúrate de tener instalado:

### Requisitos Obligatorios

- **.NET SDK 8.0 o superior**
  - Descarga: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verifica la instalación: `dotnet --version`

- **MongoDB Database Tools** (mongodump y mongorestore)
  - Descarga: https://www.mongodb.com/try/download/database-tools
  - Deben estar disponibles en el PATH del sistema
  - Verifica la instalación: `mongodump --version` y `mongorestore --version`

### Requisitos Opcionales (según el escenario de uso)

- **Docker Desktop** - Para trabajar con contenedores Docker locales
  - Windows/Mac: https://www.docker.com/products/docker-desktop
  - Linux: https://docs.docker.com/engine/install/

- **Acceso de red a MongoDB** - Para escenarios de respaldo/restauración remota

## Instalación como Herramienta Global

Esta es la forma **recomendada** de instalar la CLI, ya que permite usar el comando `mongodb-br` desde cualquier ubicación del sistema.

### Paso 1: Instalar desde NuGet

```bash
dotnet tool install --global MongoBackupRestore.Cli
```

### Paso 2: Verificar la Instalación

```bash
mongodb-br --version
mongodb-br --help
```

Deberías ver la información de versión y la ayuda de la CLI.

### Notas Importantes

- La herramienta se instala en `~/.dotnet/tools` (Linux/Mac) o `%USERPROFILE%\.dotnet\tools` (Windows)
- Asegúrate de que esta ruta esté en tu variable de entorno PATH
- En Windows, puede ser necesario reiniciar el terminal después de la instalación

## Instalación desde Código Fuente

Si deseas contribuir al proyecto o necesitas una versión personalizada, puedes instalar desde el código fuente.

### Paso 1: Clonar el Repositorio

```bash
git clone https://github.com/JoseRGWeb/mongodb-backup-restore-cli.git
cd mongodb-backup-restore-cli
```

### Paso 2: Compilar el Proyecto

```bash
dotnet build --configuration Release
```

### Paso 3: Ejecutar las Pruebas (Opcional)

```bash
dotnet test --configuration Release
```

### Paso 4: Ejecutar la Herramienta

Puedes ejecutar la herramienta directamente sin instalarla:

```bash
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- backup --db MiBaseDatos --out ./backups
```

### Paso 5: Empaquetar e Instalar Localmente (Opcional)

Si prefieres instalarla como herramienta global desde el código fuente:

```bash
# Generar el paquete NuGet
dotnet pack src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj --configuration Release --output ./nupkg

# Instalar desde el paquete local
dotnet tool install --global --add-source ./nupkg MongoBackupRestore.Cli

# Verificar la instalación
mongodb-br --version
```

## Instalación Local en un Proyecto

Puedes instalar la herramienta como dependencia local de un proyecto específico en lugar de globalmente.

### Paso 1: Crear un Manifiesto de Herramientas (si no existe)

```bash
cd tu-proyecto
dotnet new tool-manifest
```

### Paso 2: Instalar la Herramienta Localmente

```bash
dotnet tool install MongoBackupRestore.Cli
```

### Paso 3: Usar la Herramienta

```bash
dotnet tool run mongodb-br --help
# O usando el alias corto
dotnet mongodb-br --help
```

### Ventajas de la Instalación Local

- Control de versión por proyecto
- El archivo `.config/dotnet-tools.json` se puede versionar en Git
- Diferentes proyectos pueden usar diferentes versiones de la herramienta

## Verificación de la Instalación

### Verificar que la Herramienta está Instalada

```bash
# Para instalación global
dotnet tool list --global

# Para instalación local
dotnet tool list
```

### Verificar las Dependencias de MongoDB

```bash
# Verificar mongodump
mongodump --version

# Verificar mongorestore
mongorestore --version

# Verificar Docker (si es necesario)
docker --version
docker ps
```

### Ejecutar una Prueba Básica

```bash
# Mostrar la ayuda
mongodb-br --help

# Mostrar ayuda del comando backup
mongodb-br backup --help

# Mostrar ayuda del comando restore
mongodb-br restore --help
```

## Actualización de la Herramienta

### Actualizar a la Última Versión

```bash
# Para instalación global
dotnet tool update --global MongoBackupRestore.Cli

# Para instalación local
dotnet tool update MongoBackupRestore.Cli
```

### Actualizar a una Versión Específica

```bash
# Desinstalar la versión actual
dotnet tool uninstall --global MongoBackupRestore.Cli

# Instalar la versión específica
dotnet tool install --global MongoBackupRestore.Cli --version 1.0.0
```

### Verificar la Versión Actual

```bash
mongodb-br --version
```

## Desinstalación

### Desinstalar Instalación Global

```bash
dotnet tool uninstall --global MongoBackupRestore.Cli
```

### Desinstalar Instalación Local

```bash
dotnet tool uninstall MongoBackupRestore.Cli
```

### Limpiar Caché de Herramientas (Opcional)

```bash
# Limpiar el caché de paquetes NuGet
dotnet nuget locals all --clear
```

## Solución de Problemas

### Error: "No se encuentra el comando 'mongodb-br'"

**Problema**: El PATH no incluye la ruta de las herramientas .NET.

**Solución**:

En Linux/Mac, agrega esto a tu `~/.bashrc`, `~/.zshrc` o equivalente:
```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```

En Windows, agrega `%USERPROFILE%\.dotnet\tools` a las variables de entorno PATH.

### Error: "mongodump no encontrado"

**Problema**: MongoDB Database Tools no están instaladas o no están en el PATH.

**Solución**:

1. Descarga e instala MongoDB Database Tools desde https://www.mongodb.com/try/download/database-tools
2. Agrega la ruta de instalación al PATH del sistema
3. Reinicia el terminal y verifica: `mongodump --version`

### Error: "No se puede encontrar el paquete 'MongoBackupRestore.Cli'"

**Problema**: El paquete aún no está disponible en NuGet o hay un problema de conectividad.

**Solución**:

1. Verifica tu conexión a Internet
2. Asegúrate de que la fuente de NuGet esté configurada:
   ```bash
   dotnet nuget list source
   ```
3. Si es necesario, agrega la fuente de NuGet:
   ```bash
   dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org
   ```

### Error: "La herramienta ya está instalada"

**Problema**: Intentas instalar una herramienta que ya existe.

**Solución**:

```bash
# Actualizar en lugar de instalar
dotnet tool update --global MongoBackupRestore.Cli

# O desinstalar primero y luego instalar
dotnet tool uninstall --global MongoBackupRestore.Cli
dotnet tool install --global MongoBackupRestore.Cli
```

### Error: "No se puede acceder al contenedor Docker"

**Problema**: Docker no está en ejecución o el usuario no tiene permisos.

**Solución**:

1. Verifica que Docker esté en ejecución: `docker ps`
2. En Linux, agrega tu usuario al grupo docker:
   ```bash
   sudo usermod -aG docker $USER
   # Cierra sesión y vuelve a iniciarla
   ```
3. En Windows/Mac, asegúrate de que Docker Desktop esté ejecutándose

### Problemas de Permisos en Linux/Mac

**Problema**: Error de permisos al instalar herramientas globales.

**Solución**:

```bash
# No uses sudo con dotnet tool install
# En su lugar, asegúrate de que el directorio de herramientas sea accesible
mkdir -p ~/.dotnet/tools
chmod -R u+w ~/.dotnet/tools
```

## Información Adicional

### Ubicaciones de Instalación

- **Global (Linux/Mac)**: `~/.dotnet/tools`
- **Global (Windows)**: `%USERPROFILE%\.dotnet\tools`
- **Local**: `./.config/dotnet-tools.json` (manifiesto del proyecto)

### Archivos de Configuración

- **Manifiesto de herramientas local**: `.config/dotnet-tools.json`
- **Configuración de NuGet**: `~/.nuget/NuGet/NuGet.Config` (Linux/Mac) o `%APPDATA%\NuGet\NuGet.Config` (Windows)

### Variables de Entorno Soportadas

La herramienta admite las siguientes variables de entorno:

- `MONGO_URI`: Cadena de conexión completa de MongoDB
- `MONGO_HOST`, `MONGO_PORT`: Host y puerto de MongoDB
- `MONGO_USER`, `MONGO_PASSWORD`, `MONGO_AUTH_DB`: Credenciales de autenticación
- `MONGO_COMPRESSION`: Formato de compresión (none, zip, targz)
- `MONGO_RETENTION_DAYS`: Días de retención de backups
- `MONGO_ENCRYPTION_KEY`: Clave de cifrado AES-256
- `MONGO_LOG_LEVEL`: Nivel de logging (debug, info, warning, error)
- `MONGO_LOG_FILE`: Ruta del archivo de log

## Recursos Adicionales

- **Repositorio de GitHub**: https://github.com/JoseRGWeb/mongodb-backup-restore-cli
- **Documentación de .NET Tools**: https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools
- **Reporte de Issues**: https://github.com/JoseRGWeb/mongodb-backup-restore-cli/issues
- **Guía de Publicación en NuGet**: Ver `PUBLICACION_NUGET.md` en el repositorio

## Soporte

Si encuentras problemas durante la instalación:

1. Revisa esta guía de solución de problemas
2. Consulta los [Issues del repositorio](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/issues)
3. Abre un nuevo issue si tu problema no está documentado

## Próximos Pasos

Una vez instalada la herramienta, consulta:

- `README.md` - Para ejemplos de uso y documentación completa
- `mongodb-br backup --help` - Para ayuda específica del comando backup
- `mongodb-br restore --help` - Para ayuda específica del comando restore
