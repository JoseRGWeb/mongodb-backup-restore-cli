# Desarrollo y Compilación

Este documento describe cómo compilar, probar y ejecutar el proyecto.

## Requisitos Previos

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) o superior
- (Opcional para ejecutar backups) [MongoDB Database Tools](https://www.mongodb.com/try/download/database-tools)
- (Opcional para backups en Docker) [Docker Desktop](https://www.docker.com/products/docker-desktop)

## Compilación

### Compilar toda la solución
```bash
dotnet build
```

### Compilar en modo Release
```bash
dotnet build -c Release
```

### Limpiar artefactos de compilación
```bash
dotnet clean
```

## Pruebas

### Ejecutar todas las pruebas
```bash
dotnet test
```

### Ejecutar pruebas con salida detallada
```bash
dotnet test --verbosity detailed
```

### Ejecutar pruebas con cobertura (requiere coverlet)
```bash
dotnet test /p:CollectCoverage=true
```

## Ejecución

### Ejecutar la CLI directamente
```bash
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj
```

### Ejecutar la CLI con argumentos
```bash
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- backup --db testdb --out ./backup
```

### Ver ayuda
```bash
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- --help
dotnet run --project src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -- backup --help
```

## Publicación

### Publicar aplicación autónoma (Windows x64)
```bash
dotnet publish src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -c Release -r win-x64 --self-contained true -o ./publish/win-x64
```

### Publicar aplicación autónoma (Linux x64)
```bash
dotnet publish src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -c Release -r linux-x64 --self-contained true -o ./publish/linux-x64
```

### Publicar como herramienta global .NET (roadmap)
```bash
dotnet pack src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj -c Release
dotnet tool install -g --add-source ./publish MongoBackupRestore.Cli
```

## Estructura del Proyecto

```
mongodb-backup-restore-cli/
├── src/
│   ├── MongoBackupRestore.Core/       # Lógica de negocio
│   │   ├── Interfaces/                # Interfaces de servicios
│   │   ├── Models/                    # Modelos de datos
│   │   └── Services/                  # Implementación de servicios
│   └── MongoBackupRestore.Cli/        # Aplicación de consola
├── tests/
│   └── MongoBackupRestore.Tests/      # Pruebas unitarias
├── MongoBackupRestore.sln             # Solución
├── README.md                          # Documentación principal
├── IMPLEMENTATION.md                  # Documentación de implementación
└── BUILD.md                           # Este archivo
```

## Comandos Útiles

### Restaurar paquetes NuGet
```bash
dotnet restore
```

### Formatear código según estilo
```bash
dotnet format
```

### Listar proyectos en la solución
```bash
dotnet sln list
```

### Agregar referencia entre proyectos
```bash
dotnet add src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj reference src/MongoBackupRestore.Core/MongoBackupRestore.Core.csproj
```

## Variables de Entorno para Desarrollo

Puede configurar estas variables de entorno para facilitar las pruebas:

```bash
export MONGO_HOST=localhost
export MONGO_PORT=27017
export MONGO_USER=admin
export MONGO_PASSWORD=secret
export MONGO_AUTH_DB=admin
```

## Depuración

### Depurar con Visual Studio Code
1. Abrir el proyecto en VS Code
2. Instalar la extensión de C#
3. Presionar F5 para iniciar la depuración
4. Los argumentos se pueden configurar en `.vscode/launch.json`

### Depurar con Visual Studio
1. Abrir `MongoBackupRestore.sln`
2. Establecer `MongoBackupRestore.Cli` como proyecto de inicio
3. Configurar argumentos en Propiedades del proyecto > Depuración
4. Presionar F5 para iniciar

## Solución de Problemas

### Error: "mongodump no está disponible"
Instale MongoDB Database Tools desde: https://www.mongodb.com/try/download/database-tools

### Error: "Docker no está disponible"
Instale Docker Desktop desde: https://www.docker.com/products/docker-desktop

### Error de compilación
```bash
dotnet clean
dotnet restore
dotnet build
```

## CI/CD con GitHub Actions

Este proyecto utiliza GitHub Actions para integración y entrega continua. Ver detalles completos en [.github/workflows/README.md](.github/workflows/README.md).

### Workflows disponibles:
- **Build y Test**: Se ejecuta en cada push y PR para compilar y probar el código
- **Validación de PR**: Valida cambios en PRs antes de permitir el merge
- **Release**: Crea releases con binarios para múltiples plataformas

Los workflows se ejecutan automáticamente y todos deben pasar antes de mergear un PR.

## Contribuir

Antes de enviar cambios:
1. Ejecutar `dotnet build` - debe compilar sin errores
2. Ejecutar `dotnet test` - todas las pruebas deben pasar
3. Ejecutar `dotnet format` - aplicar formato de código
4. Verificar que los cambios sigan las convenciones del proyecto

Los workflows de CI/CD validarán automáticamente tus cambios cuando abras un PR.
