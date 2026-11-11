namespace ClaudeProfiles.Core.Platform;

public interface IInstanceDetector
{
    /// <summary>
    /// Indique si une instance de Claude Desktop utilise actuellement le dossier de données <paramref name="dataDirectory"/>.
    /// L'appel est synchrone et rapide (aucun parcours de processus) : il peut être répété toutes les secondes.
    /// </summary>
    /// <param name="dataDirectory">Chemin absolu du dossier de données du profil ; il n'a pas besoin d'exister.</param>
    /// <returns>
    /// <c>true</c> si une instance tourne sur ce dossier ; <c>false</c> si le dossier n'existe pas, n'a jamais été ouvert
    /// par Claude, ou ne garde que les traces d'une instance arrêtée brutalement.
    /// </returns>
    bool IsRunning(string dataDirectory);
}
