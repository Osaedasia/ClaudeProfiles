using ClaudeProfiles.Core.Profiles;

namespace ClaudeProfiles.Core.Platform;

public interface IInstanceMarker
{
    /// <summary>
    /// Donne à l'instance de Claude Desktop ouverte sur <paramref name="profile"/> des icônes propres au profil
    /// (bouton de barre des tâches distinct, icône de notification), pour la distinguer des autres instances.
    /// Sans effet si aucune instance ne tourne sur ce profil, ou sur un système qui ne permet pas de modifier
    /// les fenêtres d'une autre application.
    /// L'appel est idempotent et peu coûteux : il est prévu pour être répété toutes les quelques secondes, ce qui
    /// rétablit le marquage quand Claude ouvre une nouvelle fenêtre ou remet sa propre icône.
    /// </summary>
    /// <exception cref="IOException">Les icônes du profil n'ont pas pu être écrites sur le disque.</exception>
    void Mark(ClaudeProfile profile);
}

public sealed class NoInstanceMarker : IInstanceMarker
{
    public void Mark(ClaudeProfile profile)
    {
    }
}
