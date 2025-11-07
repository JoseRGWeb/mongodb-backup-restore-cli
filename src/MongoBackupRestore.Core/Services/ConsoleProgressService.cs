using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;
using Spectre.Console;

namespace MongoBackupRestore.Core.Services;

/// <summary>
/// Implementación del servicio de progreso en consola usando Spectre.Console
/// </summary>
public class ConsoleProgressService : IConsoleProgressService
{
    private readonly ILogger<ConsoleProgressService> _logger;
    private readonly bool _isVerbose;

    public ConsoleProgressService(ILogger<ConsoleProgressService> logger, bool isVerbose = false)
    {
        _logger = logger;
        _isVerbose = isVerbose;
    }

    /// <inheritdoc />
    public async Task ExecuteWithProgressAsync(string description, Func<Task> action)
    {
        await AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green bold"))
            .StartAsync(description, async ctx =>
            {
                _logger.LogInformation("{Description}", description);
                await action();
            });
    }

    /// <inheritdoc />
    public async Task<T> ExecuteWithProgressAsync<T>(string description, Func<Task<T>> action)
    {
        return await AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green bold"))
            .StartAsync(description, async ctx =>
            {
                _logger.LogInformation("{Description}", description);
                return await action();
            });
    }

    /// <inheritdoc />
    public void ShowSuccess(string message)
    {
        _logger.LogInformation(message);
        AnsiConsole.MarkupLine($"[green bold]✓[/] [green]{Markup.Escape(message)}[/]");
    }

    /// <inheritdoc />
    public void ShowError(string message)
    {
        _logger.LogError(message);
        AnsiConsole.MarkupLine($"[red bold]✗[/] [red]{Markup.Escape(message)}[/]");
    }

    /// <inheritdoc />
    public void ShowInfo(string message)
    {
        _logger.LogInformation(message);
        AnsiConsole.MarkupLine($"[blue]ℹ[/] {Markup.Escape(message)}");
    }

    /// <inheritdoc />
    public void ShowWarning(string message)
    {
        _logger.LogWarning(message);
        AnsiConsole.MarkupLine($"[yellow bold]⚠[/] [yellow]{Markup.Escape(message)}[/]");
    }

    /// <inheritdoc />
    public void ShowPanel(string title, string content)
    {
        _logger.LogInformation("{Title}: {Content}", title, content);
        
        var panel = new Panel(Markup.Escape(content))
        {
            Header = new PanelHeader(title),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green)
        };
        
        AnsiConsole.Write(panel);
    }

    /// <inheritdoc />
    public void ShowTable(string title, Dictionary<string, string> data)
    {
        _logger.LogInformation("{Title}", title);
        
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .Title(title);

        table.AddColumn("Propiedad");
        table.AddColumn("Valor");

        foreach (var item in data)
        {
            table.AddRow(
                Markup.Escape(item.Key), 
                Markup.Escape(item.Value)
            );
        }

        AnsiConsole.Write(table);
    }
}
