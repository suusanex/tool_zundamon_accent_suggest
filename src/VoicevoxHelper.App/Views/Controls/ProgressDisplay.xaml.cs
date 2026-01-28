using System.Windows;
using System.Windows.Controls;

namespace VoicevoxHelper.App.Views.Controls;

/// <summary>
/// 進捗表示用コントロール。
/// </summary>
public partial class ProgressDisplay : UserControl
{
    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(
            nameof(Progress),
            typeof(double),
            typeof(ProgressDisplay),
            new PropertyMetadata(0d));

    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(
            nameof(Status),
            typeof(string),
            typeof(ProgressDisplay),
            new PropertyMetadata(string.Empty));

    public ProgressDisplay()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 進捗値（0-100）。
    /// </summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>
    /// 状態テキスト。
    /// </summary>
    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }
}
