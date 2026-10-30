# CLAUDE.md — ChainRenamer

ChainRenamer est un logiciel de renommage de fichiers par **chaîne de modules**. C'est une évolution de Bulk Rename Utility : les modules s'enchaînent librement, chacun travaille sur le résultat du précédent, et on voit le résultat de chaque étape avant d'appliquer quoi que ce soit sur le disque.

L'étude complète (besoin, interface, modèle de données, catalogue des modules, choix techniques) est dans [`docs/etude.md`](docs/etude.md). Lis-la avant toute décision d'architecture.

## Travailler avec Higs76

- Échanges **en français**.
- Higs76 est un développeur expérimenté (VB.NET, ASP.NET WebForms), nouveau en C# moderne et en Avalonia. **Explique ce qui est nouveau** : syntaxe C# récente, XAML Avalonia, MVVM, bindings, async… Fais-le brièvement, au moment où ça apparaît dans le code, avec l'équivalent VB.NET quand ça aide.
- Préférer les solutions **concises et élégantes** aux variantes verbeuses.
- Projet perso : **aucune dépendance ni aucun outil payant**. Seules les licences libres sont acceptées : MIT, Apache 2.0, BSD.

## Langues (projet international, dépôt GitHub public)

| Quoi | Langue |
|---|---|
| Identifiants du code (projets, classes, membres, fichiers) | **anglais** |
| Commentaires du code | anglais |
| Textes de l'interface | **localisables** : fichiers `.resx`, anglais par défaut et traduction française, langue choisie selon la culture de Windows. Aucun texte en dur dans le XAML ni dans les ViewModels. |
| Messages de la CLI | anglais |
| `README.md` / `README.fr.md` | anglais / français, avec un lien de l'un vers l'autre en tête |
| `docs/etude.md`, ce `CLAUDE.md` | français |
| Messages de commit | **français**, à l'impératif (« Ajoute le module Case ») |
| Échanges avec Higs76 | français |

Correspondance avec l'étude (rédigée en français) :

| Étude | Code |
|---|---|
| Moteur | `Engine` |
| Fichier, FichierId | `FileItem`, `FileId` |
| Étape, EtapeId | `Step`, `StepId` |
| Résultat (une ligne) | `Item` ; la sortie d'une étape est une liste ordonnée d'`Item` (rang = position, inclus = présent) |
| Chaîne en mémoire | `Pipeline` |
| Applicateur | `Applier` |
| Chaîne (`.chaine`) | `Chain` (`.chain`) |
| Source, Filtrer, Trier | `Source`, `Filter`, `Sort` |
| Remplacer, Casse, Supprimer, Insérer, Numérotation, Extension, Capturer | `Replace`, `Case`, `Remove`, `Insert`, `Numbering`, `Extension`, `Capture` |
| Liste – remplacer / insérer | `ListReplace` / `ListInsert` |

## Environnement

- VM de développement **Windows** (sous Proxmox), éditeur **VS Code** avec l'extension C# Dev Kit et « Avalonia for Code » (version gratuite).
- .NET SDK 10 (LTS), C# avec `Nullable` activé et `ImplicitUsings`.
- Interface : **Avalonia 12**, thème Fluent, MVVM avec **CommunityToolkit.Mvvm** (MIT). Ne pas utiliser les composants payants (anciennement « Accelerate »), comme le TreeDataGrid pro, le MediaPlayer ou les Developer Tools (`AvaloniaUI.DiagnosticsSupport`, `.WithDeveloperTools()`, retirés du modèle de projet). `Avalonia.Controls.DataGrid` (MIT) convient.
- Tests : **xUnit**.
- Dépôt GitHub de Higs76 : `higs76/ChainRenamer`.

## Structure de la solution

```
ChainRenamer.slnx
Directory.Build.props         réglages communs (net10.0, Nullable, ImplicitUsings) : ne pas les répéter dans les .csproj
src/
  ChainRenamer.Engine/        bibliothèque : pipeline, modules, application des renommages. AUCUNE dépendance graphique.
  ChainRenamer.Cli/           ligne de commande : rejouer une chaîne enregistrée sur un dossier
  ChainRenamer.App/           interface Avalonia (MVVM)
tests/
  ChainRenamer.Engine.Tests/  tests unitaires du moteur
docs/
  etude.md
README.md                     anglais
README.fr.md                  français
LICENSE                       MIT, © Higs76
```

Commandes :

