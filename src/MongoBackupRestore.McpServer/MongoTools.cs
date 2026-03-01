using System.ComponentModel;
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
        [Description("Nombre de la base de datos a respaldar")] string database,
        [Description("Ruta de destino para el backup")] string outputPath,
        [Description("Host de MongoDB (por defecto: localhost)")] string host = "localhost",
        [Description("Puerto de MongoDB (por defecto: 27017)")] int port = 27017,
        [Description("URI de conexión completa (alternativa a host/port/user/password)")] string? uri = null,
        [Description("Usuario para autenticación (opcional)")] string? username = null,
        [Description("Contraseña para autenticación (opcional)")] string? password = null,
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

        if (result.Success)
        {
            var response = result.Message;
            if (!string.IsNullOrWhiteSpace(result.BackupPath))
            {
                response += $"\nRuta del backup: {result.BackupPath}";
            }
            return response;
        }

        var errorMessage = result.Message;
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            errorMessage += $"\nError: {result.Error}";
        }
        throw new InvalidOperationException(errorMessage);
    }

    /// <summary>
    /// Restaura una base de datos MongoDB desde un backup
    /// </summary>
    [McpServerTool(Name = "mongodb_restore"), Description("Restaura una base de datos MongoDB desde un backup existente. Soporta backups comprimidos (ZIP/TAR.GZ) y cifrados con AES-256. Compatible con instancias locales, contenedores Docker y conexiones remotas.")]
    public async Task<string> RestoreAsync(
        [Description("Nombre de la base de datos a restaurar")] string database,
        [Description("Ruta de origen del backup a restaurar")] string sourcePath,
        [Description("Host de MongoDB (por defecto: localhost)")] string host = "localhost",
        [Description("Puerto de MongoDB (por defecto: 27017)")] int port = 27017,
        [Description("URI de conexión completa (alternativa a host/port/user/password)")] string? uri = null,
        [Description("Usuario para autenticación (opcional)")] string? username = null,
        [Description("Contraseña para autenticación (opcional)")] string? password = null,
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

        var result = await _restoreService.ExecuteRestoreAsync(options, cancellationToken);

        if (result.Success)
        {
            return result.Message;
        }

        var errorMessage = result.Message;
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            errorMessage += $"\nError: {result.Error}";
        }
        throw new InvalidOperationException(errorMessage);
    }

    private static CompressionFormat ParseCompressionFormat(string compress) =>
        compress.ToLowerInvariant() switch
        {
            "zip" => CompressionFormat.Zip,
            "targz" or "tar.gz" or "tgz" => CompressionFormat.TarGz,
            _ => CompressionFormat.None
        };
}
