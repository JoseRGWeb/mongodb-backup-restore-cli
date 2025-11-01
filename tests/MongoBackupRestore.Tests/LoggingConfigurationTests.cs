using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Services;

namespace MongoBackupRestore.Tests;

/// <summary>
/// Tests para LoggingConfiguration
/// </summary>
public class LoggingConfigurationTests
{
    [Fact]
    public void GetLogLevel_ConModoVerbose_RetornaDebug()
    {
        // Arrange
        var verbose = true;

        // Act
        var logLevel = LoggingConfiguration.GetLogLevel(verbose);

        // Assert
        logLevel.Should().Be(LogLevel.Debug);
    }

    [Fact]
    public void GetLogLevel_SinModoVerbose_RetornaInformation()
    {
        // Arrange
        var verbose = false;

        // Act
        var logLevel = LoggingConfiguration.GetLogLevel(verbose);

        // Assert
        logLevel.Should().Be(LogLevel.Information);
    }

    [Theory]
    [InlineData("trace", LogLevel.Trace)]
    [InlineData("debug", LogLevel.Debug)]
    [InlineData("information", LogLevel.Information)]
    [InlineData("info", LogLevel.Information)]
    [InlineData("warning", LogLevel.Warning)]
    [InlineData("warn", LogLevel.Warning)]
    [InlineData("error", LogLevel.Error)]
    [InlineData("critical", LogLevel.Critical)]
    [InlineData("none", LogLevel.None)]
    public void GetLogLevel_ConVariableEntorno_RetornaNivelCorrecto(string envValue, LogLevel expectedLevel)
    {
        // Arrange
        var verbose = false;

        // Act
        var logLevel = LoggingConfiguration.GetLogLevel(verbose, envValue);

        // Assert
        logLevel.Should().Be(expectedLevel);
    }

    [Fact]
    public void GetLogLevel_ConVariableEntornoInvalida_RetornaInformation()
    {
        // Arrange
        var verbose = false;
        var invalidEnvValue = "invalid_level";

        // Act
        var logLevel = LoggingConfiguration.GetLogLevel(verbose, invalidEnvValue);

        // Assert
        logLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void GetLogLevel_VerboseTienePrioridadSobreVariableEntorno()
    {
        // Arrange
        var verbose = true;
        var envValue = "error"; // Variable de entorno sugiere Error

        // Act
        var logLevel = LoggingConfiguration.GetLogLevel(verbose, envValue);

        // Assert
        // Verbose tiene prioridad, debería ser Debug
        logLevel.Should().Be(LogLevel.Debug);
    }

    [Fact]
    public void CreateLoggerFactory_ConNivelInformation_CreaFactoryCorrectamente()
    {
        // Arrange
        var logLevel = LogLevel.Information;

        // Act
        using var loggerFactory = LoggingConfiguration.CreateLoggerFactory(logLevel);
        var logger = loggerFactory.CreateLogger("Test");

        // Assert
        loggerFactory.Should().NotBeNull();
        logger.Should().NotBeNull();
    }

    [Fact]
    public void CreateLoggerFactory_ConArchivoLog_CreaFactoryConArchivoCorrectamente()
    {
        // Arrange
        var logLevel = LogLevel.Information;
        var logFilePath = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid()}.txt");

        try
        {
            // Act
            using var loggerFactory = LoggingConfiguration.CreateLoggerFactory(logLevel, logFilePath);
            var logger = loggerFactory.CreateLogger("Test");
            logger.LogInformation("Mensaje de prueba");

            // Assert
            loggerFactory.Should().NotBeNull();
            logger.Should().NotBeNull();
            
            // Dar tiempo para que el log se escriba
            Thread.Sleep(100);
            
            // Verificar que el archivo existe
            File.Exists(logFilePath).Should().BeTrue();
            
            // Verificar que contiene contenido
            var logContent = File.ReadAllText(logFilePath);
            logContent.Should().Contain("Mensaje de prueba");
            logContent.Should().Contain("INFO");
            logContent.Should().Contain("Test");
        }
        finally
        {
            // Cleanup
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }
    }

    [Fact]
    public void CreateLoggerFactory_ConArchivoEnDirectorioNoExistente_CreaDirectorioYArchivo()
    {
        // Arrange
        var logLevel = LogLevel.Information;
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_logs_{Guid.NewGuid()}");
        var logFilePath = Path.Combine(tempDir, "app.log");

        try
        {
            // Act
            using var loggerFactory = LoggingConfiguration.CreateLoggerFactory(logLevel, logFilePath);
            var logger = loggerFactory.CreateLogger("Test");
            logger.LogInformation("Mensaje de prueba");

            // Assert
            Directory.Exists(tempDir).Should().BeTrue();
            
            // Dar tiempo para que el log se escriba
            Thread.Sleep(100);
            
            File.Exists(logFilePath).Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void CreateLoggerFactory_ConDiferentesNiveles_ConfiguraNivelCorrectamente(LogLevel logLevel)
    {
        // Arrange & Act
        using var loggerFactory = LoggingConfiguration.CreateLoggerFactory(logLevel);
        var logger = loggerFactory.CreateLogger("Test");

        // Assert
        loggerFactory.Should().NotBeNull();
        logger.Should().NotBeNull();
        
        // Verificar que se pueden escribir logs del nivel configurado o superior
        logger.IsEnabled(logLevel).Should().BeTrue();
        
        // Verificar que niveles inferiores no están habilitados (excepto Trace que es el mínimo)
        if (logLevel > LogLevel.Trace)
        {
            logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        }
    }
}
