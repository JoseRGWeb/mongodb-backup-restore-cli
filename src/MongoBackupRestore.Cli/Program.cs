using System.CommandLine;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;
using MongoBackupRestore.Core.Models;
using MongoBackupRestore.Core.Services;

// Configurar logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Crear servicios
var processRunnerLogger = loggerFactory.CreateLogger<ProcessRunner>();
var processRunner = new ProcessRunner(processRunnerLogger);

var validatorLogger = loggerFactory.CreateLogger<MongoToolsValidator>();
var toolsValidator = new MongoToolsValidator(processRunner, validatorLogger);

var connectionValidatorLogger = loggerFactory.CreateLogger<MongoConnectionValidator>();
var connectionValidator = new MongoConnectionValidator(processRunner, connectionValidatorLogger);

var backupServiceLogger = loggerFactory.CreateLogger<BackupService>();
var backupService = new BackupService(processRunner, toolsValidator, backupServiceLogger, connectionValidator);

var restoreServiceLogger = loggerFactory.CreateLogger<RestoreService>();
var restoreService = new RestoreService(processRunner, toolsValidator, restoreServiceLogger, connectionValidator);

// Crear comando raíz
var rootCommand = new RootCommand("MongoDB Backup & Restore CLI - Herramienta para gestionar copias de seguridad de MongoDB");

// Crear comando backup
var backupCommand = CreateBackupCommand(backupService, loggerFactory);
rootCommand.AddCommand(backupCommand);

// Crear comando restore
var restoreCommand = CreateRestoreCommand(restoreService, loggerFactory);
rootCommand.AddCommand(restoreCommand);

// Ejecutar CLI
return await rootCommand.InvokeAsync(args);

