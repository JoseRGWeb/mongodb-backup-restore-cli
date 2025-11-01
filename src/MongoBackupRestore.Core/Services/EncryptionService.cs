using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;

namespace MongoBackupRestore.Core.Services;

/// <summary>
/// Servicio para cifrar y descifrar archivos de backup usando AES-256-CBC con HMAC-SHA256
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly ILogger<EncryptionService> _logger;
    private const int KeySizeBytes = 32; // AES-256 requiere 32 bytes (256 bits)
    private const int IVSizeBytes = 16; // AES usa bloques de 16 bytes
    private const int HmacSizeBytes = 32; // SHA-256 produce 32 bytes
    private const int BufferSize = 81920; // 80 KB buffer para lectura/escritura
    private const string EncryptedFileExtension = ".encrypted";
    private const string FileHeader = "MONGOBR-AES256"; // Encabezado para identificar archivos cifrados

    public EncryptionService(ILogger<EncryptionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> EncryptFileAsync(
        string sourceFilePath,
        string destinationFilePath,
        string encryptionKey,
        Action<string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        // Validar parámetros
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException($"El archivo de origen no existe: {sourceFilePath}");
        }

        var (isValid, errorMessage) = ValidateEncryptionKey(encryptionKey);
        if (!isValid)
        {
            throw new ArgumentException(errorMessage, nameof(encryptionKey));
        }

        var encryptedFilePath = destinationFilePath + EncryptedFileExtension;

        _logger.LogInformation("Iniciando cifrado de archivo: {SourceFile}", sourceFilePath);
        onProgress?.Invoke("Cifrando backup...");

        try
        {
            // Derivar clave y clave HMAC desde la clave proporcionada
            var (aesKey, hmacKey) = DeriveKeys(encryptionKey);

            // Generar IV aleatorio
            var iv = GenerateIV();

            using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);
            using var destinationStream = new FileStream(encryptedFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize);

            // Escribir encabezado del archivo
            await WriteFileHeaderAsync(destinationStream, cancellationToken);

            // Escribir IV
            await destinationStream.WriteAsync(iv, 0, iv.Length, cancellationToken);

            // Reservar espacio para HMAC (se escribirá al final)
            var hmacPosition = destinationStream.Position;
            await destinationStream.WriteAsync(new byte[HmacSizeBytes], 0, HmacSizeBytes, cancellationToken);

            // Usar MemoryStream para acumular datos cifrados y calcular HMAC
            using var encryptedDataBuffer = new MemoryStream();

            // Cifrar datos
            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySizeBytes * 8;
                aes.Key = aesKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                using var cryptoStream = new CryptoStream(encryptedDataBuffer, encryptor, CryptoStreamMode.Write, leaveOpen: true);

                var buffer = new byte[BufferSize];
                int bytesRead;
                long totalBytesRead = 0;
                var sourceFileInfo = new FileInfo(sourceFilePath);

                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await cryptoStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    totalBytesRead += bytesRead;

                    if (sourceFileInfo.Length > 0)
                    {
                        var progress = (totalBytesRead * 100.0) / sourceFileInfo.Length;
                        if (progress % 10 < 0.5) // Reportar cada 10%
                        {
                            onProgress?.Invoke($"Cifrando: {progress:F0}%");
                        }
                    }
                }

                // Finalizar cifrado (añade padding)
                cryptoStream.FlushFinalBlock();
            }

            // Calcular HMAC de los datos cifrados
            encryptedDataBuffer.Seek(0, SeekOrigin.Begin);
            var hmac = await ComputeHmacAsync(encryptedDataBuffer, hmacKey, cancellationToken);

            // Escribir HMAC en su posición reservada
            destinationStream.Seek(hmacPosition, SeekOrigin.Begin);
            await destinationStream.WriteAsync(hmac, 0, hmac.Length, cancellationToken);

            // Escribir datos cifrados después del HMAC
            destinationStream.Seek(0, SeekOrigin.End);
            encryptedDataBuffer.Seek(0, SeekOrigin.Begin);
            await encryptedDataBuffer.CopyToAsync(destinationStream, BufferSize, cancellationToken);

            // Limpiar claves de la memoria
            Array.Clear(aesKey, 0, aesKey.Length);
            Array.Clear(hmacKey, 0, hmacKey.Length);

            var fileInfo = new FileInfo(encryptedFilePath);
            var sizeInMb = fileInfo.Length / (1024.0 * 1024.0);
            _logger.LogInformation("Cifrado completado. Tamaño: {Size:F2} MB", sizeInMb);
            onProgress?.Invoke($"Cifrado completado. Tamaño: {sizeInMb:F2} MB");

            return encryptedFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el cifrado");
            
            // Limpiar archivo parcial en caso de error
            if (File.Exists(encryptedFilePath))
            {
                try { File.Delete(encryptedFilePath); } catch { /* Ignorar errores de limpieza */ }
            }
            
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DecryptFileAsync(
        string sourceFilePath,
        string destinationFilePath,
        string encryptionKey,
        Action<string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        // Validar parámetros
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException($"El archivo cifrado no existe: {sourceFilePath}");
        }

        var (isValid, errorMessage) = ValidateEncryptionKey(encryptionKey);
        if (!isValid)
        {
            throw new ArgumentException(errorMessage, nameof(encryptionKey));
        }

        _logger.LogInformation("Iniciando descifrado de archivo: {SourceFile}", sourceFilePath);
        onProgress?.Invoke("Descifrando backup...");

        try
        {
            // Derivar claves
            var (aesKey, hmacKey) = DeriveKeys(encryptionKey);

            using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);

            // Leer y validar encabezado
            if (!await ValidateFileHeaderAsync(sourceStream, cancellationToken))
            {
                throw new InvalidOperationException("El archivo no es un backup cifrado válido");
            }

            // Leer IV
            var iv = new byte[IVSizeBytes];
            await sourceStream.ReadAsync(iv, 0, iv.Length, cancellationToken);

            // Leer HMAC almacenado
            var storedHmac = new byte[HmacSizeBytes];
            await sourceStream.ReadAsync(storedHmac, 0, storedHmac.Length, cancellationToken);

            // Guardar posición de inicio de datos cifrados
            var encryptedDataStartPosition = sourceStream.Position;

            // Verificar HMAC
            sourceStream.Seek(encryptedDataStartPosition, SeekOrigin.Begin);
            var computedHmac = await ComputeHmacAsync(sourceStream, hmacKey, cancellationToken);

            if (!CompareBytes(storedHmac, computedHmac))
            {
                _logger.LogError("La validación HMAC falló. La clave de cifrado puede ser incorrecta o el archivo está corrupto");
                throw new CryptographicException("Error al descifrar: clave de cifrado incorrecta o archivo corrupto");
            }

            // Volver al inicio de los datos cifrados para descifrar
            sourceStream.Seek(encryptedDataStartPosition, SeekOrigin.Begin);

            using var destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize);

            // Descifrar datos
            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySizeBytes * 8;
                aes.Key = aesKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                using var cryptoStream = new CryptoStream(sourceStream, decryptor, CryptoStreamMode.Read, leaveOpen: true);

                var buffer = new byte[BufferSize];
                int bytesRead;
                long totalBytesRead = 0;
                var sourceFileInfo = new FileInfo(sourceFilePath);

                while ((bytesRead = await cryptoStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await destinationStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    totalBytesRead += bytesRead;

                    if (sourceFileInfo.Length > 0)
                    {
                        var progress = (totalBytesRead * 100.0) / sourceFileInfo.Length;
                        if (progress % 10 < 0.5) // Reportar cada 10%
                        {
                            onProgress?.Invoke($"Descifrando: {progress:F0}%");
                        }
                    }
                }
            }

            // Limpiar claves de la memoria
            Array.Clear(aesKey, 0, aesKey.Length);
            Array.Clear(hmacKey, 0, hmacKey.Length);

            var fileInfo = new FileInfo(destinationFilePath);
            var sizeInMb = fileInfo.Length / (1024.0 * 1024.0);
            _logger.LogInformation("Descifrado completado. Tamaño: {Size:F2} MB", sizeInMb);
            onProgress?.Invoke($"Descifrado completado. Tamaño: {sizeInMb:F2} MB");

            return true;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Error criptográfico durante el descifrado");
            
            // Limpiar archivo parcial en caso de error
            if (File.Exists(destinationFilePath))
            {
                try { File.Delete(destinationFilePath); } catch { /* Ignorar errores de limpieza */ }
            }
            
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el descifrado");
            
            // Limpiar archivo parcial en caso de error
            if (File.Exists(destinationFilePath))
            {
                try { File.Delete(destinationFilePath); } catch { /* Ignorar errores de limpieza */ }
            }
            
            throw;
        }
    }

    /// <inheritdoc />
    public bool IsEncrypted(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        // Verificar por extensión
        if (!filePath.EndsWith(EncryptedFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Verificar encabezado del archivo
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var headerBytes = Encoding.ASCII.GetBytes(FileHeader);
            var fileHeaderBytes = new byte[headerBytes.Length];
            
            if (stream.Read(fileHeaderBytes, 0, fileHeaderBytes.Length) != headerBytes.Length)
            {
                return false;
            }

            return CompareBytes(headerBytes, fileHeaderBytes);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public (bool isValid, string? errorMessage) ValidateEncryptionKey(string? encryptionKey)
    {
        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            return (false, "La clave de cifrado no puede estar vacía");
        }

        if (encryptionKey.Length < 16)
        {
            return (false, "La clave de cifrado debe tener al menos 16 caracteres");
        }

        return (true, null);
    }

    /// <summary>
    /// Deriva una clave AES y una clave HMAC desde la clave proporcionada por el usuario usando PBKDF2
    /// </summary>
    private (byte[] aesKey, byte[] hmacKey) DeriveKeys(string password)
    {
        // Usar un salt fijo para asegurar que la misma contraseña produzca las mismas claves
        // En un escenario de producción real, podríamos usar un salt aleatorio almacenado con el archivo
        // Pero para simplicidad y compatibilidad, usamos un salt derivado del password
        var saltBytes = Encoding.UTF8.GetBytes("MongoBackupRestore.Salt." + password.Substring(0, Math.Min(8, password.Length)));

        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
        
        var aesKey = pbkdf2.GetBytes(KeySizeBytes);
        var hmacKey = pbkdf2.GetBytes(KeySizeBytes);

        return (aesKey, hmacKey);
    }

    /// <summary>
    /// Genera un vector de inicialización (IV) aleatorio
    /// </summary>
    private byte[] GenerateIV()
    {
        var iv = new byte[IVSizeBytes];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(iv);
        return iv;
    }

    /// <summary>
    /// Escribe el encabezado del archivo cifrado
    /// </summary>
    private async Task WriteFileHeaderAsync(Stream stream, CancellationToken cancellationToken)
    {
        var headerBytes = Encoding.ASCII.GetBytes(FileHeader);
        await stream.WriteAsync(headerBytes, 0, headerBytes.Length, cancellationToken);
    }

    /// <summary>
    /// Valida el encabezado del archivo cifrado
    /// </summary>
    private async Task<bool> ValidateFileHeaderAsync(Stream stream, CancellationToken cancellationToken)
    {
        var expectedHeaderBytes = Encoding.ASCII.GetBytes(FileHeader);
        var actualHeaderBytes = new byte[expectedHeaderBytes.Length];
        
        var bytesRead = await stream.ReadAsync(actualHeaderBytes, 0, actualHeaderBytes.Length, cancellationToken);
        
        if (bytesRead != expectedHeaderBytes.Length)
        {
            return false;
        }

        return CompareBytes(expectedHeaderBytes, actualHeaderBytes);
    }

    /// <summary>
    /// Calcula el HMAC-SHA256 de un stream
    /// </summary>
    private async Task<byte[]> ComputeHmacAsync(Stream stream, byte[] key, CancellationToken cancellationToken)
    {
        using var hmac = new HMACSHA256(key);
        
        var buffer = new byte[BufferSize];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            hmac.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return hmac.Hash ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Compara dos arrays de bytes en tiempo constante para evitar timing attacks
    /// </summary>
    private bool CompareBytes(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}
