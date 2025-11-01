namespace MongoBackupRestore.Core.Models;

/// <summary>
/// Resultado de una operación de restauración
/// </summary>
public class RestoreResult
{
    /// <summary>
    /// Indica si la restauración fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo del resultado
    /// </summary>
    public string Message { get; set; } = string.Empty;

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

    /// <summary>
    /// Ruta del directorio descomprimido temporal (usado internamente)
    /// </summary>
    public string? DecompressedPath { get; set; }

    /// <summary>
    /// Ruta del archivo descifrado temporal (usado internamente)
    /// </summary>
    public string? DecryptedPath { get; set; }
}
