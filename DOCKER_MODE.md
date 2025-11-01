# Modo Docker - Guía de Uso

Esta guía describe cómo usar MongoDB Backup & Restore CLI en modo Docker para realizar backups y restauraciones en contenedores Docker locales.

## Tabla de Contenidos
- [Descripción General](#descripción-general)
- [Requisitos Previos](#requisitos-previos)
- [Detección Automática de Contenedores](#detección-automática-de-contenedores)
- [Validación de Binarios](#validación-de-binarios)
- [Ejemplos de Uso](#ejemplos-de-uso)
- [Solución de Problemas](#solución-de-problemas)

## Descripción General

El modo Docker permite ejecutar operaciones de backup y restore directamente en contenedores Docker que ejecutan MongoDB, sin necesidad de tener MongoDB Database Tools instalados localmente en el sistema host.

### Ventajas del Modo Docker

1. **No requiere instalación local**: No necesita instalar `mongodump`/`mongorestore` en su sistema
2. **Aislamiento**: Las operaciones se ejecutan dentro del contenedor
3. **Detección automática**: Puede detectar automáticamente contenedores MongoDB en ejecución
4. **Validación previa**: Verifica que el contenedor y los binarios necesarios existan antes de ejecutar

## Requisitos Previos

1. **Docker Desktop o Docker Engine** debe estar instalado y en ejecución:
   - Windows/Mac: [Docker Desktop](https://www.docker.com/products/docker-desktop)
   - Linux: [Docker Engine](https://docs.docker.com/engine/install/)

2. **Contenedor MongoDB en ejecución** con MongoDB Database Tools instalados:
   ```bash
   # Ejemplo: Crear un contenedor MongoDB
   docker run -d --name mi-mongo -p 27017:27017 mongo:latest
   ```

3. **Permisos de Docker**: El usuario debe tener permisos para ejecutar comandos Docker

## Detección Automática de Contenedores

Cuando usa `--in-docker` sin especificar `--container-name`, la herramienta intentará detectar automáticamente contenedores que ejecutan MongoDB.

### Cómo Funciona

La detección automática:

1. **Busca contenedores por imagen**: Identifica contenedores basados en la imagen oficial `mongo`
2. **Busca por puerto expuesto**: Encuentra contenedores con el puerto 27017 expuesto
3. **Valida MongoDB**: Verifica que el contenedor tenga MongoDB o sus herramientas disponibles

### Casos de Uso

#### Un Único Contenedor MongoDB

Si solo hay un contenedor MongoDB en ejecución, se detectará y usará automáticamente:

```bash
# Backup automático
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- \
  backup --db mydb --in-docker --out ./backups/2025-11-01

# Restore automático
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- \
  restore --db mydb --in-docker --from ./backups/2025-11-01
```

**Salida esperada:**
```
Auto-detectando contenedores Docker con MongoDB...
Contenedor detectado automáticamente: mi-mongo
Contenedor Docker validado: mi-mongo
✓ Backup completado exitosamente para la base de datos 'mydb' desde el contenedor 'mi-mongo'
```

#### Múltiples Contenedores MongoDB

Si hay varios contenedores MongoDB, debe especificar cuál usar:

```bash
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- \
  backup --db mydb --in-docker --container-name mi-mongo-prod --out ./backups/2025-11-01
```

**Error si no se especifica:**
```
✗ Se encontraron múltiples contenedores con MongoDB: mi-mongo-dev, mi-mongo-prod, mi-mongo-test. 
  Especifique cuál usar con --container-name.
```

#### Sin Contenedores MongoDB

Si no hay contenedores MongoDB en ejecución:

```bash
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- \
  backup --db mydb --in-docker --out ./backups/2025-11-01
```

**Error:**
```
✗ No se encontraron contenedores Docker con MongoDB en ejecución. 
  Especifique el nombre del contenedor con --container-name o inicie un contenedor MongoDB.
```

## Validación de Binarios

Antes de ejecutar operaciones de backup o restore, la herramienta valida automáticamente:

### 1. Existencia del Contenedor

Verifica que el contenedor especificado existe:

```bash
docker inspect --format="{{.State.Running}}" nombre-contenedor
```

### 2. Estado del Contenedor

Confirma que el contenedor está en ejecución (no detenido):

**Error si está detenido:**
```
✗ El contenedor 'mi-mongo' existe pero no está en ejecución
```

### 3. Binarios MongoDB Disponibles

Valida que los binarios necesarios estén instalados en el contenedor:

- **Para backup**: Verifica `mongodump`
- **Para restore**: Verifica `mongorestore`

```bash
docker exec nombre-contenedor sh -c "command -v mongodump"
docker exec nombre-contenedor sh -c "command -v mongorestore"
```

**Error si falta un binario:**
```
✗ mongodump no está disponible en el contenedor 'mi-mongo'. 
  Asegúrese de que el contenedor tenga MongoDB Database Tools instalado.
```

### Contenedores Sin MongoDB Database Tools

Algunas imágenes de MongoDB no incluyen las Database Tools por defecto. Si encuentra este error, puede:

1. **Usar una imagen con las herramientas incluidas:**
   ```bash
   docker run -d --name mi-mongo mongo:latest
   ```

2. **Instalar las herramientas en un contenedor existente:**
   ```bash
   docker exec -it mi-mongo bash
   apt-get update
   apt-get install -y mongodb-database-tools
   ```

3. **Crear una imagen personalizada** con Dockerfile:
   ```dockerfile
   FROM mongo:latest
   RUN apt-get update && apt-get install -y mongodb-database-tools
   ```

## Ejemplos de Uso

### Backup Básico

```bash
# Con nombre de contenedor especificado
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- \
  backup --db produccion --in-docker --container-name mongo-prod --out ./backups/$(date +%Y-%m-%d)

# Con auto-detección
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- \
  backup --db produccion --in-docker --out ./backups/$(date +%Y-%m-%d)
```

### Backup con Autenticación

```bash
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- \
  backup --db produccion \
  --in-docker --container-name mongo-prod \
  --user admin --password "secreto" --auth-db admin \
  --out ./backups/$(date +%Y-%m-%d)
```

### Restore Básico

```bash
# Con nombre de contenedor
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- \
  restore --db produccion --in-docker --container-name mongo-prod --from ./backups/2025-11-01

# Con auto-detección
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- \
  restore --db produccion --in-docker --from ./backups/2025-11-01
```

### Restore con Drop (Eliminar Base de Datos Existente)

```bash
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- \
  restore --db produccion --in-docker --container-name mongo-prod \
  --from ./backups/2025-11-01 --drop
```

### Backup con Verbosidad

```bash
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- \
  backup --db produccion --in-docker --out ./backups/$(date +%Y-%m-%d) --verbose
```

**Salida verbose:**
```
Modo verbose activado
Validando herramientas de MongoDB...
Docker encontrado: versión 24.0.7
Auto-detectando contenedores Docker con MongoDB...
Se encontraron 1 contenedor(es) con MongoDB: mi-mongo
Contenedor detectado automáticamente: mi-mongo
Validando contenedor: mi-mongo
Contenedor mi-mongo validado correctamente
Binario mongodump encontrado en contenedor mi-mongo: /usr/bin/mongodump
Iniciando backup de la base de datos: produccion
Ejecutando backup en contenedor Docker: mi-mongo
Copiando backup desde el contenedor al host...
Backup copiado exitosamente al host
✓ Backup completado exitosamente para la base de datos 'produccion' desde el contenedor 'mi-mongo'
```

## Solución de Problemas

### Docker No Está Disponible

**Error:**
```
✗ Docker no está disponible. Instale Docker Desktop para usar el modo --in-docker.
  Descarga: https://www.docker.com/products/docker-desktop
```

**Solución:**
1. Instalar Docker Desktop (Windows/Mac) o Docker Engine (Linux)
2. Iniciar el servicio Docker
3. Verificar: `docker --version`

### Contenedor No Encontrado

**Error:**
```
✗ El contenedor 'mi-mongo' no existe
```

**Solución:**
1. Listar contenedores en ejecución: `docker ps`
2. Listar todos los contenedores: `docker ps -a`
3. Iniciar el contenedor si está detenido: `docker start mi-mongo`
4. Crear un nuevo contenedor si es necesario

### Contenedor No Está En Ejecución

**Error:**
```
✗ El contenedor 'mi-mongo' existe pero no está en ejecución
```

**Solución:**
```bash
docker start mi-mongo
```

### Binarios MongoDB No Disponibles

**Error:**
```
✗ mongodump no está disponible en el contenedor 'mi-mongo'. 
  Asegúrese de que el contenedor tenga MongoDB Database Tools instalado.
```

**Solución:**
Ver la sección [Contenedores Sin MongoDB Database Tools](#contenedores-sin-mongodb-database-tools)

### Error de Permisos

**Error:**
```
Error: permission denied while trying to connect to the Docker daemon socket
```

**Solución (Linux):**
```bash
# Agregar usuario al grupo docker
sudo usermod -aG docker $USER

# Cerrar sesión y volver a iniciar sesión
```

**Solución (Windows/Mac):**
Asegúrese de que Docker Desktop esté en ejecución

### Error de Conexión a MongoDB

**Error:**
```
Error de conexión: No se pudo conectar al servidor MongoDB.
```

**Solución:**
1. Verificar que MongoDB está en ejecución dentro del contenedor:
   ```bash
   docker exec mi-mongo mongosh --eval "db.adminCommand('ping')"
   ```
2. Verificar el puerto correcto (por defecto 27017)
3. Verificar credenciales si se usa autenticación

### Múltiples Contenedores Detectados

**Error:**
```
✗ Se encontraron múltiples contenedores con MongoDB: mongo1, mongo2, mongo3. 
  Especifique cuál usar con --container-name.
```

**Solución:**
Especificar explícitamente el contenedor:
```bash
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- \
  backup --db mydb --in-docker --container-name mongo1 --out ./backups/backup
```

## Notas Adicionales

### Rendimiento

- Las operaciones en modo Docker pueden ser ligeramente más lentas que las operaciones locales debido al overhead de `docker exec` y `docker cp`
- Para bases de datos grandes, considere usar volúmenes compartidos entre el host y el contenedor

### Seguridad

- Las contraseñas pasadas como argumentos pueden ser visibles en la lista de procesos
- En producción, considere usar autenticación basada en certificados o archivos de configuración
- Los backups se copian del contenedor al host, asegúrese de que el directorio de destino tenga permisos adecuados

### Limitaciones Actuales

- Solo soporta contenedores Docker locales (mismo host)
- No soporta Docker remoto (roadmap)
- No soporta contextos Docker múltiples simultáneamente

### Roadmap

- [ ] Soporte para Docker remoto (conexión a Docker hosts remotos)
- [ ] Contextos Docker múltiples
- [ ] Compresión de backups antes de copiar del contenedor
- [ ] Operaciones en paralelo para múltiples bases de datos