```
dotnet build
dotnet test
dotnet run --project src/ChainRenamer.App
dotnet run --project src/ChainRenamer.Cli -- --chain example.chain <folder>
```

## Règles d'architecture (à respecter strictement)

1. **Le moteur ne connaît pas l'interface.** `ChainRenamer.Engine` ne référence ni Avalonia ni la console. Tout ce que font la CLI et l'App passe par lui.
2. **Les modules sont purs.** Un module reçoit une liste d'éléments et en renvoie une nouvelle. Il ne touche jamais au disque. Seul l'`Applier` renomme réellement, et une seule fois, à la fin.
3. **Deux familles de modules :**
   - **liste** (Source, Filter, Sort) : ils changent quels fichiers sont inclus et leur rang ;
   - **nom** (Replace, Case, Insert, Numbering…) : ils changent le nom.
4. **Le module Source est fixe** : toujours en position 0, non déplaçable, non supprimable. Il accepte les chemins locaux et UNC (`\\serveur\partage\...`).
5. **Les identifiants sont stables.** Une étape est référencée par son `StepId` (Guid), jamais par sa position. Un fichier est référencé par son `FileId`.
6. **Recalcul en cascade, minimal.** On recalcule à partir de la première étape touchée :
   - modification ou activation d'une étape p → à partir de p ;
   - déplacement de i vers j → à partir de min(i, j).
   Les résultats des étapes antérieures restent en cache.
7. **Une étape désactivée recopie son entrée** telle quelle.
8. **La chaîne est sérialisée en JSON** (`.chain`) : liste ordonnée de { type, nom optionnel, actif, paramètres }. Le format doit être versionné (`"version": 1`).
9. **Extensibilité.** Chaque module implémente une interface commune (`IModule`) et décrit ses paramètres. Ajouter un module ne doit pas modifier le moteur.
10. **Le moteur ne produit pas de texte affiché.** Il renvoie des codes (par exemple un `enum ConflictKind`) que la CLI et l'App traduisent. Cela garde la localisation hors du moteur.

## Renommage disque : pièges à couvrir (avec tests)

- **Deux phases** : 1) chaque fichier passe par un nom temporaire unique dérivé de son `FileId` ; 2) chaque fichier reçoit son nom final. Cela gère les échanges (A↔B), les cycles et les renumérotations.
- **Changement de casse seule** (`photo.jpg` → `Photo.jpg`) : NTFS et SMB sont insensibles à la casse, donc on passe obligatoirement par la phase temporaire.
- **Conflits détectés avant application**, et visibles dans l'aperçu à chaque étape : doublons, caractères interdits (`<>:"/\|?*`), noms réservés (`CON`, `PRN`, `AUX`, `NUL`, `COM1-9`, `LPT1-9`), point ou espace final, nom vide, longueur de chemin.
- **Journal d'annulation** (JSON) écrit au fur et à mesure, pour tout défaire, même après une erreur au milieu du traitement.
- **Réseau UNC** : énumération asynchrone (latence), erreurs d'accès remontées proprement, aucun blocage de l'interface.

## Façon de travailler

- Le **moteur se développe piloté par les tests** : un module = sa classe + ses tests (cas nominal, cas limites, extension conservée…).
- Petites étapes livrables, un commit par étape cohérente.
- Avant d'ajouter une dépendance NuGet, vérifier sa licence et le dire.
- Garder `docs/etude.md` et le catalogue des modules à jour quand une décision change.
- Garder les deux README synchronisés.

## Jalon 1 (premier objectif)

1. ✅ Créer la solution et les 4 projets (plus le `.gitignore` et les deux README).
2. ✅ Moteur : modèle (`FileItem`, `Step`, `Item`), pipeline avec cache et recalcul en cascade.
3. ✅ Modules **Source**, **Replace** (texte et regex) et **Sort** (tri naturel), avec leurs tests. Le parcours du disque est fait par `FileScanner` (asynchrone), pas par le module Source. Reste à faire plus tard : Source sur des dossiers (et pas seulement des fichiers), Sort par date et taille.
4. `Applier` en deux phases, avec détection des conflits et journal, plus ses tests.
5. CLI minimale : charger une `.chain`, afficher l'aperçu, appliquer avec `--apply`.
6. App Avalonia minimale, localisable : colonne de modules, grille Original / Avant / Après, panneau de configuration. À prévoir d'abord : une étape dont le module lève une exception (regex en cours de saisie, par exemple) ne doit pas casser l'aperçu. Le pipeline la marque en erreur et recopie son entrée.
