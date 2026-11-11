namespace ClaudeProfiles.Core.Profiles;

public enum ProfileNameIssue
{
    None,
    Empty,
    TooLong,
    AlreadyUsed
}

public static class ProfileNameValidator
{
    public const int MaxLength = 40;

    public static ProfileNameIssue Validate(string name, IEnumerable<string> namesInUse)
    {
        string trimmedName = name.Trim();

        if (trimmedName.Length == 0)
        {
            return ProfileNameIssue.Empty;
        }

        if (trimmedName.Length > MaxLength)
        {
            return ProfileNameIssue.TooLong;
        }

        bool isAlreadyUsed = namesInUse.Any(usedName =>
            string.Equals(usedName.Trim(), trimmedName, StringComparison.CurrentCultureIgnoreCase));

        return isAlreadyUsed ? ProfileNameIssue.AlreadyUsed : ProfileNameIssue.None;
    }
}
