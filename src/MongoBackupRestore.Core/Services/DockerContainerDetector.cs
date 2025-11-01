using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;

namespace MongoBackupRestore.Core.Services;

/// <summary>
/// Servicio para detectar y validar contenedores Docker con MongoDB
/// </summary>
public class DockerContainerDetector : IDockerContainerDetector
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<DockerContainerDetector> _logger;

    public DockerContainerDetector(IProcessRunner processRunner, ILogger<DockerContainerDetector> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<string>> DetectMongoContainersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detectando contenedores Docker con MongoDB...");

        var containers = new List<string>();

        try
        {
            // Buscar contenedores en ejecución que tengan MongoDB
            // Usamos filtros para buscar por imagen (mongo) o puerto expuesto (27017)
            var (exitCode, output, error) = await _processRunner.RunProcessAsync(
                "docker",
                "ps --format \"{{.Names}}\" --filter \"ancestor=mongo\"",
                cancellationToken);

            if (exitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var containerName = line.Trim();
                    if (!string.IsNullOrWhiteSpace(containerName))
                    {
                        containers.Add(containerName);
                    }
                }
            }

            // También buscar contenedores con puerto 27017 publicado
            var (exitCode2, output2, error2) = await _processRunner.RunProcessAsync(
                "docker",
                "ps --format \"{{.Names}}\" --filter \"publish=27017\"",
                cancellationToken);

            if (exitCode2 == 0 && !string.IsNullOrWhiteSpace(output2))
            {
                var lines = output2.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var containerName = line.Trim();
                    if (!string.IsNullOrWhiteSpace(containerName) && !containers.Contains(containerName))
                    {
                        // Validar que realmente tenga MongoDB usando una verificación más ligera
                        var (hasMongo, _) = await CheckMongodInContainerAsync(containerName, cancellationToken);
                        
                        if (hasMongo)
                        {
                            containers.Add(containerName);
                        }
                    }
                }
            }

            _logger.LogInformation("Se encontraron {Count} contenedor(es) con MongoDB: {Containers}",
                containers.Count, string.Join(", ", containers));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al detectar contenedores Docker");
        }

        return containers;
    }

    /// <inheritdoc />
    public async Task<(bool success, string? errorMessage)> ValidateContainerAsync(
        string containerName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            return (false, "El nombre del contenedor no puede estar vacío");
        }

        _logger.LogDebug("Validando contenedor: {ContainerName}", containerName);

        try
        {
            // Verificar que el contenedor existe y está en ejecución
            var (exitCode, output, error) = await _processRunner.RunProcessAsync(
                "docker",
                $"inspect --format=\"{{{{.State.Running}}}}\" {containerName}",
                cancellationToken);

            if (exitCode != 0)
            {
                return (false, $"El contenedor '{containerName}' no existe");
            }

            var isRunning = output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
            if (!isRunning)
            {
                return (false, $"El contenedor '{containerName}' existe pero no está en ejecución");
            }

            _logger.LogDebug("Contenedor {ContainerName} validado correctamente", containerName);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar contenedor {ContainerName}", containerName);
            return (false, $"Error al validar el contenedor: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<(bool success, string? errorMessage)> ValidateMongoBinariesInContainerAsync(
        string containerName,
        bool checkMongoDump = true,
        bool checkMongoRestore = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            return (false, "El nombre del contenedor no puede estar vacío");
        }

        _logger.LogDebug("Validando binarios de MongoDB en contenedor: {ContainerName}", containerName);

        try
        {
            // Primero validar que el contenedor existe y está en ejecución
            var (containerValid, containerError) = await ValidateContainerAsync(containerName, cancellationToken);
            if (!containerValid)
            {
                return (false, containerError);
            }

            // Validar mongodump si se solicita
            if (checkMongoDump)
            {
                var (hasMongoDump, mongoDumpError) = await CheckBinaryInContainerAsync(
                    containerName, "mongodump", cancellationToken);
                
                if (!hasMongoDump)
                {
                    return (false, mongoDumpError ?? $"mongodump no está disponible en el contenedor '{containerName}'");
                }
            }

            // Validar mongorestore si se solicita
            if (checkMongoRestore)
            {
                var (hasMongoRestore, mongoRestoreError) = await CheckBinaryInContainerAsync(
                    containerName, "mongorestore", cancellationToken);
                
                if (!hasMongoRestore)
                {
                    return (false, mongoRestoreError ?? $"mongorestore no está disponible en el contenedor '{containerName}'");
                }
            }

            // Si no se pidió verificar ningún binario, solo verificar que MongoDB existe
            if (!checkMongoDump && !checkMongoRestore)
            {
                var (hasMongod, _) = await CheckBinaryInContainerAsync(
                    containerName, "mongod", cancellationToken);
                
                if (!hasMongod)
                {
                    return (false, $"MongoDB no está disponible en el contenedor '{containerName}'");
                }
            }

            _logger.LogDebug("Binarios de MongoDB validados correctamente en contenedor {ContainerName}", containerName);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar binarios en contenedor {ContainerName}", containerName);
            return (false, $"Error al validar binarios en el contenedor: {ex.Message}");
        }
    }

    private async Task<(bool success, string? errorMessage)> CheckBinaryInContainerAsync(
        string containerName,
        string binaryName,
        CancellationToken cancellationToken)
    {
        try
        {
            // Usar 'which' o 'command -v' para verificar si el binario existe
            var (exitCode, output, error) = await _processRunner.RunProcessAsync(
                "docker",
                $"exec {containerName} sh -c \"command -v {binaryName}\"",
                cancellationToken);

            if (exitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                _logger.LogDebug("Binario {BinaryName} encontrado en contenedor {ContainerName}: {Path}",
                    binaryName, containerName, output.Trim());
                return (true, null);
            }

            _logger.LogDebug("Binario {BinaryName} no encontrado en contenedor {ContainerName}",
                binaryName, containerName);
            return (false, $"El binario '{binaryName}' no está disponible en el contenedor");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar binario {BinaryName} en contenedor {ContainerName}",
                binaryName, containerName);
            return (false, $"Error al verificar el binario: {ex.Message}");
        }
    }

    /// <summary>
    /// Verificación ligera de MongoDB en el contenedor (solo verifica mongod, no las herramientas)
    /// Usado durante la detección automática para evitar overhead
    /// </summary>
    private async Task<(bool success, string? errorMessage)> CheckMongodInContainerAsync(
        string containerName,
        CancellationToken cancellationToken)
    {
        try
        {
            // Solo verificar que mongod existe (más ligero que validar todo)
            var (exitCode, output, error) = await _processRunner.RunProcessAsync(
                "docker",
                $"exec {containerName} sh -c \"command -v mongod\"",
                cancellationToken);

            return (exitCode == 0 && !string.IsNullOrWhiteSpace(output), null);
        }
        catch
        {
            return (false, null);
        }
    }
}
