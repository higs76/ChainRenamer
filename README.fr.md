[English](README.md) | **Français**

# ChainRenamer

Un logiciel de renommage de fichiers par lot, construit sur une **chaîne de modules**.

> **État :** développement actif (0.x), Windows. L'aperçu, le renommage avec annulation, les chaînes enregistrées et l'aide intégrée fonctionnent ; de nombreux modules restent à écrire.

## Le principe

Les renommeurs classiques présentent toutes leurs règles à la fois, dans des panneaux qui s'appliquent **dans un ordre fixe**, chacun **une seule fois**, et ne montrent que **le résultat final**. Quand le résultat est faux, il faut deviner quelle règle est en cause, et il est impossible de faire, par exemple, un remplacement *après* un changement de casse.

ChainRenamer remplace les panneaux par une **chaîne** :

- **Chaque module fait une seule chose simple** : trier, remplacer un texte, changer la casse… La puissance vient de leur enchaînement.
- **Chaque module travaille sur le résultat du précédent**, comme une chaîne de montage. On les empile dans l'ordre voulu, on les réordonne par glisser-déposer, et on peut utiliser plusieurs fois le même module.
- **On voit le résultat de chaque étape.** Cliquer sur une étape montre chaque nom avant et après elle. Une règle qui se trompe se repère tout de suite.
- **Rien ne touche au disque avant « Appliquer le renommage ».** Les conflits (doublons, caractères interdits, noms réservés de Windows, chemins trop longs) sont signalés à chaque étape, et un renommage appliqué peut être annulé.
- **Une chaîne est une recette.** Elle s'enregistre dans une bibliothèque et se rejoue sur un autre dossier : le même nettoyage pour chaque nouvelle saison d'une série, par exemple.

### Un exemple

Des épisodes téléchargés s'appellent `speedy.01x03.my.first.time.on.the.circuit!.mkv`. La chaîne :

| # | Module | Réglage | Le nom après l'étape |
|---|---|---|---|
| 1 | Source | `\\nas\video\Speedy` | `speedy.01x03.my.first.time.on.the.circuit!.mkv` |
| 2 | Trier | ordre naturel (`1x2` avant `1x10`) | *(même nom, fichiers dans le bon ordre)* |
| 3 | Remplacer | `.` → espace | `speedy 01x03 my first time on the circuit!.mkv` |
| 4 | Remplacer | `!` → rien, à la fin du nom | `speedy 01x03 my first time on the circuit.mkv` |
| 5 | Remplacer (avancé) | `(\d+)x(\d+)` → `S$1E$2` | `speedy S01E03 my first time on the circuit.mkv` |

L'extension n'est jamais touchée par Remplacer : seul un module Extension le fera. Si une règle convient à tous les fichiers sauf quelques-uns, on décoche ces fichiers dans la grille de l'étape : elle les laisse tels quels.

## Ce qui fonctionne aujourd'hui

- **Source** : dossier local ou partage réseau UNC, sous-dossiers ; décocher des fichiers les retire de toute la chaîne. Sélection rapide : case d'en-tête, Maj + clic, rectangle à la souris, Espace, clic droit.
- **Trier** : ordre naturel, croissant ou décroissant, ou ordre manuel par glisser-déposer.
- **Remplacer** : un texte par un autre, en respectant ou non la casse ; toutes les occurrences, la première, la dernière, au début ou à la fin du nom.
- **Remplacer (avancé)** : expressions régulières avec groupes (`$1`, `$2`…).
- **Aperçu** : colonnes Original | Avant l'étape | Après l'étape, conflits en rouge.
- **Renommage sûr** : en deux phases (gère les échanges A↔B, les cycles et les changements de casse seule sur NTFS et SMB), annulable grâce à un journal.
- **Bibliothèque de chaînes** : enregistrer, enregistrer sous, import et export d'une chaîne ou de toute la bibliothèque, emplacement réglable (un NAS ou un dossier synchronisé, par exemple).
- **Favoris**, comme dans un navigateur : rangés dans des dossiers qui deviennent des sous-menus du menu Chaînes (Séries ▸ Anime) ; Ctrl+D ajoute la chaîne ouverte.
- **Aide intégrée** (F1, ou le **?** d'une étape) : le principe de la chaîne, puis une page par module avec ses réglages et des exemples calculés par le module lui-même. Avec une recherche, en français et en anglais.
- **Interface** en anglais et en français, selon la langue de Windows.

## Prévu

- **Modules** : Casse, Supprimer, Nettoyer, Insérer, Numérotation, Extension, Filtrer, Formater les nombres, Dates, Modèle, Table de remplacement, Capturer (groupes nommés réutilisés comme variables).
- **Variables** utilisables dans tous les modules : `{dossier}`, `{date}`, plus tard l'EXIF et les tags audio.
- **Listes de dépôt** : coller ou déposer une liste de lignes (titres d'épisodes…), la ligne N va au fichier N.
- **Segment de nom** : limiter une étape à une partie du nom (« après le dernier ` - ` »).
- **Ligne de commande** : rejouer un fichier `.chain` sur un dossier, dans des scripts ou des tâches planifiées.

## Télécharger

Les versions sont publiées dans les [Releases](../../releases) : un seul `ChainRenamer.exe` pour Windows x64, .NET inclus, rien à installer.

## Technologies

- C# / .NET 10
- [Avalonia UI](https://avaloniaui.net/) 12, thème Fluent
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- xUnit

## Compiler et lancer

Il faut le [SDK .NET 10](https://dotnet.microsoft.com/download).

```
dotnet build
dotnet test
dotnet run --project src/ChainRenamer.App
```

## Organisation du projet

```
src/ChainRenamer.Engine/       le moteur : pipeline, modules, chaînes, renommage (aucune dépendance graphique)
src/ChainRenamer.Cli/          ligne de commande (à venir)
src/ChainRenamer.App/          application de bureau (Avalonia)
tests/ChainRenamer.Engine.Tests/  tests du moteur
tests/ChainRenamer.App.Tests/     tests de l'App sans écran (aide, favoris, traductions)
docs/etude.md                  l'étude de conception (en français)
```

## Licence

[MIT](LICENSE)
