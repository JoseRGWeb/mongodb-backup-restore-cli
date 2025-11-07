using System.CommandLine;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;
using MongoBackupRestore.Core.Models;
using MongoBackupRestore.Core.Services;

// Crear comando raíz
var rootCommand = new RootCommand("MongoDB Backup & Restore CLI - Herramienta para gestionar copias de seguridad de MongoDB");

// Crear comando backup
var backupCommand = CreateBackupCommand();
rootCommand.AddCommand(backupCommand);

// Crear comando restore
var restoreCommand = CreateRestoreCommand();
rootCommand.AddCommand(restoreCommand);

// Ejecutar CLI
return await rootCommand.InvokeAsync(args);

static Command CreateBackupCommand()
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
        description: "Nombre del contenedor Docker (si no se especifica, se intentará detectar automáticamente)");
    containerNameOption.AddAlias("-c");

    // Opciones de verbosidad
    var verboseOption = new Option<bool>(
        name: "--verbose",
        description: "Habilitar salida detallada",
        getDefaultValue: () => false);
    verboseOption.AddAlias("-v");

    // Opciones de compresión
    var compressOption = new Option<string?>(
        name: "--compress",
        description: "Formato de compresión para el backup (none, zip, targz)",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_COMPRESSION") ?? "none");

    // Opciones de retención
    var retentionDaysOption = new Option<int?>(
        name: "--retention-days",
        description: "Número de días para retener backups. Los backups más antiguos serán eliminados automáticamente.",
        getDefaultValue: () =>
        {
            var retentionStr = Environment.GetEnvironmentVariable("MONGO_RETENTION_DAYS");
            return int.TryParse(retentionStr, out var days) ? days : null;
        });
    retentionDaysOption.AddAlias("-r");

    // Opciones de cifrado
    var encryptOption = new Option<bool>(
        name: "--encrypt",
        description: "Cifrar el backup usando AES-256",
        getDefaultValue: () => false);
    encryptOption.AddAlias("-e");

    var encryptionKeyOption = new Option<string?>(
        name: "--encryption-key",
        description: "Clave de cifrado AES-256 (mínimo 16 caracteres). También se puede usar la variable de entorno MONGO_ENCRYPTION_KEY",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_ENCRYPTION_KEY"));
    encryptionKeyOption.AddAlias("-k");

    // Opciones de logging
    var logFileOption = new Option<string?>(
        name: "--log-file",
        description: "Ruta del archivo donde guardar los logs. También se puede usar la variable de entorno MONGO_LOG_FILE",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_LOG_FILE"));

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
    command.AddOption(compressOption);
    command.AddOption(retentionDaysOption);
    command.AddOption(encryptOption);
    command.AddOption(encryptionKeyOption);
    command.AddOption(logFileOption);

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
        var compress = context.ParseResult.GetValueForOption(compressOption);
        var retentionDays = context.ParseResult.GetValueForOption(retentionDaysOption);
        var encrypt = context.ParseResult.GetValueForOption(encryptOption);
        var encryptionKey = context.ParseResult.GetValueForOption(encryptionKeyOption);
        var logFile = context.ParseResult.GetValueForOption(logFileOption);

        // Configurar nivel de log según verbosidad
        var logLevelEnv = Environment.GetEnvironmentVariable("MONGO_LOG_LEVEL");
        var logLevel = LoggingConfiguration.GetLogLevel(verbose, logLevelEnv);
        
        // Crear logger factory con configuración dinámica
        using var loggerFactory = LoggingConfiguration.CreateLoggerFactory(logLevel, logFile);
        var logger = loggerFactory.CreateLogger("BackupCommand");
        
        if (verbose)
        {
            logger.LogDebug("Modo verbose activado");
        }
        
        if (!string.IsNullOrWhiteSpace(logFile))
        {
            logger.LogInformation("Logs guardándose en archivo: {LogFile}", logFile);
        }

        // Crear servicios con el logger factory configurado
        var processRunner = new ProcessRunner(loggerFactory.CreateLogger<ProcessRunner>());
        var toolsValidator = new MongoToolsValidator(processRunner, loggerFactory.CreateLogger<MongoToolsValidator>());
        var connectionValidator = new MongoConnectionValidator(processRunner, loggerFactory.CreateLogger<MongoConnectionValidator>());
        var containerDetector = new DockerContainerDetector(processRunner, loggerFactory.CreateLogger<DockerContainerDetector>());
        var compressionService = new CompressionService(loggerFactory.CreateLogger<CompressionService>(), processRunner);
        var encryptionService = new EncryptionService(loggerFactory.CreateLogger<EncryptionService>());
        var retentionService = new BackupRetentionService(loggerFactory.CreateLogger<BackupRetentionService>());
        var progressService = new ConsoleProgressService(loggerFactory.CreateLogger<ConsoleProgressService>(), verbose);
        var backupService = new BackupService(processRunner, toolsValidator, loggerFactory.CreateLogger<BackupService>(), connectionValidator, containerDetector, compressionService, retentionService, encryptionService, progressService);

        // Parsear formato de compresión
        var compressionFormat = CompressionFormat.None;
        if (!string.IsNullOrWhiteSpace(compress))
        {
            compressionFormat = compress.ToLowerInvariant() switch
            {
                "zip" => CompressionFormat.Zip,
                "targz" or "tar.gz" or "tgz" => CompressionFormat.TarGz,
                "none" => CompressionFormat.None,
                _ => CompressionFormat.None
            };
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
            Verbose = verbose,
            CompressionFormat = compressionFormat,
            RetentionDays = retentionDays,
            Encrypt = encrypt,
            EncryptionKey = encryptionKey
        };

        try
        {
            var result = await backupService.ExecuteBackupAsync(options);

            if (result.Success)
            {
                Console.WriteLine();
                progressService.ShowSuccess(result.Message);
                
                if (!string.IsNullOrWhiteSpace(result.BackupPath))
                {
                    progressService.ShowInfo($"Ruta del backup: {result.BackupPath}");
                }
                
                context.ExitCode = 0;
            }
            else
            {
                Console.WriteLine();
                progressService.ShowError(result.Message);

                if (!string.IsNullOrWhiteSpace(result.Error) && verbose)
                {
                    Console.WriteLine();
                    progressService.ShowInfo("Detalles del error:");
                    Console.WriteLine(result.Error);
                }

                context.ExitCode = result.ExitCode;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            progressService.ShowError($"Error inesperado: {ex.Message}");

            if (verbose)
            {
                Console.WriteLine();
                progressService.ShowInfo("Detalles del error:");
                Console.WriteLine(ex.ToString());
            }

            context.ExitCode = 1;
        }
    });

    return command;
}

static Command CreateRestoreCommand()
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
        description: "Nombre del contenedor Docker (si no se especifica, se intentará detectar automáticamente)");
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

    // Opciones de compresión (para auto-detección o especificar formato)
    var compressOption = new Option<string?>(
        name: "--compress",
        description: "Formato de compresión del backup (none, zip, targz). Se auto-detecta si no se especifica.",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_COMPRESSION") ?? "none");

    // Opciones de cifrado
    var encryptionKeyOption = new Option<string?>(
        name: "--encryption-key",
        description: "Clave de cifrado para descifrar el backup (si está cifrado). También se puede usar la variable de entorno MONGO_ENCRYPTION_KEY",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_ENCRYPTION_KEY"));
    encryptionKeyOption.AddAlias("-k");

    // Opciones de logging
    var logFileOption = new Option<string?>(
        name: "--log-file",
        description: "Ruta del archivo donde guardar los logs. También se puede usar la variable de entorno MONGO_LOG_FILE",
        getDefaultValue: () => Environment.GetEnvironmentVariable("MONGO_LOG_FILE"));

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
    command.AddOption(compressOption);
    command.AddOption(encryptionKeyOption);
    command.AddOption(logFileOption);

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
        var compress = context.ParseResult.GetValueForOption(compressOption);
        var encryptionKey = context.ParseResult.GetValueForOption(encryptionKeyOption);
        var logFile = context.ParseResult.GetValueForOption(logFileOption);

        // Configurar nivel de log según verbosidad
        var logLevelEnv = Environment.GetEnvironmentVariable("MONGO_LOG_LEVEL");
        var logLevel = LoggingConfiguration.GetLogLevel(verbose, logLevelEnv);
        
        // Crear logger factory con configuración dinámica
        using var loggerFactory = LoggingConfiguration.CreateLoggerFactory(logLevel, logFile);
        var logger = loggerFactory.CreateLogger("RestoreCommand");
        
        if (verbose)
        {
            logger.LogDebug("Modo verbose activado");
        }
        
        if (!string.IsNullOrWhiteSpace(logFile))
        {
            logger.LogInformation("Logs guardándose en archivo: {LogFile}", logFile);
        }

        // Crear servicios con el logger factory configurado
        var processRunner = new ProcessRunner(loggerFactory.CreateLogger<ProcessRunner>());
        var toolsValidator = new MongoToolsValidator(processRunner, loggerFactory.CreateLogger<MongoToolsValidator>());
        var connectionValidator = new MongoConnectionValidator(processRunner, loggerFactory.CreateLogger<MongoConnectionValidator>());
        var containerDetector = new DockerContainerDetector(processRunner, loggerFactory.CreateLogger<DockerContainerDetector>());
        var compressionService = new CompressionService(loggerFactory.CreateLogger<CompressionService>(), processRunner);
        var encryptionService = new EncryptionService(loggerFactory.CreateLogger<EncryptionService>());
        var progressService = new ConsoleProgressService(loggerFactory.CreateLogger<ConsoleProgressService>(), verbose);
        var restoreService = new RestoreService(processRunner, toolsValidator, loggerFactory.CreateLogger<RestoreService>(), connectionValidator, containerDetector, compressionService, encryptionService, progressService);

        // Parsear formato de compresión
        var compressionFormat = CompressionFormat.None;
        if (!string.IsNullOrWhiteSpace(compress))
        {
            compressionFormat = compress.ToLowerInvariant() switch
            {
                "zip" => CompressionFormat.Zip,
                "targz" or "tar.gz" or "tgz" => CompressionFormat.TarGz,
                "none" => CompressionFormat.None,
                _ => CompressionFormat.None
            };
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
            Verbose = verbose,
            CompressionFormat = compressionFormat,
            EncryptionKey = encryptionKey
        };

        try
        {
            var result = await restoreService.ExecuteRestoreAsync(options);

            if (result.Success)
            {
                Console.WriteLine();
                progressService.ShowSuccess(result.Message);
                context.ExitCode = 0;
            }
            else
            {
                Console.WriteLine();
                progressService.ShowError(result.Message);

                if (!string.IsNullOrWhiteSpace(result.Error) && verbose)
                {
                    Console.WriteLine();
                    progressService.ShowInfo("Detalles del error:");
                    Console.WriteLine(result.Error);
                }

                context.ExitCode = result.ExitCode;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            progressService.ShowError($"Error inesperado: {ex.Message}");

            if (verbose)
            {
                Console.WriteLine();
                progressService.ShowInfo("Detalles del error:");
                Console.WriteLine(ex.ToString());
            }

            context.ExitCode = 1;
        }
    });

    return command;
}
