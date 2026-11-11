using ClaudeProfiles.Core.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClaudeProfiles.ViewModels;

public sealed partial class ProfileItemViewModel : ObservableObject
{
    private readonly MainWindowViewModel _owner;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(LaunchLabel))]
    private bool _isRunning;

    public ProfileItemViewModel(ClaudeProfile profile, bool isRunning, MainWindowViewModel owner)
    {
        Profile = profile;
        _isRunning = isRunning;
        _owner = owner;
    }

    public ClaudeProfile Profile { get; }

    public string DisplayName => Profile.DisplayName;

    public string Initial => Profile.GetInitial();

    public string DataDirectory => Profile.DataDirectory;

    public bool CanDelete => !Profile.IsDefault;

    public string StatusText => IsRunning ? "Ouvert" : "Fermé";

    public string LaunchLabel => IsRunning ? "Afficher" : "Ouvrir";

    [RelayCommand]
    private void Launch() => _owner.Launch(this);

    [RelayCommand]
    private Task OpenFolderAsync() => _owner.OpenFolderAsync(this);

    [RelayCommand]
    private void Edit() => _owner.StartEditing(this);

    [RelayCommand]
    private void RequestDeletion() => _owner.RequestDeletion(this);
}
