namespace MongoBackupRestore.Core.Models;

/// <summary>
/// Política de retención para backups antiguos
/// </summary>
public class RetentionPolicy
{
    /// <summary>
    /// Número de días que se deben conservar los backups.
    /// Los backups más antiguos que este período serán eliminados.
    /// Si es null o 0, no se aplicará limpieza automática.
    /// </summary>
    public int? RetentionDays { get; set; }

    /// <summary>
    /// Ruta del directorio base donde se buscarán backups para limpieza
    /// </summary>
    public string? BackupDirectory { get; set; }

    /// <summary>
    /// Indica si la política de retención está habilitada
    /// </summary>
    public bool IsEnabled => RetentionDays.HasValue && RetentionDays.Value > 0;
}
