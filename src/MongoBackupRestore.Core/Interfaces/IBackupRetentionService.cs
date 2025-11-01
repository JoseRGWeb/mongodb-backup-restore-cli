using MongoBackupRestore.Core.Models;

namespace MongoBackupRestore.Core.Interfaces;

/// <summary>
/// Servicio para gestionar la retención y limpieza automática de backups
/// </summary>
public interface IBackupRetentionService
{
    /// <summary>
    /// Limpia backups antiguos según la política de retención
    /// </summary>
    /// <param name="backupDirectory">Directorio que contiene los backups</param>
    /// <param name="retentionDays">Número de días a retener los backups</param>
    /// <param name="dryRun">Si es true, solo simula la limpieza sin eliminar archivos</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la operación de limpieza</returns>
    Task<RetentionCleanupResult> CleanupOldBackupsAsync(
        string backupDirectory,
        int retentionDays,
        bool dryRun = false,
        CancellationToken cancellationToken = default);
}
