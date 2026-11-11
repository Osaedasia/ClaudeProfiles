using System.Diagnostics.CodeAnalysis;

namespace ClaudeProfiles.Core.Instance;

public enum ManagerRequest
{
    ShowWindow,
    OpenProfile
}

public sealed record ManagerMessage(ManagerRequest Request, string? ProfileDirectory)
{
    private const string OpenProfileArgument = "--open";
    private const string ShowWindowLine = "show";
    private const string OpenProfileLinePrefix = "open:";

    public static ManagerMessage ShowWindow { get; } = new(ManagerRequest.ShowWindow, ProfileDirectory: null);

    public static ManagerMessage OpenProfile(string profileDirectory) => new(ManagerRequest.OpenProfile, profileDirectory);

    public static ManagerMessage FromCommandLine(IReadOnlyList<string> arguments)
    {
        for (int index = 0; index < arguments.Count - 1; index++)
        {
            if (arguments[index] == OpenProfileArgument)
            {
                return OpenProfile(arguments[index + 1]);
            }
        }

        return ShowWindow;
    }

    public static string BuildOpenCommandLine(string managerExecutable, string profileDirectory) =>
        $"\"{managerExecutable}\" {OpenProfileArgument} \"{profileDirectory}\"";

    public static bool TryParseLine(string line, [NotNullWhen(true)] out ManagerMessage? message)
    {
        if (line == ShowWindowLine)
        {
            message = ShowWindow;
            return true;
        }

        if (line.StartsWith(OpenProfileLinePrefix, StringComparison.Ordinal) && line.Length > OpenProfileLinePrefix.Length)
        {
            message = OpenProfile(line[OpenProfileLinePrefix.Length..]);
            return true;
        }

        message = null;
        return false;
    }

    public string ToLine() => Request switch
    {
        ManagerRequest.ShowWindow => ShowWindowLine,
        ManagerRequest.OpenProfile => OpenProfileLinePrefix + ProfileDirectory,
        _ => throw new ArgumentOutOfRangeException(nameof(Request), Request, "Unknown manager request.")
    };
}
