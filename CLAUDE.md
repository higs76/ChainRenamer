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
| Résultat d'une étape | `StepResult` : ses `Items` et son erreur éventuelle (le module a levé une exception : l'étape recopie son entrée) |
| Chaîne en mémoire | `Pipeline` |
| Applicateur | `Applier` |
| Chaîne (`.chaine`) | `Chain` (`.chain`) |
| Source, Filtrer, Trier | `Source`, `Filter`, `Sort` |
| Remplacer, Remplacer (avancé, regex), Table de remplacement, Casse, Supprimer, Nettoyer, Insérer, Modèle, Formater les nombres, Dates, Numérotation, Extension, Capturer | `Replace`, `RegexReplace`, `ReplaceTable`, `Case`, `Remove`, `Clean`, `Insert`, `Template`, `PadNumbers`, `Dates`, `Numbering`, `Extension`, `Capture` |
| Variables communes, segment de nom | `Variables`, `Step.Segment` |
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
  ChainRenamer.App.Tests/     tests de l'App sans écran : aide, favoris, textes en et fr
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
dotnet run --project src/ChainRenamer.Cli -- --chain example.chain [--subfolders] [--apply] <folder>
```

## Règles d'architecture (à respecter strictement)

1. **Le moteur ne connaît pas l'interface.** `ChainRenamer.Engine` ne référence ni Avalonia ni la console. Tout ce que font la CLI et l'App passe par lui.
2. **Les modules sont purs.** Un module reçoit une liste d'éléments et en renvoie une nouvelle. Il ne touche jamais au disque. Seul l'`Applier` renomme réellement, et une seule fois, à la fin.
3. **Deux familles de modules :**
   - **liste** (Source, Filter, Sort) : ils changent quels fichiers sont inclus et leur rang ;
   - **nom** (Replace, Case, Insert, Numbering…) : ils changent le nom. Ils implémentent `INameModule` (un élément en sortie par élément en entrée, même ordre) ; leur étape peut alors ignorer certains fichiers (`Step.Skipped`, la « portée »), que le pipeline cache au module puis remet à leur rang, inchangés.
4. **Le module Source est fixe** : toujours en position 0, non déplaçable, non supprimable. Il accepte les chemins locaux et UNC (`\\serveur\partage\...`).
5. **Les identifiants sont stables.** Une étape est référencée par son `StepId` (Guid), jamais par sa position. Un fichier est référencé par son `FileId`.
6. **Recalcul en cascade, minimal.** On recalcule à partir de la première étape touchée :
   - modification ou activation d'une étape p → à partir de p ;
   - déplacement de i vers j → à partir de min(i, j).
   Les résultats des étapes antérieures restent en cache.
7. **Une étape désactivée recopie son entrée** telle quelle.
8. **La chaîne est sérialisée en JSON** (`.chain`) : liste ordonnée de { type, nom optionnel, actif, paramètres }. Le format doit être versionné (`"version": 1`).
9. **Extensibilité.** Chaque module implémente une interface commune (`IModule`) et porte un nom stable (`[Module("Replace")]`), écrit dans les `.chain` et servant de clé à son aide. Ajouter un module ne doit pas modifier le moteur.
10. **Le moteur ne produit pas de texte affiché.** Il renvoie des codes (par exemple un `enum ConflictKind`) que la CLI et l'App traduisent. Cela garde la localisation hors du moteur.

## Renommage disque : pièges à couvrir (avec tests)

- **Deux phases** : 1) les fichiers dont le nouveau nom est encore pris par un fichier du renommage (échange A↔B, cycle, décalage de numéros, casse seule) passent par un nom temporaire unique dérivé de leur `FileId` ; 2) les autres reçoivent directement leur nom, puis ceux qui attendaient reçoivent le leur. Depuis la 0.11.0 (`Applier.NeedingTemporary`) : avant, tous passaient par le nom temporaire, ce qui doublait les déplacements.
- **Changement de casse seule** (`photo.jpg` → `Photo.jpg`) : NTFS et SMB sont insensibles à la casse, donc on passe obligatoirement par la phase temporaire.
- **Conflits détectés avant application**, et visibles dans l'aperçu à chaque étape : doublons, caractères interdits (`<>:"/\|?*`), noms réservés (`CON`, `PRN`, `AUX`, `NUL`, `COM1-9`, `LPT1-9`), point ou espace final, nom vide, longueur de chemin.
- **Journal d'annulation** (JSON) écrit au fur et à mesure, pour tout défaire, même après une erreur au milieu du traitement.
- **Réseau UNC** : énumération asynchrone (latence), erreurs d'accès remontées proprement, aucun blocage de l'interface.

