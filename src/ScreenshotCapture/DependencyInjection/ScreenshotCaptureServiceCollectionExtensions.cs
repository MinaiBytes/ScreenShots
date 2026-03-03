using Microsoft.Extensions.DependencyInjection;
using ScreenshotCapture.Abstractions;
using ScreenshotCapture.Services;

namespace ScreenshotCapture.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> にスクリーンショット関連サービスを登録する拡張メソッドを提供します。
/// </summary>
public static class ScreenshotCaptureServiceCollectionExtensions
{
    /// <summary>
    /// スクリーンショット取得・保存機能を DI コンテナへ登録します。
    /// </summary>
    /// <param name="services">登録先のサービスコレクション。</param>
    /// <returns>メソッドチェーン可能な <paramref name="services"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> が <see langword="null"/> の場合。</exception>
    public static IServiceCollection AddScreenshotCaptureServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IScreenshotService>(static _ => new ScreenshotService());
        return services;
    }
}
