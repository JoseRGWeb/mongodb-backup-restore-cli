using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;

namespace MongoBackupRestore.Core.Services;

/// <summary>
/// Servicio para ejecutar procesos externos
/// </summary>
public class ProcessRunner : IProcessRunner
{
    private readonly ILogger<ProcessRunner> _logger;

    public ProcessRunner(ILogger<ProcessRunner> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(int exitCode, string output, string error)> RunProcessAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Ejecutando proceso: {FileName} {Arguments}", fileName, arguments);

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                _logger.LogTrace("STDOUT: {Data}", e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                _logger.LogTrace("STDERR: {Data}", e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();
            var exitCode = process.ExitCode;

            _logger.LogDebug("Proceso finalizado con código de salida: {ExitCode}", exitCode);

            return (exitCode, output, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al ejecutar el proceso: {FileName}", fileName);
            throw;
        }
    }
}
