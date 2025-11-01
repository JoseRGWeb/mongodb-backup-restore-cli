# CI/CD con GitHub Actions

Este documento describe los workflows de CI/CD implementados para el proyecto MongoDB Backup Restore CLI.

## Workflows Disponibles

### 1. Build y Test (`build-test.yml`)

**Propósito**: Compilar la solución .NET y ejecutar todas las pruebas automatizadas.

**Se ejecuta en**:
- Cada push a las ramas `main` y `develop`
- Cada pull request hacia las ramas `main` y `develop`

**Pasos**:
1. Checkout del código
2. Configuración de .NET 8.0
3. Restauración de dependencias NuGet
4. Compilación de la solución en modo Release
5. Ejecución de todas las pruebas unitarias y funcionales
6. Publicación de resultados de pruebas
7. Subida de artefactos de build (retención: 7 días)

**Estado**: Se requiere que este workflow pase exitosamente para mergear PRs.

### 2. Validación de Pull Request (`pr-validation.yml`)

**Propósito**: Validar que los cambios propuestos en un PR cumplen con los estándares de calidad.

**Se ejecuta en**:
- Cuando se abre un PR
- Cuando se actualizan los commits de un PR
- Cuando se reabre un PR
- Cuando un PR draft se marca como ready for review

**Pasos**:
1. Checkout completo del código (con historial)
2. Configuración de .NET 8.0
3. Restauración de dependencias
4. Verificación de formato de código con `dotnet format`
5. Compilación en modo Debug
6. Compilación en modo Release
7. Ejecución de pruebas con reporte de cobertura
8. Generación y envío de reporte de cobertura a Codecov
9. Listado de archivos modificados en el PR

**Características**:
- No se ejecuta en PRs marcados como draft
- La verificación de formato es opcional (continue-on-error: true)
- Genera reportes de cobertura de código

### 3. Release (`release.yml`)

**Propósito**: Crear releases con binarios compilados para múltiples plataformas.

**Se ejecuta en**:
- Cuando se publica un release en GitHub
- Cuando se crea un tag con formato `v*.*.*` (ejemplo: v1.0.0)

**Pasos**:
1. Checkout del código
2. Configuración de .NET 8.0
3. Extracción de versión desde el tag
4. Restauración de dependencias
5. Compilación en modo Release
6. Ejecución de pruebas
7. Publicación para múltiples plataformas:
   - Linux x64 (archivo .tar.gz)
   - Windows x64 (archivo .zip)
   - macOS x64 (archivo .tar.gz)
   - macOS ARM64 (archivo .tar.gz)
8. Creación de archivos comprimidos
9. Subida de artefactos (retención: 90 días)
10. Adjuntar binarios al release de GitHub

**Configuración de publicación**:
- Self-contained: Incluye el runtime de .NET
- PublishSingleFile: Empaqueta todo en un único ejecutable
- PublishTrimmed: Reduce el tamaño eliminando código no utilizado

## Configuración de Protección de Ramas

Se recomienda configurar las siguientes reglas de protección para las ramas principales:

### Para la rama `main`:
1. Ir a Settings → Branches → Add branch protection rule
2. Branch name pattern: `main`
3. Activar las siguientes opciones:
   - ✅ Require a pull request before merging
   - ✅ Require status checks to pass before merging
     - Agregar: `Compilar y Probar`
     - Agregar: `Validar PR`
   - ✅ Require branches to be up to date before merging
   - ✅ Do not allow bypassing the above settings

### Para la rama `develop`:
1. Configuración similar a `main`
2. Se puede permitir merges directos para desarrollo rápido

## Badges de Estado

Puedes agregar estos badges al README.md para mostrar el estado de los workflows:

```markdown
[![Build y Test](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/actions/workflows/build-test.yml/badge.svg)](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/actions/workflows/build-test.yml)
[![Validación de PR](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/actions/workflows/pr-validation.yml/badge.svg)](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/actions/workflows/pr-validation.yml)
[![Release](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/actions/workflows/release.yml/badge.svg)](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/actions/workflows/release.yml)
```

## Secretos y Variables

Actualmente los workflows no requieren secretos adicionales. El token `GITHUB_TOKEN` se proporciona automáticamente.

Si en el futuro necesitas agregar secretos (por ejemplo, para publicar en NuGet):
1. Ir a Settings → Secrets and variables → Actions
2. Click en "New repository secret"
3. Agregar el nombre y valor del secreto

## Artefactos Generados

### Build Artifacts (workflow: build-test.yml)
- **Ubicación**: Disponibles en la página del workflow run
- **Contenido**: Binarios compilados en modo Release
- **Retención**: 7 días
- **Uso**: Debugging, testing manual

### Release Artifacts (workflow: release.yml)
- **Ubicación**: Adjuntos al release en GitHub + disponibles en workflow run
- **Contenido**: Ejecutables self-contained para todas las plataformas
- **Retención**: 90 días en artifacts, permanente en releases
- **Uso**: Distribución a usuarios finales

## Monitoreo y Depuración

### Ver resultados de workflows
1. Ir a la pestaña "Actions" en el repositorio
2. Seleccionar el workflow que quieres revisar
3. Click en una ejecución específica para ver detalles

### Descargar artefactos
1. Abrir una ejecución exitosa del workflow
2. Scroll hasta la sección "Artifacts"
3. Click en el nombre del artefacto para descargarlo

### Re-ejecutar un workflow fallido
1. Abrir la ejecución fallida
2. Click en "Re-run jobs" → "Re-run all jobs"

## Mantenimiento

### Actualizar versiones de acciones
Las acciones de GitHub se mantienen con versionado semántico. Revisa regularmente:
- `actions/checkout`: Actualmente en v4
- `actions/setup-dotnet`: Actualmente en v4
- `actions/upload-artifact`: Actualmente en v4

### Actualizar versión de .NET
Si el proyecto migra a una nueva versión de .NET:
1. Actualizar `dotnet-version` en todos los workflows
2. Actualizar la documentación BUILD.md

## Próximos Pasos (Roadmap)

- [ ] Agregar análisis de seguridad con CodeQL
- [ ] Implementar deploy automático a contenedores Docker
- [ ] Agregar análisis de calidad de código con SonarQube
- [ ] Implementar notificaciones a Slack/Teams en releases
- [ ] Agregar tests de integración con MongoDB real
- [ ] Publicar paquete NuGet automáticamente en releases