Mise en œuvre (`Engine/Renaming`) :
- `RenamePlan.Create(files, output, exists?)` : les renommages et les conflits (`ConflictKind`). Sans `exists`, pas d'accès disque : c'est ainsi que chaque étape de l'aperçu montre ses conflits. Avec `exists`, la vérification finale juste avant d'appliquer (un fichier a pu apparaître depuis le chargement).
- `Applier.ApplyAsync` : nom temporaire `~chainrenamer-<FileId>.tmp`, `File.Move` sans écrasement ; à la moindre erreur, tout est défait.
- Journal en **JSON Lines** (une ligne par déplacement, vidée sur le disque à chaque ligne). Chaque annulation y est aussi écrite (`"Undo": true`) : `Journal.ToUndo` sait ce qui reste à défaire, et `UndoAsync` peut être relancé sans risque. L'App range les journaux dans `%LOCALAPPDATA%\ChainRenamer\journals\`.

## Façon de travailler

- Le **moteur se développe piloté par les tests** : un module = sa classe + ses tests (cas nominal, cas limites, extension conservée…).
- Petites étapes livrables, un commit par étape cohérente.
- Avant d'ajouter une dépendance NuGet, vérifier sa licence et le dire.
- Garder `docs/etude.md` et le catalogue des modules à jour quand une décision change.
- Garder les deux README synchronisés.

## L'App (Avalonia)

