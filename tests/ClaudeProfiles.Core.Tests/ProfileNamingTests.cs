using ClaudeProfiles.Core.Profiles;

namespace ClaudeProfiles.Core.Tests;

public sealed class ProfileNamingTests
{
    [Theory]
    [InlineData("Travail", "Travail")]
    [InlineData("Client Dupont & Fils", "Client-Dupont-Fils")]
    [InlineData("Équipe été", "Equipe-ete")]
    [InlineData("***", "Profile")]
    public void ToSlug_GivesAPortableFolderName(string displayName, string expectedSlug)
    {
        Assert.Equal(expectedSlug, ProfileFolderName.ToSlug(displayName));
    }

    [Theory]
    [InlineData("   ", ProfileNameIssue.Empty)]
    [InlineData("TRAVAIL", ProfileNameIssue.AlreadyUsed)]
    [InlineData("Travail 2", ProfileNameIssue.None)]
    public void Validate_ComparesWithNamesInUse(string name, ProfileNameIssue expectedIssue)
    {
        Assert.Equal(expectedIssue, ProfileNameValidator.Validate(name, ["Perso", "Travail"]));
    }

    [Fact]
    public void Validate_RejectsNamesLongerThanTheLimit()
    {
        string tooLongName = new('a', ProfileNameValidator.MaxLength + 1);

        Assert.Equal(ProfileNameIssue.TooLong, ProfileNameValidator.Validate(tooLongName, []));
    }
}
