# Guía de Contribución

¡Gracias por tu interés en contribuir a MongoDB Backup & Restore CLI! Este documento proporciona las directrices y mejores prácticas para contribuir al proyecto.

## Tabla de Contenidos

- [Código de Conducta](#código-de-conducta)
- [¿Cómo puedo contribuir?](#cómo-puedo-contribuir)
- [Configuración del Entorno de Desarrollo](#configuración-del-entorno-de-desarrollo)
- [Convenciones de Commits](#convenciones-de-commits)
- [Estructura de Ramas](#estructura-de-ramas)
- [Proceso de Pull Request](#proceso-de-pull-request)
- [Guías de Estilo](#guías-de-estilo)
- [Testing](#testing)
- [Proceso de Releases](#proceso-de-releases)
- [Recursos Adicionales](#recursos-adicionales)

## Código de Conducta

Este proyecto se adhiere a un Código de Conducta que todos los contribuidores deben seguir. Por favor, lee [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) para conocer los detalles.

## ¿Cómo puedo contribuir?

Hay muchas formas de contribuir a este proyecto:

### Reportar Bugs

- **Antes de reportar**, verifica que el bug no haya sido reportado anteriormente en los [Issues](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/issues).
- **Usa la plantilla de issue** para bugs si está disponible.
- **Proporciona detalles**:
  - Versión de .NET SDK
  - Sistema operativo
  - Versión de MongoDB y herramientas (mongodump/mongorestore)
  - Pasos para reproducir el problema
  - Comportamiento esperado vs. comportamiento actual
  - Logs relevantes (usa `--verbose` para obtener logs detallados)

### Sugerir Mejoras

- **Verifica primero** si la mejora ya fue sugerida en los Issues.
- **Describe el caso de uso** y cómo beneficiaría a otros usuarios.
- **Proporciona ejemplos** de cómo se usaría la nueva funcionalidad.

### Contribuir con Código

1. **Fork el repositorio** y crea tu rama desde `main`.
2. **Implementa tu cambio** siguiendo las guías de estilo.
3. **Añade o actualiza tests** para cubrir tus cambios.
4. **Asegúrate de que todos los tests pasen** (`dotnet test`).
5. **Documenta tus cambios** si afectan el uso de la herramienta.
6. **Abre un Pull Request** con una descripción clara.

### Mejorar Documentación

La documentación es crucial para el proyecto. Puedes contribuir:

- Corrigiendo errores tipográficos o gramaticales
- Mejorando la claridad de las explicaciones
- Añadiendo ejemplos de uso
- Traduciendo documentación
- Creando tutoriales o guías

## Configuración del Entorno de Desarrollo

### Requisitos

- .NET SDK 8.0 o superior
- Git
- Docker Desktop (opcional, para testing con contenedores)
- MongoDB Database Tools (mongodump/mongorestore)
- Un editor de código (Visual Studio, VS Code, Rider, etc.)

### Configuración Inicial

```bash
# 1. Fork el repositorio en GitHub

# 2. Clonar tu fork
git clone https://github.com/TU_USUARIO/mongodb-backup-restore-cli.git
cd mongodb-backup-restore-cli

# 3. Añadir el repositorio original como upstream
git remote add upstream https://github.com/JoseRGWeb/mongodb-backup-restore-cli.git

# 4. Restaurar dependencias
dotnet restore

# 5. Compilar el proyecto
dotnet build

# 6. Ejecutar los tests
dotnet test

# 7. Verificar formato de código
dotnet format --verify-no-changes
```

### Mantener tu Fork Actualizado

```bash
# Obtener los últimos cambios de upstream
git fetch upstream

# Actualizar tu rama main local
git checkout main
git merge upstream/main

# Actualizar tu fork en GitHub
git push origin main
```

## Convenciones de Commits

Este proyecto utiliza [Conventional Commits](https://www.conventionalcommits.org/es/) para mensajes de commit claros y consistentes. Esto facilita la generación automática de changelogs y el versionado semántico.

### Formato

```
<tipo>[ámbito opcional]: <descripción>

[cuerpo opcional]

[footer(s) opcional(es)]
```

### Tipos de Commit

- **feat**: Una nueva funcionalidad para el usuario
  ```
  feat: añadir soporte para MongoDB Atlas
  feat(backup): implementar compresión incremental
  ```

- **fix**: Corrección de un bug
  ```
  fix: corregir error al conectar con Docker remoto
  fix(restore): resolver problema con autenticación MongoDB 6.0
  ```

- **docs**: Cambios solo en documentación
  ```
  docs: actualizar README con ejemplos de cifrado
  docs(contributing): añadir guía de debugging
  ```

- **style**: Cambios que no afectan el significado del código (espacios, formato, etc.)
  ```
  style: aplicar formato dotnet format
  style(core): reorganizar using statements
  ```

- **refactor**: Cambio de código que no corrige bugs ni añade funcionalidades
  ```
  refactor: simplificar lógica de detección de contenedores
  refactor(cli): extraer validación de argumentos a clase separada
  ```

- **perf**: Mejora de rendimiento
  ```
  perf: optimizar compresión de archivos grandes
  perf(backup): reducir uso de memoria en backups grandes
  ```

- **test**: Añadir o corregir tests
  ```
  test: añadir tests para cifrado AES-256
  test(integration): añadir tests end-to-end con Docker
  ```

- **build**: Cambios en el sistema de build o dependencias externas
  ```
  build: actualizar a .NET 8.0.1
  build(deps): actualizar System.CommandLine a 2.0.0
  ```

- **ci**: Cambios en archivos de configuración de CI/CD
  ```
  ci: añadir workflow para publicación en NuGet
  ci(github-actions): mejorar cache de dependencias
  ```

- **chore**: Otros cambios que no modifican src o test
  ```
  chore: actualizar .gitignore
  chore(release): preparar versión 1.2.0
  ```

### Reglas para Buenos Commits

1. **Usa el imperativo** en la descripción: "añadir" en lugar de "añadido" o "añade"
2. **Primera línea corta** (máximo 72 caracteres)
3. **No termines con punto** la primera línea
4. **Separa la descripción del cuerpo** con una línea en blanco
5. **Explica el "qué" y el "por qué"**, no el "cómo"
6. **Un commit por cambio lógico**

### Ejemplos de Buenos Commits

```
feat(backup): añadir soporte para retención de backups

Implementa la funcionalidad de retención automática de backups
por número de días. Los backups más antiguos se eliminan 
automáticamente después de crear un nuevo backup.

Incluye:
- Nueva opción --retention-days
- Variable de entorno MONGO_RETENTION_DAYS
- Logs detallados de limpieza
- Tests unitarios e integración

Closes #42
```

```
fix(docker): corregir detección de contenedores en Windows

La detección automática de contenedores MongoDB fallaba en
Windows debido a diferencias en el formato de salida de
docker ps. Se normaliza el parsing para funcionar en todos
los sistemas operativos.

Fixes #58
```

### Breaking Changes

Si tu cambio rompe compatibilidad hacia atrás, usa `BREAKING CHANGE:` en el footer:

```
feat(cli)!: cambiar nombre de opción --out a --output

BREAKING CHANGE: La opción --out se ha renombrado a --output
para mayor claridad. Los scripts existentes deben actualizarse.

Migration: Reemplazar --out con --output en todos los comandos.
```

## Estructura de Ramas

El proyecto utiliza una estrategia de branching simplificada:

### Ramas Principales

- **`main`**: Rama principal de desarrollo
  - Siempre debe estar en un estado funcional
  - Los merges requieren PR y revisión
  - Protegida contra push directo
  - Base para todas las ramas de features

### Ramas de Trabajo

#### Feature Branches (Funcionalidades)

```bash
# Formato: feature/descripcion-corta
git checkout -b feature/compresion-incremental

# Ejemplos:
feature/soporte-mongodb-atlas
feature/backup-programado
feature/integracion-azure-blob
```

#### Fix Branches (Correcciones)

```bash
# Formato: fix/descripcion-del-problema
git checkout -b fix/error-conexion-ssl

# Ejemplos:
fix/timeout-backups-grandes
fix/validacion-credenciales
fix/formato-logs-windows
```

#### Docs Branches (Documentación)

```bash
# Formato: docs/tema
git checkout -b docs/guia-docker-avanzado

# Ejemplos:
docs/ejemplos-ci-cd
docs/traduccion-ingles
docs/troubleshooting
```

#### Chore Branches (Mantenimiento)

```bash
# Formato: chore/descripcion
git checkout -b chore/actualizar-dependencias

# Ejemplos:
chore/mejorar-ci
chore/refactorizar-tests
chore/actualizar-sdk-net9
```

### Workflow de Trabajo con Ramas

```bash
# 1. Actualizar main
git checkout main
git pull upstream main

# 2. Crear rama de feature
git checkout -b feature/mi-funcionalidad

# 3. Hacer cambios y commits
git add .
git commit -m "feat: implementar mi funcionalidad"

# 4. Mantener la rama actualizada (recomendado para ramas de larga duración)
git fetch upstream
git rebase upstream/main

# 5. Subir rama a tu fork
git push origin feature/mi-funcionalidad

# 6. Abrir Pull Request en GitHub
```

## Proceso de Pull Request

### Antes de Abrir el PR

1. **Actualiza tu rama** con los últimos cambios de `main`
2. **Ejecuta todos los tests**: `dotnet test`
3. **Verifica el formato**: `dotnet format --verify-no-changes`
4. **Compila en Release**: `dotnet build --configuration Release`
5. **Revisa tus cambios**: `git diff main`

### Crear el Pull Request

1. **Título descriptivo** siguiendo Conventional Commits:
   ```
   feat: añadir soporte para MongoDB Atlas
   fix: corregir timeout en backups grandes
   docs: mejorar ejemplos de cifrado
   ```

2. **Descripción completa** que incluya:
   - **¿Qué cambia este PR?** - Resumen claro de los cambios
   - **¿Por qué es necesario?** - Contexto y motivación
   - **¿Cómo se ha probado?** - Pasos de testing
   - **Checklist**:
     - [ ] Tests añadidos/actualizados
     - [ ] Documentación actualizada
     - [ ] Código formateado (`dotnet format`)
     - [ ] Todos los tests pasan
     - [ ] Sin breaking changes (o documentados)
   - **Issues relacionados**: `Closes #123` o `Fixes #456`

3. **Screenshots/Logs** (si aplica):
   - Capturas de salida de la CLI
   - Logs de ejecución
   - Evidencia de testing

### Ejemplo de Descripción de PR

```markdown
## Descripción

Este PR implementa soporte para retención automática de backups, permitiendo
eliminar backups antiguos basándose en una política de días.

## Motivación

Los usuarios necesitan gestionar el espacio de almacenamiento de backups
automáticamente sin scripts externos (#42).

## Cambios Principales

- Nueva opción `--retention-days` en comando backup
- Variable de entorno `MONGO_RETENTION_DAYS`
- Servicio `BackupRetentionService` para limpieza
- Logs detallados de archivos eliminados y espacio liberado

## Cómo se ha probado

- [ ] Tests unitarios para `BackupRetentionService`
- [ ] Tests de integración con backups reales
- [ ] Testing manual en Windows, Linux y macOS
- [ ] Testing con Docker y MongoDB local

## Checklist

- [x] Tests añadidos
- [x] Documentación actualizada (README.md, EJEMPLOS_END_TO_END.md)
- [x] Código formateado
- [x] Todos los tests pasan
- [x] Sin breaking changes

## Issues

Closes #42
```

### Proceso de Revisión

1. **Revisión automática**: GitHub Actions ejecutará builds y tests
2. **Revisión de código**: Los mantenedores revisarán tu código
3. **Cambios solicitados**: Realiza los cambios sugeridos en nuevos commits
4. **Aprobación**: Una vez aprobado, el PR será merged

### Durante la Revisión

- **Responde a comentarios** de manera constructiva
- **Haz cambios en nuevos commits** (no forces push que reescribe historia)
- **Mantén la discusión en GitHub** para transparencia
- **Sé paciente**: Las revisiones pueden tomar tiempo

### Después del Merge

- **Elimina tu rama** de feature (GitHub lo puede hacer automáticamente)
- **Actualiza tu fork**:
  ```bash
  git checkout main
  git pull upstream main
  git push origin main
  ```

## Guías de Estilo

### Estilo de Código C#

El proyecto sigue las convenciones de C# y .NET estándar:

1. **Formato automático**:
   ```bash
   # Aplicar formato automático
   dotnet format
   
   # Verificar formato
   dotnet format --verify-no-changes
   ```

2. **Naming Conventions**:
   - **PascalCase**: Clases, métodos, propiedades públicas
   - **camelCase**: Variables locales, parámetros
   - **_camelCase**: Campos privados (con underscore)
   
   ```csharp
   public class BackupService
   {
       private readonly ILogger _logger;
       
       public async Task<bool> CreateBackupAsync(string databaseName)
       {
           var backupPath = GetBackupPath();
           // ...
       }
   }
   ```

3. **Organización de archivos**:
   - Un tipo público por archivo
   - Nombre de archivo igual al tipo principal
   - Usar namespaces que reflejen la estructura de carpetas

4. **Comentarios y documentación**:
   ```csharp
   /// <summary>
   /// Crea un backup de la base de datos especificada.
   /// </summary>
   /// <param name="databaseName">Nombre de la base de datos a respaldar.</param>
   /// <returns>True si el backup fue exitoso, false en caso contrario.</returns>
   public async Task<bool> CreateBackupAsync(string databaseName)
   ```

5. **Manejo de errores**:
   - Usa excepciones para condiciones excepcionales
   - Log apropiado en todos los niveles
   - Valida argumentos temprano
   
   ```csharp
   public void ProcessBackup(string path)
   {
       ArgumentNullException.ThrowIfNull(path);
       
       try
       {
           // Procesamiento
       }
       catch (IOException ex)
       {
           _logger.LogError(ex, "Error al procesar backup: {Path}", path);
           throw;
       }
   }
   ```

6. **Async/Await**:
   - Usa async/await consistentemente
   - Métodos async deben terminar en `Async`
   - Evita `async void` excepto en event handlers

### Estilo de Documentación

1. **Formato Markdown**:
   - Usa headings jerárquicos (#, ##, ###)
   - Incluye tabla de contenidos para docs largos
   - Usa code blocks con sintaxis highlighting

2. **Ejemplos de código**:
   - Incluye comentarios explicativos
   - Muestra entrada y salida esperada
   - Usa casos de uso realistas

3. **Idioma**:
   - Documentación principal en español
   - Código y variables en inglés
   - Comentarios de código en español

## Testing

### Tipos de Tests

1. **Tests Unitarios**:
   - Ubicados en `tests/MongoBackupRestore.Tests/`
   - Un archivo de test por clase a testear
   - Nombre: `{ClaseName}Tests.cs`
   
   ```csharp
   public class BackupServiceTests
   {
       [Fact]
       public async Task CreateBackup_WithValidOptions_ReturnsSuccess()
       {
           // Arrange
           var service = CreateService();
           
           // Act
           var result = await service.CreateBackupAsync("testdb");
           
           // Assert
           Assert.True(result);
       }
   }
   ```

2. **Tests de Integración**:
   - Usan contenedores Docker con Testcontainers
   - Prueban integración con MongoDB real
   - Pueden ser más lentos

3. **Tests End-to-End**:
   - Ejecutan la CLI completa
   - Validan escenarios de usuario completos

### Ejecutar Tests

```bash
# Todos los tests
dotnet test

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Solo una categoría
dotnet test --filter "Category=Unit"

# Un test específico
dotnet test --filter "FullyQualifiedName~BackupServiceTests.CreateBackup"

# Modo verbose
dotnet test --verbosity detailed
```

### Guías para Escribir Tests

1. **Nombres descriptivos**:
   ```csharp
   [Fact]
   public void CreateBackup_WhenDatabaseNotExists_ThrowsException()
   ```

2. **Patrón AAA** (Arrange, Act, Assert):
   ```csharp
   [Fact]
   public void Method_Condition_ExpectedResult()
   {
       // Arrange
       var input = "test";
       
       // Act
       var result = Method(input);
       
       // Assert
       Assert.Equal("expected", result);
   }
   ```

3. **Tests independientes**:
   - No dependen de orden de ejecución
   - Limpian sus recursos
   - Usan fixtures para compartir setup

4. **Cobertura**:
   - Objetivo: >80% de cobertura
   - Prioriza lógica de negocio crítica
   - No perseguir 100% a toda costa

## Proceso de Releases

El proyecto usa [Versionado Semántico](https://semver.org/lang/es/) (SemVer):

```
MAJOR.MINOR.PATCH

Ejemplo: 1.4.2
```

- **MAJOR**: Cambios incompatibles (breaking changes)
- **MINOR**: Nueva funcionalidad compatible hacia atrás
- **PATCH**: Correcciones de bugs compatibles

### Crear un Release

Los releases son creados por los mantenedores:

1. **Preparación**:
   - Asegurar que `main` esté estable
   - Todos los tests pasando
   - Documentación actualizada

2. **Actualizar versión**:
   - Actualizar en `*.csproj` files
   - Actualizar CHANGELOG (si existe)

3. **Crear tag**:
   ```bash
   git tag -a v1.4.0 -m "Release 1.4.0: Añadir soporte de retención"
   git push upstream v1.4.0
   ```

4. **GitHub Actions**:
   - Se ejecuta automáticamente el workflow de release
   - Compila binarios para todas las plataformas
   - Crea release en GitHub
   - Publica en NuGet

5. **Publicar Release Notes**:
   - Resumen de cambios
   - Nuevas funcionalidades
   - Correcciones de bugs
   - Breaking changes (si aplica)
   - Instrucciones de migración (si aplica)

### Ejemplo de Release Notes

```markdown
## MongoDB Backup & Restore CLI v1.4.0

### Nuevas Funcionalidades

- **Retención automática de backups** (#42)
  - Nueva opción `--retention-days` para limpieza automática
  - Variable de entorno `MONGO_RETENTION_DAYS`
  - Logs detallados de espacio liberado

### Mejoras

- Mejor detección de contenedores Docker en Windows (#58)
- Optimización de uso de memoria en backups grandes (#61)

### Correcciones

- Corregido timeout en backups de bases de datos >10GB (#55)
- Solucionado problema con caracteres especiales en contraseñas (#60)

### Breaking Changes

Ninguno.

### Instalación

```bash
dotnet tool update --global MongoBackupRestore.Cli
```

### Documentación

Ver [README.md](README.md) y [EJEMPLOS_END_TO_END.md](EJEMPLOS_END_TO_END.md)
```

## Recursos Adicionales

### Documentación del Proyecto

- [README.md](README.md) - Documentación principal
- [EJEMPLOS_END_TO_END.md](EJEMPLOS_END_TO_END.md) - Ejemplos de uso
- [VARIABLES_ENTORNO.md](VARIABLES_ENTORNO.md) - Variables de entorno
- [SEGURIDAD.md](SEGURIDAD.md) - Mejores prácticas de seguridad
- [LOGS_Y_DEBUGGING.md](LOGS_Y_DEBUGGING.md) - Debugging y troubleshooting

### Enlaces Externos

- [Conventional Commits](https://www.conventionalcommits.org/es/)
- [Semantic Versioning](https://semver.org/lang/es/)
- [.NET Code Style](https://docs.microsoft.com/es-es/dotnet/fundamentals/code-analysis/code-style-rule-options)
- [MongoDB Database Tools](https://www.mongodb.com/docs/database-tools/)

### Comunidad

- **Issues**: [GitHub Issues](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/issues)
- **Discusiones**: [GitHub Discussions](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/discussions)
- **Security**: Ver [SECURITY.md](SECURITY.md) para reportar vulnerabilidades

### Contacto

Para preguntas o soporte:
- Abre un [Issue](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/issues)
- Inicia una [Discussion](https://github.com/JoseRGWeb/mongodb-backup-restore-cli/discussions)

---

## Agradecimientos

¡Gracias por contribuir a MongoDB Backup & Restore CLI! Cada contribución, por pequeña que sea, hace que este proyecto sea mejor para toda la comunidad.

**¡Happy Coding! 🚀**
