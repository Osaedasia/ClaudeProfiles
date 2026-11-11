using ClaudeProfiles.Core.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClaudeProfiles.ViewModels;

public enum EditorPurpose
{
    Create,
    Edit
}

public sealed partial class ProfileEditorViewModel : ObservableObject
{
    private readonly IReadOnlyCollection<string> _namesInUse;
    private readonly Func<ProfileEditorViewModel, CancellationToken, Task> _save;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NameError))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name;

    [ObservableProperty]
    private ProfileColor _color;

    public ProfileEditorViewModel(
        EditorPurpose purpose,
        string name,
        ProfileColor color,
        IReadOnlyCollection<string> namesInUse,
        Func<ProfileEditorViewModel, CancellationToken, Task> save)
    {
        Purpose = purpose;
        _name = name;
        _color = color;
        _namesInUse = namesInUse;
        _save = save;
    }

    public EditorPurpose Purpose { get; }

    public string Title => Purpose == EditorPurpose.Create ? "Nouveau profil" : "Modifier le profil";

    public string SaveLabel => Purpose == EditorPurpose.Create ? "Créer le profil" : "Enregistrer";

    public IReadOnlyList<ProfileColor> AvailableColors { get; } = Enum.GetValues<ProfileColor>();

    public string? NameError => NameIssue switch
    {
        ProfileNameIssue.None or ProfileNameIssue.Empty => null,
        ProfileNameIssue.TooLong => $"{ProfileNameValidator.MaxLength} caractères maximum.",
        ProfileNameIssue.AlreadyUsed => "Un profil porte déjà ce nom.",
        _ => throw new ArgumentOutOfRangeException(nameof(NameIssue), NameIssue, "Unknown profile name issue.")
    };

    private ProfileNameIssue NameIssue => ProfileNameValidator.Validate(Name, _namesInUse);

    private bool CanSave() => NameIssue == ProfileNameIssue.None;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync(CancellationToken cancellationToken) => _save(this, cancellationToken);
}
