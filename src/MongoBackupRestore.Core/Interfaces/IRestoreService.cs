using MongoBackupRestore.Core.Models;

namespace MongoBackupRestore.Core.Interfaces;

/// <summary>
/// Interfaz para ejecutar operaciones de restauración de MongoDB
/// </summary>
public interface IRestoreService
{
    /// <summary>
    /// Ejecuta una restauración de MongoDB con las opciones especificadas
    /// </summary>
    /// <param name="options">Opciones de restauración</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la operación de restauración</returns>
    Task<RestoreResult> ExecuteRestoreAsync(RestoreOptions options, CancellationToken cancellationToken = default);
}
