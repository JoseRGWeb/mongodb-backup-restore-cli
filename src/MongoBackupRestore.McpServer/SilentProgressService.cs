using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;

namespace MongoBackupRestore.McpServer;

/// <summary>
/// Implementación silenciosa del servicio de progreso para el servidor MCP.
/// No escribe nada a stdout/stderr para no interferir con el transporte STDIO del protocolo MCP.
/// Solo registra los mensajes importantes mediante el logger (que escribe a stderr).
/// </summary>
public class SilentProgressService : IConsoleProgressService
{
    private readonly ILogger<SilentProgressService> _logger;

    public SilentProgressService(ILogger<SilentProgressService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteWithProgressAsync(string description, Func<Action<string>, Task> action)
    {
        _logger.LogDebug("{Description}", description);

        // Proporcionamos un callback vacío que no escribe a la consola
        Action<string> noOpUpdate = (_) => { };
        await action(noOpUpdate);
    }

    /// <inheritdoc />
    public async Task<T> ExecuteWithProgressAsync<T>(string description, Func<Action<string>, Task<T>> action)
    {
        _logger.LogDebug("{Description}", description);

        // Proporcionamos un callback vacío que no escribe a la consola
        Action<string> noOpUpdate = (_) => { };
        return await action(noOpUpdate);
    }

    /// <inheritdoc />
    public void ShowSuccess(string message)
    {
        _logger.LogInformation("✓ {Message}", message);
    }

    /// <inheritdoc />
    public void ShowError(string message)
    {
        _logger.LogError("✗ {Message}", message);
    }

    /// <inheritdoc />
    public void ShowInfo(string message)
    {
        _logger.LogInformation("ℹ {Message}", message);
    }

    /// <inheritdoc />
    public void ShowWarning(string message)
    {
        _logger.LogWarning("⚠ {Message}", message);
    }

    /// <inheritdoc />
    public void ShowPanel(string title, string content)
    {
        _logger.LogInformation("{Title}: {Content}", title, content);
    }

    /// <inheritdoc />
    public void ShowTable(string title, Dictionary<string, string> data)
    {
        _logger.LogInformation("{Title}", title);
        foreach (var item in data)
        {
            _logger.LogInformation("  {Key}: {Value}", item.Key, item.Value);
        }
    }
}
