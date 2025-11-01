namespace MongoBackupRestore.Core.Models;

/// <summary>
/// Resultado de una operación de backup
/// </summary>
public class BackupResult
{
    /// <summary>
    /// Indica si el backup fue exitoso
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo del resultado
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Ruta del backup generado (si fue exitoso)
    /// </summary>
    public string? BackupPath { get; set; }

    /// <summary>
    /// Código de salida del proceso
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Salida estándar del proceso
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Error estándar del proceso
    /// </summary>
    public string? Error { get; set; }
}
