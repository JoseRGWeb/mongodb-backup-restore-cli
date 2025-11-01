namespace MongoBackupRestore.Core.Models;

/// <summary>
/// Resultado de la operación de limpieza de backups
/// </summary>
public class RetentionResult
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo del resultado
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Número de archivos/directorios eliminados
    /// </summary>
    public int DeletedCount { get; set; }

    /// <summary>
    /// Tamaño total liberado en bytes
    /// </summary>
    public long BytesFreed { get; set; }

    /// <summary>
    /// Lista de rutas de backups eliminados
    /// </summary>
    public List<string> DeletedPaths { get; set; } = new();

    /// <summary>
    /// Error si la operación falló
    /// </summary>
    public string? Error { get; set; }
}
