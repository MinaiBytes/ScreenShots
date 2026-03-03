namespace ScreenshotCapture.Abstractions;

/// <summary>
/// スクリーンショットの取得と PNG 保存（固定保存先への保存）を提供するサービスを表します。
/// </summary>
public interface IScreenshotService
{
    /// <summary>
    /// プライマリディスプレイをキャプチャし、固定保存先ルールに従って PNG で非同期保存します。
    /// </summary>
    /// <param name="cancellationToken">キャンセル トークン。</param>
    /// <returns>保存したファイルパスを返すタスク。</returns>
    Task<string> CaptureAndSaveAsync(CancellationToken cancellationToken = default);
}
