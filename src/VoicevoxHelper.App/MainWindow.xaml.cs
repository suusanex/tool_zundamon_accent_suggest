using System.Windows;
using VoicevoxHelper.Core.Interfaces;

namespace VoicevoxHelper.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;

    public MainWindow(INavigationService navigationService)
    {
        _navigationService = navigationService;
        InitializeComponent();
        _navigationService.SetFrame(MainFrame);
    }
}