using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;
using MongoBackupRestore.Core.Models;

namespace MongoBackupRestore.Core.Services;

/// <summary>
/// Servicio para gestionar la retención y limpieza automática de backups antiguos
/// </summary>
public class RetentionService : IRetentionService
{
    private readonly ILogger<RetentionService> _logger;

    public RetentionService(ILogger<RetentionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RetentionResult> CleanupOldBackupsAsync(RetentionPolicy policy, CancellationToken cancellationToken = default)
    {
        var result = new RetentionResult { Success = true };

        // Validar la política
        if (!policy.IsEnabled)
        {
            _logger.LogInformation("Política de retención no habilitada. No se realizará limpieza.");
            result.Message = "Política de retención no habilitada";
            return result;
        }

        if (string.IsNullOrWhiteSpace(policy.BackupDirectory))
        {
            _logger.LogWarning("Directorio de backups no especificado. No se puede realizar limpieza.");
            result.Success = false;
            result.Message = "Directorio de backups no especificado";
            return result;
        }

        if (!Directory.Exists(policy.BackupDirectory))
        {
            _logger.LogWarning("Directorio de backups no existe: {Directory}", policy.BackupDirectory);
            result.Success = false;
            result.Message = $"El directorio de backups no existe: {policy.BackupDirectory}";
            return result;
        }

        _logger.LogInformation("Iniciando limpieza de backups con retención de {Days} días en: {Directory}", 
            policy.RetentionDays, policy.BackupDirectory);

        try
        {
            var cutoffDate = DateTime.Now.AddDays(-policy.RetentionDays!.Value);
            _logger.LogInformation("Fecha de corte para retención: {CutoffDate}", cutoffDate);

            // Obtener todos los archivos y directorios en el directorio de backups
            var directoryInfo = new DirectoryInfo(policy.BackupDirectory);
            
            // Buscar archivos de backup (directorios y archivos comprimidos)
            var items = new List<FileSystemInfo>();
            items.AddRange(directoryInfo.GetDirectories());
            items.AddRange(directoryInfo.GetFiles("*.zip"));
            items.AddRange(directoryInfo.GetFiles("*.tar.gz"));
            items.AddRange(directoryInfo.GetFiles("*.tgz"));

            var itemsToDelete = items
                .Where(item => item.LastWriteTime < cutoffDate)
                .OrderBy(item => item.LastWriteTime)
                .ToList();

            if (itemsToDelete.Count == 0)
            {
                _logger.LogInformation("No se encontraron backups antiguos para eliminar");
                result.Message = $"No se encontraron backups más antiguos que {policy.RetentionDays} días";
                return result;
            }

            _logger.LogInformation("Se encontraron {Count} backups antiguos para eliminar", itemsToDelete.Count);

            // Eliminar cada backup antiguo
            foreach (var item in itemsToDelete)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Limpieza de backups cancelada por el usuario");
                    result.Success = false;
                    result.Message = "Limpieza cancelada";
                    return result;
                }

                await DeleteBackupItemAsync(item, result);
            }

            var message = $"Limpieza completada: {result.DeletedCount} backup(s) eliminado(s), {FormatBytes(result.BytesFreed)} liberados";
            _logger.LogInformation(message);
            result.Message = message;
            result.Success = true;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la limpieza de backups");
            result.Success = false;
            result.Message = "Error durante la limpieza de backups";
            result.Error = ex.Message;
            return result;
        }
    }

    private async Task DeleteBackupItemAsync(FileSystemInfo item, RetentionResult result)
    {
        try
        {
            long itemSize = 0;

            if (item is DirectoryInfo dirInfo)
            {
                // Calcular tamaño del directorio
                itemSize = await Task.Run(() => CalculateDirectorySize(dirInfo)).ConfigureAwait(false);
                
                _logger.LogInformation("Eliminando directorio de backup: {Path} (Tamaño: {Size}, Fecha: {Date})",
                    item.FullName, FormatBytes(itemSize), item.LastWriteTime);
                
                dirInfo.Delete(recursive: true);
            }
            else if (item is FileInfo fileInfo)
            {
                itemSize = fileInfo.Length;
                
                _logger.LogInformation("Eliminando archivo de backup: {Path} (Tamaño: {Size}, Fecha: {Date})",
                    item.FullName, FormatBytes(itemSize), item.LastWriteTime);
                
                fileInfo.Delete();
            }

            result.DeletedCount++;
            result.BytesFreed += itemSize;
            result.DeletedPaths.Add(item.FullName);
            
            _logger.LogInformation("✓ Eliminado exitosamente: {Path}", item.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar: {Path}", item.FullName);
            // Continuar con el siguiente elemento en lugar de fallar completamente
        }
    }

    private long CalculateDirectorySize(DirectoryInfo directory)
    {
        try
        {
            long size = 0;
            
            // Sumar tamaño de archivos
            var files = directory.GetFiles("*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                size += file.Length;
            }
            
            return size;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al calcular tamaño del directorio: {Path}", directory.FullName);
            return 0;
        }
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
