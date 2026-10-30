# ChainRenamer — étude de démarrage

*Synthèse des échanges du 29 octobre 2026. Maquette : https://claude.ai/artifact/GNaX3GvCNdMa9Vj1fVfJJR*

## 1. Le besoin

Bulk Rename Utility (https://www.bulkrenameutility.co.uk/) est puissant, mais ses modules sont figés. Pour un renommage avancé, il faut renommer réellement une première fois, puis repartir du résultat pour réutiliser un module. C'est lent et risqué.

ChainRenamer part d'une source, montre chaque résultat intermédiaire, et permet d'enchaîner librement les modules. On peut ensuite enregistrer la chaîne pour la rejouer sur de futurs fichiers.

## 2. Ce que fait Bulk Rename Utility, et ses limites

On choisit un dossier, les fichiers s'affichent avec une colonne « Nouveau nom » qui sert d'aperçu. Les règles sont réparties en panneaux numérotés, traités de gauche à droite :

1. RegEx
2. Name (garder, supprimer, inverser, nom fixe)
3. Replace
4. Case
5. Remove
6. Move/Copy (déplacer une partie du nom)
7. Add (préfixe, suffixe, insertion, tags EXIF et ID3)
8. Auto Date
9. Append Folder Name
10. Numbering
11. Extension
12. Filters
13. Copy/Move to Location
14. Special (attributs, horodatages, translitération, JavaScript)

Il propose aussi l'import CSV, un batch d'annulation, un journal et la récursion dans les sous-dossiers.

Ses limites :
- **ordre fixe** : impossible, par exemple, de faire Replace avant RegEx (demande récurrente sur leur forum depuis 2010) ;
- **chaque module n'existe qu'une fois** ;
- **seul le résultat final** est visible ;
- **interface dense** : tous les panneaux sont affichés en même temps.

## 3. Interface (validée)

**Colonne de gauche : la chaîne de modules**
- Les blocs s'empilent de haut en bas, dans l'ordre d'exécution, ce qui rend l'accumulation visible.
- Le bloc n°1 **Source** est fixe : dossier local ou UNC, sélection des fichiers à traiter (par exemple 25 sur 100).
- Les autres blocs ont une **poignée** pour les réordonner par glisser-déposer. Chacun affiche son numéro, son type, un **nom optionnel** (pour distinguer deux modules du même type), un résumé de ses réglages et son nombre de modifications.
- Chaque bloc peut être dupliqué, désactivé ou supprimé. Un bouton « + Ajouter un module » termine la liste.
- **Une étape désactivée doit sauter aux yeux**, aussi bien dans la colonne étendue que réduite : bloc grisé et semi-transparent, titre barré, icône « pause », mention « désactivée » à la place du nombre de modifications. Quand elle est sélectionnée, l'aperçu le rappelle par un bandeau : « Étape désactivée : la sortie est identique à l'entrée ».
- **La colonne se réduit** : il ne reste que le numéro et une icône par module, et le survol affiche le détail en surimpression.

**Partie droite, en haut : l'aperçu**
- Grille à trois colonnes : **Original** (source) | **Avant** (sortie du module précédent) | **Après** (ce module).
- Les changements sont surlignés.
- La colonne Original se masque ou se réaffiche d'une flèche, pratique sur les petits écrans.
- Résumé au-dessus de la grille : nombre de noms, de modifications et de conflits.

**Partie droite, en bas : la configuration du module sélectionné**
- Les réglages du module, sa case « actif » et son nom optionnel.

**Barre du haut**
- Ouvrir et enregistrer une chaîne, et « Appliquer le renommage ».

## 4. Modèle de données du moteur

Le moteur fonctionne comme une petite base de données en mémoire :

| Table | Contenu |
|---|---|
| **Fichier** | `FichierId`, chemin d'origine, métadonnées (dates, taille, EXIF…) |
| **Etape** | `EtapeId` (Guid stable), `Position`, `Type`, `Nom?`, `Parametres`, `Actif` |
| **Resultat** | clé (`EtapeId`, `FichierId`) → `Nom`, `Rang`, `Inclus`, variables capturées |

Règles :
- l'entrée de l'étape p est la sortie de l'étape active précédente ;
- le recalcul part de la première étape touchée. Modifier l'étape p recalcule à partir de p ; déplacer une étape de i vers j recalcule à partir de min(i, j) ;
- une étape désactivée recopie son entrée ;
- pas de SGBD : des collections en mémoire suffisent. La chaîne est enregistrée en JSON.

**Mise en œuvre (jalon 1, étape 2).** Une ligne de Resultat est un `Item(FileId, Name)`, et chaque étape produit une liste ordonnée d'`Item` :
- le **rang** est la position dans la liste : le module Trier réordonne la liste ;
- un fichier est **inclus** tant qu'il est présent dans la liste : le module Filtrer le retire. L'aperçu retrouve les fichiers exclus à une étape en comparant son entrée et sa sortie.

Les modules « nom » n'ont donc jamais à ignorer les fichiers exclus. Les modules sont des `record` immuables : changer un réglage, c'est remplacer le module de l'étape, ce qui déclenche le recalcul. Renommer une étape ne recalcule rien.

**Renommage disque** : deux phases. Chaque fichier passe d'abord par un nom temporaire unique dérivé de son `FichierId`, puis reçoit son nom final. Cela couvre les échanges, les cycles et les changements de casse seule sur NTFS et SMB. Les conflits sont détectés avant l'application, et un journal permet l'annulation.

## 5. Catalogue des modules

Principe : chaque module fait une seule chose simple, et la puissance vient de l'enchaînement.

**Modules « liste »**
- **Source** (fixe) : dossier local ou UNC, sélection, sous-dossiers, fichiers et/ou dossiers.
- **Filtrer** : extension, motif, regex, taille, date.
- **Trier** : tri naturel, date, taille, métadonnée, ordre manuel.

**Modules « nom »**
- **Remplacer** : texte ou regex.
- **Casse** : minuscules, MAJUSCULES, Titre (avec mots exclus), Phrase.
- **Supprimer** : n premiers ou derniers caractères, entre deux positions, entre deux textes.
- **Insérer** : à une position, avant ou après un texte, à un marqueur.
- **Numérotation** : début, pas, remplissage (001), position.
- **Extension** : changer l'extension, changer sa casse.
- **Capturer** : regex à groupes nommés, qui crée des variables (`{saison}`, `{episode}`) réutilisables plus loin.

**Listes de dépôt**
On colle une liste de lignes, par exemple des titres copiés depuis TheTVDB. La ligne N va au fichier de rang N, d'où l'intérêt d'un module Trier juste avant.
- **Liste – remplacer** : chaque ligne devient le nom ; l'extension est conservée.
- **Liste – insérer** : la ligne est insérée à une position, après un texte ou à un marqueur.
- Un avertissement apparaît si le nombre de lignes diffère du nombre de fichiers.

**Marqueurs de placement**
Un module pose un repère (par exemple `$$`) selon des conditions, à un endroit variable. Un module suivant insère à ce repère, un dernier le retire. Ce sont des modules ordinaires, sans fonctionnalité spéciale dans le moteur. Option de confort : avertir si un marqueur subsiste dans le nom final.

**Plus tard**
- Métadonnées : EXIF, ID3, vidéo.
- Variables : `{dossier}`, `{date}`, `{compteur}`.
- Récupération de titres via une API de séries.
- Module script.

## 6. Choix techniques

Contraintes : projet perso, aucune licence payante, application de bureau Windows avec un portage possible plus tard.

| Techno | Plateformes | Verdict |
|---|---|---|
| WinForms | Windows | Daté ; le glisser-déposer et l'aperçu riche seraient à coder à la main |
| WPF | Windows | Mature, mais Windows seulement |
| WinUI 3 | Windows 10 et 11 | Look Windows 11 natif, plateforme officielle ; Windows seulement, pas de designer visuel |
| **Avalonia UI** | Windows, macOS, Linux | **Retenu** : XAML proche de WPF, framework MIT, thème Fluent |
| Uno Platform | Toutes | Trop lourd à mettre en place pour ce besoin |
| .NET MAUI | Windows, macOS, mobile | Pas de Linux, orienté mobile |

Décisions :
- **C#, .NET 10, Avalonia 12, xUnit**.
- Éditeur **VS Code** sur la VM Windows, avec l'extension « Avalonia for Code » (version gratuite, usage non commercial). Pas de designer : on écrit le XAML à la main, avec la prévisualisation et le Hot Reload.
- Architecture **à la HandBrake** : un moteur, plusieurs interfaces.
  - `ChainRenamer.Engine` : bibliothèque sans interface ;
  - `ChainRenamer.Cli` : rejoue une chaîne, utilisable dans des scripts et des tâches planifiées ;
  - `ChainRenamer.App` : l'interface Avalonia ;
  - `ChainRenamer.Engine.Tests` : les tests.

**Projet international** (décision du 30 octobre 2026). Le nom de travail « Renommeur » devient **ChainRenamer**, avec un dépôt GitHub public.
- Le code est en anglais : `Step`, `FileItem`, `StepResult`, `Applier`, modules `Replace`, `Sort`… La correspondance complète avec les termes de cette étude se trouve dans le `CLAUDE.md`.
- L'interface est localisable : fichiers `.resx`, anglais par défaut et traduction française. Le moteur ne produit aucun texte affiché : il renvoie des codes que les interfaces traduisent.
- Il y a deux README : `README.md` en anglais et `README.fr.md` en français. Cette étude et les commits restent en français.
- Les chaînes enregistrées portent l'extension `.chain`.

## 7. Idées de confort (à prioriser plus tard)

- Bibliothèque de chaînes enregistrées.
- Source : historique des dossiers récemment choisis, et dossiers mis en favoris (idée de Higs76, 31/10/2026).
- Glisser-déposer de fichiers et de dossiers dans la Source.
- Menu contextuel de l'Explorateur : « Renommer avec ChainRenamer ».
- Export de l'aperçu en CSV, pour un essai à blanc.
- Affichage de toutes les étapes côte à côte sur grand écran (4K).

## 8. Jalons

1. **Fondations** : solution, moteur avec cache et recalcul, modules Source, Remplacer et Trier, Applicateur en deux phases, CLI minimale, App minimale.
2. **Modules de base** : Casse, Supprimer, Insérer, Numérotation, Extension, Filtrer.
3. **Puissance** : Capturer avec ses variables, Listes de dépôt, marqueurs.
4. **Confort** : enregistrement des chaînes dans l'App, colonne réductible, annulation, menu de l'Explorateur.
5. **Métadonnées** : EXIF, ID3.
