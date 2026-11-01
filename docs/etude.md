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
- « Appliquer le renommage », bien visible.

**Menu (décidé le 31/10/2026)**
```
Fichier   Nouvelle chaîne · Enregistrer · Enregistrer sous… · Importer… · Exporter… · Tout exporter… · Quitter
Chaînes   ★ favoris (dossiers en sous-menus) · Toutes les chaînes ▸ · Dans les favoris · Organiser les favoris… · Supprimer · Gérer la bibliothèque…
Aide      Aide de ChainRenamer (F1) · Rechercher les mises à jour · À propos
```
- **Bibliothèque** : un dossier contenant un fichier `.chain` par chaîne (le nom de la chaîne est le nom du fichier), par défaut `%APPDATA%\ChainRenamer\chains\`. Son emplacement est **réglable** : sur un NAS ou un dossier synchronisé, l'utilisateur la sauvegarde et la partage entre ses PC sans rien exporter.
- Un clic dans le menu Chaînes charge la chaîne ; les favoris (étoile) sont en tête. Enregistrer écrase la chaîne ouverte, Enregistrer sous en crée une autre (pour partir d'une chaîne existante). Une marque dans le titre signale les changements non enregistrés.
- **Favoris (décidé le 01/11/2026)**, comme ceux d'un navigateur : un arbre de dossiers (Séries ▸ Anime), dans l'ordre choisi par l'utilisateur, enregistré dans `favorites.json` à côté des chaînes. Les dossiers deviennent des sous-menus du menu Chaînes. Ctrl+D ajoute ou retire la chaîne ouverte ; la fenêtre « Organiser les favoris » crée, renomme, monte, descend et déplace. Un favori n'est qu'un raccourci : le retirer ne supprime pas la chaîne, supprimer la chaîne retire ses favoris. À l'import d'une sauvegarde et au déplacement de la bibliothèque, les dossiers de même nom fusionnent. Les favoris de la 0.7 (simple liste de noms) sont relus.
- Exporter / Importer une chaîne (son fichier `.chain`, à envoyer à quelqu'un) ; Tout exporter : un `.zip` des chaînes et des préférences, pour une réinstallation.
- Charger une chaîne remplace les étapes et garde la Source : **le dossier source n'est pas enregistré dans la chaîne**. Une chaîne est une recette qu'on rejoue sur n'importe quel dossier ; les dossiers récurrents relèvent de l'historique et des favoris de la Source.

**Aide (décidée le 31/10/2026)**
- Une fenêtre construite comme l'App : la liste des modules à gauche, la page du module choisi à droite (à quoi il sert, ses options, des exemples concrets). Le **?** du panneau d'un module ouvre l'aide sur ce module.
- Les exemples sont **calculés par le moteur** : la page décrit des noms d'entrée et des réglages, le vrai module produit le résultat. Ils restent justes quand un module évolue, et un test le vérifie. Les textes sont dans les `.resx`.
- La clé d'une page est le type du module tel qu'il est écrit dans le `.chain` (`Replace`). Elle servira d'ancre au site d'aide, prévu quand le dépôt sera public (GitHub Pages est payant sur un dépôt privé). L'aide intégrée marche aussi hors connexion.
- **Réalisée en 0.8.0.** Pages générales (« Bien démarrer » : le principe de la chaîne, les cases à cocher, appliquer et annuler, chaînes et favoris, raccourcis) puis une page par module : présentation, réglages (avec les libellés de l'écran), exemples. F1 ou le **?** du panneau ouvrent la page de l'étape sélectionnée ; une recherche ignore la casse et les accents. Les textes sont dans `HelpTexts.resx`, avec un balisage minimal (`` `code` ``, `**gras**`, paragraphes séparés par une ligne vide, puces « • »). Des tests vérifient que chaque module a sa page, que chaque exemple change quelque chose, et que l'anglais et le français ont les mêmes textes.

**Mises à jour (décidées le 31/10/2026)**
1. D'abord prévenir : au démarrage, l'App demande à GitHub la dernière version publiée et propose le lien de téléchargement ; désactivable dans les préférences. L'API de GitHub ne répond pas sur un dépôt privé : à faire quand le dépôt sera public.
2. Plus tard, installer seul (par exemple avec Velopack, licence à vérifier), ce qui suppose un vrai installateur.

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

**Portée d'une étape (31/10/2026, demande de Higs76).** On cherche souvent une règle qui convient à tous les fichiers, et quelques-uns en sont les « dommages collatéraux ». Une étape de module « nom » peut donc ignorer certains fichiers (`Step.Skipped`, décochés dans la grille de l'étape) : le pipeline ne donne au module que les autres, puis remet les fichiers ignorés à leur rang, inchangés. Le module n'en sait rien et reste pur. Une Numérotation numérote ainsi les seuls fichiers concernés, de 1 à k. Les modules « liste » ignorent la portée. Elle est propre à la session, comme les exclusions de Source et l'ordre manuel de Trier ; une portée par condition (« seulement si le nom contient… »), enregistrable, viendra plus tard.

Deux sortes de cases à cocher se ressemblent donc : décocher dans Source retire le fichier de toute la chaîne ; décocher dans une étape la fait seulement sauter pour ce fichier. Une infobulle les distingue, et la page d'aide « Cases à cocher et sélection » l'explique. Sélection rapide dans la grille : case d'en-tête (tout, rien, état partiel), Maj + clic pour une plage, Espace sur les lignes sélectionnées, clic droit pour cocher, décocher ou inverser. Sélection par rectangle comme dans l'Explorateur : appuyer sur une ligne et glisser ; avec Ctrl, les lignes s'ajoutent à la sélection.

**Lisibilité de l'aperçu (retours d'un testeur, 31/10/2026).** Les en-têtes nomment l'étape choisie : « Original (source) », « Avant l'étape 2 · Remplacer », « Après l'étape 2 · Remplacer », avec une infobulle chacun. La colonne Original peut être masquée (petits écrans). Chaque module a une description d'une phrase, affichée à côté du nom de l'étape. Le menu d'ajout suit un ordre logique : modules « liste » d'abord, puis modules « nom » du plus simple au plus avancé ; Remplacer (regex) s'appelle « Remplacer (avancé) ».

**Format `.chain`** (JSON, versionné). Les étapes qui suivent la Source, chacune avec son type, son nom éventuel, son état actif et les réglages de son module :
```json
{
  "version": 1,
  "steps": [
    { "type": "Replace", "name": "Points", "enabled": true,
      "settings": { "find": ".", "replaceWith": " ", "matchCase": false, "occurrence": "all" } }
  ]
}
```
- Le type est un nom stable, déclaré par l'attribut `[Module("Replace")]` de chaque module : renommer la classe ne casse pas les fichiers. Ajouter un module ne touche pas au moteur.
- Un réglage absent prend sa valeur par défaut : une nouvelle option ne rend pas les anciens fichiers illisibles.
- Ne sont pas enregistrés : la Source (dossier, sous-dossiers, fichiers décochés), la portée des étapes et l'ordre manuel de Trier, qui dépendent des fichiers chargés. Les `StepId` sont recréés au chargement.
- Une erreur de lecture est un code (`ChainError` : fichier invalide, version non prise en charge, module inconnu, réglages invalides), que la CLI et l'App traduisent.

**Renommage disque** : deux phases. Chaque fichier passe d'abord par un nom temporaire unique dérivé de son `FichierId`, puis reçoit son nom final. Cela couvre les échanges, les cycles et les changements de casse seule sur NTFS et SMB. Les conflits sont détectés avant l'application, et un journal permet l'annulation.

## 5. Catalogue des modules

Principe : chaque module fait une seule chose simple, et la puissance vient de l'enchaînement.

Le catalogue a été revu le 31/10/2026 en passant en revue les 14 panneaux de Bulk Rename Utility : chacun a été repris, découpé ou écarté (voir la fin de cette section).

**Aide.** Chaque module aura une aide accessible depuis son panneau, qui explique ses options **par un exemple concret** (« `Episode 5` devient `Episode 05` »). C'est indispensable pour les options moins évidentes, comme la remise à zéro de la numérotation.

**Modules « liste »**
- **Source** (fixe) : dossier local ou UNC, sélection, sous-dossiers (avec une profondeur, y compris « sans les fichiers du dossier racine »), fichiers et/ou dossiers, fichiers cachés.
- **Filtrer** : extension, motif, regex, taille, date, longueur du nom. Fait : un critère par étape (on enchaîne plusieurs Filtrer pour les combiner, ce qui fait un « et »), garder ou retirer ; le motif couvre tout le nom (`*.mkv;*.mp4`), les dates comptent en jours entiers, une borne vide ne limite pas. Taille et date viennent de l'énumération du dossier (`FileItem`, puis `Item`).
- **Trier** : tri naturel, date, taille, métadonnée, ordre manuel.

**Modules « nom »**
- **Remplacer** : un texte par un autre, dans le nom sans l'extension, en respectant ou non la casse ; occurrences : toutes (par défaut), la première, la dernière, au début du nom, à la fin du nom.
- **Remplacer (avancé)** : la même chose avec une expression régulière et ses groupes (`$1`). Séparé de Remplacer, qui reste simple pour le cas courant (décision du 31/10/2026).
- **Table de remplacement** : une liste de paires « texte → remplacement », appliquées dans l'ordre ; chaque ligne a sa case active, pour garder une paire sans l'appliquer. Elle se saisit dans une vraie liste, se colle ou se charge depuis un fichier. Exemples : translittération (`ß` → `ss`, `œ` → `oe`), vocabulaire maison réutilisé d'une chaîne à l'autre (`VOSTFR` → `VOST`, `S01` → `Saison 1`).
- **Casse** : minuscules, MAJUSCULES, Titre, Phrase.
  - Les petits mots laissés en minuscules dans un titre dépendent de la langue (français : de, la, le, et, du, des… ; anglais : a, an, the, of…).
  - Les exceptions (mots dont la casse est imposée, comme `TV` ou `iPhone`) se saisissent dans une liste.
  - Deux cases : chiffres romains en majuscules (`Part iii` → `Part III`), sigles laissés tels quels.
  - Option « espace avant les majuscules » (`MyHoliday` → `My Holiday`).
- **Supprimer**, par la position : n premiers ou derniers caractères, entre deux positions (négatives : depuis la fin), avant ou après un texte, entre deux textes (`[1080p]`, `(VOSTFR)`).
- **Nettoyer**, par des cases à cocher : espaces au début et à la fin, doubles espaces, accents (é → e), chiffres, symboles, emojis, points en tête. Le module qu'on met souvent en fin de chaîne.
- **Insérer** : au début, à la fin, à une position, avant ou après un texte, à un marqueur. Le texte peut contenir des variables.
- **Modèle** : le nom entier est remplacé par un modèle, par exemple `{nom} - {dossier} - {n}`.
- **Formater les nombres** : complète un nombre du nom par des zéros (`Episode 5` → `Episode 05`). Fait : tous les nombres, le premier ou le dernier (pour épargner `x265`, `1080p` ou l'année) ; nombre de chiffres fixe, ou automatique (autant que le plus long nombre de la liste) ; option pour retirer les zéros en trop. Seuls les chiffres 0 à 9 comptent.
- **Dates** : trouve une date dans le nom et la réécrit dans un autre format (`01.04.2024` → `2024-04-01`).
- **Numérotation** : début, pas, remplissage (`001`), séparateur, position (début, fin, position) ; chiffres, lettres ou chiffres romains. Remise à zéro **quand le dossier change** ou **quand une variable change** : avec `{saison}` capturée plus haut, les épisodes sont numérotés saison par saison.
- **Extension** : garder, minuscules, majuscules, Titre ; extension imposée, extension ajoutée (`.holiday` → `.holiday.gif`), extension retirée.
- **Capturer** : regex à groupes nommés, qui crée des variables (`{saison}`, `{episode}`) réutilisables plus loin. Déplacer un morceau du nom, c'est Capturer, Supprimer, puis Insérer la variable ailleurs.

**Réalisés en 0.9.0 (01/11/2026) : Casse, Supprimer, Nettoyer, Insérer, Numérotation, Extension.** Précisions de mise en œuvre :
- Casse : les petits mots ambigus (`up`, `off`, `so`) ne sont pas dans la liste anglaise ; les chiffres romains s'arrêtent à XXXIX, pour ne pas prendre « mix », « dix » ou « civil » pour des nombres ; un sigle (mot déjà en majuscules) est gardé sauf si tout le nom est en majuscules (`THE MATRIX` devient `The Matrix`) ; en français, le mot qui suit `l'` ou `d'` prend sa majuscule (`de l'Épisode`). La langue des petits mots d'une nouvelle étape suit celle de l'interface.
- Supprimer : premiers, derniers, d'une position à une autre, avant ou après un texte (première ou dernière occurrence), chaque partie entre deux textes ; textes compris ou non.
- Nettoyer : espaces au début et à la fin et espaces doubles (cochés par défaut), points en tête (sur le nom entier : `.config` n'a pas de radical), accents et ligatures (`œ` → `oe`, `ß` → `ss`), chiffres, symboles (emojis compris), emojis seuls.
- Insérer : au début, à la fin (avant l'extension), après n caractères (négatif : depuis la fin), avant ou après un texte. Les variables viendront avec le jalon 3.
- Numérotation : réutilise les emplacements d'Insérer ; séparateur au début et à la fin ; chiffres, lettres (`z` puis `aa`) ou romains ; remise à zéro par dossier, grâce au dossier porté par chaque `Item` (`Item.Folder`, rempli par le pipeline). La remise à zéro par variable attend Capturer.
- Extension : minuscules, majuscules, initiale, remplacer, ajouter, supprimer ; « garder » revient à ne pas mettre le module.

**Variables communes**
Plutôt que des modules dédiés (Auto Date, Append Folder Name de BRU), le moteur fournit des variables utilisables dans tous les modules qui acceptent du texte (Insérer, Modèle, Remplacer…) :
- `{dossier}`, `{dossier:2}` (le dossier parent, ou celui de niveau 2) ;
- `{date:…}`, avec un sélecteur de source (création, modification, prise de vue, date du jour), de format et un décalage horaire ;
- `{taille}` ;
- les variables de Capturer ;
- plus tard les métadonnées (`{exif.…}`, ID3, vidéo).

**Segment de nom (option d'étape, à confirmer sur des cas concrets)**
La portée dit *quels fichiers* une étape touche ; le segment dirait *quelle partie du nom* : « ne travailler qu'entre … et … » (des positions, ou des textes comme du premier `(` au premier `)`). Le pipeline découpe le nom, donne le morceau au module, puis recolle, comme il le fait pour la portée : les modules restent purs. Exemples :
- `Serie - 01x01 - le titre de l'épisode` : une Casse Titre limitée à ce qui suit le dernier ` - ` ne touche pas au nom de la série ;
- `Film (2019) [1080p]` : un Remplacer ou un Nettoyer limité à ce qui est avant ` (` ne risque pas d'abîmer l'année ni les étiquettes.

**Listes de dépôt**
On colle une liste de lignes (presse-papiers), ou on dépose un fichier texte dans le module, par exemple des titres copiés depuis TheTVDB.
- **Liste – remplacer** : chaque ligne devient le nom ; l'extension est conservée. La ligne N va au fichier de rang N, d'où l'intérêt d'un module Trier juste avant.
- **Liste – insérer** : la ligne est insérée à une position, après un texte ou à un marqueur.
- Mode **paires** : chaque ligne donne « ancien nom → nouveau nom » ; le fichier est retrouvé par son nom et non par son rang (l'« Import Rename-Pairs » de BRU).
- Un avertissement apparaît si le nombre de lignes diffère du nombre de fichiers, ou si une paire ne trouve pas son fichier.

**Ce qu'on ne reprend pas de BRU**
- Name → Reverse (nom à l'envers) et Remove (nom vidé : un Modèle vide le fait).
- Move/Copy : couvert par Capturer + Supprimer + Insérer.
- Les bases de numérotation exotiques (2 à 36, chiffres hébreux).
- Special → Order : notre chaîne est libre. Attributs et horodatages des fichiers : ce n'est pas du renommage.
- Special → JavaScript : pas pour l'instant (voir « Module script » plus bas).
- New Location (copier ou déplacer vers un autre dossier) n'est pas un module : ce sera une option « copier vers… » de l'Applicateur, pour travailler sans risque sur une copie.

**Marqueurs de placement**
Un module pose un repère (par exemple `$$`) selon des conditions, à un endroit variable. Un module suivant insère à ce repère, un dernier le retire. Ce sont des modules ordinaires, sans fonctionnalité spéciale dans le moteur. Option de confort : avertir si un marqueur subsiste dans le nom final.

**Plus tard**
- Métadonnées : EXIF, ID3, vidéo.
- Applicateur : « copier vers… » un autre dossier, en gardant ou non l'arborescence.
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
  - `ChainRenamer.Engine.Tests` : les tests du moteur ;
  - `ChainRenamer.App.Tests` : les tests de l'App qui n'ont pas besoin d'écran (aide, favoris, textes traduits).

**Projet international** (décision du 30 octobre 2026). Le nom de travail « Renommeur » devient **ChainRenamer**, avec un dépôt GitHub public.
- Le code est en anglais : `Step`, `FileItem`, `StepResult`, `Applier`, modules `Replace`, `Sort`… La correspondance complète avec les termes de cette étude se trouve dans le `CLAUDE.md`.
- L'interface est localisable : fichiers `.resx`, anglais par défaut et traduction française. Le moteur ne produit aucun texte affiché : il renvoie des codes que les interfaces traduisent.
- Il y a deux README : `README.md` en anglais et `README.fr.md` en français. Cette étude et les commits restent en français.
- Les chaînes enregistrées portent l'extension `.chain`.

## 7. Idées de confort (à prioriser plus tard)

- Source : historique des dossiers récemment choisis, et dossiers mis en favoris (idée de Higs76, 31/10/2026).
- Glisser-déposer de fichiers et de dossiers dans la Source.
- Menu contextuel de l'Explorateur : « Renommer avec ChainRenamer ».
- Export de l'aperçu en CSV, pour un essai à blanc.
- Affichage de toutes les étapes côte à côte sur grand écran (4K).

## 8. Jalons

1. **Fondations** : solution, moteur avec cache et recalcul, modules Source, Remplacer et Trier, Applicateur en deux phases, CLI minimale, App minimale. ✅
2. **Modules de base** : Casse, Supprimer, Nettoyer, Insérer, Numérotation, Extension (faits en 0.9.0), Filtrer, Formater les nombres (faits en 0.10.0) ; aide par module. ✅
3. **Puissance** : variables communes, Capturer, Modèle, Dates, Table de remplacement, Listes de dépôt, segment de nom, marqueurs.
4. **Confort** : enregistrement des chaînes dans l'App, colonne réductible, annulation, menu de l'Explorateur.
5. **Métadonnées** : EXIF, ID3.
