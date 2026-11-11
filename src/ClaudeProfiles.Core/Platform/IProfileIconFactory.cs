using ClaudeProfiles.Core.Profiles;

namespace ClaudeProfiles.Core.Platform;

public sealed record ProfileIconSet(string TaskbarIconFile, string TrayIconFile);

public interface IProfileIconFactory
{
    /// <summary>
    /// Fournit les fichiers .ico qui identifient <paramref name="profile"/> : une icône de barre des tâches
    /// (logo teinté de la couleur du profil, avec son initiale) et une icône de notification (logo teinté seul).
    /// Les fichiers sont créés au premier appel puis réutilisés tant que le nom et la couleur du profil ne changent
    /// pas ; un changement produit de nouveaux chemins, ce qui permet à l'appelant de détecter qu'il doit réappliquer.
    /// Doit être appelé sur le thread de l'interface : le rendu des icônes en dépend.
    /// </summary>
    /// <param name="profile">Profil à représenter.</param>
    /// <param name="claudeLogoDirectory">
    /// Dossier contenant les logos PNG de l'installation de Claude (« Square44x44Logo.targetsize-N.png »), ou
    /// <c>null</c> s'il est inconnu : les icônes sont alors dessinées sans le logo de Claude.
    /// </param>
    /// <returns>Les chemins absolus de deux fichiers .ico existants.</returns>
    /// <exception cref="IOException">Les fichiers n'ont pas pu être écrits.</exception>
    ProfileIconSet GetIcons(ClaudeProfile profile, string? claudeLogoDirectory);
}
