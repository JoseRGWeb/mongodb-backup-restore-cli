using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Models;
using MongoBackupRestore.Core.Services;
using Moq;

namespace MongoBackupRestore.Tests;

/// <summary>
/// Pruebas para el servicio CompressionService
/// </summary>
public class CompressionServiceTests : IDisposable
{
    private readonly Mock<ILogger<CompressionService>> _mockLogger;
    private readonly Mock<Core.Interfaces.IProcessRunner> _mockProcessRunner;
    private readonly CompressionService _compressionService;
    private readonly string _testDirectory;

    public CompressionServiceTests()
    {
        _mockLogger = new Mock<ILogger<CompressionService>>();
        _mockProcessRunner = new Mock<Core.Interfaces.IProcessRunner>();
        _compressionService = new CompressionService(_mockLogger.Object, _mockProcessRunner.Object);
        
        // Crear directorio temporal para pruebas
        _testDirectory = Path.Combine(Path.GetTempPath(), $"compression-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        // Limpiar directorio temporal
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public void GetFileExtension_ConFormatoZip_RetornaExtensionZip()
    {
        // Act
        var extension = _compressionService.GetFileExtension(CompressionFormat.Zip);

        // Assert
        extension.Should().Be(".zip");
    }

    [Fact]
    public void GetFileExtension_ConFormatoTarGz_RetornaExtensionTarGz()
    {
        // Act
        var extension = _compressionService.GetFileExtension(CompressionFormat.TarGz);

        // Assert
        extension.Should().Be(".tar.gz");
    }

    [Fact]
    public void GetFileExtension_ConFormatoNone_RetornaVacio()
    {
        // Act
        var extension = _compressionService.GetFileExtension(CompressionFormat.None);

        // Assert
        extension.Should().BeEmpty();
    }

    [Fact]
    public void DetectFormat_ConArchivoZip_RetornaFormatoZip()
    {
        // Arrange
        var filePath = "/path/to/backup.zip";

        // Act
        var format = _compressionService.DetectFormat(filePath);

        // Assert
        format.Should().Be(CompressionFormat.Zip);
    }

    [Fact]
    public void DetectFormat_ConArchivoTarGz_RetornaFormatoTarGz()
    {
        // Arrange
        var filePath = "/path/to/backup.tar.gz";

        // Act
        var format = _compressionService.DetectFormat(filePath);

        // Assert
        format.Should().Be(CompressionFormat.TarGz);
    }

    [Fact]
    public void DetectFormat_ConArchivoGz_RetornaFormatoTarGz()
    {
        // Arrange
        var filePath = "/path/to/backup.gz";

        // Act
        var format = _compressionService.DetectFormat(filePath);

        // Assert
        format.Should().Be(CompressionFormat.TarGz);
    }

    [Fact]
    public void DetectFormat_ConArchivoSinExtension_RetornaFormatoNone()
    {
        // Arrange
        var filePath = "/path/to/backup";

        // Act
        var format = _compressionService.DetectFormat(filePath);

        // Assert
        format.Should().Be(CompressionFormat.None);
    }

    [Fact]
    public async Task CompressAsync_ConFormatoNone_LanzaExcepcion()
    {
        // Arrange
        var sourceDir = Path.Combine(_testDirectory, "source");
        Directory.CreateDirectory(sourceDir);
        var destFile = Path.Combine(_testDirectory, "backup");

        // Act
        Func<Task> act = async () => await _compressionService.CompressAsync(
            sourceDir,
            destFile,
            CompressionFormat.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*formato de compresión no puede ser None*");
    }

    [Fact]
    public async Task CompressAsync_ConDirectorioInexistente_LanzaExcepcion()
    {
        // Arrange
        var sourceDir = Path.Combine(_testDirectory, "nonexistent");
        var destFile = Path.Combine(_testDirectory, "backup");

        // Act
        Func<Task> act = async () => await _compressionService.CompressAsync(
            sourceDir,
            destFile,
            CompressionFormat.Zip);

        // Assert
        await act.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task CompressAsync_ConFormatoZip_CreaArchivoZip()
    {
        // Arrange
        var sourceDir = Path.Combine(_testDirectory, "source");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "test.txt"), "contenido de prueba");
        
        var destFile = Path.Combine(_testDirectory, "backup");

        // Act
        var result = await _compressionService.CompressAsync(
            sourceDir,
            destFile,
            CompressionFormat.Zip);

        // Assert
        result.Should().EndWith(".zip");
        File.Exists(result).Should().BeTrue();
    }

    [Fact]
    public async Task CompressAsync_ConFormatoTarGz_CreaArchivoTarGz()
    {
        // Arrange
        var sourceDir = Path.Combine(_testDirectory, "source");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "test.txt"), "contenido de prueba");
        
        var destFile = Path.Combine(_testDirectory, "backup");

        // Act
        var result = await _compressionService.CompressAsync(
            sourceDir,
            destFile,
            CompressionFormat.TarGz);

        // Assert
        result.Should().EndWith(".tar.gz");
        File.Exists(result).Should().BeTrue();
    }

    [Fact]
    public async Task DecompressAsync_ConArchivoInexistente_LanzaExcepcion()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDirectory, "nonexistent.zip");
        var destDir = Path.Combine(_testDirectory, "dest");

        // Act
        Func<Task> act = async () => await _compressionService.DecompressAsync(
            sourceFile,
            destDir);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task CompressYDecompress_ConFormatoZip_RestauranArchivos()
    {
        // Arrange
        var sourceDir = Path.Combine(_testDirectory, "source");
        Directory.CreateDirectory(sourceDir);
        var testContent = "contenido de prueba";
        File.WriteAllText(Path.Combine(sourceDir, "test.txt"), testContent);
        
        var destFile = Path.Combine(_testDirectory, "backup");
        var extractDir = Path.Combine(_testDirectory, "extracted");

        // Act - Comprimir
        var compressedFile = await _compressionService.CompressAsync(
            sourceDir,
            destFile,
            CompressionFormat.Zip);

        // Act - Descomprimir
        var success = await _compressionService.DecompressAsync(
            compressedFile,
            extractDir);

        // Assert
        success.Should().BeTrue();
        Directory.Exists(extractDir).Should().BeTrue();
        var extractedFile = Path.Combine(extractDir, "test.txt");
        File.Exists(extractedFile).Should().BeTrue();
        File.ReadAllText(extractedFile).Should().Be(testContent);
    }

    [Fact]
    public async Task CompressYDecompress_ConFormatoTarGz_RestauranArchivos()
    {
        // Arrange
        var sourceDir = Path.Combine(_testDirectory, "source");
        Directory.CreateDirectory(sourceDir);
        var testContent = "contenido de prueba";
        File.WriteAllText(Path.Combine(sourceDir, "test.txt"), testContent);
        
        var destFile = Path.Combine(_testDirectory, "backup");
        var extractDir = Path.Combine(_testDirectory, "extracted");

        // Act - Comprimir
        var compressedFile = await _compressionService.CompressAsync(
            sourceDir,
            destFile,
            CompressionFormat.TarGz);

        // Act - Descomprimir
        var success = await _compressionService.DecompressAsync(
            compressedFile,
            extractDir);

        // Assert
        success.Should().BeTrue();
        Directory.Exists(extractDir).Should().BeTrue();
        var extractedFile = Path.Combine(extractDir, "test.txt");
        File.Exists(extractedFile).Should().BeTrue();
        File.ReadAllText(extractedFile).Should().Be(testContent);
    }
}
