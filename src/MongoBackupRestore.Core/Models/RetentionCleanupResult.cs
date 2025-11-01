namespace MongoBackupRestore.Core.Models;

/// <summary>
/// Resultado de una operación de limpieza de backups por retención
/// </summary>
public class RetentionCleanupResult
{
    /// <summary>
    /// Indica si la limpieza fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo del resultado
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Número total de backups encontrados
    /// </summary>
    public int TotalBackupsFound { get; set; }

    /// <summary>
    /// Número de backups eliminados
    /// </summary>
    public int BackupsDeleted { get; set; }

    /// <summary>
    /// Número de backups retenidos
    /// </summary>
    public int BackupsRetained { get; set; }

    /// <summary>
    /// Lista de archivos/directorios eliminados
    /// </summary>
    public List<string> DeletedItems { get; set; } = new();

    /// <summary>
    /// Lista de archivos/directorios retenidos
    /// </summary>
    public List<string> RetainedItems { get; set; } = new();

    /// <summary>
    /// Espacio liberado en bytes
    /// </summary>
    public long SpaceFreedBytes { get; set; }

    /// <summary>
    /// Errores ocurridos durante la limpieza
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
