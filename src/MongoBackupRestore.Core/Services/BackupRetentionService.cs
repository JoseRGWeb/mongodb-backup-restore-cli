using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;
using MongoBackupRestore.Core.Models;

namespace MongoBackupRestore.Core.Services;

/// <summary>
/// Servicio para gestionar la retención y limpieza automática de backups
/// </summary>
public class BackupRetentionService : IBackupRetentionService
{
    private readonly ILogger<BackupRetentionService> _logger;

    public BackupRetentionService(ILogger<BackupRetentionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RetentionCleanupResult> CleanupOldBackupsAsync(
        string backupDirectory,
        int retentionDays,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        var result = new RetentionCleanupResult { Success = true };

        try
        {
            // Validar parámetros
            if (string.IsNullOrWhiteSpace(backupDirectory))
            {
                result.Success = false;
                result.Message = "El directorio de backups no puede estar vacío";
                return result;
            }

            if (retentionDays <= 0)
            {
                result.Success = false;
                result.Message = "Los días de retención deben ser mayores que cero";
                return result;
            }

            if (!Directory.Exists(backupDirectory))
            {
                _logger.LogWarning("El directorio de backups no existe: {Directory}", backupDirectory);
                result.Success = true;
                result.Message = $"El directorio de backups no existe: {backupDirectory}";
                return result;
            }

            _logger.LogInformation(
                "Iniciando limpieza de backups antiguos. Directorio: {Directory}, Retención: {Days} días, Modo: {Mode}",
                backupDirectory,
                retentionDays,
                dryRun ? "DRY-RUN" : "REAL");

            // Calcular fecha límite
            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            _logger.LogInformation("Fecha límite de retención: {CutoffDate}", cutoffDate.ToString("yyyy-MM-dd HH:mm:ss"));

            // Obtener todos los elementos del directorio (archivos y subdirectorios)
            var backupItems = await GetBackupItemsAsync(backupDirectory, cancellationToken);
            result.TotalBackupsFound = backupItems.Count;

            _logger.LogInformation("Total de backups encontrados: {Count}", result.TotalBackupsFound);

            // Procesar cada elemento
            foreach (var item in backupItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var itemDate = GetItemCreationDate(item);

                    if (itemDate < cutoffDate)
                    {
                        // Backup antiguo - debe ser eliminado
                        var itemSize = GetItemSize(item);
                        var itemName = Path.GetFileName(item.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                        _logger.LogInformation(
                            "Backup antiguo identificado: {Item}, Fecha: {Date}, Tamaño: {Size} bytes",
                            itemName,
                            itemDate.ToString("yyyy-MM-dd HH:mm:ss"),
                            itemSize);

                        if (!dryRun)
                        {
                            await DeleteItemAsync(item, cancellationToken);
                            result.SpaceFreedBytes += itemSize;
                            _logger.LogInformation("✓ Backup eliminado: {Item}", itemName);
                        }
                        else
                        {
                            _logger.LogInformation("✓ [DRY-RUN] Se eliminaría: {Item}", itemName);
                        }

                        result.DeletedItems.Add(item);
                        result.BackupsDeleted++;
                    }
                    else
                    {
                        // Backup dentro del período de retención
                        var itemName = Path.GetFileName(item.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                        _logger.LogDebug(
                            "Backup retenido: {Item}, Fecha: {Date}",
                            itemName,
                            itemDate.ToString("yyyy-MM-dd HH:mm:ss"));

                        result.RetainedItems.Add(item);
                        result.BackupsRetained++;
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Error al procesar {item}: {ex.Message}";
                    _logger.LogError(ex, "Error al procesar backup: {Item}", item);
                    result.Errors.Add(error);
                }
            }

            // Generar mensaje de resumen
            result.Message = GenerateSummaryMessage(result, retentionDays, dryRun);
            _logger.LogInformation(result.Message);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la limpieza de backups");
            result.Success = false;
            result.Message = $"Error durante la limpieza de backups: {ex.Message}";
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    /// <summary>
    /// Obtiene todos los elementos de backup del directorio (archivos comprimidos y directorios)
    /// </summary>
    private async Task<List<string>> GetBackupItemsAsync(string directory, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var items = new List<string>();

            // Obtener archivos comprimidos de backup (.zip, .tar.gz, .tgz)
            var backupFiles = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    return ext == ".zip" || ext == ".gz" || ext == ".tgz" ||
                           f.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            items.AddRange(backupFiles);

            // Obtener directorios de backup (que no sean temporales)
            var backupDirs = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly)
                .Where(d =>
                {
                    var dirName = Path.GetFileName(d);
                    return !dirName.StartsWith(".") && !dirName.StartsWith("temp", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            items.AddRange(backupDirs);

            return items;
        }, cancellationToken);
    }

    /// <summary>
    /// Obtiene la fecha de creación de un elemento (archivo o directorio)
    /// </summary>
    private DateTime GetItemCreationDate(string path)
    {
        if (File.Exists(path))
        {
            return File.GetCreationTime(path);
        }
        else if (Directory.Exists(path))
        {
            return Directory.GetCreationTime(path);
        }

        return DateTime.MinValue;
    }

    /// <summary>
    /// Obtiene el tamaño total de un elemento (archivo o directorio)
    /// </summary>
    private long GetItemSize(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }
            else if (Directory.Exists(path))
            {
                var dirInfo = new DirectoryInfo(path);
                return dirInfo.GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al calcular tamaño de {Path}", path);
        }

        return 0;
    }

    /// <summary>
    /// Elimina un elemento (archivo o directorio)
    /// </summary>
    private async Task DeleteItemAsync(string path, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Genera el mensaje de resumen de la operación de limpieza
    /// </summary>
    private string GenerateSummaryMessage(RetentionCleanupResult result, int retentionDays, bool dryRun)
    {
        var spaceFreedMB = result.SpaceFreedBytes / (1024.0 * 1024.0);
        var mode = dryRun ? "[DRY-RUN] " : "";

        if (result.BackupsDeleted == 0)
        {
            return $"{mode}No se encontraron backups antiguos para eliminar. " +
                   $"Total de backups: {result.TotalBackupsFound}, Retención: {retentionDays} días";
        }

        var message = $"{mode}Limpieza de backups completada. " +
                     $"Eliminados: {result.BackupsDeleted}, Retenidos: {result.BackupsRetained}, " +
                     $"Espacio liberado: {spaceFreedMB:F2} MB";

        if (result.Errors.Count > 0)
        {
            message += $", Errores: {result.Errors.Count}";
        }

        return message;
    }
}
