using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VoicevoxHelper.Core.Interfaces;

namespace VoicevoxHelper.App.Services;

/// <summary>
/// フレームベースの画面遷移サービス。
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationService> _logger;
    private Frame? _frame;

    public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void SetFrame(object frame)
    {
        if (frame is not Frame wpfFrame)
        {
            throw new ArgumentException("Frame is required.", nameof(frame));
        }

        _frame = wpfFrame;
    }

    public bool Navigate(Type pageType)
    {
        if (_frame is null)
        {
            return false;
        }

        if (!typeof(Page).IsAssignableFrom(pageType))
        {
            return false;
        }

        try
        {
            var page = _serviceProvider.GetRequiredService(pageType);
            if (page is not Page wpfPage)
            {
                throw new InvalidOperationException($"Resolved service is not a WPF Page: {pageType.FullName}");
            }

            return _frame.Navigate(wpfPage);
        }
        catch (Exception ex)
        {
            _logger.LogError("Navigation failed: {Exception}", ex.ToString());
            return false;
        }
    }

    public bool Navigate<TPage>() where TPage : class
        => Navigate(typeof(TPage));
}
