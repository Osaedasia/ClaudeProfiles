namespace ClaudeProfiles.Core.Profiles;

public sealed record ClaudeProfile(string DisplayName, string DataDirectory, ProfileColor Color, bool IsDefault)
{
    private const string InitialOfUnnamedProfile = "?";

    public string GetInitial() =>
        DisplayName.Length == 0 ? InitialOfUnnamedProfile : DisplayName[..1].ToUpperInvariant();
}
