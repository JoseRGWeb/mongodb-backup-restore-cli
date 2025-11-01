# Publicación en NuGet

Este documento describe el proceso para empaquetar y publicar la CLI de MongoDB Backup & Restore como herramienta global de .NET en NuGet.

## Requisitos Previos

- .NET SDK 8.0 o superior instalado
- Cuenta en [NuGet.org](https://www.nuget.org/)
- Clave de API de NuGet (se obtiene desde la cuenta de NuGet.org)

## Configuración del Paquete

El proyecto `MongoBackupRestore.Cli` ya está configurado como herramienta global de .NET con las siguientes características:

- **PackageId**: `MongoBackupRestore.Cli`
- **Comando de herramienta**: `mongodb-br`
- **Licencia**: MIT
- **Repositorio**: https://github.com/JoseRGWeb/mongodb-backup-restore-cli

## Versionado Semántico

El proyecto sigue el versionado semántico (SemVer):

- **MAJOR**: Cambios incompatibles en la API
- **MINOR**: Nueva funcionalidad compatible con versiones anteriores
- **PATCH**: Correcciones de errores compatibles con versiones anteriores

### Actualizar la Versión

Antes de publicar, actualiza la versión en el archivo `src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj`:

```xml
<Version>1.0.0</Version>
```

Cambia el número de versión según corresponda (ej: 1.0.1, 1.1.0, 2.0.0).

## Proceso de Empaquetado

### 1. Compilar en Modo Release

```bash
dotnet build --configuration Release
```

### 2. Ejecutar las Pruebas

Asegúrate de que todas las pruebas pasen antes de publicar:

```bash
dotnet test --configuration Release
```

### 3. Generar el Paquete NuGet

```bash
dotnet pack src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj --configuration Release --output ./nupkg
```

Este comando genera el archivo `.nupkg` en el directorio `./nupkg`.

### 4. Verificar el Contenido del Paquete

Es recomendable verificar el contenido del paquete antes de publicar:

```bash
# Instalar la herramienta de exploración de paquetes (si aún no está instalada)
dotnet tool install --global NuGetPackageExplorer

# O usar unzip para ver el contenido
unzip -l ./nupkg/MongoBackupRestore.Cli.1.0.0.nupkg
```

## Publicación en NuGet

### 1. Obtener la Clave de API de NuGet

1. Inicia sesión en [NuGet.org](https://www.nuget.org/)
2. Ve a tu perfil → **API Keys**
3. Crea una nueva clave de API con permisos para publicar paquetes
4. Guarda la clave de forma segura (solo se muestra una vez)

### 2. Configurar la Clave de API (Opcional)

Puedes configurar la clave de API para no tener que especificarla en cada comando:

```bash
dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org
```

### 3. Publicar el Paquete

```bash
dotnet nuget push ./nupkg/MongoBackupRestore.Cli.1.0.0.nupkg --api-key TU_CLAVE_API --source https://api.nuget.org/v3/index.json
```

Reemplaza `TU_CLAVE_API` con tu clave de API de NuGet y ajusta el nombre del archivo según la versión.

### 4. Verificar la Publicación

Después de publicar, el paquete estará disponible en https://www.nuget.org/packages/MongoBackupRestore.Cli/ (puede tardar unos minutos en aparecer en el índice de búsqueda).

## Publicación Automatizada con GitHub Actions

Para automatizar el proceso de publicación, se puede crear un workflow de GitHub Actions. A continuación se muestra un ejemplo básico:

```yaml
name: Publicar en NuGet

on:
  release:
    types: [published]

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --configuration Release --no-restore
      
      - name: Test
        run: dotnet test --configuration Release --no-build --verbosity normal
      
      - name: Pack
        run: dotnet pack src/MongoBackupRestore.Cli/MongoBackupRestore.Cli.csproj --configuration Release --output ./nupkg
      
      - name: Push to NuGet
        run: dotnet nuget push ./nupkg/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
        env:
          NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
```

Para usar este workflow:

1. Guarda este archivo en `.github/workflows/publish-nuget.yml`
2. Agrega tu clave de API de NuGet como secreto en GitHub:
   - Ve a tu repositorio → **Settings** → **Secrets and variables** → **Actions**
   - Crea un nuevo secreto llamado `NUGET_API_KEY` con tu clave de API
3. Crea un nuevo release en GitHub para activar la publicación

## Validación Post-Publicación

Una vez publicado el paquete, verifica la instalación:

```bash
# Instalar globalmente
dotnet tool install --global MongoBackupRestore.Cli

# Verificar la instalación
mongodb-br --version
mongodb-br --help

# Desinstalar (si es necesario)
dotnet tool uninstall --global MongoBackupRestore.Cli
```

## Actualización de Versiones

Para publicar una nueva versión:

1. Actualiza el número de versión en `MongoBackupRestore.Cli.csproj`
2. Actualiza el `CHANGELOG.md` (si existe) con los cambios de la nueva versión
3. Ejecuta el proceso de empaquetado y publicación descrito anteriormente
4. Crea un nuevo tag de Git y release en GitHub con el mismo número de versión

## Mejores Prácticas

1. **Nunca** publiques versiones sin probar
2. Usa versionado semántico consistentemente
3. Documenta todos los cambios en cada versión
4. Prueba la instalación del paquete antes de anunciar el release
5. Mantén la clave de API de NuGet segura y nunca la incluyas en el código fuente
6. Considera usar pre-releases (ej: 1.0.0-beta.1) para versiones de prueba

## Solución de Problemas

### Error: "The package already exists"

Si intentas publicar una versión que ya existe en NuGet, obtendrás este error. Debes:
- Incrementar el número de versión
- O eliminar la versión existente (solo posible dentro de las primeras 72 horas y si no tiene descargas)

### Error: "Invalid API key"

Verifica que:
- La clave de API sea correcta
- La clave no haya expirado
- La clave tenga permisos para publicar paquetes

### El paquete no aparece en NuGet.org

Después de publicar, el paquete puede tardar unos minutos en:
- Ser validado por NuGet
- Aparecer en el índice de búsqueda
- Estar disponible para instalación

## Referencias

- [Documentación oficial de .NET Global Tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools)
- [Crear y publicar un paquete NuGet](https://docs.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package-using-the-dotnet-cli)
- [Versionado Semántico](https://semver.org/lang/es/)
