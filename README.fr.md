[English](README.md) | **Français**

# ChainRenamer

Un logiciel de renommage de fichiers par lot, construit sur une **chaîne de modules**.

Les renommeurs classiques appliquent leurs règles dans un ordre fixe, chacune une seule fois, et ne montrent que le résultat final. ChainRenamer permet d'empiler les modules dans n'importe quel ordre et d'utiliser plusieurs fois le même. On voit le résultat de **chaque étape** avant que rien ne touche au disque. Une chaîne s'enregistre pour être rejouée plus tard sur de nouveaux fichiers, depuis l'application ou en ligne de commande.

> **État :** début du développement. Rien d'utilisable pour l'instant.

## Fonctionnalités prévues

- **Modules « liste »**, qui décident quels fichiers sont inclus et dans quel ordre : Source (dossiers locaux et partages réseau UNC), Filtrer, Trier (tri naturel, date, taille…).
- **Modules « nom »**, qui modifient le nom : Remplacer, Remplacer (regex), Casse, Supprimer, Insérer, Numérotation, Extension, Capturer (groupes nommés d'une regex, réutilisés comme variables).
- **Listes de dépôt** : on colle une liste de lignes, par exemple des titres d'épisodes, et la ligne N va au fichier N.
- **Aperçu étape par étape** : Original | Avant | Après, avec les changements surlignés et les conflits signalés (doublons, caractères interdits, noms réservés, longueur de chemin).
- **Renommage sûr** : renommage en deux phases (il gère les échanges, les cycles et les changements de casse seule) et journal d'annulation.
- **Ligne de commande** : rejouer un fichier `.chain` sur un dossier, dans des scripts ou des tâches planifiées.
- **Interface** en anglais et en français.

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
dotnet run --project src/ChainRenamer.Cli -- --chain example.chain <dossier>
```

## Organisation du projet

```
src/ChainRenamer.Engine/       le moteur : pipeline, modules, renommage (aucune dépendance graphique)
src/ChainRenamer.Cli/          ligne de commande
src/ChainRenamer.App/          application de bureau (Avalonia)
tests/ChainRenamer.Engine.Tests/
```

## Licence

[MIT](LICENSE)
