using Microsoft.Extensions.Logging;
using Moq;
using MongoBackupRestore.Core.Services;
using Xunit;

namespace MongoBackupRestore.Tests.Services;

public class ConsoleProgressServiceTests
{
    private readonly Mock<ILogger<ConsoleProgressService>> _mockLogger;
    private readonly ConsoleProgressService _service;

    public ConsoleProgressServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConsoleProgressService>>();
        _service = new ConsoleProgressService(_mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteWithProgressAsync_ShouldExecuteAction()
    {
        // Arrange
        var actionExecuted = false;
        var description = "Test operation";

        // Act
        await _service.ExecuteWithProgressAsync(description, async (updateStatus) =>
        {
            await Task.Delay(10);
            actionExecuted = true;
        });

        // Assert
        Assert.True(actionExecuted);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(description)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithProgressAsync_WithResult_ShouldReturnResult()
    {
        // Arrange
        var expectedResult = 42;
        var description = "Test operation with result";

        // Act
        var result = await _service.ExecuteWithProgressAsync(description, async (updateStatus) =>
        {
            await Task.Delay(10);
            return expectedResult;
        });

        // Assert
        Assert.Equal(expectedResult, result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(description)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ShowSuccess_ShouldLogInformation()
    {
        // Arrange
        var message = "Operation completed successfully";

        // Act
        _service.ShowSuccess(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ShowError_ShouldLogError()
    {
        // Arrange
        var message = "Operation failed";

        // Act
        _service.ShowError(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ShowInfo_ShouldLogInformation()
    {
        // Arrange
        var message = "Some information";

        // Act
        _service.ShowInfo(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ShowWarning_ShouldLogWarning()
    {
        // Arrange
        var message = "Warning message";

        // Act
        _service.ShowWarning(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ShowPanel_ShouldLogTitleAndContent()
    {
        // Arrange
        var title = "Test Panel";
        var content = "Panel content";

        // Act
        _service.ShowPanel(title, content);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(title) && v.ToString()!.Contains(content)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ShowTable_ShouldLogTitle()
    {
        // Arrange
        var title = "Test Table";
        var data = new Dictionary<string, string>
        {
            { "Key1", "Value1" },
            { "Key2", "Value2" }
        };

        // Act
        _service.ShowTable(title, data);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(title)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
