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
            new PropertyMetadata(string.Empty));

    public ErrorDisplay()
    {
        InitializeComponent();
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
