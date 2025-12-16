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
        CancellationToken cancellationToken = default,
        bool logError = true,
        Action<string>? onOutput = null,
        Action<string>? onError = null)
    {
        // lgtm[cs/cleartext-storage-of-sensitive-information]
        // NOTA DE SEGURIDAD: Los argumentos pueden contener credenciales porque mongodump/mongorestore
        // las requieren como parámetros de línea de comandos. Los argumentos se sanitizan antes de
        // escribirlos en los logs. Esta es una limitación inherente de las herramientas de MongoDB.
        // Sanitizar argumentos para logging (ocultar contraseñas)
        var sanitizedArguments = SanitizeArgumentsForLogging(arguments);
        _logger.LogDebug("Ejecutando proceso: {FileName} {Arguments}", fileName, sanitizedArguments);

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
                onOutput?.Invoke(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                _logger.LogTrace("STDERR: {Data}", e.Data);
                onError?.Invoke(e.Data);
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
            if (logError)
            {
                _logger.LogError(ex, "Error al ejecutar el proceso: {FileName}", fileName);
            }
            else
            {
                _logger.LogDebug(ex, "Error al ejecutar el proceso (esperado): {FileName}", fileName);
            }
            throw;
        }
    }

    /// <summary>
    /// Sanitiza los argumentos del comando para logging, ocultando información sensible
    /// </summary>
    private static string SanitizeArgumentsForLogging(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return arguments;
        }

        // Ocultar contraseñas en los argumentos
        // Buscar patrones como --password "..." o --password ...
        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            arguments,
            @"--password\s+""[^""]*""",
            "--password \"***\"",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"--password\s+\S+",
            "--password ***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Ocultar URIs con contraseñas (mongodb://user:password@host)
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"(mongodb(?:\+srv)?://[^:]+:)([^@]+)(@)",
            "$1***$3",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return sanitized;
    }
}
