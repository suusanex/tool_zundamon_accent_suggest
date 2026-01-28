using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using VoicevoxHelper.Core.Interfaces;

namespace VoicevoxHelper.App.Services;

/// <summary>
/// フレームベースの画面遷移サービス。
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService> _logger;
    private Frame? _frame;

    public NavigationService(ILogger<NavigationService> logger)
    {
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
            return _frame.Navigate(pageType);
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
