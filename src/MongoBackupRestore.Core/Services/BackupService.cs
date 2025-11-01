using System.Text;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;
using MongoBackupRestore.Core.Models;

namespace MongoBackupRestore.Core.Services;

/// <summary>
/// Servicio para realizar backups de MongoDB
/// </summary>
public class BackupService : IBackupService
{
    private readonly IProcessRunner _processRunner;
    private readonly IMongoToolsValidator _toolsValidator;
    private readonly IMongoConnectionValidator? _connectionValidator;
    private readonly IDockerContainerDetector? _containerDetector;
    private readonly ICompressionService? _compressionService;
    private readonly IBackupRetentionService? _retentionService;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        IProcessRunner processRunner,
        IMongoToolsValidator toolsValidator,
        ILogger<BackupService> logger,
        IMongoConnectionValidator? connectionValidator = null,
        IDockerContainerDetector? containerDetector = null,
        ICompressionService? compressionService = null,
        IBackupRetentionService? retentionService = null)
    {
        _processRunner = processRunner;
        _toolsValidator = toolsValidator;
        _connectionValidator = connectionValidator;
        _containerDetector = containerDetector;
        _compressionService = compressionService;
        _retentionService = retentionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BackupResult> ExecuteBackupAsync(BackupOptions options, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando backup de la base de datos: {Database}", options.Database);

        // Si está en modo Docker sin nombre de contenedor, intentar auto-detección
        if (options.InDocker && string.IsNullOrWhiteSpace(options.ContainerName))
        {
            var detectResult = await AutoDetectContainerAsync(options, cancellationToken);
            if (!detectResult.Success)
            {
                return detectResult;
            }
        }

        // Validar opciones
        var validationResult = ValidateOptions(options);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        // Validar herramientas disponibles
        var toolsInfo = await _toolsValidator.ValidateToolsAsync(cancellationToken);
        var toolsValidationResult = ValidateRequiredTools(options, toolsInfo);
        if (!toolsValidationResult.Success)
        {
            return toolsValidationResult;
        }

        // Validar contenedor Docker si está en modo Docker
        if (options.InDocker && _containerDetector != null)
        {
            var containerValidationResult = await ValidateDockerContainerAsync(options, cancellationToken);
            if (!containerValidationResult.Success)
            {
                return containerValidationResult;
            }
        }

        // Validar credenciales si se proporcionan
        if (_connectionValidator != null && HasAuthenticationCredentials(options))
        {
            var (success, errorMessage) = await _connectionValidator.ValidateConnectionAsync(
                options.Host,
                options.Port,
                options.Username,
                options.Password,
                options.AuthenticationDatabase,
                options.Uri,
                cancellationToken);

            if (!success)
            {
                return new BackupResult
                {
                    Success = false,
                    Message = errorMessage ?? "Error al validar las credenciales de autenticación",
                    ExitCode = 1
                };
            }
        }

        // Crear directorio de salida si no existe
        try
        {
            Directory.CreateDirectory(options.OutputPath);
            _logger.LogInformation("Directorio de salida: {OutputPath}", options.OutputPath);
        }
        catch (Exception ex)
        {
            return new BackupResult
            {
                Success = false,
                Message = $"Error al crear el directorio de salida: {ex.Message}",
                ExitCode = 1
            };
        }

        // Ejecutar backup según el modo
        if (options.InDocker)
        {
            return await ExecuteDockerBackupAsync(options, cancellationToken);
        }
        else
        {
            return await ExecuteLocalBackupAsync(options, cancellationToken);
        }
    }

    private BackupResult ValidateOptions(BackupOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Database))
        {
            return new BackupResult
            {
                Success = false,
                Message = "El nombre de la base de datos es obligatorio (--db)",
                ExitCode = 1
            };
        }

        if (string.IsNullOrWhiteSpace(options.OutputPath))
        {
            return new BackupResult
            {
                Success = false,
                Message = "La ruta de salida es obligatoria (--out)",
                ExitCode = 1
            };
        }

        // La validación del nombre del contenedor se ha movido a después de la auto-detección
        // para permitir que se detecte automáticamente si no se proporciona

        return new BackupResult { Success = true };
    }

    private BackupResult ValidateRequiredTools(BackupOptions options, MongoToolsInfo toolsInfo)
    {
        if (options.InDocker)
        {
            if (!toolsInfo.DockerAvailable)
            {
                return new BackupResult
                {
                    Success = false,
                    Message = "Docker no está disponible. Instale Docker Desktop para usar el modo --in-docker.\n" +
                             "Descarga: https://www.docker.com/products/docker-desktop",
                    ExitCode = 127
                };
            }
        }
        else
        {
            if (!toolsInfo.MongoDumpAvailable)
            {
                return new BackupResult
                {
                    Success = false,
                    Message = "mongodump no está disponible. Instale MongoDB Database Tools.\n" +
                             "Descarga: https://www.mongodb.com/try/download/database-tools",
                    ExitCode = 127
                };
            }
        }

        return new BackupResult { Success = true };
    }

    private bool HasAuthenticationCredentials(BackupOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.Username) || !string.IsNullOrWhiteSpace(options.Uri);
    }

    private async Task<BackupResult> ExecuteLocalBackupAsync(BackupOptions options, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ejecutando backup local...");

        var arguments = BuildMongoDumpArguments(options);
        var commandName = MongoToolsValidator.GetMongoCommandName("mongodump");
        
        _logger.LogDebug("Comando: {Command} {Arguments}", commandName, arguments);

        var (exitCode, output, error) = await _processRunner.RunProcessAsync(
            commandName,
            arguments,
            cancellationToken);

        if (exitCode == 0)
        {
            var message = $"Backup completado exitosamente para la base de datos '{options.Database}'";
            _logger.LogInformation(message);

            // Comprimir el backup si se especificó un formato de compresión
            var backupPath = options.OutputPath;
            if (options.CompressionFormat != CompressionFormat.None)
            {
                var compressionResult = await CompressBackupAsync(options, cancellationToken);
                if (!compressionResult.Success)
                {
                    return compressionResult;
                }
                backupPath = compressionResult.BackupPath!;
                message = $"Backup completado y comprimido exitosamente para la base de datos '{options.Database}'";
            }

            // Aplicar política de retención si está configurada
            if (options.RetentionDays.HasValue && options.RetentionDays.Value > 0)
            {
                await ApplyRetentionPolicyAsync(options, cancellationToken);
            }

            return new BackupResult
            {
                Success = true,
                Message = message,
                BackupPath = backupPath,
                ExitCode = exitCode,
                Output = output,
                Error = error
            };
        }
        else
        {
            var message = AnalyzeBackupError(error, output, exitCode);
            _logger.LogError(message);
            _logger.LogError("Error: {Error}", error);

            return new BackupResult
            {
                Success = false,
                Message = message,
                ExitCode = exitCode,
                Output = output,
                Error = error
            };
        }
    }

    private async Task<BackupResult> ExecuteDockerBackupAsync(BackupOptions options, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ejecutando backup en contenedor Docker: {ContainerName}", options.ContainerName);

        // Crear directorio temporal dentro del contenedor
        var tempPath = "/tmp/mongodb-backup";
        var arguments = BuildDockerMongoDumpArguments(options, tempPath);

        _logger.LogDebug("Comando: docker exec {Arguments}", arguments);

        var (exitCode, output, error) = await _processRunner.RunProcessAsync(
            "docker",
            arguments,
            cancellationToken);

        if (exitCode != 0)
        {
            var message = $"Error al ejecutar mongodump en el contenedor (código de salida: {exitCode})";
            _logger.LogError(message);
            _logger.LogError("Error: {Error}", error);

            return new BackupResult
            {
                Success = false,
                Message = message,
                ExitCode = exitCode,
                Output = output,
                Error = error
            };
        }

        // Copiar el backup desde el contenedor al host
        var copyResult = await CopyBackupFromContainerAsync(options, tempPath, cancellationToken);
        if (!copyResult.Success)
        {
            return copyResult;
        }

        // Limpiar directorio temporal en el contenedor
        await CleanupContainerTempAsync(options.ContainerName!, tempPath, cancellationToken);

        var successMessage = $"Backup completado exitosamente para la base de datos '{options.Database}' desde el contenedor '{options.ContainerName}'";
        _logger.LogInformation(successMessage);

        // Comprimir el backup si se especificó un formato de compresión
        var backupPath = options.OutputPath;
        if (options.CompressionFormat != CompressionFormat.None)
        {
            var compressionResult = await CompressBackupAsync(options, cancellationToken);
            if (!compressionResult.Success)
            {
                return compressionResult;
            }
            backupPath = compressionResult.BackupPath!;
            successMessage = $"Backup completado y comprimido exitosamente para la base de datos '{options.Database}' desde el contenedor '{options.ContainerName}'";
        }

        // Aplicar política de retención si está configurada
        if (options.RetentionDays.HasValue && options.RetentionDays.Value > 0)
        {
            await ApplyRetentionPolicyAsync(options, cancellationToken);
        }

        return new BackupResult
        {
            Success = true,
            Message = successMessage,
            BackupPath = backupPath,
            ExitCode = 0,
            Output = output,
            Error = error
        };
    }

    private async Task<BackupResult> CopyBackupFromContainerAsync(
        BackupOptions options,
        string containerPath,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Copiando backup desde el contenedor al host...");

        // Validar que containerName no sea null (ya validado anteriormente)
        if (string.IsNullOrWhiteSpace(options.ContainerName))
        {
            throw new InvalidOperationException("El nombre del contenedor no puede ser nulo en este punto");
        }

        var copyArgs = $"cp {options.ContainerName}:{containerPath}/. {options.OutputPath}";
        var (exitCode, output, error) = await _processRunner.RunProcessAsync(
            "docker",
            copyArgs,
            cancellationToken);

        if (exitCode != 0)
        {
            var message = $"Error al copiar el backup desde el contenedor (código de salida: {exitCode})";
            _logger.LogError(message);
            _logger.LogError("Error: {Error}", error);

            return new BackupResult
            {
                Success = false,
                Message = message,
                ExitCode = exitCode,
                Error = error
            };
        }

        _logger.LogInformation("Backup copiado exitosamente al host");
        return new BackupResult { Success = true };
    }

    private async Task CleanupContainerTempAsync(
        string containerName,
        string tempPath,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Limpiando directorio temporal en el contenedor...");
            var cleanupArgs = $"exec {containerName} rm -rf {tempPath}";
            await _processRunner.RunProcessAsync("docker", cleanupArgs, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al limpiar directorio temporal en el contenedor");
        }
    }

    private string BuildMongoDumpArguments(BackupOptions options)
    {
        var args = new StringBuilder();

        // Base de datos
        args.Append($"--db {options.Database}");

        // Ruta de salida
        args.Append($" --out {options.OutputPath}");

        // URI o Host/Port
        if (!string.IsNullOrWhiteSpace(options.Uri))
        {
            // lgtm[cs/cleartext-storage-of-sensitive-information]
            // La URI puede contener credenciales. Es una limitación de mongodump que requiere
            // las credenciales como argumentos. En producción, considere usar autenticación basada
            // en certificados o Kerberos en lugar de contraseñas en la URI.
            args.Append($" --uri \"{options.Uri}\"");
        }
        else
        {
            args.Append($" --host {options.Host}");
            args.Append($" --port {options.Port}");

            // Autenticación
            if (!string.IsNullOrWhiteSpace(options.Username))
            {
                args.Append($" --username {options.Username}");

                if (!string.IsNullOrWhiteSpace(options.Password))
                {
                    // lgtm[cs/cleartext-storage-of-sensitive-information]
                    // NOTA DE SEGURIDAD: La contraseña se pasa como argumento de línea de comandos,
                    // lo cual puede ser visible en la lista de procesos. Esto es una limitación de
                    // mongodump. En un entorno de producción, considere:
                    // 1. Usar autenticación basada en certificados o Kerberos
                    // 2. Usar variables de entorno MONGODB_PASSWORD (si mongodump lo soporta)
                    // 3. Usar archivos de configuración con permisos restrictivos
                    // 4. Ejecutar mongodump sin --password para que pida la contraseña interactivamente
                    args.Append($" --password \"{options.Password}\"");
                }

                args.Append($" --authenticationDatabase {options.AuthenticationDatabase}");
            }
        }

        return args.ToString();
    }

    private string BuildDockerMongoDumpArguments(BackupOptions options, string tempPath)
    {
        var args = new StringBuilder();

        args.Append($"exec {options.ContainerName} mongodump");

        // Base de datos
        args.Append($" --db {options.Database}");

        // Ruta de salida temporal en el contenedor
        args.Append($" --out {tempPath}");

        // Host siempre es localhost dentro del contenedor
        args.Append(" --host localhost");
        args.Append($" --port {options.Port}");

        // Autenticación
        if (!string.IsNullOrWhiteSpace(options.Username))
        {
            args.Append($" --username {options.Username}");

            if (!string.IsNullOrWhiteSpace(options.Password))
            {
                // lgtm[cs/cleartext-storage-of-sensitive-information]
                // NOTA DE SEGURIDAD: La contraseña se pasa como argumento de línea de comandos,
                // lo cual puede ser visible en la lista de procesos. Esto es una limitación de
                // mongodump. Dentro de un contenedor Docker, el riesgo es menor ya que el proceso
                // está aislado, pero en producción considere usar autenticación basada en certificados.
                args.Append($" --password \"{options.Password}\"");
            }

            args.Append($" --authenticationDatabase {options.AuthenticationDatabase}");
        }

        return args.ToString();
    }

    private string AnalyzeBackupError(string error, string output, int exitCode)
    {
        var combinedError = $"{error} {output}".ToLower();

        // Errores de autenticación
        if (combinedError.Contains("authentication failed") ||
            combinedError.Contains("auth failed") ||
            combinedError.Contains("unauthorized") ||
            combinedError.Contains("not authorized") ||
            combinedError.Contains("login failed"))
        {
            return "Error de autenticación: Las credenciales proporcionadas son incorrectas o el usuario no tiene permisos suficientes. " +
                   "Verifique el nombre de usuario (--user), contraseña (--password) y base de datos de autenticación (--auth-db).";
        }

        // Errores de conexión
        if (combinedError.Contains("connection refused") ||
            combinedError.Contains("connect failed") ||
            combinedError.Contains("econnrefused") ||
            combinedError.Contains("couldn't connect to server"))
        {
            return "Error de conexión: No se pudo conectar al servidor MongoDB. " +
                   "Verifique que el host (--host), puerto (--port) y servicio MongoDB estén disponibles.";
        }

        // Error de base de datos no encontrada
        if (combinedError.Contains("database") && combinedError.Contains("not found"))
        {
            return "Error: La base de datos especificada no existe en el servidor MongoDB.";
        }

        // Error genérico
        return $"Error al ejecutar mongodump (código de salida: {exitCode}). " +
               "Use --verbose para ver más detalles del error.";
    }

    private async Task<BackupResult> AutoDetectContainerAsync(BackupOptions options, CancellationToken cancellationToken)
    {
        if (_containerDetector == null)
        {
            return new BackupResult
            {
                Success = false,
                Message = "No se puede auto-detectar contenedores: el detector de contenedores no está disponible",
                ExitCode = 1
            };
        }

        _logger.LogInformation("Auto-detectando contenedores Docker con MongoDB...");

        var containers = await _containerDetector.DetectMongoContainersAsync(cancellationToken);

        if (containers.Count == 0)
        {
            return new BackupResult
            {
                Success = false,
                Message = "No se encontraron contenedores Docker con MongoDB en ejecución. " +
                         "Especifique el nombre del contenedor con --container-name o inicie un contenedor MongoDB.",
                ExitCode = 1
            };
        }

        if (containers.Count > 1)
        {
            return new BackupResult
            {
                Success = false,
                Message = $"Se encontraron múltiples contenedores con MongoDB: {string.Join(", ", containers)}. " +
                         "Especifique cuál usar con --container-name.",
                ExitCode = 1
            };
        }

        // Usar el único contenedor encontrado
        options.ContainerName = containers[0];
        _logger.LogInformation("Contenedor detectado automáticamente: {ContainerName}", options.ContainerName);

        return new BackupResult { Success = true };
    }

    private async Task<BackupResult> ValidateDockerContainerAsync(BackupOptions options, CancellationToken cancellationToken)
    {
        if (_containerDetector == null || string.IsNullOrWhiteSpace(options.ContainerName))
        {
            return new BackupResult { Success = true };
        }

        // Validar que el contenedor existe y está en ejecución
        var (containerValid, containerError) = await _containerDetector.ValidateContainerAsync(
            options.ContainerName, cancellationToken);

        if (!containerValid)
        {
            return new BackupResult
            {
                Success = false,
                Message = containerError ?? $"Error al validar el contenedor '{options.ContainerName}'",
                ExitCode = 1
            };
        }

        // Validar que mongodump existe en el contenedor
        var (binariesValid, binariesError) = await _containerDetector.ValidateMongoBinariesInContainerAsync(
            options.ContainerName, checkMongoDump: true, checkMongoRestore: false, cancellationToken);

        if (!binariesValid)
        {
            return new BackupResult
            {
                Success = false,
                Message = binariesError ?? $"mongodump no está disponible en el contenedor '{options.ContainerName}'. " +
                         "Asegúrese de que el contenedor tenga MongoDB Database Tools instalado.",
                ExitCode = 127
            };
        }

        _logger.LogInformation("Contenedor Docker validado: {ContainerName}", options.ContainerName);
        return new BackupResult { Success = true };
    }

    private async Task<BackupResult> CompressBackupAsync(BackupOptions options, CancellationToken cancellationToken)
    {
        if (_compressionService == null)
        {
            return new BackupResult
            {
                Success = false,
                Message = "El servicio de compresión no está disponible. No se puede comprimir el backup.",
                ExitCode = 1
            };
        }

        try
        {
            _logger.LogInformation("Iniciando compresión del backup en formato {Format}...", options.CompressionFormat);
            
            var destinationFile = Path.Combine(
                Path.GetDirectoryName(options.OutputPath) ?? ".",
                $"{Path.GetFileName(options.OutputPath)}_{DateTime.Now:yyyyMMdd_HHmmss}");

            var compressedFilePath = await _compressionService.CompressAsync(
                options.OutputPath,
                destinationFile,
                options.CompressionFormat,
                message => _logger.LogInformation(message),
                cancellationToken);

            // Eliminar el directorio sin comprimir después de la compresión exitosa
            if (Directory.Exists(options.OutputPath))
            {
                _logger.LogDebug("Eliminando directorio sin comprimir: {OutputPath}", options.OutputPath);
                Directory.Delete(options.OutputPath, recursive: true);
            }

            return new BackupResult
            {
                Success = true,
                BackupPath = compressedFilePath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al comprimir el backup");
            return new BackupResult
            {
                Success = false,
                Message = $"Error al comprimir el backup: {ex.Message}",
                ExitCode = 1
            };
        }
    }

    /// <summary>
    /// Aplica la política de retención de backups eliminando backups antiguos
    /// </summary>
    private async Task ApplyRetentionPolicyAsync(BackupOptions options, CancellationToken cancellationToken)
    {
        if (_retentionService == null)
        {
            _logger.LogWarning("El servicio de retención no está disponible. No se aplicará la política de retención.");
            return;
        }

        try
        {
            _logger.LogInformation("Aplicando política de retención de {Days} días...", options.RetentionDays!.Value);

            // Obtener el directorio padre donde están todos los backups
            var backupParentDir = Path.GetDirectoryName(options.OutputPath);
            if (string.IsNullOrWhiteSpace(backupParentDir))
            {
                backupParentDir = ".";
            }

            var cleanupResult = await _retentionService.CleanupOldBackupsAsync(
                backupParentDir,
                options.RetentionDays.Value,
                dryRun: false,
                cancellationToken);

            if (cleanupResult.Success)
            {
                _logger.LogInformation("Política de retención aplicada exitosamente: {Message}", cleanupResult.Message);
            }
            else
            {
                _logger.LogWarning("La política de retención no se pudo aplicar completamente: {Message}", cleanupResult.Message);
            }

            // Log de errores si los hay
            if (cleanupResult.Errors.Count > 0)
            {
                foreach (var error in cleanupResult.Errors)
                {
                    _logger.LogWarning("Error en retención: {Error}", error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al aplicar política de retención");
        }
    }
}