- **Un module dans l'App** = `ViewModels/Modules/XxxSettingsViewModel` (hérite de `ModuleSettingsViewModel`, fabrique le module avec `ToModule()`, et se remplit depuis un module chargé par son constructeur `XxxSettingsViewModel(XxxModule? module = null)`) + `Views/Modules/XxxSettingsView.axaml` (trouvée par le `ViewLocator` grâce au nom) + ses textes dans les deux `.resx` + une ligne `ModuleType.Of<XxxModule>(…)` dans `ModuleCatalog` + **sa page d'aide** dans `HelpCatalog` (clé = nom `[Module]`, réglages avec les libellés de `Strings`, exemples `HelpExample` calculés par le vrai module) et ses textes dans `HelpTexts.resx` / `HelpTexts.fr.resx`. Côté moteur, le module porte `[Module("Xxx")]`. Les tests de `ChainRenamer.App.Tests` refusent un module sans page ou un exemple qui ne change rien.
- **Aide** : `HelpWindow` (une seule, à côté de la fenêtre principale), ouverte par F1, le menu Aide ou le **?** du panneau, à la page de l'étape sélectionnée. Balisage des textes : `` `code` ``, `**gras**`, ligne vide entre paragraphes (`Views/RichText.cs`).
- **Favoris** : arbre `Favorite` (moteur, `favorites.json`), sous-menus du menu Chaînes, fenêtre `FavoritesWindow` / `FavoritesViewModel` (chaque changement est enregistré aussitôt). Ctrl+D ajoute ou retire la chaîne ouverte.
- **Chaînes et bibliothèque** : `MainViewModel.Chains.cs` (partie de `MainViewModel`). Une chaîne est « modifiée » quand son JSON diffère du dernier enregistré. Les questions (renommer ? abandonner les modifications ? supprimer ?) passent par `Confirm(texte, oui, action)`, affichée dans la barre du haut ; « Enregistrer sous » y saisit le nom. Les sélecteurs de fichiers et le menu Chaînes (reconstruit à chaque ouverture) sont dans `MainWindow.axaml.cs`.
- **Préférences** : `Preferences` (record), dans `%APPDATA%\ChainRenamer\settings.json` : emplacement de la bibliothèque, colonne Original, langue. Fenêtre **Outils > Paramètres…** (`SettingsWindow`, liée directement à `MainViewModel`, partie `MainViewModel.Settings.cs`) ; la langue s'applique au redémarrage (bouton « Redémarrer maintenant »). Un harnais d'essai change `Preferences.FilePath` pour ne pas toucher aux réglages de Higs76.
- **Textes** : `Resources/Strings.resx` (anglais) et `Strings.fr.resx`. La classe `Strings` est générée par MSBuild (métadonnées `StronglyTyped*` dans le `.csproj`). En XAML : `{x:Static res:Strings.Cle}`.
- Langue de l'interface : `--lang en` / `--lang fr`, sinon celle des paramètres, sinon celle de Windows.
- **Gros dossiers** (mesuré sur 50 000 fichiers, 01/11/2026) : la grille se remplit par `RowCollection.ReplaceAll` (une seule notification Reset, pas une par ligne) ; `UpdateRows` réordonne sur place, sans `IndexOf` sur toute la liste, et repasse par un Reset au-delà de 50 déplacements ; un changement de réglage passe par `RefreshSoon` (`Dispatcher.Post` en priorité Background) pour que des frappes rapides ne coûtent qu'un recalcul. Résultat : chargement 0,9 s, une frappe 0,3 à 0,7 s selon la chaîne (avant : 3 s et 1,6 à 3,5 s). Le moteur seul calcule 9 étapes sur 50 000 noms en 250 ms environ. Sur le NAS (`travail\charge`, 2 000 fichiers de 0 à 5 Mo, 5,3 Go) : énumération 0,65 s, vérification du disque avant application 1,5 s, environ 6 ms par déplacement : 12 s pour 2 000 renommages directs, 25 s si tous passent par un nom temporaire.
- **Pas de police Inter** : ses ligatures affichent `01x01` sous la forme `01×01`. L'aperçu doit montrer les caractères exacts, donc on garde la police du système.
- **Vérifier visuellement** : l'App peut être rendue sans écran (Avalonia.Headless + Skia, dans un projet jetable hors du dépôt) pour enregistrer des captures. Ne jamais faire `await` d'une tâche réellement asynchrone dans le harnais : pomper `Dispatcher.UIThread.RunJobs()` jusqu'à ce qu'elle soit terminée, sinon tout se bloque.

## Versions

- SemVer, une seule fois dans `Directory.Build.props` (`<Version>`), affichée dans le titre de l'App. Chaque version publiée : une étiquette `vX.Y.Z` et une Release GitHub (notes en anglais puis en français, `--prerelease` tant qu'on est en 0.x) avec l'exécutable autonome zippé, `ChainRenamer-X.Y.Z-win-x64.zip`.
- Fabriquer l'exécutable (un seul `ChainRenamer.exe`, .NET embarqué ; ne zipper que l'exe, pas les `.pdb`) :
  ```
  dotnet publish src/ChainRenamer.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o <dossier>
  ```
  Même commande pour `src/ChainRenamer.Cli`, zippée à part : `ChainRenamer.Cli-X.Y.Z-win-x64.zip` (depuis la 0.10.0).

## Jalon 1 (premier objectif)

Ordre revu le 31/10/2026 : l'interface d'abord, en aperçu seul, pour que Higs76 puisse tester tôt sur son PC principal ; l'Applier ensuite.

