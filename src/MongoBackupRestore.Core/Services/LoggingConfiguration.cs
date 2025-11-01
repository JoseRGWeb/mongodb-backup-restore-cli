using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MongoBackupRestore.Core.Services;

/// <summary>
/// Servicio para configurar el logging de la aplicación
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Obtiene el nivel de log desde las opciones de verbosidad
    /// </summary>
    /// <param name="verbose">Indica si el modo verbose está activado</param>
    /// <param name="logLevelEnv">Nivel de log desde variable de entorno (opcional)</param>
    /// <returns>El nivel de log configurado</returns>
    public static LogLevel GetLogLevel(bool verbose, string? logLevelEnv = null)
    {
        // Prioridad: opción --verbose > variable de entorno > valor por defecto
        if (verbose)
        {
            return LogLevel.Debug;
        }

        if (!string.IsNullOrWhiteSpace(logLevelEnv))
        {
            return ParseLogLevel(logLevelEnv);
        }

        return LogLevel.Information;
    }

    /// <summary>
    /// Parsea un string a LogLevel
    /// </summary>
    /// <param name="level">String del nivel de log</param>
    /// <returns>LogLevel correspondiente</returns>
    private static LogLevel ParseLogLevel(string level)
    {
        return level.ToLowerInvariant() switch
        {
            "trace" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "information" or "info" => LogLevel.Information,
            "warning" or "warn" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "critical" => LogLevel.Critical,
            "none" => LogLevel.None,
            _ => LogLevel.Information
        };
    }

    /// <summary>
    /// Configura un logger factory con las opciones especificadas
    /// </summary>
    /// <param name="logLevel">Nivel de log a configurar</param>
    /// <param name="logFilePath">Ruta del archivo de log (opcional)</param>
    /// <returns>ILoggerFactory configurado</returns>
    public static ILoggerFactory CreateLoggerFactory(LogLevel logLevel, string? logFilePath = null)
    {
        var factory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            // Configurar logging a consola con formato simple
            builder.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                options.SingleLine = true;
            });

            // Configurar logging a archivo si se especifica
            if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                // Asegurar que el directorio existe
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Agregar proveedor de archivo
                builder.Services.Add(ServiceDescriptor.Singleton<ILoggerProvider>(new SimpleFileLoggerProvider(logFilePath)));
            }

            // Establecer nivel mínimo de log
            builder.SetMinimumLevel(logLevel);
        });

        return factory;
    }
}

/// <summary>
/// Proveedor de logger simple a archivo
/// </summary>
internal class SimpleFileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public SimpleFileLoggerProvider(string filePath)
    {
        _filePath = filePath;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SimpleFileLogger(categoryName, _filePath, _lock);
    }

    public void Dispose()
    {
        // No hay recursos que liberar
    }
}

/// <summary>
/// Logger simple que escribe a archivo
/// </summary>
internal class SimpleFileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _filePath;
    private readonly object _lock;

    public SimpleFileLogger(string categoryName, string filePath, object lockObject)
    {
        _categoryName = categoryName;
        _filePath = filePath;
        _lock = lockObject;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var logLevelString = GetLogLevelString(logLevel);
        var logLine = $"[{timestamp}] [{logLevelString}] {_categoryName}: {message}";

        if (exception != null)
        {
            logLine += Environment.NewLine + exception.ToString();
        }

        lock (_lock)
        {
            try
            {
                // Nota: Se usa AppendAllText por simplicidad. Para escenarios de alto volumen,
                // considere usar StreamWriter con buffering.
                File.AppendAllText(_filePath, logLine + Environment.NewLine);
            }
            catch
            {
                // Ignorar errores de escritura al archivo
            }
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT",
            _ => "INFO"
        };
    }
}
