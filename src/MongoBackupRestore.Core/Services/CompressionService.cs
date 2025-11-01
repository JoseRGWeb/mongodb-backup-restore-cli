using System.Formats.Tar;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;
using MongoBackupRestore.Core.Models;

namespace MongoBackupRestore.Core.Services;

/// <summary>
/// Servicio para comprimir y descomprimir archivos de backup
/// </summary>
public class CompressionService : ICompressionService
{
    private readonly ILogger<CompressionService> _logger;
    private readonly IProcessRunner _processRunner;

    public CompressionService(ILogger<CompressionService> logger, IProcessRunner processRunner)
    {
        _logger = logger;
        _processRunner = processRunner;
    }

    /// <inheritdoc />
    public async Task<string> CompressAsync(
        string sourceDirectory,
        string destinationFile,
        CompressionFormat format,
        Action<string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        if (format == CompressionFormat.None)
        {
            throw new ArgumentException("El formato de compresión no puede ser None", nameof(format));
        }

        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"El directorio de origen no existe: {sourceDirectory}");
        }

        var extension = GetFileExtension(format);
        var compressedFile = destinationFile + extension;

        _logger.LogInformation("Iniciando compresión en formato {Format}...", format);
        onProgress?.Invoke($"Comprimiendo backup en formato {format}...");

        try
        {
            switch (format)
            {
                case CompressionFormat.Zip:
                    await CompressZipAsync(sourceDirectory, compressedFile, onProgress, cancellationToken);
                    break;

                case CompressionFormat.TarGz:
                    await CompressTarGzAsync(sourceDirectory, compressedFile, onProgress, cancellationToken);
                    break;

                default:
                    throw new NotSupportedException($"Formato de compresión no soportado: {format}");
            }

            var fileInfo = new FileInfo(compressedFile);
            var sizeInMb = fileInfo.Length / (1024.0 * 1024.0);
            _logger.LogInformation("Compresión completada. Tamaño: {Size:F2} MB", sizeInMb);
            onProgress?.Invoke($"Compresión completada. Tamaño: {sizeInMb:F2} MB");

            return compressedFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la compresión");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DecompressAsync(
        string sourceFile,
        string destinationDirectory,
        Action<string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceFile))
        {
            throw new FileNotFoundException($"El archivo de origen no existe: {sourceFile}");
        }

        var format = DetectFormat(sourceFile);
        if (format == CompressionFormat.None)
        {
            throw new InvalidOperationException($"No se pudo detectar el formato de compresión del archivo: {sourceFile}");
        }

        _logger.LogInformation("Iniciando descompresión de formato {Format}...", format);
        onProgress?.Invoke($"Descomprimiendo backup en formato {format}...");

        try
        {
            Directory.CreateDirectory(destinationDirectory);

            switch (format)
            {
                case CompressionFormat.Zip:
                    await DecompressZipAsync(sourceFile, destinationDirectory, onProgress, cancellationToken);
                    break;

                case CompressionFormat.TarGz:
                    await DecompressTarGzAsync(sourceFile, destinationDirectory, onProgress, cancellationToken);
                    break;

                default:
                    throw new NotSupportedException($"Formato de compresión no soportado: {format}");
            }

            _logger.LogInformation("Descompresión completada");
            onProgress?.Invoke("Descompresión completada");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la descompresión");
            return false;
        }
    }

    /// <inheritdoc />
    public CompressionFormat DetectFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        // Verificar extensiones compuestas como .tar.gz
        if (filePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
        {
            return CompressionFormat.TarGz;
        }

        return extension switch
        {
            ".zip" => CompressionFormat.Zip,
            ".gz" => CompressionFormat.TarGz,
            _ => CompressionFormat.None
        };
    }

    /// <inheritdoc />
    public string GetFileExtension(CompressionFormat format)
    {
        return format switch
        {
            CompressionFormat.Zip => ".zip",
            CompressionFormat.TarGz => ".tar.gz",
            CompressionFormat.None => string.Empty,
            _ => throw new ArgumentException($"Formato no soportado: {format}", nameof(format))
        };
    }

    private async Task CompressZipAsync(
        string sourceDirectory,
        string destinationFile,
        Action<string>? onProgress,
        CancellationToken cancellationToken)
    {
        onProgress?.Invoke("Creando archivo ZIP...");

        await Task.Run(() =>
        {
            // Eliminar archivo de destino si existe
            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }

            ZipFile.CreateFromDirectory(
                sourceDirectory,
                destinationFile,
                CompressionLevel.Optimal,
                includeBaseDirectory: false);
        }, cancellationToken);

        onProgress?.Invoke("Archivo ZIP creado exitosamente");
    }

    private async Task CompressTarGzAsync(
        string sourceDirectory,
        string destinationFile,
        Action<string>? onProgress,
        CancellationToken cancellationToken)
    {
        onProgress?.Invoke("Creando archivo TAR.GZ...");

        await Task.Run(async () =>
        {
            // Eliminar archivo de destino si existe
            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }

            // Crear archivo TAR temporal
            var tarFile = Path.ChangeExtension(destinationFile, ".tar");
            
            try
            {
                // Crear archivo TAR
                await using (var tarStream = File.Create(tarFile))
                {
                    await TarFile.CreateFromDirectoryAsync(
                        sourceDirectory,
                        tarStream,
                        includeBaseDirectory: false,
                        cancellationToken);
                }

                // Comprimir con GZIP
                await using (var sourceStream = File.OpenRead(tarFile))
                await using (var destinationStream = File.Create(destinationFile))
                await using (var gzipStream = new GZipStream(destinationStream, CompressionLevel.Optimal))
                {
                    await sourceStream.CopyToAsync(gzipStream, cancellationToken);
                }
            }
            finally
            {
                // Eliminar archivo TAR temporal
                if (File.Exists(tarFile))
                {
                    File.Delete(tarFile);
                }
            }
        }, cancellationToken);

        onProgress?.Invoke("Archivo TAR.GZ creado exitosamente");
    }

    private async Task DecompressZipAsync(
        string sourceFile,
        string destinationDirectory,
        Action<string>? onProgress,
        CancellationToken cancellationToken)
    {
        onProgress?.Invoke("Extrayendo archivo ZIP...");

        await Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(sourceFile, destinationDirectory, overwriteFiles: true);
        }, cancellationToken);

        onProgress?.Invoke("Archivo ZIP extraído exitosamente");
    }

    private async Task DecompressTarGzAsync(
        string sourceFile,
        string destinationDirectory,
        Action<string>? onProgress,
        CancellationToken cancellationToken)
    {
        onProgress?.Invoke("Extrayendo archivo TAR.GZ...");

        await Task.Run(async () =>
        {
            // Descomprimir GZIP primero a un archivo TAR temporal
            var tarFile = Path.Combine(Path.GetTempPath(), $"temp_{Guid.NewGuid()}.tar");

            try
            {
                // Descomprimir GZIP
                await using (var sourceStream = File.OpenRead(sourceFile))
                await using (var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                await using (var tarStream = File.Create(tarFile))
                {
                    await gzipStream.CopyToAsync(tarStream, cancellationToken);
                }

                // Extraer TAR
                await using (var tarStream = File.OpenRead(tarFile))
                {
                    await TarFile.ExtractToDirectoryAsync(tarStream, destinationDirectory, overwriteFiles: true, cancellationToken);
                }
            }
            finally
            {
                // Eliminar archivo TAR temporal
                if (File.Exists(tarFile))
                {
                    File.Delete(tarFile);
                }
            }
        }, cancellationToken);

        onProgress?.Invoke("Archivo TAR.GZ extraído exitosamente");
    }
}
