# Implementación del Comando Backup

Este documento describe la implementación del comando `backup` para la CLI de MongoDB Backup & Restore.

## Arquitectura

La implementación sigue un diseño en capas con separación de responsabilidades:

### MongoBackupRestore.Core

Biblioteca de clases que contiene la lógica de negocio:

#### Modelos (`Models/`)
- **BackupOptions**: Opciones para realizar un backup
  - Configuración de base de datos (nombre, host, puerto)
  - Credenciales de autenticación
  - Configuración de Docker
  - Ruta de destino

- **BackupResult**: Resultado de una operación de backup
  - Estado de éxito/error
  - Mensaje descriptivo
  - Código de salida
  - Salida y errores del proceso

- **MongoToolsInfo**: Información sobre herramientas disponibles
  - mongodump (disponibilidad y versión)
  - mongorestore (disponibilidad y versión)
  - Docker (disponibilidad y versión)

#### Interfaces (`Interfaces/`)
- **IProcessRunner**: Ejecuta procesos externos
- **IMongoToolsValidator**: Valida herramientas de MongoDB
#### Interfaces (`Interfaces/`)
- **IProcessRunner**: Ejecuta procesos externos
- **IMongoToolsValidator**: Valida herramientas de MongoDB
- **IMongoConnectionValidator**: Valida conexiones y credenciales de MongoDB
- **IBackupService**: Servicio principal de backup
- **IRestoreService**: Servicio principal de restore

#### Servicios (`Services/`)
- **ProcessRunner**: Ejecuta comandos del sistema operativo
  - Captura salida estándar y de error
  - Manejo de cancelación
  - Logging detallado

- **MongoToolsValidator**: Valida herramientas disponibles
  - Detecta mongodump, mongorestore, Docker
  - Extrae versiones usando expresiones regulares
  - Cross-platform (Windows/Linux)

- **MongoConnectionValidator**: Valida conexiones y credenciales de MongoDB
  - Valida credenciales antes de ejecutar operaciones
  - Usa mongosh/mongo para verificar conexión
  - Analiza errores de autenticación y conexión
  - Proporciona mensajes de error claros y específicos

- **BackupService**: Servicio principal de backup
  - Valida opciones de entrada
  - Valida credenciales de autenticación (si se proporcionan)
  - Ejecuta backup local con mongodump
  - Ejecuta backup en Docker
    - Crea backup dentro del contenedor
    - Copia archivos al host
    - Limpia archivos temporales
  - Manejo completo de errores con mensajes específicos

- **RestoreService**: Servicio principal de restore
  - Valida opciones de entrada
  - Valida credenciales de autenticación (si se proporcionan)
  - Ejecuta restore local con mongorestore
  - Ejecuta restore en Docker
    - Copia backup al contenedor
    - Ejecuta restore dentro del contenedor
    - Limpia archivos temporales
  - Manejo completo de errores con mensajes específicos

### MongoBackupRestore.Cli

Aplicación de consola que expone la CLI:

#### Program.cs
- Configuración de logging con Microsoft.Extensions.Logging
- Configuración de System.CommandLine
- Comando raíz con descripción
- Comando `backup` con todas las opciones
- Soporte de variables de entorno:
  - `MONGO_HOST`, `MONGO_PORT`
  - `MONGO_USER`, `MONGO_PASSWORD`, `MONGO_AUTH_DB`
  - `MONGO_URI`
- Handler del comando con manejo de errores
- Códigos de salida apropiados

### MongoBackupRestore.Tests

Pruebas unitarias con xUnit, Moq y FluentAssertions:

- **BackupServiceTests**: Pruebas de validación y lógica de backup
  - Validación de opciones requeridas
  - Validación de herramientas disponibles
  - Validación de credenciales de autenticación ⭐ NUEVO
  - Escenarios de error

- **RestoreServiceTests**: Pruebas de validación y lógica de restore
  - Validación de opciones requeridas
  - Validación de herramientas disponibles
  - Validación de credenciales de autenticación ⭐ NUEVO
  - Validación de caracteres peligrosos
  - Escenarios de error

- **MongoConnectionValidatorTests**: Pruebas de validación de conexión ⭐ NUEVO
  - Validación de conexión exitosa
  - Detección de errores de autenticación
  - Detección de errores de conexión
  - Detección de errores de timeout
  - Manejo de mongosh/mongo no disponible

- **MongoToolsValidatorTests**: Pruebas de detección de herramientas
  - Detección individual de herramientas
  - Extracción de versiones
  - Manejo de herramientas no disponibles

## Escenarios Soportados

### 1. MongoDB Local (Windows/Linux)

```bash
dotnet run --project src/MongoBackupRestore.Cli -- backup \
  --db MiBaseDeDatos \
  --host localhost \
  --port 27017 \
  --out ./backups/2025-11-01
```

