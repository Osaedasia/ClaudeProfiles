namespace ClaudeProfiles.Core.Profiles;

public sealed class ProfileInUseException(ClaudeProfile profile)
    : InvalidOperationException($"Claude Desktop is running on profile '{profile.DisplayName}' ({profile.DataDirectory}).")
{
    public ClaudeProfile Profile { get; } = profile;
}

public sealed class ProfileNameRejectedException(string name, ProfileNameIssue issue)
    : ArgumentException($"Profile name '{name}' was rejected: {issue}.", nameof(name))
{
    public ProfileNameIssue Issue { get; } = issue;
}

public sealed class ProfileStoreCorruptedException(string filePath, Exception innerException)
    : Exception($"The profile file '{filePath}' could not be read.", innerException)
{
    public string FilePath { get; } = filePath;
}
