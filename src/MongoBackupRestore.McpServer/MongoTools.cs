using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MongoBackupRestore.Core.Interfaces;
using MongoBackupRestore.Core.Models;

namespace MongoBackupRestore.McpServer;

/// <summary>
/// Herramientas MCP para backup y restore de MongoDB
/// </summary>
[McpServerToolType]
public class MongoTools
{
    private readonly IBackupService _backupService;
    private readonly IRestoreService _restoreService;
    private readonly ILogger<MongoTools> _logger;

    public MongoTools(IBackupService backupService, IRestoreService restoreService, ILogger<MongoTools> logger)
    {
        _backupService = backupService;
        _restoreService = restoreService;
        _logger = logger;
    }

    /// <summary>
    /// Realiza una copia de seguridad de una base de datos MongoDB
    /// </summary>
    [McpServerTool(Name = "mongodb_backup"), Description("Realiza una copia de seguridad (backup) de una base de datos MongoDB. Soporta instancias locales, contenedores Docker y conexiones remotas. Permite compresión ZIP/TAR.GZ, cifrado AES-256 y retención automática de backups.")]
    public async Task<string> BackupAsync(
        [Required]
        [Description("Nombre de la base de datos a respaldar")] string database,
        [Required]
        [Description("Ruta de destino para el backup")] string outputPath,
        [Required]
        [Description("Usuario para autenticación")] string username,
        [Required]
        [Description("Contraseña para autenticación")] string password,
        [Description("Host de MongoDB (por defecto: localhost)")] string host = "localhost",
        [Description("Puerto de MongoDB (por defecto: 27017)")] int port = 27017,
        [Description("URI de conexión completa (alternativa a host/port/user/password)")] string? uri = null,
        [Description("Base de datos de autenticación (por defecto: admin)")] string authDb = "admin",
        [Description("Ejecutar dentro de un contenedor Docker")] bool inDocker = false,
        [Description("Nombre del contenedor Docker (si no se especifica, se detecta automáticamente)")] string? containerName = null,
        [Description("Formato de compresión: none, zip o targz")] string compress = "none",
        [Description("Número de días para retener backups (null para no aplicar retención)")] int? retentionDays = null,
        [Description("Cifrar el backup usando AES-256")] bool encrypt = false,
        [Description("Clave de cifrado AES-256 (mínimo 16 caracteres)")] string? encryptionKey = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MCP: Iniciando backup de base de datos '{Database}'", database);

        var startTime = DateTime.Now;
        var compressionFormat = ParseCompressionFormat(compress);

        var options = new BackupOptions
        {
            Database = database,
            OutputPath = outputPath,
            Host = host,
            Port = port,
            Uri = uri,
            Username = username,
            Password = password,
            AuthenticationDatabase = authDb,
            InDocker = inDocker,
            ContainerName = containerName,
            CompressionFormat = compressionFormat,
            RetentionDays = retentionDays,
            Encrypt = encrypt,
            EncryptionKey = encryptionKey
        };

        var result = await _backupService.ExecuteBackupAsync(options, cancellationToken);
        var duration = DateTime.Now - startTime;

        if (result.Success)
        {
            var backupPath = result.BackupPath ?? outputPath;
            var backupSize = GetDirectoryOrFileSize(backupPath);

            var response = $"✅ Backup completado exitosamente\n";
            response += $"\n📋 Resumen de la operación:\n";
            response += $"  • Base de datos: {database}\n";
            response += $"  • Servidor: {(uri ?? $"{host}:{port}")}\n";
            response += $"  • Ruta del backup: {backupPath}\n";
            response += $"  • Tamaño del backup: {FormatSize(backupSize)}\n";
            response += $"  • Compresión: {(compressionFormat == CompressionFormat.None ? "Sin compresión" : compress.ToUpperInvariant())}\n";
            response += $"  • Cifrado: {(encrypt ? "AES-256" : "No")}\n";
            if (retentionDays.HasValue)
                response += $"  • Retención: {retentionDays} días\n";
            response += $"  • Duración: {FormatDuration(duration)}\n";
            response += $"  • Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            return response;
        }

        var errorMessage = $"❌ Error en el backup de '{database}'\n";
        errorMessage += $"  • Servidor: {(uri ?? $"{host}:{port}")}\n";
        errorMessage += $"  • Mensaje: {result.Message}";
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            errorMessage += $"\n  • Detalle: {result.Error}";
        }
        errorMessage += $"\n  • Duración: {FormatDuration(duration)}";
        throw new InvalidOperationException(errorMessage);
    }

    /// <summary>
    /// Restaura una base de datos MongoDB desde un backup
    /// </summary>
    [McpServerTool(Name = "mongodb_restore"), Description("Restaura una base de datos MongoDB desde un backup existente. Soporta backups comprimidos (ZIP/TAR.GZ) y cifrados con AES-256. Compatible con instancias locales, contenedores Docker y conexiones remotas.")]
    public async Task<string> RestoreAsync(
        [Required]
        [Description("Nombre de la base de datos a restaurar")] string database,
        [Required]
        [Description("Ruta de origen del backup a restaurar")] string sourcePath,
        [Required]
        [Description("Usuario para autenticación")] string username,
        [Required]
        [Description("Contraseña para autenticación")] string password,
        [Description("Host de MongoDB (por defecto: localhost)")] string host = "localhost",
        [Description("Puerto de MongoDB (por defecto: 27017)")] int port = 27017,
        [Description("URI de conexión completa (alternativa a host/port/user/password)")] string? uri = null,
        [Description("Base de datos de autenticación (por defecto: admin)")] string authDb = "admin",
        [Description("Ejecutar dentro de un contenedor Docker")] bool inDocker = false,
        [Description("Nombre del contenedor Docker (si no se especifica, se detecta automáticamente)")] string? containerName = null,
        [Description("Eliminar la base de datos antes de restaurar")] bool drop = false,
        [Description("Formato de compresión del backup: none, zip o targz (se auto-detecta si no se especifica)")] string compress = "none",
        [Description("Clave de cifrado para descifrar el backup (si está cifrado)")] string? encryptionKey = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MCP: Iniciando restore de base de datos '{Database}' desde '{SourcePath}'", database, sourcePath);

        var compressionFormat = ParseCompressionFormat(compress);

        var options = new RestoreOptions
        {
            Database = database,
            SourcePath = sourcePath,
            Host = host,
            Port = port,
            Uri = uri,
            Username = username,
            Password = password,
            AuthenticationDatabase = authDb,
            InDocker = inDocker,
            ContainerName = containerName,
            Drop = drop,
            CompressionFormat = compressionFormat,
            EncryptionKey = encryptionKey
        };

        var startTime = DateTime.Now;
        var result = await _restoreService.ExecuteRestoreAsync(options, cancellationToken);
        var duration = DateTime.Now - startTime;

        if (result.Success)
        {
            var response = $"✅ Restauración completada exitosamente\n";
            response += $"\n📋 Resumen de la operación:\n";
            response += $"  • Base de datos: {database}\n";
            response += $"  • Servidor: {(uri ?? $"{host}:{port}")}\n";
            response += $"  • Origen: {sourcePath}\n";
            response += $"  • Compresión: {(compressionFormat == CompressionFormat.None ? "Sin compresión" : compress.ToUpperInvariant())}\n";
            response += $"  • Drop previo: {(drop ? "Sí" : "No")}\n";
            response += $"  • Duración: {FormatDuration(duration)}\n";
            response += $"  • Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            return response;
        }

        var errorMessage = $"❌ Error en la restauración de '{database}'\n";
        errorMessage += $"  • Servidor: {(uri ?? $"{host}:{port}")}\n";
        errorMessage += $"  • Origen: {sourcePath}\n";
        errorMessage += $"  • Mensaje: {result.Message}";
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            errorMessage += $"\n  • Detalle: {result.Error}";
        }
        errorMessage += $"\n  • Duración: {FormatDuration(duration)}";
        throw new InvalidOperationException(errorMessage);
    }

    private static CompressionFormat ParseCompressionFormat(string compress) =>
        compress.ToLowerInvariant() switch
        {
            "zip" => CompressionFormat.Zip,
            "targz" or "tar.gz" or "tgz" => CompressionFormat.TarGz,
            _ => CompressionFormat.None
        };

    /// <summary>
    /// Obtiene el tamaño de un directorio o archivo en bytes
    /// </summary>
    private static long GetDirectoryOrFileSize(string path)
    {
        try
        {
            if (File.Exists(path))
                return new FileInfo(path).Length;

            if (Directory.Exists(path))
                return new DirectoryInfo(path)
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(f => f.Length);
        }
        catch { }
        return 0;
    }

    /// <summary>
    /// Formatea un tamaño en bytes a una representación legible
    /// </summary>
    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "desconocido";
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:F2} {sizes[order]}";
    }

    /// <summary>
    /// Formatea una duración a representación legible
    /// </summary>
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 60)
            return $"{duration.TotalSeconds:F1} segundos";
        if (duration.TotalMinutes < 60)
            return $"{duration.Minutes}m {duration.Seconds}s";
        return $"{duration.Hours}h {duration.Minutes}m {duration.Seconds}s";
    }
}
