using ClaudeProfiles.Core.Platform;
using ClaudeProfiles.Core.Profiles;

namespace ClaudeProfiles.ViewModels;

internal static class UserMessages
{
    public static string? Describe(Exception exception) => exception switch
    {
        ClaudeNotInstalledException =>
            "Claude Desktop est introuvable sur cet ordinateur. Installez-le depuis claude.ai/download, puis réessayez.",
        ProfileInUseException inUse =>
            $"Claude est encore ouvert avec le profil « {inUse.Profile.DisplayName} ». Quittez-le complètement (y compris depuis la zone de notification) avant de le supprimer.",
        ProfileNameRejectedException =>
            "Ce nom de profil n'est pas accepté.",
        ProfileStoreCorruptedException corrupted =>
            $"Le fichier des profils est illisible : {corrupted.FilePath}",
        UnauthorizedAccessException or IOException =>
            $"L'opération a échoué sur le disque : {exception.Message}",
        _ => null
    };

    public static string UnknownProfile(string directory) =>
        $"Aucun profil n'utilise le dossier {directory}. Il a peut-être été supprimé.";

    public static string InstanceCannotBeMarked(string profileName, string reason) =>
        $"Les icônes du profil « {profileName} » n'ont pas pu être appliquées : {reason}";

    public static string FolderCannotBeOpened(string directory) =>
        $"Impossible d'ouvrir le dossier {directory}. Il n'existe peut-être pas encore : il est créé à la première ouverture de Claude avec ce profil.";
}
