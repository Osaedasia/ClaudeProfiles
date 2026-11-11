# Profils Claude

Petit lanceur multi-profils pour Claude Desktop (Avalonia, .NET 10) : chaque profil ouvre Claude sur son propre
dossier de données (`--user-data-dir`), donc avec sa propre connexion, ses conversations et ses réglages.
Plusieurs profils peuvent tourner en même temps.

## Ce que fait l'application

- liste le profil d'origine de Claude et tous les profils créés, avec leur état (ouvert / fermé) ;
- ouvre un profil, ou ramène au premier plan sa fenêtre s'il est déjà ouvert ;
- crée, renomme, recolore et supprime des profils (suppression refusée tant que Claude tourne dessus) ;
- reprend automatiquement les dossiers `Claude-<nom>` créés à la main ;
- reste dans la zone de notification (menu : ouvrir un profil, gérer les profils, quitter) ; fermer la fenêtre ne
  quitte pas l'application ;
- sous Windows, marque chaque instance d'un profil autre que celui d'origine : bouton de barre des tâches séparé
  avec le logo Claude teinté de la couleur du profil et son initiale, icône de notification teintée, info-bulle
  « Claude · <profil> ». Épingler ce bouton rouvre directement le profil (`ClaudeProfiles.exe --open <dossier>`).

Le marquage n'existe que sous Windows : macOS et Linux ne permettent pas de changer l'icône du Dock ou de la barre
de menus d'une autre application sans la modifier, ce qui casserait sa signature et ses mises à jour.

### Pièges connus sous Windows

- Claude Desktop est un paquet MSIX : tout dossier qu'**il** crée dans AppData est redirigé vers
  `%LOCALAPPDATA%\Packages\Claude_…\LocalCache`, invisible des autres applications. Profils Claude crée donc
  lui-même les dossiers d'un profil avant de lancer Claude.
- Un programme lancé depuis Claude Desktop (par exemple par Claude Code) hérite de ce conteneur. Profils Claude le
  détecte et se relance par l'Explorateur.
- L'Explorateur garde en cache les icônes par chemin de fichier : le nom des icônes générées porte un numéro de
  version du dessin (`ArtworkVersion`), à incrémenter à chaque modification du rendu.

## Où sont les données

| Système | Profil d'origine | Profils créés | Liste des profils |
|---|---|---|---|
| Windows | `%APPDATA%\Claude` | `%APPDATA%\Claude-<nom>` (+ journaux dans `%LOCALAPPDATA%\Claude-<nom>`) | `%APPDATA%\ClaudeProfiles\profiles.json` |
| macOS | `~/Library/Application Support/Claude` | `~/Library/Application Support/Claude-<nom>` | `~/Library/Application Support/ClaudeProfiles/profiles.json` |
| Linux | `~/.config/Claude` | `~/.config/Claude-<nom>` | `~/.config/ClaudeProfiles/profiles.json` |

Sous Linux, Claude Desktop n'a pas de version officielle : l'application cherche un exécutable `claude-desktop` dans le `PATH`.

## Compiler, tester, publier

```bash
dotnet build
dotnet test
dotnet publish src/ClaudeProfiles -c Release -r win-x64 -p:SelfContained=false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o publish/win-x64
```

Remplacer `win-x64` par `osx-arm64`, `osx-x64` ou `linux-x64` pour les autres systèmes. L'exécutable publié
(environ 27 Mo) suffit, avec le runtime .NET 10 installé ; les fichiers `.pdb` déposés à côté sont les symboles de
débogage de SkiaSharp et HarfBuzz et peuvent être supprimés.
