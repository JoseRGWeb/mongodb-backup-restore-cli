using System.Text;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;
using MongoBackupRestore.Core.Models;

namespace MongoBackupRestore.Core.Services;

/// <summary>
/// Servicio para realizar restauraciones de MongoDB
/// </summary>
public class RestoreService : IRestoreService
{
    private readonly IProcessRunner _processRunner;
    private readonly IMongoToolsValidator _toolsValidator;
    private readonly ILogger<RestoreService> _logger;

    public RestoreService(
        IProcessRunner processRunner,
        IMongoToolsValidator toolsValidator,
        ILogger<RestoreService> logger)
    {
        _processRunner = processRunner;
        _toolsValidator = toolsValidator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RestoreResult> ExecuteRestoreAsync(RestoreOptions options, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando restauración de la base de datos: {Database}", options.Database);

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

        // Validar que la ruta de origen existe
        var pathValidationResult = ValidateSourcePath(options);
        if (!pathValidationResult.Success)
        {
            return pathValidationResult;
        }

        // Ejecutar restore según el modo
        if (options.InDocker)
        {
            return await ExecuteDockerRestoreAsync(options, cancellationToken);
        }
        else
        {
            return await ExecuteLocalRestoreAsync(options, cancellationToken);
        }
    }

    private RestoreResult ValidateOptions(RestoreOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Database))
        {
            return new RestoreResult
            {
                Success = false,
                Message = "El nombre de la base de datos es obligatorio (--db)",
                ExitCode = 1
            };
        }

        // Validar que el nombre de la base de datos no contenga caracteres peligrosos
        if (options.Database.Contains('"') || options.Database.Contains('\'') || 
            options.Database.Contains(';') || options.Database.Contains('&') ||
            options.Database.Contains('|') || options.Database.Contains('`'))
        {
            return new RestoreResult
            {
                Success = false,
                Message = "El nombre de la base de datos contiene caracteres no permitidos",
                ExitCode = 1
            };
        }

        if (string.IsNullOrWhiteSpace(options.SourcePath))
        {
            return new RestoreResult
            {
                Success = false,
                Message = "La ruta de origen es obligatoria (--from)",
                ExitCode = 1
            };
        }

        if (options.InDocker && string.IsNullOrWhiteSpace(options.ContainerName))
        {
            return new RestoreResult
            {
                Success = false,
                Message = "El nombre del contenedor es obligatorio cuando se usa --in-docker (--container-name)",
                ExitCode = 1
            };
        }

        // Validar que el nombre del contenedor no contenga caracteres peligrosos
        if (options.InDocker && !string.IsNullOrWhiteSpace(options.ContainerName))
        {
            if (options.ContainerName.Contains('"') || options.ContainerName.Contains('\'') || 
                options.ContainerName.Contains(';') || options.ContainerName.Contains('&') ||
                options.ContainerName.Contains('|') || options.ContainerName.Contains('`'))
            {
                return new RestoreResult
                {
                    Success = false,
                    Message = "El nombre del contenedor contiene caracteres no permitidos",
                    ExitCode = 1
                };
            }
        }

        // Validar que el username no contenga caracteres peligrosos
        if (!string.IsNullOrWhiteSpace(options.Username))
        {
            if (options.Username.Contains('"') || options.Username.Contains('\'') || 
                options.Username.Contains(';') || options.Username.Contains('&') ||
                options.Username.Contains('|') || options.Username.Contains('`'))
            {
                return new RestoreResult
                {
                    Success = false,
                    Message = "El nombre de usuario contiene caracteres no permitidos",
                    ExitCode = 1
                };
            }
        }

        // Validar que el host no contenga caracteres peligrosos (excepto en URI)
        if (string.IsNullOrWhiteSpace(options.Uri) && !string.IsNullOrWhiteSpace(options.Host))
        {
            if (options.Host.Contains('"') || options.Host.Contains('\'') || 
                options.Host.Contains(';') || options.Host.Contains('&') ||
                options.Host.Contains('|') || options.Host.Contains('`'))
            {
                return new RestoreResult
                {
                    Success = false,
                    Message = "El host contiene caracteres no permitidos",
                    ExitCode = 1
                };
            }
        }

