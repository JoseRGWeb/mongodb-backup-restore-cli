using MongoBackupRestore.Core.Models;

namespace MongoBackupRestore.Core.Interfaces;

/// <summary>
/// Interfaz para validar las herramientas de MongoDB
/// </summary>
public interface IMongoToolsValidator
{
    /// <summary>
    /// Valida la disponibilidad de las herramientas de MongoDB
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Información sobre las herramientas disponibles</returns>
    Task<MongoToolsInfo> ValidateToolsAsync(CancellationToken cancellationToken = default);
}
