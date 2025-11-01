using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Models;
using MongoBackupRestore.Core.Services;
using Moq;

namespace MongoBackupRestore.Tests;

/// <summary>
/// Pruebas para el servicio RestoreService
/// </summary>
public class RestoreServiceTests : IDisposable
{
    private readonly Mock<Core.Interfaces.IProcessRunner> _mockProcessRunner;
    private readonly Mock<Core.Interfaces.IMongoToolsValidator> _mockToolsValidator;
    private readonly Mock<ILogger<RestoreService>> _mockLogger;
    private readonly RestoreService _restoreService;
    private readonly string _testSourcePath;

    public RestoreServiceTests()
    {
        _mockProcessRunner = new Mock<Core.Interfaces.IProcessRunner>();
        _mockToolsValidator = new Mock<Core.Interfaces.IMongoToolsValidator>();
        _mockLogger = new Mock<ILogger<RestoreService>>();

        _restoreService = new RestoreService(
            _mockProcessRunner.Object,
            _mockToolsValidator.Object,
            _mockLogger.Object);

        // Crear directorio temporal para pruebas
        _testSourcePath = Path.Combine(Path.GetTempPath(), "mongodb-restore-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_testSourcePath);
    }

    [Fact]
    public async Task ExecuteRestoreAsync_SinNombreDeBaseDeDatos_RetornaError()
    {
        // Arrange
        var options = new RestoreOptions
        {
            Database = "",
            SourcePath = _testSourcePath
        };

        // Act
        var result = await _restoreService.ExecuteRestoreAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("nombre de la base de datos es obligatorio");
        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteRestoreAsync_SinRutaDeOrigen_RetornaError()
    {
        // Arrange
        var options = new RestoreOptions
        {
            Database = "testdb",
            SourcePath = ""
        };

        // Act
        var result = await _restoreService.ExecuteRestoreAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("ruta de origen es obligatoria");
        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteRestoreAsync_EnDockerSinNombreContenedor_RetornaError()
    {
        // Arrange
        var options = new RestoreOptions
        {
            Database = "testdb",
            SourcePath = _testSourcePath,
            InDocker = true,
            ContainerName = ""
        };

        // Act
        var result = await _restoreService.ExecuteRestoreAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("nombre del contenedor es obligatorio");
        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteRestoreAsync_RutaOrigenNoExiste_RetornaError()
    {
        // Arrange
        var options = new RestoreOptions
        {
            Database = "testdb",
            SourcePath = "/ruta/que/no/existe"
        };

        var toolsInfo = new MongoToolsInfo
        {
            MongoRestoreAvailable = true
        };

        _mockToolsValidator
            .Setup(x => x.ValidateToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolsInfo);

        // Act
        var result = await _restoreService.ExecuteRestoreAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("ruta de origen no existe");
        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteRestoreAsync_MongoRestoreNoDisponible_RetornaError()
    {
        // Arrange
        var options = new RestoreOptions
        {
            Database = "testdb",
            SourcePath = _testSourcePath
        };

        var toolsInfo = new MongoToolsInfo
        {
            MongoRestoreAvailable = false
        };

        _mockToolsValidator
            .Setup(x => x.ValidateToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolsInfo);

        // Act
        var result = await _restoreService.ExecuteRestoreAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("mongorestore no está disponible");
        result.ExitCode.Should().Be(127);
    }

    [Fact]
    public async Task ExecuteRestoreAsync_DockerNoDisponible_RetornaError()
    {
        // Arrange
        var options = new RestoreOptions
        {
            Database = "testdb",
            SourcePath = _testSourcePath,
            InDocker = true,
            ContainerName = "mongo-container"
        };

        var toolsInfo = new MongoToolsInfo
        {
            DockerAvailable = false
        };

        _mockToolsValidator
            .Setup(x => x.ValidateToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolsInfo);

        // Act
        var result = await _restoreService.ExecuteRestoreAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Docker no está disponible");
        result.ExitCode.Should().Be(127);
    }

    [Fact]
    public async Task ExecuteRestoreAsync_NombreBaseDeDatosConCaracteresPeligrosos_RetornaError()
    {
        // Arrange
        var options = new RestoreOptions
        {
            Database = "test;db",
            SourcePath = _testSourcePath
        };

        // Act
        var result = await _restoreService.ExecuteRestoreAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("caracteres no permitidos");
        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteRestoreAsync_NombreContenedorConCaracteresPeligrosos_RetornaError()
    {
        // Arrange
        var options = new RestoreOptions
        {
            Database = "testdb",
            SourcePath = _testSourcePath,
            InDocker = true,
            ContainerName = "mongo;container"
        };

        // Act
        var result = await _restoreService.ExecuteRestoreAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("caracteres no permitidos");
        result.ExitCode.Should().Be(1);
    }

    public void Dispose()
    {
        // Limpiar directorio temporal
        if (Directory.Exists(_testSourcePath))
        {
            Directory.Delete(_testSourcePath, true);
        }
    }
}
