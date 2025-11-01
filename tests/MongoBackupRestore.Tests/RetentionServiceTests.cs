using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoBackupRestore.Core.Models;
using MongoBackupRestore.Core.Services;
using Moq;

namespace MongoBackupRestore.Tests;

/// <summary>
/// Pruebas para el servicio RetentionService
/// </summary>
public class RetentionServiceTests : IDisposable
{
    private readonly Mock<ILogger<RetentionService>> _mockLogger;
    private readonly RetentionService _retentionService;
    private readonly string _testDirectory;

    public RetentionServiceTests()
    {
        _mockLogger = new Mock<ILogger<RetentionService>>();
        _retentionService = new RetentionService(_mockLogger.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"retention_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_PoliticaNoHabilitada_NoRealizaLimpieza()
    {
        // Arrange
        var policy = new RetentionPolicy
        {
            RetentionDays = null,
            BackupDirectory = _testDirectory
        };

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(policy);

        // Assert
        result.Success.Should().BeTrue();
        result.DeletedCount.Should().Be(0);
        result.Message.Should().Contain("no habilitada");
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_DirectorioNoEspecificado_RetornaError()
    {
        // Arrange
        var policy = new RetentionPolicy
        {
            RetentionDays = 7,
            BackupDirectory = null
        };

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(policy);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no especificado");
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_DirectorioNoExiste_RetornaError()
    {
        // Arrange
        var policy = new RetentionPolicy
        {
            RetentionDays = 7,
            BackupDirectory = Path.Combine(Path.GetTempPath(), "no_existe")
        };

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(policy);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no existe");
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_NoHayBackupsAntiguos_NoEliminaNada()
    {
        // Arrange
        // Crear un directorio de backup reciente
        var recentBackupDir = Path.Combine(_testDirectory, "backup_recent");
        Directory.CreateDirectory(recentBackupDir);
        File.WriteAllText(Path.Combine(recentBackupDir, "test.bson"), "test data");

        var policy = new RetentionPolicy
        {
            RetentionDays = 7,
            BackupDirectory = _testDirectory
        };

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(policy);

        // Assert
        result.Success.Should().BeTrue();
        result.DeletedCount.Should().Be(0);
        result.Message.Should().Contain("No se encontraron backups");
        Directory.Exists(recentBackupDir).Should().BeTrue();
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_BackupsAntiguos_EliminaDirectoriosAntiguos()
    {
        // Arrange
        var oldBackupDir = Path.Combine(_testDirectory, "backup_old");
        Directory.CreateDirectory(oldBackupDir);
        File.WriteAllText(Path.Combine(oldBackupDir, "test.bson"), "test data");
        
        // Modificar fecha de creación para simular backup antiguo
        Directory.SetLastWriteTime(oldBackupDir, DateTime.Now.AddDays(-10));

        var policy = new RetentionPolicy
        {
            RetentionDays = 7,
            BackupDirectory = _testDirectory
        };

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(policy);

        // Assert
        result.Success.Should().BeTrue();
        result.DeletedCount.Should().Be(1);
        result.BytesFreed.Should().BeGreaterThan(0);
        result.DeletedPaths.Should().Contain(oldBackupDir);
        Directory.Exists(oldBackupDir).Should().BeFalse();
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_ArchivosComprimidosAntiguos_EliminaArchivos()
    {
        // Arrange
        var oldZipFile = Path.Combine(_testDirectory, "backup_old.zip");
        File.WriteAllText(oldZipFile, "test zip data");
        File.SetLastWriteTime(oldZipFile, DateTime.Now.AddDays(-10));

        var oldTarGzFile = Path.Combine(_testDirectory, "backup_old.tar.gz");
        File.WriteAllText(oldTarGzFile, "test tar.gz data");
        File.SetLastWriteTime(oldTarGzFile, DateTime.Now.AddDays(-15));

        var policy = new RetentionPolicy
        {
            RetentionDays = 7,
            BackupDirectory = _testDirectory
        };

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(policy);

        // Assert
        result.Success.Should().BeTrue();
        result.DeletedCount.Should().Be(2);
        result.DeletedPaths.Should().Contain(oldZipFile);
        result.DeletedPaths.Should().Contain(oldTarGzFile);
        File.Exists(oldZipFile).Should().BeFalse();
        File.Exists(oldTarGzFile).Should().BeFalse();
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_MezclaDeBackupsAntiguosYRecientes_SoloEliminaAntiguos()
    {
        // Arrange
        // Backup antiguo (debe eliminarse)
        var oldBackupDir = Path.Combine(_testDirectory, "backup_old");
        Directory.CreateDirectory(oldBackupDir);
        File.WriteAllText(Path.Combine(oldBackupDir, "data.bson"), "old data");
        Directory.SetLastWriteTime(oldBackupDir, DateTime.Now.AddDays(-10));

        // Backup reciente (debe conservarse)
        var recentBackupDir = Path.Combine(_testDirectory, "backup_recent");
        Directory.CreateDirectory(recentBackupDir);
        File.WriteAllText(Path.Combine(recentBackupDir, "data.bson"), "recent data");

        var policy = new RetentionPolicy
        {
            RetentionDays = 7,
            BackupDirectory = _testDirectory
        };

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(policy);

        // Assert
        result.Success.Should().BeTrue();
        result.DeletedCount.Should().Be(1);
        result.DeletedPaths.Should().Contain(oldBackupDir);
        result.DeletedPaths.Should().NotContain(recentBackupDir);
        Directory.Exists(oldBackupDir).Should().BeFalse();
        Directory.Exists(recentBackupDir).Should().BeTrue();
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_VariosBackupsAntiguos_EliminaTodos()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var backupDir = Path.Combine(_testDirectory, $"backup_{i}");
            Directory.CreateDirectory(backupDir);
            File.WriteAllText(Path.Combine(backupDir, "data.bson"), $"data {i}");
            Directory.SetLastWriteTime(backupDir, DateTime.Now.AddDays(-(10 + i)));
        }

        var policy = new RetentionPolicy
        {
            RetentionDays = 7,
            BackupDirectory = _testDirectory
        };

        // Act
        var result = await _retentionService.CleanupOldBackupsAsync(policy);

        // Assert
        result.Success.Should().BeTrue();
        result.DeletedCount.Should().Be(5);
        result.DeletedPaths.Should().HaveCount(5);
    }
}
