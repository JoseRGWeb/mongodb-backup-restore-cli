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
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        IProcessRunner processRunner,
        IMongoToolsValidator toolsValidator,
        ILogger<BackupService> logger)
    {
        _processRunner = processRunner;
        _toolsValidator = toolsValidator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BackupResult> ExecuteBackupAsync(BackupOptions options, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando backup de la base de datos: {Database}", options.Database);

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

        if (options.InDocker && string.IsNullOrWhiteSpace(options.ContainerName))
        {
            return new BackupResult
            {
                Success = false,
                Message = "El nombre del contenedor es obligatorio cuando se usa --in-docker (--container-name)",
                ExitCode = 1
            };
        }

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

    private async Task<BackupResult> ExecuteLocalBackupAsync(BackupOptions options, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ejecutando backup local...");

        var arguments = BuildMongoDumpArguments(options);
        _logger.LogDebug("Comando: mongodump {Arguments}", arguments);

        var (exitCode, output, error) = await _processRunner.RunProcessAsync(
            "mongodump",
            arguments,
            cancellationToken);

        if (exitCode == 0)
        {
            var message = $"Backup completado exitosamente para la base de datos '{options.Database}'";
            _logger.LogInformation(message);

            return new BackupResult
            {
                Success = true,
                Message = message,
                BackupPath = options.OutputPath,
                ExitCode = exitCode,
                Output = output,
                Error = error
            };
        }
        else
        {
            var message = $"Error al ejecutar mongodump (código de salida: {exitCode})";
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

        return new BackupResult
        {
            Success = true,
            Message = successMessage,
            BackupPath = options.OutputPath,
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
                args.Append($" --password \"{options.Password}\"");
            }

            args.Append($" --authenticationDatabase {options.AuthenticationDatabase}");
        }

        return args.ToString();
    }
}