1. ✅ Créer la solution et les 4 projets (plus le `.gitignore` et les deux README).
2. ✅ Moteur : modèle (`FileItem`, `Step`, `Item`), pipeline avec cache et recalcul en cascade.
3. ✅ Modules **Source**, **Replace** (texte et regex) et **Sort** (tri naturel), avec leurs tests. Le parcours du disque est fait par `FileScanner` (asynchrone), pas par le module Source. Reste à faire plus tard : Source sur des dossiers (et pas seulement des fichiers), Sort par date et taille.
4. ✅ Pipeline robuste : une étape dont le module lève une exception (regex en cours de saisie) recopie son entrée, et `Pipeline.GetResult` la signale en erreur.
5. ✅ App en aperçu seul, localisable : colonne de modules (monter, descendre, supprimer, désactiver), grille Original / Avant / Après, panneau de réglages de Source, Replace et Sort.
6. ✅ Version 0.1.0 publiée : exécutable autonome et Release GitHub.
   - 0.2.0 (retours de Higs76) : le nom de l'étape est séparé et étiqueté dans le panneau (il était pris pour le texte à remplacer) ; Sort accepte un ordre manuel (`SortModule.ManualOrder`, propre à la session).
   - 0.3.0 : poignées ≡ de glisser-déposer pour les modules et pour les fichiers (étape Sort), avec défilement automatique au bord. Mécanisme commun : `Views/DragReorder.cs`, qui repère l'élément de classe `handle` dans le modèle d'un élément de liste. En headless, les `DispatcherTimer` ne se déclenchent pas : simuler des mouvements de souris pour tester.
7. ✅ `Applier` en deux phases, avec détection des conflits et journal, plus ses tests ; barre du haut de l'App : « Appliquer le renommage » (confirmation intégrée), « Annuler ce renommage », conflits en rouge dans la grille. Version 0.4.0, essayée sur `\\MonNas\video\travail`.
   - Consolidation (retours de Higs76, 31/10/2026) : portée des étapes « nom » (`Step.Skipped`) ; Replace classique (casse, occurrences toutes, première ou dernière ; nom sans extension) et regex sortie dans `RegexReplace` ; sélection rapide dans la grille (case d'en-tête, Maj + clic, Espace, clic droit : cocher, décocher, inverser).
   - 0.6.0 (retours d'un testeur) : sélection par rectangle (`Views/MarqueeSelect.cs`, Ctrl pour ajouter) ; en-têtes « Avant / Après l'étape n · nom » ; colonne Original masquable ; description de chaque module (`ModuleSettingsViewModel.Description`) à côté du nom de l'étape ; menu d'ajout ordonné (Trier, Remplacer, Remplacer (avancé)).
   - 0.7.0 : format `.chain` (`Chain`, JSON versionné, types `[Module("…")]`, erreurs `ChainError`) ; bibliothèque (`ChainLibrary` : dossier de `.chain` + `favorites.json`, import, export, zip de sauvegarde, déplacement) ; menus Fichier et Chaînes ; préférences ; Remplacer « au début / à la fin du nom » ; README qui explique le principe.
   - 0.8.0 : favoris en dossiers comme dans un navigateur ; aide intégrée (pages générales et une page par module, exemples calculés, recherche) ; projet `ChainRenamer.App.Tests`.
8. ✅ CLI minimale : charger une `.chain` (chemin ou nom dans la bibliothèque, celle des paramètres de l'App par défaut), afficher l'aperçu et les conflits, appliquer avec `--apply`, annuler avec `--undo <journal>`. Codes de sortie 0 / 1 (conflits, erreur) / 2 (arguments). Le moteur fournit `new Pipeline(source, chain)` et `Journal.NewPath()`, partagés avec l'App.

## Jalon 2 (en cours)

- ✅ 0.9.0 : fenêtre Outils > Paramètres (langue de l'interface, appliquée au redémarrage) ; modules **Casse**, **Supprimer**, **Nettoyer**, **Insérer**, **Numérotation** (remise à zéro par dossier via `Item.Folder`), **Extension**, chacun avec ses tests, son panneau et sa page d'aide. Les listes déroulantes des réglages utilisent `Choice(Value, Label)`.
- ✅ CLI (étape 8 du jalon 1).
- ✅ Module **Filtrer** (un critère par étape : nom, extension, taille, date de modification, longueur ; garder ou retirer). `FileItem` et `Item` portent la taille et la date, lues par `FileScanner` pendant l'énumération (aucun accès de plus par fichier sur un partage). L'aide montre les fichiers retirés, barrés.
- ✅ Module **Formater les nombres** (`PadNumbers` : tous, premier ou dernier nombre ; chiffres fixes ou automatiques ; zéros en trop).
- ✅ Test de charge sur 50 000 fichiers (voir « Gros dossiers » dans la partie App).
- Version 0.10.0 : jalon 2 terminé.
