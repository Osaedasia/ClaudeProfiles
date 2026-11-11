using ClaudeProfiles.Core.Instance;

namespace ClaudeProfiles.Core.Tests;

public sealed class ManagerMessageTests
{
    [Fact]
    public void FromCommandLine_WithoutArguments_AsksToShowTheWindow()
    {
        Assert.Equal(ManagerMessage.ShowWindow, ManagerMessage.FromCommandLine([]));
    }

    [Fact]
    public void FromCommandLine_WithOpen_AsksToOpenThatProfile()
    {
        ManagerMessage message = ManagerMessage.FromCommandLine(["--open", @"C:\Data\Claude-Travail"]);

        Assert.Equal(ManagerMessage.OpenProfile(@"C:\Data\Claude-Travail"), message);
    }

    [Fact]
    public void BuildOpenCommandLine_QuotesPathsWithSpaces()
    {
        string commandLine = ManagerMessage.BuildOpenCommandLine(@"C:\Program Files\Profils\ClaudeProfiles.exe", @"C:\Mes données\Claude-Travail");

        Assert.Equal("\"C:\\Program Files\\Profils\\ClaudeProfiles.exe\" --open \"C:\\Mes données\\Claude-Travail\"", commandLine);
    }

    [Theory]
    [InlineData("show")]
    [InlineData(@"open:C:\Data\Claude-Travail")]
    public void Line_RoundTrips(string line)
    {
        Assert.True(ManagerMessage.TryParseLine(line, out ManagerMessage? message));
        Assert.Equal(line, message.ToLine());
    }

    [Theory]
    [InlineData("")]
    [InlineData("open:")]
    [InlineData("quit")]
    public void TryParseLine_RejectsUnknownLines(string line)
    {
        Assert.False(ManagerMessage.TryParseLine(line, out _));
    }
}
