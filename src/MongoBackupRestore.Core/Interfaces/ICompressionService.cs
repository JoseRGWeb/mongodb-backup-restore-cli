using MongoBackupRestore.Core.Models;

namespace MongoBackupRestore.Core.Interfaces;

/// <summary>
/// Servicio para comprimir y descomprimir archivos de backup
/// </summary>
public interface ICompressionService
{
    /// <summary>
    /// Comprime un directorio en un archivo ZIP o TAR.GZ
    /// </summary>
    /// <param name="sourceDirectory">Directorio de origen a comprimir</param>
    /// <param name="destinationFile">Archivo de destino (sin extensión, se añadirá automáticamente)</param>
    /// <param name="format">Formato de compresión</param>
    /// <param name="onProgress">Callback para informar del progreso (opcional)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Ruta del archivo comprimido generado</returns>
    Task<string> CompressAsync(
        string sourceDirectory,
        string destinationFile,
        CompressionFormat format,
        Action<string>? onProgress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Descomprime un archivo ZIP o TAR.GZ en un directorio
    /// </summary>
    /// <param name="sourceFile">Archivo comprimido de origen</param>
    /// <param name="destinationDirectory">Directorio de destino</param>
    /// <param name="onProgress">Callback para informar del progreso (opcional)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>True si fue exitoso, False en caso contrario</returns>
    Task<bool> DecompressAsync(
        string sourceFile,
        string destinationDirectory,
        Action<string>? onProgress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detecta el formato de compresión de un archivo
    /// </summary>
    /// <param name="filePath">Ruta del archivo</param>
    /// <returns>Formato de compresión detectado</returns>
    CompressionFormat DetectFormat(string filePath);

    /// <summary>
    /// Obtiene la extensión de archivo para un formato de compresión
    /// </summary>
    /// <param name="format">Formato de compresión</param>
    /// <returns>Extensión del archivo (ej: ".zip", ".tar.gz")</returns>
    string GetFileExtension(CompressionFormat format);
}
