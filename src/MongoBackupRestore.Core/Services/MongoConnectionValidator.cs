using System.Text;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;

namespace MongoBackupRestore.Core.Services;

/// <summary>
/// Servicio para validar conexiones a MongoDB
/// </summary>
public class MongoConnectionValidator : IMongoConnectionValidator
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<MongoConnectionValidator> _logger;

    public MongoConnectionValidator(
        IProcessRunner processRunner,
        ILogger<MongoConnectionValidator> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> ValidateConnectionAsync(
        string host,
        int port,
        string? username,
        string? password,
        string authenticationDatabase,
        string? uri,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validando conexión a MongoDB...");

        // Construir comando mongosh o mongo para validar la conexión
        var commandName = await GetMongoShellCommandNameAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(commandName))
        {
            _logger.LogWarning("No se encontró mongosh ni mongo para validar la conexión");
            // Si no hay shell de mongo disponible, no podemos validar pero no bloqueamos la operación
            // mongodump/mongorestore harán su propia validación
            return (true, null);
        }

        var arguments = BuildValidationArguments(host, port, username, password, authenticationDatabase, uri);

        _logger.LogDebug("Ejecutando validación: {Command} {Arguments}", commandName, SanitizeArgumentsForLogging(arguments));

        var (exitCode, output, error) = await _processRunner.RunProcessAsync(
            commandName,
            arguments,
            cancellationToken);

        if (exitCode == 0)
        {
            _logger.LogInformation("Validación de conexión exitosa");
            return (true, null);
        }

        // Analizar el error para determinar si es un problema de autenticación
        var errorMessage = AnalyzeConnectionError(error, output);
        _logger.LogWarning("Validación de conexión falló: {ErrorMessage}", errorMessage);

        return (false, errorMessage);
    }

    private async Task<string?> GetMongoShellCommandNameAsync(CancellationToken cancellationToken)
    {
        // Intentar mongosh primero (versión moderna)
        var commandNames = new[] { "mongosh", "mongo" };

        foreach (var cmd in commandNames)
        {
            var fullCommand = MongoToolsValidator.GetMongoCommandName(cmd);
            try
            {
                var testResult = await _processRunner.RunProcessAsync(
                    fullCommand,
                    "--version",
                    cancellationToken).ConfigureAwait(false);

                if (testResult.exitCode == 0)
                {
                    return fullCommand;
                }
            }
            catch
            {
                // Continuar con el siguiente comando
            }
        }

        return null;
    }

    private string BuildValidationArguments(
        string host,
        int port,
        string? username,
        string? password,
        string authenticationDatabase,
        string? uri)
    {
        var args = new StringBuilder();

        // URI o Host/Port
        if (!string.IsNullOrWhiteSpace(uri))
        {
            args.Append($"\"{uri}\"");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                // lgtm[cs/cleartext-storage-of-sensitive-information]
                // NOTA DE SEGURIDAD: La contraseña se incluye en la URI de conexión para validar
                // las credenciales. Este es un proceso temporal que solo ocurre durante la validación.
                // La URI nunca se almacena en disco. En producción, considere usar autenticación
                // basada en certificados o Kerberos para evitar el uso de contraseñas.
                // Construir URI con autenticación
                var passwordPart = !string.IsNullOrWhiteSpace(password) ? $":{password}" : "";
                var connectionUri = $"mongodb://{username}{passwordPart}@{host}:{port}/{authenticationDatabase}";
                args.Append($"\"{connectionUri}\"");
            }
            else
            {
                // Sin autenticación
                args.Append($"--host {host} --port {port}");
            }
        }

        // Ejecutar un comando simple para validar la conexión
        args.Append(" --eval \"db.adminCommand('ping')\" --quiet");

        return args.ToString();
    }

    private string SanitizeArgumentsForLogging(string arguments)
    {
        // Ocultar contraseñas en los logs
        var sanitized = arguments;
        
        // Reemplazar contraseñas en URIs
        var uriPasswordPattern = @"://([^:]+):([^@]+)@";
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            uriPasswordPattern,
            "://$1:****@");

        return sanitized;
    }

    private string AnalyzeConnectionError(string error, string output)
    {
        var combinedError = $"{error} {output}".ToLower();

        // Errores de autenticación
        if (combinedError.Contains("authentication failed") ||
            combinedError.Contains("auth failed") ||
            combinedError.Contains("unauthorized") ||
            combinedError.Contains("not authorized"))
        {
            return "Error de autenticación: Las credenciales proporcionadas son incorrectas o el usuario no tiene permisos suficientes. " +
                   "Verifique el nombre de usuario, contraseña y base de datos de autenticación (--auth-db).";
        }

        // Errores de conexión
        if (combinedError.Contains("connection refused") ||
            combinedError.Contains("connect failed") ||
            combinedError.Contains("econnrefused"))
        {
            return "Error de conexión: No se pudo conectar al servidor MongoDB. " +
                   "Verifique que el host, puerto y servicio MongoDB estén disponibles.";
        }

        // Timeout
        if (combinedError.Contains("timeout") ||
            combinedError.Contains("timed out"))
        {
            return "Error de conexión: Tiempo de espera agotado al conectar con MongoDB. " +
                   "Verifique que el servidor esté accesible y que no haya problemas de red.";
        }

        // Error de DNS
        if (combinedError.Contains("getaddrinfo") ||
            combinedError.Contains("unknown host") ||
            combinedError.Contains("nodename nor servname"))
        {
            return "Error de conexión: No se pudo resolver el host especificado. " +
                   "Verifique que el nombre del host sea correcto.";
        }

        // Error genérico
        return $"Error al validar la conexión: {error}";
    }
}
