using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Interfaces;
using MongoBackupRestore.Core.Services;
using Moq;

namespace MongoBackupRestore.Tests;

/// <summary>
/// Pruebas para el servicio MongoToolsValidator
/// </summary>
public class MongoToolsValidatorTests
{
    private readonly Mock<IProcessRunner> _mockProcessRunner;
    private readonly Mock<ILogger<MongoToolsValidator>> _mockLogger;
    private readonly MongoToolsValidator _validator;

    public MongoToolsValidatorTests()
    {
        _mockProcessRunner = new Mock<IProcessRunner>();
        _mockLogger = new Mock<ILogger<MongoToolsValidator>>();
        _validator = new MongoToolsValidator(_mockProcessRunner.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ValidateToolsAsync_MongoDumpDisponible_RetornaInformacionCorrecta()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongodump")),
                "--version",
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>(), It.IsAny<System.Action<string>?>(), It.IsAny<System.Action<string>?>()))
            .ReturnsAsync((0, "mongodump version: 100.9.5", ""));

        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongorestore")),
                "--version",
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>(), It.IsAny<System.Action<string>?>(), It.IsAny<System.Action<string>?>()))
            .ThrowsAsync(new Exception("Not found"));

        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                "docker",
                "--version",
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>(), It.IsAny<System.Action<string>?>(), It.IsAny<System.Action<string>?>()))
            .ThrowsAsync(new Exception("Not found"));

        // Act
        var result = await _validator.ValidateToolsAsync();

        // Assert
        result.MongoDumpAvailable.Should().BeTrue();
        result.MongoDumpVersion.Should().Be("100.9.5");
        result.MongoRestoreAvailable.Should().BeFalse();
        result.DockerAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateToolsAsync_TodasLasHerramientasDisponibles_RetornaTodasDisponibles()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongodump")),
                "--version",
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>(), It.IsAny<System.Action<string>?>(), It.IsAny<System.Action<string>?>()))
            .ReturnsAsync((0, "mongodump version: 100.9.5", ""));

        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.Is<string>(s => s.Contains("mongorestore")),
                "--version",
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>(), It.IsAny<System.Action<string>?>(), It.IsAny<System.Action<string>?>()))
            .ReturnsAsync((0, "mongorestore version: 100.9.5", ""));

        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                "docker",
                "--version",
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>(), It.IsAny<System.Action<string>?>(), It.IsAny<System.Action<string>?>()))
            .ReturnsAsync((0, "Docker version 24.0.7, build afdd53b", ""));

        // Act
        var result = await _validator.ValidateToolsAsync();

        // Assert
        result.MongoDumpAvailable.Should().BeTrue();
        result.MongoDumpVersion.Should().Be("100.9.5");
        result.MongoRestoreAvailable.Should().BeTrue();
        result.MongoRestoreVersion.Should().Be("100.9.5");
        result.DockerAvailable.Should().BeTrue();
        result.DockerVersion.Should().Be("24.0.7");
    }

    [Fact]
    public async Task ValidateToolsAsync_NingunaHerramientaDisponible_RetornaNingunaDisponible()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>(), It.IsAny<System.Action<string>?>(), It.IsAny<System.Action<string>?>()))
            .ThrowsAsync(new Exception("Not found"));

        // Act
        var result = await _validator.ValidateToolsAsync();

        // Assert
        result.MongoDumpAvailable.Should().BeFalse();
        result.MongoRestoreAvailable.Should().BeFalse();
        result.DockerAvailable.Should().BeFalse();
    }
}

