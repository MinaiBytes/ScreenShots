using ScreenshotCapture.Services;

namespace ScreenshotCapture.Tests;

public sealed class ScreenshotServiceSaveDirectoryTests
{
    [Fact]
    public void EnsureSaveDirectory_UsesDDrive_WhenAvailable()
    {
        var createdDirectories = new List<string>();
        var sut = new ScreenshotService(
            directoryExists: path => path == ScreenshotService.PrimaryRoot,
            createDirectory: createdDirectories.Add);

        var directory = sut.EnsureSaveDirectory();

        var expectedDirectory = Path.Combine(
            ScreenshotService.PrimaryRoot,
            ScreenshotService.SubDirectoryName);

        Assert.Equal(expectedDirectory, directory);
        Assert.Single(createdDirectories);
        Assert.Equal(expectedDirectory, createdDirectories[0]);
    }

    [Fact]
    public void EnsureSaveDirectory_FallsBackToCDrive_WhenDDriveMissing()
    {
        var createdDirectories = new List<string>();
        var sut = new ScreenshotService(
            directoryExists: _ => false,
            createDirectory: createdDirectories.Add);

        var directory = sut.EnsureSaveDirectory();

        var expectedDirectory = Path.Combine(
            ScreenshotService.FallbackRoot,
            ScreenshotService.SubDirectoryName);

        Assert.Equal(expectedDirectory, directory);
        Assert.Single(createdDirectories);
        Assert.Equal(expectedDirectory, createdDirectories[0]);
    }

    [Fact]
    public void EnsureSaveDirectory_UsesCachedDirectory_OnSecondCall()
    {
        var createdDirectories = new List<string>();
        var directoryExistsCallCount = 0;
        var sut = new ScreenshotService(
            directoryExists: path =>
            {
                directoryExistsCallCount++;
                return path == ScreenshotService.PrimaryRoot;
            },
            createDirectory: createdDirectories.Add);

        var first = sut.EnsureSaveDirectory();
        var second = sut.EnsureSaveDirectory();

        var expectedDirectory = Path.Combine(
            ScreenshotService.PrimaryRoot,
            ScreenshotService.SubDirectoryName);

        Assert.Equal(expectedDirectory, first);
        Assert.Equal(expectedDirectory, second);
        Assert.Single(createdDirectories);
        Assert.Equal(1, directoryExistsCallCount);
    }
}
