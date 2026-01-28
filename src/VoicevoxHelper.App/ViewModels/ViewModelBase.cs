using CommunityToolkit.Mvvm.ComponentModel;

namespace VoicevoxHelper.App.ViewModels;

/// <summary>
/// ViewModel基底クラス。
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _errorMessage = string.Empty;
}
