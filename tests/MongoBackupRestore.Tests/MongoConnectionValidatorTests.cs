using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Services;
using Moq;

namespace MongoBackupRestore.Tests;

/// <summary>
/// Pruebas para el servicio MongoConnectionValidator
/// </summary>
public class MongoConnectionValidatorTests
{
    private readonly Mock<Core.Interfaces.IProcessRunner> _mockProcessRunner;
    private readonly Mock<ILogger<MongoConnectionValidator>> _mockLogger;
    private readonly MongoConnectionValidator _validator;

    public MongoConnectionValidatorTests()
    {
        _mockProcessRunner = new Mock<Core.Interfaces.IProcessRunner>();
        _mockLogger = new Mock<ILogger<MongoConnectionValidator>>();
        _validator = new MongoConnectionValidator(_mockProcessRunner.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ValidateConnectionAsync_SinMongoShellDisponible_RetornaExito()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, "", "command not found"));

        // Act
        var (success, errorMessage) = await _validator.ValidateConnectionAsync(
            "localhost",
            27017,
            "user",
            "password",
            "admin",
            null);

        // Assert
        success.Should().BeTrue("porque si no hay mongosh/mongo disponible, no se bloquea la operación");
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateConnectionAsync_ConexionExitosa_RetornaExito()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongosh") || s.Contains("mongo")),
                "--version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "MongoDB shell version v6.0.0", ""));

        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongosh") || s.Contains("mongo")),
                It.Is<string>(s => s.Contains("db.adminCommand('ping')")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "{ ok: 1 }", ""));

        // Act
        var (success, errorMessage) = await _validator.ValidateConnectionAsync(
            "localhost",
            27017,
            "user",
            "password",
            "admin",
            null);

        // Assert
        success.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateConnectionAsync_ErrorAutenticacion_RetornaMensajeClaro()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongosh") || s.Contains("mongo")),
                "--version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "MongoDB shell version v6.0.0", ""));

        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongosh") || s.Contains("mongo")),
                It.Is<string>(s => s.Contains("db.adminCommand('ping')")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, "", "MongoServerError: Authentication failed."));

        // Act
        var (success, errorMessage) = await _validator.ValidateConnectionAsync(
            "localhost",
            27017,
            "user",
            "wrongpassword",
            "admin",
            null);

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().NotBeNull();
        errorMessage.Should().Contain("autenticación");
        errorMessage.Should().Contain("credenciales");
    }

    [Fact]
    public async Task ValidateConnectionAsync_ErrorConexion_RetornaMensajeClaro()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongosh") || s.Contains("mongo")),
                "--version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "MongoDB shell version v6.0.0", ""));

        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongosh") || s.Contains("mongo")),
                It.Is<string>(s => s.Contains("db.adminCommand('ping')")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, "", "MongoNetworkError: connect ECONNREFUSED 127.0.0.1:27017"));

        // Act
        var (success, errorMessage) = await _validator.ValidateConnectionAsync(
            "localhost",
            27017,
            null,
            null,
            "admin",
            null);

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().NotBeNull();
        errorMessage.Should().Contain("conexión");
        errorMessage.Should().Contain("servidor MongoDB");
    }

    [Fact]
    public async Task ValidateConnectionAsync_ErrorTimeout_RetornaMensajeClaro()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongosh") || s.Contains("mongo")),
                "--version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "MongoDB shell version v6.0.0", ""));

        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongosh") || s.Contains("mongo")),
                It.Is<string>(s => s.Contains("db.adminCommand('ping')")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, "", "MongoNetworkError: connection timed out"));

        // Act
        var (success, errorMessage) = await _validator.ValidateConnectionAsync(
            "remote.example.com",
            27017,
            null,
            null,
            "admin",
            null);

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().NotBeNull();
        errorMessage.Should().Contain("Tiempo de espera agotado");
    }

    [Fact]
    public async Task ValidateConnectionAsync_ConURI_UsaURIEnValidacion()
    {
        // Arrange
        var uri = "mongodb://user:password@localhost:27017/admin";
        
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongosh") || s.Contains("mongo")),
                "--version",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "MongoDB shell version v6.0.0", ""));

        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongosh") || s.Contains("mongo")),
                It.Is<string>(s => s.Contains(uri)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "{ ok: 1 }", ""));

        // Act
        var (success, errorMessage) = await _validator.ValidateConnectionAsync(
            "localhost",
            27017,
            null,
            null,
            "admin",
            uri);

        // Assert
        success.Should().BeTrue();
        errorMessage.Should().BeNull();
    }
}
