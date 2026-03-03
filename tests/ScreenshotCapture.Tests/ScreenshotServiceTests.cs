using ScreenshotCapture.Services;

namespace ScreenshotCapture.Tests;

public sealed class ScreenshotServiceTests
{
    [Fact]
    public void CapturePrimaryScreen_CanCaptureScreenshot()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var sut = new ScreenshotService();

        using var bitmap = sut.CapturePrimaryScreen();

        Assert.True(bitmap.Width > 0);
        Assert.True(bitmap.Height > 0);
    }

    [Fact]
    public void SaveAsPng_CanSaveCapturedDataAsPng()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var sut = new ScreenshotService();
        var filePath = Path.Combine(Path.GetTempPath(), $"screenshot_test_{Guid.NewGuid():N}.png");

        try
        {
            using var bitmap = sut.CapturePrimaryScreen();

            sut.SaveAsPng(bitmap, filePath);

            Assert.True(File.Exists(filePath));
            var header = File.ReadAllBytes(filePath).Take(8).ToArray();
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, header);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
