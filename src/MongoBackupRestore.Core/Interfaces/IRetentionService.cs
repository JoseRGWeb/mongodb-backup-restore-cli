using MongoBackupRestore.Core.Models;

namespace MongoBackupRestore.Core.Interfaces;

/// <summary>
/// Interfaz para el servicio de retención y limpieza de backups
/// </summary>
public interface IRetentionService
{
    /// <summary>
    /// Ejecuta la limpieza de backups antiguos según la política de retención
    /// </summary>
    /// <param name="policy">Política de retención a aplicar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la operación de limpieza</returns>
    Task<RetentionResult> CleanupOldBackupsAsync(RetentionPolicy policy, CancellationToken cancellationToken = default);
}
