# Démarrer un nouveau projet : VS Code + Git + GitHub

Procédure générale, indépendante du langage, à donner à Claude au début d'un nouveau projet.
Poste : VM Windows 11 Pro, VS Code + extension Claude Code, session non administrateur.

## 1. Avant tout
- Lire le `CLAUDE.md` et les documents de `docs/` du projet.
- Répondre en français. Expliquer brièvement toute notion ou tout outil récent.
- Proposer et valider avant les gros changements. Ne commiter et pousser que sur demande.
- Vérifier ce qui est installé avant de proposer une installation, puis demander l'accord.

## 2. État du poste (déjà en place, à vérifier seulement)
```powershell
git --version
git config --show-origin user.name     # attendu : Higs76, depuis le .gitconfig global de l'utilisateur
git config --show-origin user.email    # attendu : 18425700+higs76@users.noreply.github.com
gh auth status                         # attendu : connecté à github.com en tant que higs76
```
- Identité Git **publique sans nom réel** : pseudo + adresse noreply fournie par GitHub. À ne pas remplacer par le vrai nom ou une adresse personnelle.
- Côté GitHub, l'option *Settings → Emails → Block command line pushes that expose my email* est activée.
- Si l'identité était vide sur une autre machine :
  ```powershell
  git config --global user.name  "Higs76"
  git config --global user.email "18425700+higs76@users.noreply.github.com"
  git config --global init.defaultBranch main
  ```

## 3. Créer le dépôt local
Au choix :
- **VS Code** : icône *Contrôle de code source* (branche, à gauche) → *Initialiser le dépôt* ;
- **terminal** : `git init`.

Puis, **avant le premier commit** :
1. Ajouter un `.gitignore` adapté au langage, pour exclure les sorties de build, les dépendances, les fichiers d'IDE et les secrets.
   Exemples : `dotnet new gitignore` (.NET), modèles de github.com/github/gitignore pour les autres langages.
2. Vérifier qu'aucun secret n'est présent : mots de passe, chaînes de connexion, clés d'API, fichiers `.env`.
3. Premier commit :
   ```powershell
   git branch -M main
   git add .
   git commit -m "Premier commit"
   ```

## 4. Publier sur GitHub
Au choix :
- **VS Code** : bouton *Publier la branche* → *dépôt privé* ou *public*. VS Code crée le dépôt sur le compte et le relie.
- **Terminal / Claude** :
  ```powershell
  gh repo create NOM --private --source . --remote origin --push
  ```
  - `--source .` : utilise le dépôt local courant.
  - `--remote origin` : enregistre l'adresse GitHub sous le nom habituel `origin`.
  - `--push` : envoie les commits.

Par défaut, créer en **privé**, puis passer en public si besoin. Un dépôt public expose **tout l'historique**.

Opérations courantes ensuite :
| Besoin | Commande |
|---|---|
| Rendre public / privé | `gh repo edit higs76/NOM --visibility public --accept-visibility-change-consequences` |
| Renommer | GitHub → *Settings → Repository name*, puis `git remote set-url origin https://github.com/higs76/NOUVEAU.git` |
| Vérifier le lien | `git remote -v` et `git status -sb` (doit afficher `main...origin/main`) |
| Lister ses dépôts | `gh repo list higs76` |
| Cycle quotidien | VS Code : message → *Valider* → *Synchroniser* (ou `git add`, `git commit`, `git push`) |

À éviter : réutiliser un ancien dépôt existant pour un nouveau projet ; en créer un propre.

## 5. Pièges rencontrés
- **Outil installé mais introuvable dans VS Code** (`dotnet`, `node`, `python`… après un `winget install`) : VS Code hérite de l'environnement de l'Explorateur Windows, qui date d'avant l'installation. Relancer VS Code ne suffit pas : il faut redémarrer l'Explorateur (Gestionnaire des tâches), fermer la session ou redémarrer la VM. En attendant, appeler l'outil par son chemin complet.
- **Avertissements « LF will be replaced by CRLF »** : normaux sous Windows, sans conséquence.
- **`gh api ... --jq '...'` sous PowerShell 5.1** : les guillemets sont mal transmis. Passer par l'outil Bash.
- **Actions administrateur** (fonctionnalités Windows, services…) : la session de Claude n'a pas les droits. Préparer une commande ou un script que le développeur lance lui-même, puis demander de copier la sortie.
- **Avant de supprimer ou d'écraser** : vérifier la cible. Préférer un nouveau dossier temporaire à un nettoyage.
