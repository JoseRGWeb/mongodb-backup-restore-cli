using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Services;
using Moq;

namespace MongoBackupRestore.Tests;

/// <summary>
/// Tests para el servicio de cifrado de backups
/// </summary>
public class EncryptionServiceTests : IDisposable
{
    private readonly EncryptionService _encryptionService;
    private readonly Mock<ILogger<EncryptionService>> _mockLogger;
    private readonly string _testDirectory;
    private readonly List<string> _filesToCleanup;

    public EncryptionServiceTests()
    {
        _mockLogger = new Mock<ILogger<EncryptionService>>();
        _encryptionService = new EncryptionService(_mockLogger.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"encryption-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _filesToCleanup = new List<string>();
    }

    public void Dispose()
    {
        // Limpiar archivos de prueba
        foreach (var file in _filesToCleanup)
        {
            if (File.Exists(file))
            {
                try { File.Delete(file); } catch { }
            }
        }

        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, true); } catch { }
        }
    }

    [Fact]
    public void ValidateEncryptionKey_WithValidKey_ReturnsTrue()
    {
        // Arrange
        var key = "MiClaveSegura123456";

        // Act
        var (isValid, errorMessage) = _encryptionService.ValidateEncryptionKey(key);

        // Assert
        isValid.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateEncryptionKey_WithShortKey_ReturnsFalse()
    {
        // Arrange
        var key = "corta";

        // Act
        var (isValid, errorMessage) = _encryptionService.ValidateEncryptionKey(key);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("al menos 16 caracteres");
    }

    [Fact]
    public void ValidateEncryptionKey_WithNullKey_ReturnsFalse()
    {
        // Arrange
        string? key = null;

        // Act
        var (isValid, errorMessage) = _encryptionService.ValidateEncryptionKey(key);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("no puede estar vacía");
    }

    [Fact]
    public void ValidateEncryptionKey_WithEmptyKey_ReturnsFalse()
    {
        // Arrange
        var key = "";

        // Act
        var (isValid, errorMessage) = _encryptionService.ValidateEncryptionKey(key);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("no puede estar vacía");
    }

    [Fact]
    public async Task EncryptFileAsync_WithValidFile_CreatesEncryptedFile()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDirectory, "test-source.txt");
        var destinationFile = Path.Combine(_testDirectory, "test-encrypted");
        var encryptionKey = "MiClaveSegura123456";
        var testContent = "Este es un contenido de prueba para cifrar.";

        await File.WriteAllTextAsync(sourceFile, testContent);
        _filesToCleanup.Add(sourceFile);

        // Act
        var encryptedPath = await _encryptionService.EncryptFileAsync(
            sourceFile,
            destinationFile,
            encryptionKey);

        _filesToCleanup.Add(encryptedPath);

        // Assert
        File.Exists(encryptedPath).Should().BeTrue();
        encryptedPath.Should().EndWith(".encrypted");
        
        // El archivo cifrado debe ser diferente al original
        var encryptedContent = await File.ReadAllBytesAsync(encryptedPath);
        var sourceContent = await File.ReadAllBytesAsync(sourceFile);
        encryptedContent.Should().NotBeEquivalentTo(sourceContent);
    }

    [Fact]
    public async Task EncryptFileAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDirectory, "non-existent.txt");
        var destinationFile = Path.Combine(_testDirectory, "encrypted");
        var encryptionKey = "MiClaveSegura123456";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _encryptionService.EncryptFileAsync(sourceFile, destinationFile, encryptionKey));
    }

    [Fact]
    public async Task EncryptFileAsync_WithInvalidKey_ThrowsArgumentException()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDirectory, "test.txt");
        var destinationFile = Path.Combine(_testDirectory, "encrypted");
        var encryptionKey = "corta";

        await File.WriteAllTextAsync(sourceFile, "test content");
        _filesToCleanup.Add(sourceFile);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _encryptionService.EncryptFileAsync(sourceFile, destinationFile, encryptionKey));
    }

    [Fact]
    public async Task DecryptFileAsync_WithValidEncryptedFile_RestoresOriginalContent()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDirectory, "original.txt");
        var encryptedFile = Path.Combine(_testDirectory, "encrypted");
        var decryptedFile = Path.Combine(_testDirectory, "decrypted.txt");
        var encryptionKey = "MiClaveSegura123456";
        var originalContent = "Este es el contenido original que debe ser restaurado después del cifrado y descifrado.";

        await File.WriteAllTextAsync(sourceFile, originalContent);
        _filesToCleanup.Add(sourceFile);

        // Cifrar
        var encryptedPath = await _encryptionService.EncryptFileAsync(
            sourceFile,
            encryptedFile,
            encryptionKey);
        _filesToCleanup.Add(encryptedPath);

        // Act - Descifrar
        var success = await _encryptionService.DecryptFileAsync(
            encryptedPath,
            decryptedFile,
            encryptionKey);
        _filesToCleanup.Add(decryptedFile);

        // Assert
        success.Should().BeTrue();
        File.Exists(decryptedFile).Should().BeTrue();
        
        var decryptedContent = await File.ReadAllTextAsync(decryptedFile);
        decryptedContent.Should().Be(originalContent);
    }

    [Fact]
    public async Task DecryptFileAsync_WithWrongKey_ThrowsCryptographicException()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDirectory, "original.txt");
        var encryptedFile = Path.Combine(_testDirectory, "encrypted");
        var decryptedFile = Path.Combine(_testDirectory, "decrypted.txt");
        var correctKey = "MiClaveSegura123456";
        var wrongKey = "ClaveIncorrecta1234";

        await File.WriteAllTextAsync(sourceFile, "Contenido secreto");
        _filesToCleanup.Add(sourceFile);

        // Cifrar con la clave correcta
        var encryptedPath = await _encryptionService.EncryptFileAsync(
            sourceFile,
            encryptedFile,
            correctKey);
        _filesToCleanup.Add(encryptedPath);

        // Act & Assert - Intentar descifrar con clave incorrecta
        await Assert.ThrowsAsync<System.Security.Cryptography.CryptographicException>(async () =>
            await _encryptionService.DecryptFileAsync(encryptedPath, decryptedFile, wrongKey));
    }

    [Fact]
    public async Task EncryptAndDecrypt_WithLargeFile_MaintainsIntegrity()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDirectory, "large-file.bin");
        var encryptedFile = Path.Combine(_testDirectory, "large-encrypted");
        var decryptedFile = Path.Combine(_testDirectory, "large-decrypted.bin");
        var encryptionKey = "ClaveParaArchivoGrande123";

        // Crear un archivo grande (1 MB)
        var largeContent = new byte[1024 * 1024]; // 1 MB
        new Random(42).NextBytes(largeContent); // Usar seed para reproducibilidad
        await File.WriteAllBytesAsync(sourceFile, largeContent);
        _filesToCleanup.Add(sourceFile);

        // Act - Cifrar
        var encryptedPath = await _encryptionService.EncryptFileAsync(
            sourceFile,
            encryptedFile,
            encryptionKey);
        _filesToCleanup.Add(encryptedPath);

        // Act - Descifrar
        var success = await _encryptionService.DecryptFileAsync(
            encryptedPath,
            decryptedFile,
            encryptionKey);
        _filesToCleanup.Add(decryptedFile);

        // Assert
        success.Should().BeTrue();
        var decryptedContent = await File.ReadAllBytesAsync(decryptedFile);
        decryptedContent.Should().BeEquivalentTo(largeContent);
    }

    [Fact]
    public void IsEncrypted_WithEncryptedFile_ReturnsTrue()
    {
        // Arrange
        var encryptedFile = Path.Combine(_testDirectory, "test.encrypted");
        
        // Crear un archivo con el encabezado correcto
        using (var fs = File.Create(encryptedFile))
        {
            var header = System.Text.Encoding.ASCII.GetBytes("MONGOBR-AES256");
            fs.Write(header, 0, header.Length);
        }
        _filesToCleanup.Add(encryptedFile);

        // Act
        var isEncrypted = _encryptionService.IsEncrypted(encryptedFile);

        // Assert
        isEncrypted.Should().BeTrue();
    }

    [Fact]
    public void IsEncrypted_WithNonEncryptedFile_ReturnsFalse()
    {
        // Arrange
        var normalFile = Path.Combine(_testDirectory, "normal.txt");
        File.WriteAllText(normalFile, "Just a normal file");
        _filesToCleanup.Add(normalFile);

        // Act
        var isEncrypted = _encryptionService.IsEncrypted(normalFile);

        // Assert
        isEncrypted.Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "does-not-exist.txt");

        // Act
        var isEncrypted = _encryptionService.IsEncrypted(nonExistentFile);

        // Assert
        isEncrypted.Should().BeFalse();
    }

    [Fact]
    public async Task EncryptFileAsync_WithSameKeySameContent_ProducesDifferentCiphertext()
    {
        // Este test verifica que el IV aleatorio hace que el mismo contenido
        // cifrado con la misma clave produzca diferentes resultados
        
        // Arrange
        var sourceFile1 = Path.Combine(_testDirectory, "source1.txt");
        var sourceFile2 = Path.Combine(_testDirectory, "source2.txt");
        var encryptedFile1 = Path.Combine(_testDirectory, "encrypted1");
        var encryptedFile2 = Path.Combine(_testDirectory, "encrypted2");
        var encryptionKey = "MiClaveSegura123456";
        var content = "Contenido idéntico en ambos archivos";

        await File.WriteAllTextAsync(sourceFile1, content);
        await File.WriteAllTextAsync(sourceFile2, content);
        _filesToCleanup.AddRange(new[] { sourceFile1, sourceFile2 });

        // Act
        var encrypted1 = await _encryptionService.EncryptFileAsync(sourceFile1, encryptedFile1, encryptionKey);
        var encrypted2 = await _encryptionService.EncryptFileAsync(sourceFile2, encryptedFile2, encryptionKey);
        _filesToCleanup.AddRange(new[] { encrypted1, encrypted2 });

        // Assert
        var content1 = await File.ReadAllBytesAsync(encrypted1);
        var content2 = await File.ReadAllBytesAsync(encrypted2);
        
        // Los archivos cifrados deben ser diferentes (debido a diferentes IVs)
        content1.Should().NotBeEquivalentTo(content2);
    }

    [Fact]
    public async Task DecryptFileAsync_WithCorruptedFile_ThrowsException()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDirectory, "original.txt");
        var encryptedFile = Path.Combine(_testDirectory, "encrypted");
        var decryptedFile = Path.Combine(_testDirectory, "decrypted.txt");
        var encryptionKey = "MiClaveSegura123456";

        await File.WriteAllTextAsync(sourceFile, "Contenido original");
        _filesToCleanup.Add(sourceFile);

        // Cifrar
        var encryptedPath = await _encryptionService.EncryptFileAsync(
            sourceFile,
            encryptedFile,
            encryptionKey);
        _filesToCleanup.Add(encryptedPath);

        // Corromper el archivo cifrado (modificar algunos bytes)
        var encryptedBytes = await File.ReadAllBytesAsync(encryptedPath);
        encryptedBytes[encryptedBytes.Length / 2] ^= 0xFF; // Invertir bits en la mitad del archivo
        await File.WriteAllBytesAsync(encryptedPath, encryptedBytes);

        // Act & Assert
        await Assert.ThrowsAsync<System.Security.Cryptography.CryptographicException>(async () =>
            await _encryptionService.DecryptFileAsync(encryptedPath, decryptedFile, encryptionKey));
    }
}
