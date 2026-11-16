# Compteur retraite

Un petit programme Windows 11 qui affiche, **sur le bureau, par-dessus le fond d'écran et derrière les icônes**, le nombre de jours travaillés restant avant la retraite, avec un pourcentage de progression.

- Le fond d'écran n'est jamais modifié.
- Rien à installer : un seul fichier `CompteurRetraite.exe`, sans droits administrateur.
- Un écran de réglage contient le prénom, la date de retraite, un calendrier des jours non travaillés, les actions en lot (par exemple « je ne travaille plus le mercredi ») et l'apparence.
- Les week-ends et les jours fériés français sont retirés automatiquement.
- Le programme se met à jour tout seul depuis les releases GitHub.

Conception : [docs/superpowers/specs/2026-11-15-compteur-retraite-design.md](docs/superpowers/specs/2026-11-15-compteur-retraite-design.md)

## Organisation

| Dossier | Rôle |
|---|---|
| `src/CompteurRetraite.Core` | Toute la logique : fériés, calendrier, compteur, réglages, mise à jour. Testable partout. |
| `src/CompteurRetraite.App` | L'application Windows (WPF) : compteur sur le bureau, icône près de l'horloge, réglages, installation. |
| `tests/CompteurRetraite.Core.Tests` | Tests automatiques (xUnit). |
| `docs/test-manuel-windows.md` | Ce qu'il faut vérifier à la main sur un vrai Windows. |

## Compiler et tester (Linux ou Windows)

Il faut le SDK .NET 10. Sous Linux, il peut être installé dans le dossier personnel :

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir "$HOME/.dotnet"
```

Puis :

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test
scripts/publier.sh 0.3.1     # produit publish/CompteurRetraite.exe en version 0.3.1, qui se mettra à jour depuis caminin/Retraite
```

**Numéro de version :** un exe donné à la main ne remplace celui déjà installé que si sa version est **plus grande**. Avec la même version, il ouvre seulement les réglages. Augmentez donc le numéro à chaque livraison : 0.2.0, 0.3.0…

Sous Windows, `CompteurRetraite.exe --sans-installation` lance le compteur sans l'installer, ce qui est pratique pour essayer.

## Tester sur un Windows 11 virtuel (sans autre PC)

Une machine virtuelle Windows 11 (LTSC 2024, base 24H2, évaluation gratuite de 90 jours) tourne dans Docker grâce à [dockur/windows](https://github.com/dockur/windows) :

```bash
sudo systemctl start docker        # moteur Docker natif : Docker Desktop n'a pas accès à KVM
tools/vm/vm.sh demarrer            # 1re fois : téléchargement + installation automatique (~30 min)
```

- L'écran de Windows s'affiche dans le navigateur : <http://localhost:8006>.
- Le dossier `publish/` du projet apparaît dans Windows sous le nom **Shared**, sur le bureau (lecteur `Z:`). Après `scripts/publier.sh`, le nouvel exe y est donc directement.
- La VM est un Windows 11 Pro 25H2 non activé (filigrane), en français, avec un **clavier US**. Le visualiseur web envoie du QWERTY, et un clavier AZERTY dans Windows inverserait les touches.
- Le compte Windows est `Docker`, mot de passe `admin` (valeurs par défaut de dockur, pour cette VM de test uniquement). Un accès SSH par clé est installé automatiquement ; la clé est rangée dans `~/VMs/compteur-retraite-ssh`.
- Commandes utiles :

  | Commande | Effet |
  |---|---|
  | `tools/vm/vm.sh installer-exe` | Copie `publish/CompteurRetraite.exe` dans Téléchargements et le lance, comme un double-clic. |
  | `tools/vm/vm.sh ssh '<commande PowerShell>'` | Exécute une commande dans Windows, par exemple pour lire `$env:LOCALAPPDATA\CompteurRetraite\journal.txt`. |
  | `tools/vm/vm.sh diag` | Liste les fenêtres du bureau et celles du compteur, pour comprendre un problème d'affichage. |
- Le disque de la VM (environ 20 Go) est dans `~/VMs/compteur-retraite`, hors du projet.
- `tools/vm/vm.sh arreter` arrête Windows proprement, `tools/vm/vm.sh etat` affiche son état.

## Dépôt GitHub

Le code est sur <https://github.com/caminin/Retraite>. Ce dépôt doit rester **public** : le compteur y lit les releases sans mot de passe.

À chaque envoi, GitHub compile et lance les tests sur Windows (`.github/workflows/ci.yml`).

## Publier une version

```bash
git tag v0.2.0
git push origin v0.2.0
```

GitHub lance les tests, compile l'exe et crée une release qui contient `CompteurRetraite.exe` (`.github/workflows/release.yml`). Les compteurs déjà installés la récupèrent d'eux-mêmes, au plus tard 24 heures après, ou à l'ouverture de session suivante.

**Important :** seuls les exe à partir de la version 0.3.0 connaissent l'adresse du dépôt et savent se mettre à jour. Une version 0.2.x déjà installée doit être remplacée une fois à la main, par un exe téléchargé depuis les [releases](https://github.com/caminin/Retraite/releases).

## Installer chez Jean-Philippe

1. Téléchargez `CompteurRetraite.exe` depuis la [dernière release](https://github.com/caminin/Retraite/releases/latest).
2. Double-cliquez dessus. Si Windows affiche « Windows a protégé votre ordinateur », cliquez sur **Informations complémentaires** puis **Exécuter quand même**. Le programme n'est pas signé numériquement.
3. La fenêtre de bienvenue s'ouvre : indiquez la date du premier jour de retraite. C'est fini.

Le programme est copié dans `%LocalAppData%\Programs\CompteurRetraite\` et ses réglages sont dans `%LocalAppData%\CompteurRetraite\`. Pour le retirer, ouvrez les Réglages puis cliquez sur « Désinstaller le compteur ».