static Command CreateBackupCommand(IBackupService backupService, ILoggerFactory loggerFactory)
{
    var command = new Command("backup", "Realiza una copia de seguridad de una base de datos MongoDB");

    // Opciones obligatorias
    var dbOption = new Option<string>(
        name: "--db",
        description: "Nombre de la base de datos a respaldar")
    {
        IsRequired = true
    };
    dbOption.AddAlias("-d");

    var outOption = new Option<string>(
        name: "--out",
        description: "Ruta de destino para el backup")
    {
        IsRequired = true
    };
    outOption.AddAlias("-o");

    // Opciones de conexión
    var hostOption = new Option<string>(
        name: "--host",
        description: "Host de MongoDB",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_HOST") ?? "localhost");
    hostOption.AddAlias("-h");

    var portOption = new Option<int>(
        name: "--port",
        description: "Puerto de MongoDB",
        getDefaultValue: () =>
        {
            var portStr = Environment.GetEnvironmentVariable("MONGO_PORT");
            return int.TryParse(portStr, out var port) ? port : 27017;
        });
    portOption.AddAlias("-p");

    var userOption = new Option<string?>(
        name: "--user",
        description: "Usuario para autenticación",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_USER"));
    userOption.AddAlias("-u");

    var passwordOption = new Option<string?>(
        name: "--password",
        description: "Contraseña para autenticación",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_PASSWORD"));

    var authDbOption = new Option<string>(
        name: "--auth-db",
        description: "Base de datos de autenticación",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_AUTH_DB") ?? "admin");

    var uriOption = new Option<string?>(
        name: "--uri",
        description: "URI de conexión completa (alternativa a host/port/user/password)",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_URI"));

    // Opciones de Docker
    var inDockerOption = new Option<bool>(
        name: "--in-docker",
        description: "Ejecutar dentro de un contenedor Docker",
        getDefaultValue: () => false);

    var containerNameOption = new Option<string?>(
        name: "--container-name",
        description: "Nombre del contenedor Docker");
    containerNameOption.AddAlias("-c");

    // Opciones de verbosidad
    var verboseOption = new Option<bool>(
        name: "--verbose",
        description: "Habilitar salida detallada",
        getDefaultValue: () => false);
    verboseOption.AddAlias("-v");

    // Agregar opciones al comando
    command.AddOption(dbOption);
    command.AddOption(outOption);
    command.AddOption(hostOption);
    command.AddOption(portOption);
    command.AddOption(userOption);
    command.AddOption(passwordOption);
    command.AddOption(authDbOption);
    command.AddOption(uriOption);
    command.AddOption(inDockerOption);
    command.AddOption(containerNameOption);
    command.AddOption(verboseOption);

    // Handler del comando
    command.SetHandler(async (context) =>
    {
        var database = context.ParseResult.GetValueForOption(dbOption)!;
        var outputPath = context.ParseResult.GetValueForOption(outOption)!;
        var host = context.ParseResult.GetValueForOption(hostOption)!;
        var port = context.ParseResult.GetValueForOption(portOption);
        var username = context.ParseResult.GetValueForOption(userOption);
        var password = context.ParseResult.GetValueForOption(passwordOption);
        var authDb = context.ParseResult.GetValueForOption(authDbOption)!;
        var uri = context.ParseResult.GetValueForOption(uriOption);
        var inDocker = context.ParseResult.GetValueForOption(inDockerOption);
        var containerName = context.ParseResult.GetValueForOption(containerNameOption);
        var verbose = context.ParseResult.GetValueForOption(verboseOption);

        // Configurar nivel de log según verbosidad
        if (verbose)
        {
            loggerFactory.CreateLogger("Root").LogInformation("Modo verbose activado");
        }

        var options = new BackupOptions
        {
            Database = database,
            OutputPath = outputPath,
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            AuthenticationDatabase = authDb,
            Uri = uri,
            InDocker = inDocker,
            ContainerName = containerName,
            Verbose = verbose
        };

        try
        {
            var result = await backupService.ExecuteBackupAsync(options);

            if (result.Success)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ " + result.Message);
                Console.ResetColor();
                Console.WriteLine($"Ruta del backup: {result.BackupPath}");
                context.ExitCode = 0;
            }
            else
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ " + result.Message);
                Console.ResetColor();

                if (!string.IsNullOrWhiteSpace(result.Error) && verbose)
                {
                    Console.WriteLine();
                    Console.WriteLine("Detalles del error:");
                    Console.WriteLine(result.Error);
                }

                context.ExitCode = result.ExitCode;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Error inesperado: {ex.Message}");
            Console.ResetColor();

            if (verbose)
            {
                Console.WriteLine();
                Console.WriteLine("Detalles del error:");
                Console.WriteLine(ex.ToString());
            }

            context.ExitCode = 1;
        }
    });

    return command;
}

static Command CreateRestoreCommand(IRestoreService restoreService, ILoggerFactory loggerFactory)
{
    var command = new Command("restore", "Restaura una base de datos MongoDB desde un backup");

    // Opciones obligatorias
    var dbOption = new Option<string>(
        name: "--db",
        description: "Nombre de la base de datos a restaurar")
    {
        IsRequired = true
    };
    dbOption.AddAlias("-d");

    var fromOption = new Option<string>(
        name: "--from",
        description: "Ruta de origen del backup")
    {
        IsRequired = true
    };
    fromOption.AddAlias("-f");

    // Opciones de conexión
    var hostOption = new Option<string>(
        name: "--host",
        description: "Host de MongoDB",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_HOST") ?? "localhost");
    hostOption.AddAlias("-h");

    var portOption = new Option<int>(
        name: "--port",
        description: "Puerto de MongoDB",
        getDefaultValue: () =>
        {
            var portStr = Environment.GetEnvironmentVariable("MONGO_PORT");
            return int.TryParse(portStr, out var port) ? port : 27017;
        });
    portOption.AddAlias("-p");

    var userOption = new Option<string?>(
        name: "--user",
        description: "Usuario para autenticación",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_USER"));
    userOption.AddAlias("-u");

    var passwordOption = new Option<string?>(
        name: "--password",
        description: "Contraseña para autenticación",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_PASSWORD"));

    var authDbOption = new Option<string>(
        name: "--auth-db",
        description: "Base de datos de autenticación",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_AUTH_DB") ?? "admin");

    var uriOption = new Option<string?>(
        name: "--uri",
        description: "URI de conexión completa (alternativa a host/port/user/password)",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_URI"));

    // Opciones de Docker
    var inDockerOption = new Option<bool>(
        name: "--in-docker",
        description: "Ejecutar dentro de un contenedor Docker",
        getDefaultValue: () => false);

    var containerNameOption = new Option<string?>(
        name: "--container-name",
        description: "Nombre del contenedor Docker");
    containerNameOption.AddAlias("-c");

    // Opciones adicionales de restore
    var dropOption = new Option<bool>(
        name: "--drop",
        description: "Eliminar la base de datos antes de restaurar",
        getDefaultValue: () => false);

    // Opciones de verbosidad
    var verboseOption = new Option<bool>(
        name: "--verbose",
        description: "Habilitar salida detallada",
        getDefaultValue: () => false);
    verboseOption.AddAlias("-v");

    // Agregar opciones al comando
    command.AddOption(dbOption);
    command.AddOption(fromOption);
    command.AddOption(hostOption);
    command.AddOption(portOption);
    command.AddOption(userOption);
    command.AddOption(passwordOption);
    command.AddOption(authDbOption);
    command.AddOption(uriOption);
    command.AddOption(inDockerOption);
    command.AddOption(containerNameOption);
    command.AddOption(dropOption);
    command.AddOption(verboseOption);

    // Handler del comando
    command.SetHandler(async (context) =>
    {
        var database = context.ParseResult.GetValueForOption(dbOption)!;
        var sourcePath = context.ParseResult.GetValueForOption(fromOption)!;
        var host = context.ParseResult.GetValueForOption(hostOption)!;
        var port = context.ParseResult.GetValueForOption(portOption);
        var username = context.ParseResult.GetValueForOption(userOption);
        var password = context.ParseResult.GetValueForOption(passwordOption);
        var authDb = context.ParseResult.GetValueForOption(authDbOption)!;
        var uri = context.ParseResult.GetValueForOption(uriOption);
        var inDocker = context.ParseResult.GetValueForOption(inDockerOption);
        var containerName = context.ParseResult.GetValueForOption(containerNameOption);
        var drop = context.ParseResult.GetValueForOption(dropOption);
        var verbose = context.ParseResult.GetValueForOption(verboseOption);

        // Configurar nivel de log según verbosidad
        if (verbose)
        {
            loggerFactory.CreateLogger("Root").LogInformation("Modo verbose activado");
        }

        var options = new RestoreOptions
        {
            Database = database,
            SourcePath = sourcePath,
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            AuthenticationDatabase = authDb,
            Uri = uri,
            InDocker = inDocker,
            ContainerName = containerName,
            Drop = drop,
            Verbose = verbose
        };

        try
        {
            var result = await restoreService.ExecuteRestoreAsync(options);

            if (result.Success)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ " + result.Message);
                Console.ResetColor();
                context.ExitCode = 0;
            }
            else
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ " + result.Message);
                Console.ResetColor();

                if (!string.IsNullOrWhiteSpace(result.Error) && verbose)
                {
                    Console.WriteLine();
                    Console.WriteLine("Detalles del error:");
                    Console.WriteLine(result.Error);
                }

                context.ExitCode = result.ExitCode;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Error inesperado: {ex.Message}");
            Console.ResetColor();

            if (verbose)
            {
                Console.WriteLine();
                Console.WriteLine("Detalles del error:");
                Console.WriteLine(ex.ToString());
            }

            context.ExitCode = 1;
        }
    });

    return command;
}