Con autenticación:
```bash
dotnet run --project src/MongoBackupRestore.Cli -- backup \
  --db MiBaseDeDatos \
  --user admin \
  --password "mipassword" \
  --auth-db admin \
  --out ./backups/2025-11-01
```

### 2. Contenedor Docker Local

```bash
dotnet run --project src/MongoBackupRestore.Cli -- backup \
  --db MiBaseDeDatos \
  --in-docker \
  --container-name mongo \
  --out ./backups/2025-11-01
```

### 3. Instancia Remota

```bash
dotnet run --project src/MongoBackupRestore.Cli -- backup \
  --db MiBaseDeDatos \
  --host mongo.example.com \
  --port 27017 \
  --user myuser \
  --password "mypassword" \
  --out ./backups/2025-11-01
```

### 4. Usando URI de Conexión

```bash
dotnet run --project src/MongoBackupRestore.Cli -- backup \
  --db MiBaseDeDatos \
  --uri "mongodb://user:password@host:27017/admin" \
  --out ./backups/2025-11-01
```

### 5. Con Variables de Entorno

```bash
export MONGO_HOST=localhost
export MONGO_PORT=27017
export MONGO_USER=admin
export MONGO_PASSWORD=secret

dotnet run --project src/MongoBackupRestore.Cli -- backup \
  --db MiBaseDeDatos \
  --out ./backups/2025-11-01
```

## Validaciones Implementadas

1. **Validación de Opciones**
   - Base de datos es obligatoria
   - Ruta de salida es obligatoria (backup) / Ruta de origen es obligatoria (restore)
   - Nombre de contenedor obligatorio con `--in-docker`
   - Validación de caracteres peligrosos en nombres de base de datos y contenedores

2. **Validación de Herramientas**
   - mongodump requerido para backup local/remoto
   - mongorestore requerido para restore local/remoto
   - Docker requerido para backup/restore en contenedor
   - Mensajes de error amigables con enlaces de descarga

3. **Validación de Credenciales de Autenticación** ⭐ NUEVO
   - Validación de conexión antes de ejecutar backup/restore
   - Uso de mongosh/mongo para verificar credenciales
   - Detección temprana de errores de autenticación
   - Mensajes de error específicos y claros:
     - Errores de autenticación (credenciales incorrectas)
     - Errores de conexión (servidor no disponible)
     - Errores de timeout (tiempo de espera agotado)
     - Errores de DNS (host no resuelve)

4. **Validación de Versiones**
   - Detección y visualización de versiones instaladas
   - Información mostrada al usuario antes de ejecutar

## Manejo de Errores de Autenticación

La CLI ahora proporciona mensajes de error específicos y claros para problemas de autenticación:

### Ejemplos de mensajes de error:

**Error de autenticación:**
```
Error de autenticación: Las credenciales proporcionadas son incorrectas o el usuario no tiene permisos suficientes.
Verifique el nombre de usuario (--user), contraseña (--password) y base de datos de autenticación (--auth-db).
```

**Error de conexión:**
```
Error de conexión: No se pudo conectar al servidor MongoDB.
Verifique que el host (--host), puerto (--port) y servicio MongoDB estén disponibles.
```

**Error de timeout:**
```
Error de conexión: Tiempo de espera agotado al conectar con MongoDB.
Verifique que el servidor esté accesible y que no haya problemas de red.
```

## Códigos de Salida

- **0**: Éxito
- **1**: Error de validación u otro error general
- **127**: Herramienta requerida no encontrada

## Logs

El sistema implementa logging estructurado con niveles:
- **Information**: Operaciones principales
- **Warning**: Herramientas no disponibles
- **Error**: Errores de ejecución
- **Debug/Trace**: Detalles de ejecución (con `--verbose`)

## Compilación y Ejecución

### Compilar la solución
```bash
dotnet build
```

### Ejecutar las pruebas
```bash
dotnet test
```

### Ejecutar la CLI
```bash
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- backup --help
```

## Dependencias

- **.NET 8.0**: Framework base
- **System.CommandLine (2.0.0-beta4)**: Parser de línea de comandos
- **Microsoft.Extensions.Logging.Abstractions (8.0.0)**: Logging en Core
- **Microsoft.Extensions.Logging.Console (8.0.0)**: Logging en CLI

### Dependencias de Testing
- **xUnit**: Framework de pruebas
- **Moq**: Biblioteca de mocking
- **FluentAssertions**: Aserciones fluidas

## Próximos Pasos (Roadmap)

- Comando `restore` para restaurar backups
- Compresión de backups (ZIP/TAR.GZ)
- Políticas de retención
- Cifrado de backups
- Publicación como .NET Global Tool
- CI/CD con GitHub Actions