        return new RestoreResult { Success = true };
    }

    private RestoreResult ValidateRequiredTools(RestoreOptions options, MongoToolsInfo toolsInfo)
    {
        if (options.InDocker)
        {
            if (!toolsInfo.DockerAvailable)
            {
                return new RestoreResult
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
            if (!toolsInfo.MongoRestoreAvailable)
            {
                return new RestoreResult
                {
                    Success = false,
                    Message = "mongorestore no está disponible. Instale MongoDB Database Tools.\n" +
                             "Descarga: https://www.mongodb.com/try/download/database-tools",
                    ExitCode = 127
                };
            }
        }

        return new RestoreResult { Success = true };
    }

    private RestoreResult ValidateSourcePath(RestoreOptions options)
    {
        if (!Directory.Exists(options.SourcePath))
        {
            return new RestoreResult
            {
                Success = false,
                Message = $"La ruta de origen no existe: {options.SourcePath}",
                ExitCode = 1
            };
        }

        _logger.LogInformation("Ruta de origen validada: {SourcePath}", options.SourcePath);
        return new RestoreResult { Success = true };
    }

    private async Task<RestoreResult> ExecuteLocalRestoreAsync(RestoreOptions options, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ejecutando restauración local...");

        var arguments = BuildMongoRestoreArguments(options);
        var commandName = MongoToolsValidator.GetMongoCommandName("mongorestore");
        
        _logger.LogDebug("Comando: {Command} {Arguments}", commandName, arguments);

        var (exitCode, output, error) = await _processRunner.RunProcessAsync(
            commandName,
            arguments,
            cancellationToken);

        if (exitCode == 0)
        {
            var message = $"Restauración completada exitosamente para la base de datos '{options.Database}'";
            _logger.LogInformation(message);

            return new RestoreResult
            {
                Success = true,
                Message = message,
                ExitCode = exitCode,
                Output = output,
                Error = error
            };
        }
        else
        {
            var message = $"Error al ejecutar mongorestore (código de salida: {exitCode})";
            _logger.LogError(message);
            _logger.LogError("Error: {Error}", error);

            return new RestoreResult
            {
                Success = false,
                Message = message,
                ExitCode = exitCode,
                Output = output,
                Error = error
            };
        }
    }

    private async Task<RestoreResult> ExecuteDockerRestoreAsync(RestoreOptions options, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ejecutando restauración en contenedor Docker: {ContainerName}", options.ContainerName);

        // Crear directorio temporal dentro del contenedor
        var tempPath = "/tmp/mongodb-restore";
        
        // Copiar el backup al contenedor
        var copyResult = await CopyBackupToContainerAsync(options, tempPath, cancellationToken);
        if (!copyResult.Success)
        {
            return copyResult;
        }

        // Ejecutar mongorestore dentro del contenedor
        var arguments = BuildDockerMongoRestoreArguments(options, tempPath);

        _logger.LogDebug("Comando: docker exec {Arguments}", arguments);

        var (exitCode, output, error) = await _processRunner.RunProcessAsync(
            "docker",
            arguments,
            cancellationToken);

        // Limpiar directorio temporal en el contenedor
        await CleanupContainerTempAsync(options.ContainerName!, tempPath, cancellationToken);

        if (exitCode != 0)
        {
            var message = $"Error al ejecutar mongorestore en el contenedor (código de salida: {exitCode})";
            _logger.LogError(message);
            _logger.LogError("Error: {Error}", error);

            return new RestoreResult
            {
                Success = false,
                Message = message,
                ExitCode = exitCode,
                Output = output,
                Error = error
            };
        }

        var successMessage = $"Restauración completada exitosamente para la base de datos '{options.Database}' en el contenedor '{options.ContainerName}'";
        _logger.LogInformation(successMessage);

        return new RestoreResult
        {
            Success = true,
            Message = successMessage,
            ExitCode = 0,
            Output = output,
            Error = error
        };
    }

    private async Task<RestoreResult> CopyBackupToContainerAsync(
        RestoreOptions options,
        string containerPath,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Copiando backup al contenedor...");

        // Validar que containerName no sea null (ya validado anteriormente)
        if (string.IsNullOrWhiteSpace(options.ContainerName))
        {
            throw new InvalidOperationException("El nombre del contenedor no puede ser nulo en este punto");
        }

        var copyArgs = $"cp {options.SourcePath}/. {options.ContainerName}:{containerPath}";
        var (exitCode, output, error) = await _processRunner.RunProcessAsync(
            "docker",
            copyArgs,
            cancellationToken);

        if (exitCode != 0)
        {
            var message = $"Error al copiar el backup al contenedor (código de salida: {exitCode})";
            _logger.LogError(message);
            _logger.LogError("Error: {Error}", error);

            return new RestoreResult
            {
                Success = false,
                Message = message,
                ExitCode = exitCode,
                Error = error
            };
        }

        _logger.LogInformation("Backup copiado exitosamente al contenedor");
        return new RestoreResult { Success = true };
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

    private string BuildMongoRestoreArguments(RestoreOptions options)
    {
        var args = new StringBuilder();

        // Opciones de drop
        if (options.Drop)
        {
            args.Append("--drop ");
        }

        // Base de datos
        args.Append($"--nsInclude=\"{options.Database}.*\"");

        // URI o Host/Port
        if (!string.IsNullOrWhiteSpace(options.Uri))
        {
            // lgtm[cs/cleartext-storage-of-sensitive-information]
            // La URI puede contener credenciales. Es una limitación de mongorestore que requiere
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
                    // mongorestore. En un entorno de producción, considere:
                    // 1. Usar autenticación basada en certificados o Kerberos
                    // 2. Usar variables de entorno MONGODB_PASSWORD (si mongorestore lo soporta)
                    // 3. Usar archivos de configuración con permisos restrictivos
                    // 4. Ejecutar mongorestore sin --password para que pida la contraseña interactivamente
                    args.Append($" --password \"{options.Password}\"");
                }

                args.Append($" --authenticationDatabase {options.AuthenticationDatabase}");
            }
        }

        // Ruta de origen del backup
        args.Append($" {options.SourcePath}");

        return args.ToString();
    }

    private string BuildDockerMongoRestoreArguments(RestoreOptions options, string tempPath)
    {
        var args = new StringBuilder();

        args.Append($"exec {options.ContainerName} mongorestore");

        // Opciones de drop
        if (options.Drop)
        {
            args.Append(" --drop");
        }

        // Base de datos
        args.Append($" --nsInclude=\"{options.Database}.*\"");

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
                // mongorestore. Dentro de un contenedor Docker, el riesgo es menor ya que el proceso
                // está aislado, pero en producción considere usar autenticación basada en certificados.
                args.Append($" --password \"{options.Password}\"");
            }

            args.Append($" --authenticationDatabase {options.AuthenticationDatabase}");
        }

        // Ruta de origen temporal en el contenedor
        args.Append($" {tempPath}");

        return args.ToString();
    }
}
