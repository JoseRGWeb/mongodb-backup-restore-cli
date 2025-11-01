using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Services;
using Moq;

namespace MongoBackupRestore.Tests;

/// <summary>
/// Pruebas para el servicio BackupRetentionService
/// </summary>
public class BackupRetentionServiceTests : IDisposable
{
    private readonly Mock<ILogger<BackupRetentionService>> _mockLogger;
    private readonly BackupRetentionService _retentionService;
    private readonly string _testDirectory;

    public BackupRetentionServiceTests()
    {
        _mockLogger = new Mock<ILogger<BackupRetentionService>>();
        _retentionService = new BackupRetentionService(_mockLogger.Object);
        
        // Crear directorio temporal para las pruebas
        _testDirectory = Path.Combine(Path.GetTempPath(), $"backup-retention-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        // Limpiar directorio de prueba
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_ConDirectorioVacio_RetornaSinError()
    {
        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(_testDirectory, 7);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalBackupsFound.Should().Be(0);
        result.BackupsDeleted.Should().Be(0);
        result.BackupsRetained.Should().Be(0);
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_ConDirectorioInvalido_RetornaError()
    {
        // Act
        var result = await _retentionService.CleanupOldBackupsAsync("", 7);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no puede estar vacío");
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_ConDiasDeRetencionInvalidos_RetornaError()
    {
        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(_testDirectory, 0);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("mayores que cero");
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_ConDirectorioNoExistente_RetornaSinError()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(nonExistentDir, 7);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("no existe");
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_ConBackupsAntiguos_EliminaBackupsCorrectamente()
    {
        // Arrange - Crear backups antiguos y nuevos
        var oldBackupDir = Path.Combine(_testDirectory, "old-backup");
        var newBackupDir = Path.Combine(_testDirectory, "new-backup");
        Directory.CreateDirectory(oldBackupDir);
        Directory.CreateDirectory(newBackupDir);

        // Crear archivos de prueba
        File.WriteAllText(Path.Combine(oldBackupDir, "test.bson"), "test data");
        File.WriteAllText(Path.Combine(newBackupDir, "test.bson"), "test data");

        // Cambiar fecha de creación del backup antiguo
        Directory.SetCreationTime(oldBackupDir, DateTime.Now.AddDays(-10));

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(_testDirectory, 7);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalBackupsFound.Should().Be(2);
        result.BackupsDeleted.Should().Be(1);
        result.BackupsRetained.Should().Be(1);
        Directory.Exists(oldBackupDir).Should().BeFalse();
        Directory.Exists(newBackupDir).Should().BeTrue();
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_ConArchivosComprimidosAntiguos_EliminaArchivosCorrectamente()
    {
        // Arrange
        var oldBackupFile = Path.Combine(_testDirectory, "old-backup.zip");
        var newBackupFile = Path.Combine(_testDirectory, "new-backup.zip");
        File.WriteAllText(oldBackupFile, "test data");
        File.WriteAllText(newBackupFile, "test data");

        // Cambiar fecha de creación del archivo antiguo
        File.SetCreationTime(oldBackupFile, DateTime.Now.AddDays(-30));

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(_testDirectory, 7);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalBackupsFound.Should().Be(2);
        result.BackupsDeleted.Should().Be(1);
        result.BackupsRetained.Should().Be(1);
        File.Exists(oldBackupFile).Should().BeFalse();
        File.Exists(newBackupFile).Should().BeTrue();
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_ConDryRunTrue_NoEliminaArchivos()
    {
        // Arrange
        var oldBackupFile = Path.Combine(_testDirectory, "old-backup.tar.gz");
        File.WriteAllText(oldBackupFile, "test data");
        File.SetCreationTime(oldBackupFile, DateTime.Now.AddDays(-15));

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(_testDirectory, 7, dryRun: true);

        // Assert
        result.Success.Should().BeTrue();
        result.BackupsDeleted.Should().Be(1);
        File.Exists(oldBackupFile).Should().BeTrue(); // El archivo NO debe ser eliminado en modo dry-run
        result.Message.Should().Contain("DRY-RUN");
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_ConMezclaDeBackups_IdentificaCorrectamente()
    {
        // Arrange
        var oldDir = Path.Combine(_testDirectory, "backup-old");
        var newDir = Path.Combine(_testDirectory, "backup-new");
        var oldZip = Path.Combine(_testDirectory, "backup-old.zip");
        var newZip = Path.Combine(_testDirectory, "backup-new.zip");
        var oldTarGz = Path.Combine(_testDirectory, "backup-old.tar.gz");

        Directory.CreateDirectory(oldDir);
        Directory.CreateDirectory(newDir);
        File.WriteAllText(oldZip, "data");
        File.WriteAllText(newZip, "data");
        File.WriteAllText(oldTarGz, "data");

        // Configurar fechas
        Directory.SetCreationTime(oldDir, DateTime.Now.AddDays(-20));
        File.SetCreationTime(oldZip, DateTime.Now.AddDays(-20));
        File.SetCreationTime(oldTarGz, DateTime.Now.AddDays(-20));
        Directory.SetCreationTime(newDir, DateTime.Now.AddDays(-1));
        File.SetCreationTime(newZip, DateTime.Now.AddDays(-1));

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(_testDirectory, 7);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalBackupsFound.Should().Be(5);
        result.BackupsDeleted.Should().Be(3);
        result.BackupsRetained.Should().Be(2);
        
        // Verificar que solo los nuevos existen
        Directory.Exists(newDir).Should().BeTrue();
        File.Exists(newZip).Should().BeTrue();
        Directory.Exists(oldDir).Should().BeFalse();
        File.Exists(oldZip).Should().BeFalse();
        File.Exists(oldTarGz).Should().BeFalse();
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_CalculaEspacioLiberado()
    {
        // Arrange
        var oldBackupFile = Path.Combine(_testDirectory, "old-backup.zip");
        var testData = new string('X', 1024 * 100); // 100 KB
        File.WriteAllText(oldBackupFile, testData);
        File.SetCreationTime(oldBackupFile, DateTime.Now.AddDays(-10));

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(_testDirectory, 7);

        // Assert
        result.Success.Should().BeTrue();
        result.SpaceFreedBytes.Should().BeGreaterThan(0);
        result.Message.Should().Contain("MB");
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_IgnoraDirectoriosOcultos()
    {
        // Arrange
        var hiddenDir = Path.Combine(_testDirectory, ".hidden");
        var normalDir = Path.Combine(_testDirectory, "normal");
        Directory.CreateDirectory(hiddenDir);
        Directory.CreateDirectory(normalDir);
        
        File.WriteAllText(Path.Combine(hiddenDir, "test.txt"), "data");
        File.WriteAllText(Path.Combine(normalDir, "test.txt"), "data");
        
        Directory.SetCreationTime(hiddenDir, DateTime.Now.AddDays(-30));
        Directory.SetCreationTime(normalDir, DateTime.Now.AddDays(-30));

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(_testDirectory, 7);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalBackupsFound.Should().Be(1); // Solo debe encontrar el directorio normal
        Directory.Exists(hiddenDir).Should().BeTrue(); // El directorio oculto no debe ser eliminado
        Directory.Exists(normalDir).Should().BeFalse(); // El directorio normal debe ser eliminado
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_IgnoraDirectoriosTemporales()
    {
        // Arrange
        var tempDir = Path.Combine(_testDirectory, "temp-backup");
        var normalDir = Path.Combine(_testDirectory, "backup");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(normalDir);
        
        Directory.SetCreationTime(tempDir, DateTime.Now.AddDays(-30));
        Directory.SetCreationTime(normalDir, DateTime.Now.AddDays(-30));

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(_testDirectory, 7);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalBackupsFound.Should().Be(1); // Solo debe encontrar el directorio normal
        Directory.Exists(tempDir).Should().BeTrue(); // El directorio temporal no debe ser eliminado
        Directory.Exists(normalDir).Should().BeFalse(); // El directorio normal debe ser eliminado
    }
}
