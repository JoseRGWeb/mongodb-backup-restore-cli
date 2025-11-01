using MongoBackupRestore.Core.Models;

namespace MongoBackupRestore.Core.Interfaces;

/// <summary>
/// Interfaz para ejecutar operaciones de backup de MongoDB
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Ejecuta un backup de MongoDB con las opciones especificadas
    /// </summary>
    /// <param name="options">Opciones de backup</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la operación de backup</returns>
    Task<BackupResult> ExecuteBackupAsync(BackupOptions options, CancellationToken cancellationToken = default);
}
