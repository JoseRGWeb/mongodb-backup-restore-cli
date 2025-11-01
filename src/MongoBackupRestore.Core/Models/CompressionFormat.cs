namespace MongoBackupRestore.Core.Models;

/// <summary>
/// Formato de compresión para backups
/// </summary>
public enum CompressionFormat
{
    /// <summary>
    /// Sin compresión
    /// </summary>
    None,
    
    /// <summary>
    /// Formato ZIP
    /// </summary>
    Zip,
    
    /// <summary>
    /// Formato TAR.GZ (gzip)
    /// </summary>
    TarGz
}
