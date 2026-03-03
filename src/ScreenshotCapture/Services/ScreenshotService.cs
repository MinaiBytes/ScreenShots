using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.Versioning;
using System.Windows.Forms;
using ScreenshotCapture.Abstractions;

namespace ScreenshotCapture.Services;

/// <summary>
/// プライマリディスプレイのスクリーンキャプチャと PNG 保存を行う Windows 専用サービスです。
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ScreenshotService : IScreenshotService
{
    /// <summary>
    /// 現在時刻を取得するデリゲートです。
    /// </summary>
    private readonly Func<DateTimeOffset> _nowProvider;

    /// <summary>
    /// ディレクトリ存在判定を行うデリゲートです。
    /// </summary>
    private readonly Func<string, bool> _directoryExists;

    /// <summary>
    /// ディレクトリ作成を行うデリゲートです。
    /// </summary>
    private readonly Action<string> _createDirectory;

    /// <summary>
    /// 解決済みの保存先ディレクトリをキャッシュします。
    /// 初回解決後は同一インスタンス内で再利用されます。
    /// </summary>
    private string? _cachedSaveDirectory;

    /// <summary>
    /// 優先保存先ルートです。
    /// </summary>
    public const string PrimaryRoot = @"D:\";

    /// <summary>
    /// フォールバック保存先ルートです。
    /// </summary>
    public const string FallbackRoot = @"C:\";

    /// <summary>
    /// 保存先サブディレクトリ名です。
    /// </summary>
    public const string SubDirectoryName = "Screenshots";

    /// <summary>
    /// <see cref="ScreenshotService"/> を初期化します。
    /// </summary>
    /// <param name="directoryExists">ディレクトリ存在判定関数。未指定時は <see cref="Directory.Exists(string?)"/> を使用します。</param>
    /// <param name="createDirectory">ディレクトリ作成処理。未指定時は <see cref="Directory.CreateDirectory(string)"/> を使用します。</param>
    /// <param name="nowProvider">現在時刻取得処理。未指定時は <see cref="DateTimeOffset.Now"/> を使用します。</param>
    public ScreenshotService(
        Func<string, bool>? directoryExists = null,
        Action<string>? createDirectory = null,
        Func<DateTimeOffset>? nowProvider = null)
    {
        _directoryExists = directoryExists ?? Directory.Exists;
        _createDirectory = createDirectory ?? (path =>
        {
            _ = Directory.CreateDirectory(path);
        });
        _nowProvider = nowProvider ?? (() => DateTimeOffset.Now);
    }

    /// <summary>
    /// プライマリディスプレイ全体をキャプチャします（テスト用に internal 公開）。
    /// </summary>
    /// <param name="cancellationToken">キャンセル トークン。</param>
    /// <returns>キャプチャしたビットマップ。</returns>
    internal Bitmap CapturePrimaryScreen(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var primaryScreen = Screen.PrimaryScreen;
        if (primaryScreen is null)
        {
            throw new InvalidOperationException("Primary screen is not available.");
        }

        return CaptureBounds(primaryScreen.Bounds, cancellationToken);
    }

    /// <summary>
    /// ビットマップを PNG 形式で保存します（テスト用に internal 公開）。
    /// </summary>
    /// <param name="bitmap">保存対象ビットマップ。</param>
    /// <param name="filePath">保存先ファイルパス。</param>
    /// <param name="cancellationToken">キャンセル トークン。</param>
    internal void SaveAsPng(Bitmap bitmap, string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.None);

        bitmap.Save(stream, ImageFormat.Png);
    }

    /// <inheritdoc />
    public string CaptureAndSave(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = CreateSaveFilePath(_nowProvider());

        using var bitmap = CapturePrimaryScreen(cancellationToken);
        SaveAsPng(bitmap, filePath, cancellationToken);
        return filePath;
    }

    /// <summary>
    /// 固定保存先ルールに基づき保存先ディレクトリを解決し、作成処理を呼び出します。
    /// 初回解決後はキャッシュを返します。
    /// </summary>
    /// <returns>保存先ディレクトリパス。</returns>
    internal string EnsureSaveDirectory()
    {
        var cachedDirectory = _cachedSaveDirectory;
        if (cachedDirectory is not null)
        {
            return cachedDirectory;
        }

        var root = _directoryExists(PrimaryRoot) ? PrimaryRoot : FallbackRoot;
        var directoryPath = Path.Combine(root, SubDirectoryName);
        _createDirectory(directoryPath);
        _cachedSaveDirectory ??= directoryPath;
        return _cachedSaveDirectory;
    }

    /// <summary>
    /// 指定範囲の画面内容をキャプチャしてビットマップを生成します。
    /// </summary>
    /// <param name="bounds">キャプチャ対象範囲。</param>
    /// <param name="cancellationToken">キャンセル トークン。</param>
    /// <returns>キャプチャしたビットマップ。</returns>
    /// <exception cref="InvalidOperationException">キャプチャ対象サイズが不正な場合。</exception>
    private static Bitmap CaptureBounds(Rectangle bounds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException("Capture target size is invalid.");
        }

        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(
                sourceX: bounds.Left,
                sourceY: bounds.Top,
                destinationX: 0,
                destinationY: 0,
                blockRegionSize: bounds.Size,
                copyPixelOperation: CopyPixelOperation.SourceCopy);

            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 指定時刻を用いて保存先ファイルパスを生成します。
    /// </summary>
    /// <param name="timestamp">ファイル名生成に使用する時刻。</param>
    /// <returns>保存先ファイルパス。</returns>
    private string CreateSaveFilePath(DateTimeOffset timestamp)
    {
        var directoryPath = EnsureSaveDirectory();
        var fileName =
            "screenshot_" +
            timestamp.LocalDateTime.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) +
            ".png";

        return Path.Combine(directoryPath, fileName);
    }
}
