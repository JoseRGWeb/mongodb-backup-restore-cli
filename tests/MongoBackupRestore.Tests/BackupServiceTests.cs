using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Models;
using MongoBackupRestore.Core.Services;
using Moq;

namespace MongoBackupRestore.Tests;

/// <summary>
/// Pruebas para el servicio BackupService
/// </summary>
public class BackupServiceTests
{
    private readonly Mock<Core.Interfaces.IProcessRunner> _mockProcessRunner;
    private readonly Mock<Core.Interfaces.IMongoToolsValidator> _mockToolsValidator;
    private readonly Mock<ILogger<BackupService>> _mockLogger;
    private readonly BackupService _backupService;

    public BackupServiceTests()
    {
        _mockProcessRunner = new Mock<Core.Interfaces.IProcessRunner>();
        _mockToolsValidator = new Mock<Core.Interfaces.IMongoToolsValidator>();
        _mockLogger = new Mock<ILogger<BackupService>>();

        _backupService = new BackupService(
            _mockProcessRunner.Object,
            _mockToolsValidator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteBackupAsync_SinNombreDeBaseDeDatos_RetornaError()
    {
        // Arrange
        var options = new BackupOptions
        {
            Database = "",
            OutputPath = "/tmp/backup"
        };

        // Act
        var result = await _backupService.ExecuteBackupAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("nombre de la base de datos es obligatorio");
        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteBackupAsync_SinRutaDeSalida_RetornaError()
    {
        // Arrange
        var options = new BackupOptions
        {
            Database = "testdb",
            OutputPath = ""
        };

        // Act
        var result = await _backupService.ExecuteBackupAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("ruta de salida es obligatoria");
        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteBackupAsync_EnDockerSinNombreContenedor_RetornaError()
    {
        // Arrange
        var options = new BackupOptions
        {
            Database = "testdb",
            OutputPath = "/tmp/backup",
            InDocker = true,
            ContainerName = ""
        };

        // Act
        var result = await _backupService.ExecuteBackupAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("nombre del contenedor es obligatorio");
        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteBackupAsync_MongoDumpNoDisponible_RetornaError()
    {
        // Arrange
        var options = new BackupOptions
        {
            Database = "testdb",
            OutputPath = "/tmp/backup"
        };

        var toolsInfo = new MongoToolsInfo
        {
            MongoDumpAvailable = false
        };

        _mockToolsValidator
            .Setup(x => x.ValidateToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolsInfo);

        // Act
        var result = await _backupService.ExecuteBackupAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("mongodump no está disponible");
        result.ExitCode.Should().Be(127);
    }

    [Fact]
    public async Task ExecuteBackupAsync_DockerNoDisponible_RetornaError()
    {
        // Arrange
        var options = new BackupOptions
        {
            Database = "testdb",
            OutputPath = "/tmp/backup",
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
        var result = await _backupService.ExecuteBackupAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Docker no está disponible");
        result.ExitCode.Should().Be(127);
    }

    [Fact]
    public async Task ExecuteBackupAsync_ConCredencialesInvalidas_ValidaYRetornaError()
    {
        // Arrange
        var mockConnectionValidator = new Mock<Core.Interfaces.IMongoConnectionValidator>();
        var backupService = new BackupService(
            _mockProcessRunner.Object,
            _mockToolsValidator.Object,
            _mockLogger.Object,
            mockConnectionValidator.Object);

        var options = new BackupOptions
        {
            Database = "testdb",
            OutputPath = "/tmp/backup",
            Username = "admin",
            Password = "wrongpassword",
            AuthenticationDatabase = "admin"
        };

        var toolsInfo = new MongoToolsInfo
        {
            MongoDumpAvailable = true
        };

        _mockToolsValidator
            .Setup(x => x.ValidateToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolsInfo);

        mockConnectionValidator
            .Setup(x => x.ValidateConnectionAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Error de autenticación: Las credenciales son incorrectas"));

        // Act
        var result = await backupService.ExecuteBackupAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("autenticación");
        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteBackupAsync_ConCredencialesValidas_ProcedeConBackup()
    {
        // Arrange
        var mockConnectionValidator = new Mock<Core.Interfaces.IMongoConnectionValidator>();
        var backupService = new BackupService(
            _mockProcessRunner.Object,
            _mockToolsValidator.Object,
            _mockLogger.Object,
            mockConnectionValidator.Object);

        var options = new BackupOptions
        {
            Database = "testdb",
            OutputPath = "/tmp/backup",
            Username = "admin",
            Password = "correctpassword",
            AuthenticationDatabase = "admin"
        };

        var toolsInfo = new MongoToolsInfo
        {
            MongoDumpAvailable = true
        };

        _mockToolsValidator
            .Setup(x => x.ValidateToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolsInfo);

        mockConnectionValidator
            .Setup(x => x.ValidateConnectionAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "backup completed", ""));

        // Act
        var result = await backupService.ExecuteBackupAsync(options);

        // Assert
        result.Success.Should().BeTrue();
        mockConnectionValidator.Verify(x => x.ValidateConnectionAsync(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
