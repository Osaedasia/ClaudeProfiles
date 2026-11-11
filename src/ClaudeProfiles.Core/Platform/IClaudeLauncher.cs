using ClaudeProfiles.Core.Profiles;

namespace ClaudeProfiles.Core.Platform;

public interface IClaudeLauncher
{
    /// <summary>
    /// Démarre Claude Desktop sur le profil donné, ou ramène au premier plan l'instance qui l'utilise déjà :
    /// un même profil n'est jamais ouvert deux fois. Le profil par défaut est lancé sans dossier de données
    /// explicite, exactement comme par le raccourci d'origine de Claude.
    /// L'appel rend la main dès que le processus est lancé, sans attendre l'ouverture de la fenêtre.
    /// </summary>
    /// <exception cref="ClaudeNotInstalledException">Claude Desktop est introuvable à ses emplacements d'installation connus.</exception>
    void Launch(ClaudeProfile profile);
}

public sealed class ClaudeNotInstalledException(IEnumerable<string> searchedLocations)
    : Exception($"Claude Desktop was not found. Searched: {string.Join(", ", searchedLocations)}.");
