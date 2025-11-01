using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Services;
using Moq;

namespace MongoBackupRestore.Tests;

/// <summary>
/// Pruebas para el servicio DockerContainerDetector
/// </summary>
public class DockerContainerDetectorTests
{
    private readonly Mock<Core.Interfaces.IProcessRunner> _mockProcessRunner;
    private readonly Mock<ILogger<DockerContainerDetector>> _mockLogger;
    private readonly DockerContainerDetector _containerDetector;

    public DockerContainerDetectorTests()
    {
        _mockProcessRunner = new Mock<Core.Interfaces.IProcessRunner>();
        _mockLogger = new Mock<ILogger<DockerContainerDetector>>();

        _containerDetector = new DockerContainerDetector(
            _mockProcessRunner.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task DetectMongoContainersAsync_CuandoHayContenedores_RetornaLista()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                "docker",
                "ps --format \"{{.Names}}\" --filter \"ancestor=mongo\"",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "mongo-container\n", ""));

        // Act
        var result = await _containerDetector.DetectMongoContainersAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("mongo-container");
    }

    [Fact]
    public async Task DetectMongoContainersAsync_CuandoNoHayContenedores_RetornaListaVacia()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                "docker",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "", ""));

        // Act
        var result = await _containerDetector.DetectMongoContainersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateContainerAsync_ConContenedorEnEjecucion_RetornaExito()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                "docker",
                "inspect --format=\"{{.State.Running}}\" test-container",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "true\n", ""));

        // Act
        var (success, errorMessage) = await _containerDetector.ValidateContainerAsync("test-container");

        // Assert
        success.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateContainerAsync_ConContenedorDetenido_RetornaError()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                "docker",
                "inspect --format=\"{{.State.Running}}\" test-container",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "false\n", ""));

        // Act
        var (success, errorMessage) = await _containerDetector.ValidateContainerAsync("test-container");

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().Contain("no está en ejecución");
    }

    [Fact]
    public async Task ValidateContainerAsync_ConContenedorInexistente_RetornaError()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                "docker",
                "inspect --format=\"{{.State.Running}}\" inexistente",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, "", "Error: No such container"));

        // Act
        var (success, errorMessage) = await _containerDetector.ValidateContainerAsync("inexistente");

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().Contain("no existe");
    }

    [Fact]
    public async Task ValidateContainerAsync_ConNombreVacio_RetornaError()
    {
        // Act
        var (success, errorMessage) = await _containerDetector.ValidateContainerAsync("");

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().Contain("no puede estar vacío");
    }

    [Fact]
    public async Task ValidateMongoBinariesInContainerAsync_ConBinariosDisponibles_RetornaExito()
    {
        // Arrange - Contenedor existe y está en ejecución
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                "docker",
                "inspect --format=\"{{.State.Running}}\" test-container",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "true\n", ""));

        // Arrange - mongodump existe
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                "docker",
                "exec test-container sh -c \"command -v mongodump\"",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "/usr/bin/mongodump\n", ""));

        // Arrange - mongorestore existe
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                "docker",
                "exec test-container sh -c \"command -v mongorestore\"",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "/usr/bin/mongorestore\n", ""));

        // Act
        var (success, errorMessage) = await _containerDetector.ValidateMongoBinariesInContainerAsync(
            "test-container", checkMongoDump: true, checkMongoRestore: true);

        // Assert
        success.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateMongoBinariesInContainerAsync_SinMongoDump_RetornaError()
    {
        // Arrange - Contenedor existe y está en ejecución
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                "docker",
                "inspect --format=\"{{.State.Running}}\" test-container",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, "true\n", ""));

        // Arrange - mongodump no existe
        _mockProcessRunner
            .Setup(x => x.RunProcessAsync(
                "docker",
                "exec test-container sh -c \"command -v mongodump\"",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, "", "command not found"));

        // Act
        var (success, errorMessage) = await _containerDetector.ValidateMongoBinariesInContainerAsync(
            "test-container", checkMongoDump: true, checkMongoRestore: false);

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().Contain("mongodump");
    }

    [Fact]
    public async Task ValidateMongoBinariesInContainerAsync_ConNombreVacio_RetornaError()
    {
        // Act
        var (success, errorMessage) = await _containerDetector.ValidateMongoBinariesInContainerAsync("");

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().Contain("no puede estar vacío");
    }
}
