using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MongoBackupRestore.Core.Services;
using MongoBackupRestore.McpServer;

var builder = Host.CreateApplicationBuilder(args);

// Configurar logging (stderr para no interferir con el transporte STDIO del MCP)
builder.Logging.AddConsole(options =>
{
    options.FormatterName = "simple";
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Registrar servicios del Core
builder.Services.AddSingleton<MongoBackupRestore.Core.Interfaces.IProcessRunner>(sp =>
    new MongoBackupRestore.Core.Services.ProcessRunner(sp.GetRequiredService<ILogger<ProcessRunner>>()));

builder.Services.AddSingleton<MongoBackupRestore.Core.Interfaces.IMongoToolsValidator>(sp =>
    new MongoToolsValidator(
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IProcessRunner>(),
        sp.GetRequiredService<ILogger<MongoToolsValidator>>()));

builder.Services.AddSingleton<MongoBackupRestore.Core.Interfaces.IMongoConnectionValidator>(sp =>
    new MongoConnectionValidator(
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IProcessRunner>(),
        sp.GetRequiredService<ILogger<MongoConnectionValidator>>()));

builder.Services.AddSingleton<MongoBackupRestore.Core.Interfaces.IDockerContainerDetector>(sp =>
    new DockerContainerDetector(
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IProcessRunner>(),
        sp.GetRequiredService<ILogger<DockerContainerDetector>>()));

builder.Services.AddSingleton<MongoBackupRestore.Core.Interfaces.ICompressionService>(sp =>
    new CompressionService(
        sp.GetRequiredService<ILogger<CompressionService>>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IProcessRunner>()));

builder.Services.AddSingleton<MongoBackupRestore.Core.Interfaces.IEncryptionService>(sp =>
    new EncryptionService(sp.GetRequiredService<ILogger<EncryptionService>>()));

builder.Services.AddSingleton<MongoBackupRestore.Core.Interfaces.IBackupRetentionService>(sp =>
    new BackupRetentionService(sp.GetRequiredService<ILogger<BackupRetentionService>>()));

builder.Services.AddSingleton<MongoBackupRestore.Core.Interfaces.IConsoleProgressService>(sp =>
    new ConsoleProgressService(sp.GetRequiredService<ILogger<ConsoleProgressService>>(), false));

builder.Services.AddSingleton<MongoBackupRestore.Core.Interfaces.IBackupService>(sp =>
    new BackupService(
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IProcessRunner>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IMongoToolsValidator>(),
        sp.GetRequiredService<ILogger<BackupService>>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IMongoConnectionValidator>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IDockerContainerDetector>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.ICompressionService>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IBackupRetentionService>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IEncryptionService>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IConsoleProgressService>()));

builder.Services.AddSingleton<MongoBackupRestore.Core.Interfaces.IRestoreService>(sp =>
    new RestoreService(
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IProcessRunner>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IMongoToolsValidator>(),
        sp.GetRequiredService<ILogger<RestoreService>>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IMongoConnectionValidator>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IDockerContainerDetector>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.ICompressionService>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IEncryptionService>(),
        sp.GetRequiredService<MongoBackupRestore.Core.Interfaces.IConsoleProgressService>()));

// Registrar herramientas MCP
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(MongoTools).Assembly);

var app = builder.Build();
await app.RunAsync();
