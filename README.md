# ScreenshotCapture (WPF/.NET 8)

Windows 向けのシンプルなスクリーンショット取得・PNG保存サービスです（プライマリディスプレイ固定）。

## 構成

- `ScreenshotService`
  - プライマリディスプレイ全体をキャプチャ
  - PNG保存
  - 固定保存先ルールでの保存
  - 保存先: `D:\\Screenshots\\` 優先 / 無ければ `C:\\Screenshots\\`
  - ファイル名: `screenshot_yyyyMMdd_HHmmss.png`
  - 公開APIは `CaptureAndSave()` のみ（取得/保存の詳細は内部実装）
- `AddScreenshotCaptureServices()`
  - DI登録用拡張メソッド

## DI登録例

```csharp
using Microsoft.Extensions.DependencyInjection;
using ScreenshotCapture.Abstractions;
using ScreenshotCapture.DependencyInjection;

var services = new ServiceCollection();
services.AddScreenshotCaptureServices();

using var provider = services.BuildServiceProvider();
var screenshotService = provider.GetRequiredService<IScreenshotService>();

var savedPath = screenshotService.CaptureAndSave();
```

## 手順（DI + ViewModel + 成功/失敗の受信）

### 1. DIにサービスを登録する

```csharp
using Microsoft.Extensions.DependencyInjection;
using ScreenshotCapture.DependencyInjection;

var services = new ServiceCollection();
services.AddScreenshotCaptureServices();
```

### 2. コードビハインドを使わず、ViewModel内でトリガーと成功/失敗受信を行う（CommunityToolkit.Mvvm）

イベントを同じ ViewModel 内で受けて、表示用プロパティに反映する形にすると、コードビハインド不要で運用できます。

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenshotCapture.Abstractions;

public partial class SampleViewModel : ObservableObject
{
    private readonly IScreenshotService _screenshotService;

    public SampleViewModel(IScreenshotService screenshotService)
    {
        _screenshotService = screenshotService;
        ScreenshotCompleted += OnScreenshotCompleted;
    }

    public event EventHandler<ScreenshotCompletedEventArgs>? ScreenshotCompleted;

    [ObservableProperty]
    private bool? isLastCaptureSuccess;

    [ObservableProperty]
    private string? lastSavedPath;

    [ObservableProperty]
    private string? lastErrorMessage;

    [RelayCommand]
    private void CaptureScreenshot()
    {
        try
        {
            var savedPath = _screenshotService.CaptureAndSave();
            ScreenshotCompleted?.Invoke(this, ScreenshotCompletedEventArgs.Success(savedPath));
        }
        catch (Exception ex)
        {
            ScreenshotCompleted?.Invoke(this, ScreenshotCompletedEventArgs.Failure(ex));
        }
    }

    private void OnScreenshotCompleted(object? sender, ScreenshotCompletedEventArgs e)
    {
        IsLastCaptureSuccess = e.IsSuccess;
        LastSavedPath = e.SavedPath;
        LastErrorMessage = e.Exception?.Message;
    }
}

public sealed class ScreenshotCompletedEventArgs : EventArgs
{
    private ScreenshotCompletedEventArgs(bool isSuccess, string? savedPath, Exception? exception)
    {
        IsSuccess = isSuccess;
        SavedPath = savedPath;
        Exception = exception;
    }

    public bool IsSuccess { get; }
    public string? SavedPath { get; }
    public Exception? Exception { get; }

    public static ScreenshotCompletedEventArgs Success(string savedPath) =>
        new(true, savedPath, null);

    public static ScreenshotCompletedEventArgs Failure(Exception exception) =>
        new(false, null, exception);
}
```

XAML 側は、例えば `Button` から生成される `CaptureScreenshotCommand` をバインドし、
`IsLastCaptureSuccess` / `LastSavedPath` / `LastErrorMessage` をバインドして表示を分岐できます。
