using System.Runtime.Versioning;
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
    /// スクリーンショット取得・保存機能を DI コンテナへ Singleton として登録します。
    /// </summary>
    /// <remarks>
    /// <see cref="IScreenshotService"/> は Singleton で登録されます。
    /// CommunityToolkit.Mvvm の <c>[RelayCommand]</c> と組み合わせる場合は、
    /// ViewModel メソッドに <see cref="CancellationToken"/> を受け取るシグネチャを使用すると
    /// キャンセル対応の <c>IAsyncRelayCommand</c> が自動生成されます。
    /// </remarks>
    /// <param name="services">登録先のサービスコレクション。</param>
    /// <returns>メソッドチェーン可能な <paramref name="services"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> が <see langword="null"/> の場合。</exception>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddScreenshotCaptureServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IScreenshotService>(static _ => new ScreenshotService());
        return services;
    }
}
