namespace MongoBackupRestore.Core.Interfaces;

/// <summary>
/// Servicio para cifrar y descifrar archivos de backup usando AES-256
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Cifra un archivo usando AES-256-CBC con HMAC-SHA256 para autenticación
    /// </summary>
    /// <param name="sourceFilePath">Ruta del archivo a cifrar</param>
    /// <param name="destinationFilePath">Ruta del archivo cifrado de destino (sin extensión, se añadirá .encrypted)</param>
    /// <param name="encryptionKey">Clave de cifrado (mínimo 16 caracteres)</param>
    /// <param name="onProgress">Callback para informar del progreso (opcional)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Ruta del archivo cifrado generado</returns>
    Task<string> EncryptFileAsync(
        string sourceFilePath,
        string destinationFilePath,
        string encryptionKey,
        Action<string>? onProgress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Descifra un archivo cifrado con AES-256-CBC y valida su integridad con HMAC-SHA256
    /// </summary>
    /// <param name="sourceFilePath">Ruta del archivo cifrado (.encrypted)</param>
    /// <param name="destinationFilePath">Ruta del archivo descifrado de destino</param>
    /// <param name="encryptionKey">Clave de descifrado</param>
    /// <param name="onProgress">Callback para informar del progreso (opcional)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>True si el descifrado fue exitoso y la integridad verificada</returns>
    Task<bool> DecryptFileAsync(
        string sourceFilePath,
        string destinationFilePath,
        string encryptionKey,
        Action<string>? onProgress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si un archivo está cifrado comprobando su extensión y encabezado
    /// </summary>
    /// <param name="filePath">Ruta del archivo a verificar</param>
    /// <returns>True si el archivo está cifrado</returns>
    bool IsEncrypted(string filePath);

    /// <summary>
    /// Valida que una clave de cifrado cumple con los requisitos mínimos de seguridad
    /// </summary>
    /// <param name="encryptionKey">Clave de cifrado a validar</param>
    /// <returns>Tupla con éxito y mensaje de error si falla</returns>
    (bool isValid, string? errorMessage) ValidateEncryptionKey(string? encryptionKey);
}
