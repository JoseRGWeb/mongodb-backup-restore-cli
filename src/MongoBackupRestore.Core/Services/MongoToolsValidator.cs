using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;
using MongoBackupRestore.Core.Models;

namespace MongoBackupRestore.Core.Services;

/// <summary>
/// Servicio para validar las herramientas de MongoDB
/// </summary>
public class MongoToolsValidator : IMongoToolsValidator
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<MongoToolsValidator> _logger;

    public MongoToolsValidator(IProcessRunner processRunner, ILogger<MongoToolsValidator> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MongoToolsInfo> ValidateToolsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validando herramientas de MongoDB...");

        var toolsInfo = new MongoToolsInfo();

        // Validar mongodump
        await ValidateMongoDumpAsync(toolsInfo, cancellationToken);

        // Validar mongorestore
        await ValidateMongoRestoreAsync(toolsInfo, cancellationToken);

        // Validar Docker
        await ValidateDockerAsync(toolsInfo, cancellationToken);

        LogToolsStatus(toolsInfo);

        return toolsInfo;
    }

    private async Task ValidateMongoDumpAsync(MongoToolsInfo toolsInfo, CancellationToken cancellationToken)
    {
        try
        {
            var (exitCode, output, error) = await _processRunner.RunProcessAsync(
                GetCommandName("mongodump"),
                "--version",
                cancellationToken);

            if (exitCode == 0)
            {
                toolsInfo.MongoDumpAvailable = true;
                toolsInfo.MongoDumpPath = GetCommandName("mongodump");
                toolsInfo.MongoDumpVersion = ExtractVersion(output);
                _logger.LogInformation("mongodump encontrado: versión {Version}", toolsInfo.MongoDumpVersion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "mongodump no está disponible");
        }
    }

    private async Task ValidateMongoRestoreAsync(MongoToolsInfo toolsInfo, CancellationToken cancellationToken)
    {
        try
        {
            var (exitCode, output, error) = await _processRunner.RunProcessAsync(
                GetCommandName("mongorestore"),
                "--version",
                cancellationToken);

            if (exitCode == 0)
            {
                toolsInfo.MongoRestoreAvailable = true;
                toolsInfo.MongoRestorePath = GetCommandName("mongorestore");
                toolsInfo.MongoRestoreVersion = ExtractVersion(output);
                _logger.LogInformation("mongorestore encontrado: versión {Version}", toolsInfo.MongoRestoreVersion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "mongorestore no está disponible");
        }
    }

    private async Task ValidateDockerAsync(MongoToolsInfo toolsInfo, CancellationToken cancellationToken)
    {
        try
        {
            var (exitCode, output, error) = await _processRunner.RunProcessAsync(
                "docker",
                "--version",
                cancellationToken);

            if (exitCode == 0)
            {
                toolsInfo.DockerAvailable = true;
                toolsInfo.DockerVersion = ExtractDockerVersion(output);
                _logger.LogInformation("Docker encontrado: versión {Version}", toolsInfo.DockerVersion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Docker no está disponible");
        }
    }

    private static string GetCommandName(string baseCommand)
    {
        // En Windows, los comandos pueden tener extensión .exe
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? $"{baseCommand}.exe" 
            : baseCommand;
    }

    private static string ExtractVersion(string output)
    {
        // Buscar patrón de versión como "version v100.9.5" o "version: 100.9.5"
        var match = Regex.Match(output, @"version[:\s]+v?(\d+\.\d+\.\d+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "desconocida";
    }

    private static string ExtractDockerVersion(string output)
    {
        // Buscar patrón de versión como "Docker version 24.0.7"
        var match = Regex.Match(output, @"version\s+(\d+\.\d+\.\d+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "desconocida";
    }

    private void LogToolsStatus(MongoToolsInfo toolsInfo)
    {
        _logger.LogInformation("=== Estado de herramientas ===");
        _logger.LogInformation("mongodump: {Status}", 
            toolsInfo.MongoDumpAvailable ? $"✓ (v{toolsInfo.MongoDumpVersion})" : "✗ No disponible");
        _logger.LogInformation("mongorestore: {Status}", 
            toolsInfo.MongoRestoreAvailable ? $"✓ (v{toolsInfo.MongoRestoreVersion})" : "✗ No disponible");
        _logger.LogInformation("Docker: {Status}", 
            toolsInfo.DockerAvailable ? $"✓ (v{toolsInfo.DockerVersion})" : "✗ No disponible");
        _logger.LogInformation("==============================");
    }
}
