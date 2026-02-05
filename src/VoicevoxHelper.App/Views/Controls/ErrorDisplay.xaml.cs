using System.Windows;
using System.Windows.Controls;

namespace VoicevoxHelper.App.Views.Controls;

/// <summary>
/// エラー表示用コントロール。
/// </summary>
public partial class ErrorDisplay : UserControl
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(
            nameof(Message),
            typeof(string),
            typeof(ErrorDisplay),
            new PropertyMetadata(string.Empty, OnMessageChanged));

    public ErrorDisplay()
    {
        InitializeComponent();
    }

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ErrorDisplay control)
        {
            return;
        }

        control.Visibility = string.IsNullOrWhiteSpace(control.Message) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// 表示するエラーメッセージ。
    /// </summary>
    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }
}
