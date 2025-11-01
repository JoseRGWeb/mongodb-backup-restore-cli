namespace MongoBackupRestore.Core.Interfaces;

/// <summary>
/// Interfaz para detectar y validar contenedores Docker con MongoDB
/// </summary>
public interface IDockerContainerDetector
{
    /// <summary>
    /// Detecta contenedores Docker que ejecutan MongoDB
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de nombres de contenedores Docker con MongoDB</returns>
    Task<List<string>> DetectMongoContainersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida que un contenedor existe y está en ejecución
    /// </summary>
    /// <param name="containerName">Nombre del contenedor</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Tupla con (éxito, mensaje de error)</returns>
    Task<(bool success, string? errorMessage)> ValidateContainerAsync(
        string containerName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida que los binarios de MongoDB existen dentro del contenedor
    /// </summary>
    /// <param name="containerName">Nombre del contenedor</param>
    /// <param name="checkMongoDump">Si se debe verificar mongodump</param>
    /// <param name="checkMongoRestore">Si se debe verificar mongorestore</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Tupla con (éxito, mensaje de error)</returns>
    Task<(bool success, string? errorMessage)> ValidateMongoBinariesInContainerAsync(
        string containerName,
        bool checkMongoDump = true,
        bool checkMongoRestore = true,
        CancellationToken cancellationToken = default);
}
