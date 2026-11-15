# Compteur retraite — document de conception

Date : 15/11/2026 · Statut : en attente de validation finale

## 1. Intention

Jean-Philippe part bientôt à la retraite. On veut qu'il voie chaque jour, sur le bureau de son PC Windows 11, combien de **jours travaillés** il lui reste, avec une petite phrase positive qui change tous les jours.

Il utilise peu l'ordinateur : tout doit se faire par des fenêtres simples, à la souris, sans jamais ouvrir un fichier texte. Son fond d'écran lui appartient : le programme ne le modifie pas, il affiche le compteur **par-dessus**.

## 2. Objectifs, non-objectifs, anti-objectifs

**Objectifs**
- Afficher sur le bureau, derrière les icônes et les fenêtres, le nombre de jours travaillés restants, un pourcentage de progression et une phrase du jour.
- Un écran de réglage simple : prénom, date de retraite, calendrier des jours non travaillés, actions en lot, apparence.
- Installation en un double-clic, sans droits administrateur, sans rien d'autre à installer.
- Mise à jour automatique depuis GitHub, prête dans le code, activée quand le dépôt existera.
- Tests automatiques sur toute la logique (calculs, réglages, phrases, mise à jour).

**Non-objectifs (pour cette version)**
- Actions en lot limitées à une période (« de septembre à décembre ») : plus tard.
- Plusieurs écrans : le compteur s'affiche sur l'écran principal seulement.
- Compteur toujours visible au-dessus des fenêtres.
- Signature du programme (avertissement SmartScreen accepté au premier lancement).

**Anti-objectifs (échec même si « ça marche »)**
- Le fond d'écran de Jean-Philippe est modifié ou remplacé.
- Il doit éditer un fichier, taper une commande ou comprendre un message technique.
- Le compteur gêne : il capte des clics, apparaît dans Alt+Tab ou la barre des tâches, ou passe devant ses fenêtres.
- Une mise à jour perd ses réglages ou ses jours de vacances.

## 3. Contraintes

- Windows 11 (y compris 24H2 et suivantes), compte utilisateur standard, **PC personnel** de Jean-Philippe (non géré par une entreprise). Si le « Contrôle intelligent des applications » est actif, il bloquera l'exe non signé : le guide de test manuel indique comment le vérifier.
- Le dépôt GitHub sera **public** *(hypothèse de l'assistant)*. La mise à jour utilise l'API publique, sans jeton.
- Développement et compilation depuis Linux (vérifié : WPF .NET 10 se compile sous Linux).
- Un seul fichier livré, sans prérequis sur le PC cible.

## 4. Approches envisagées

| Approche | Pour | Contre |
|---|---|---|
| .NET Framework 4.8 + WinForms | ~200 Ko, rien à embarquer | Rendu daté |
| **.NET 10 + WPF, exe autonome compressé** (retenue) | Rendu moderne, version maintenue jusqu'en 11/2028, se compile sous Linux | Exe d'environ 62 Mo |
| Avalonia | Joli, écrans testables sans Windows | Dépendance de plus, moins direct pour l'intégration au bureau |

Choix de l'utilisateur : .NET 10, pour un rendu plus joli. WPF est la conséquence de ce choix, et la compilation sous Linux a été vérifiée par un essai.

.NET 8 a été écarté car il n'est plus maintenu depuis le 10/11/2026.

## 5. Ce qui a été décidé avec l'utilisateur

### 5.1 Le compte des jours
- On compte les **jours travaillés**. Ne sont pas comptés :
  - les week-ends ;
  - les jours fériés français (voir 6.2) ;
  - les jours marqués « non travaillé » dans le calendrier ;
  - les jours concernés par une action en lot.
- **Actions en lot** : par exemple « Ne travaille pas le [mercredi] », qui s'applique **à partir d'aujourd'hui et jusqu'à la retraite**. On peut ensuite rectifier à la main n'importe quel jour.

### 5.2 Le pourcentage
- Il part de **0 % le jour de l'installation** et atteint **100 % le jour de la retraite**.
- La date d'installation est enregistrée au premier lancement. Ni une mise à jour ni une réinstallation ne la remettent à zéro.

### 5.3 La phrase du jour
- **100 phrases** positives et bienveillantes, **au vouvoiement**, qui peuvent utiliser le prénom (« Jean-Philippe » par défaut, modifiable).
- Deux types de phrases liées au contexte :
  - les **saisons et moments de l'année** : rentrée, fêtes, printemps, avant l'été… ;
  - les **étapes du compte à rebours** : sous 1 000, 500, 365 ou 100 jours, mi-parcours, dernier mois, dernière semaine, dernier jour.
- Pas de répétition avant d'avoir vu toutes les phrases générales.

### 5.4 L'écran de réglage
- Une vraie fenêtre, et aucun fichier de réglages à éditer.

### 5.5 Mise à jour
- Préparée pour GitHub. Le dépôt sera ajouté plus tard ; d'ici là, la mise à jour est désactivée.

## 6. Conception détaillée (choix techniques de l'assistant)

### 6.1 Organisation du code

```
CompteurRetraite.sln
src/CompteurRetraite.Core/     net10.0         toute la logique, aucune dépendance Windows
src/CompteurRetraite.App/      net10.0-windows WPF : calque, réglages, icône, installation
tests/CompteurRetraite.Core.Tests/  net10.0    xUnit, s'exécute sous Linux et Windows
.github/workflows/ci.yml       compile + tests sur windows-latest à chaque push
.github/workflows/release.yml  sur tag vX.Y.Z : publie l'exe en asset de release
docs/test-manuel-windows.md    liste de vérifications à faire sur un PC Windows
```

Tout ce qui peut être testé vit dans `Core`. L'App reste une couche fine : affichage et appels Windows.

### 6.2 Calendrier de travail (Core)

Le statut d'un jour se décide dans cet ordre de priorité (le premier qui s'applique l'emporte) :
1. **Correction manuelle** du jour (travaillé ou non travaillé) ;
2. **Règle en lot** active ce jour-là (jour de semaine, date de début) ;
3. **Jour férié** français ;
4. **Week-end** ;
5. sinon **travaillé**.

- **Jours fériés** (France métropolitaine) : 1er janvier, lundi de Pâques, 1er mai, 8 mai, Ascension, lundi de Pentecôte, 14 juillet, 15 août, 1er novembre, 11 novembre, 25 décembre. Pâques est calculé par l'algorithme grégorien anonyme.
  - Le lundi de Pentecôte est compté comme férié. Il peut être rectifié à la main s'il est travaillé (journée de solidarité).
- **Week-ends** : non cliquables, jamais travaillés.
- **Appliquer une règle en lot** : on enregistre « jour de semaine X non travaillé à partir de la date D ». Les corrections manuelles existantes sur les jours X à partir de D sont **effacées**, pour que le résultat soit celui affiché à l'écran. Les corrections faites ensuite l'emportent.
- **Action inverse** : « Retravaille le [mercredi] » à partir d'aujourd'hui. Elle termine la règle à la veille, et efface aussi les corrections futures de ce jour de semaine.
- **Jours restants** : nombre de jours travaillés depuis **aujourd'hui inclus** jusqu'à la date de retraite **exclue**. Dans l'écran de réglage, la date est libellée « Premier jour de retraite ».
- **Pourcentage** = jours travaillés entre l'installation et hier ÷ jours travaillés entre l'installation et la retraite, arrondi à l'unité et borné entre 0 et 100.
  - Les jours de vacances ne font donc pas avancer la barre.
  - Si la date de retraite est avant ou égale à la date d'installation, le pourcentage vaut 100.

### 6.3 Phrases (Core)

- Les 100 phrases sont stockées dans un fichier JSON intégré au programme. Chacune a un texte (avec `{prenom}` éventuel) et une catégorie :
  - `general` ;
  - `saison` (avec un intervalle de dates jour/mois) ;
  - `etape` (avec une condition).
- **Choix de la phrase du jour**, de façon déterministe pour une date donnée :
  1. si une étape est franchie **aujourd'hui** (premier jour sous le seuil, mi-parcours, dernier jour…), on prend la phrase de cette étape ;
  2. sinon, un jour sur trois, une phrase de saison valable à cette date, s'il y en a ;
  3. sinon, la phrase générale suivante dans un ordre mélangé, enregistré dans les réglages. On remélange quand toutes ont été vues.
- La phrase choisie pour une date est mémorisée : elle ne change pas si le programme redémarre dans la journée.
- **Après la retraite**, le jour J et les suivants, un message spécial s'affiche : « Bonne retraite, {prenom} ! », à la place du chiffre et de la barre.
- Répartition prévue : environ 60 phrases générales, 20 de saison et 20 d'étape. Les phrases seront soumises à l'utilisateur pour relecture. Cette relecture ne bloque pas le développement : une première version des phrases est livrée, et le fichier JSON pourra être remplacé ensuite sans toucher au code.

### 6.4 Réglages et données (Core)

- Un fichier JSON stocké dans `%LocalAppData%\CompteurRetraite\donnees.json`. Il contient :
  - le prénom et la date de retraite ;
  - la date d'installation ;
  - les règles en lot et les corrections manuelles ;
  - l'apparence (coin, taille, thème) ;
  - l'ordre de mélange et l'historique des phrases ;
  - un numéro de version de schéma.
- Écriture atomique : on écrit dans un fichier temporaire, puis on le renomme. Si le fichier est corrompu, on le met de côté en `.bak` et on repart des valeurs par défaut, en gardant la date d'installation si elle est lisible.

### 6.5 Calque sur le bureau (App)

- Une fenêtre WPF sans bordure, à fond transparent, avec les styles Windows : clics traversants, pas d'activation, absente de la barre des tâches et d'Alt+Tab.
- **Placement derrière les icônes** :
  - on envoie le message `0x052C` à `Progman`, puis on rattache la fenêtre à la couche `WorkerW` sur les versions avant 24H2, ou directement à `Progman` sur 24H2 et suivantes ;
  - **repli** si le rattachement échoue : une fenêtre placée tout en bas de la pile, au-dessus du fond d'écran.
- On se rattache de nouveau au redémarrage de l'Explorateur (message `TaskbarCreated`) et lors d'un changement de résolution ou d'écran.
- **Contenu** :
  - la phrase du jour ;
  - le grand chiffre ;
  - le libellé « jours travaillés avant la retraite » ;
  - une fine barre de progression avec le pourcentage.
  - Texte clair avec une ombre douce, ou foncé selon le réglage.
- **Rafraîchissement** à minuit, à la sortie de veille, à un changement d'heure système, et à chaque modification des réglages.

### 6.6 Icône près de l'horloge (App)

- Elle utilise `NotifyIcon` de WinForms, en activant WinForms en plus de WPF.
- **Menu** : « Réglages… », « Masquer le compteur » ou « Afficher le compteur », « Quitter ».
- Un double-clic ouvre les réglages.
- Une seule instance tourne à la fois : relancer le programme ouvre les réglages de l'instance déjà en marche.

### 6.7 Écran de réglage (App)

Grands caractères, gros boutons, aucun jargon. La fenêtre contient :
- **Prénom**.
- **Premier jour de retraite**, avec un sélecteur de date.
- **Calendrier mensuel** :
  - flèches pour changer de mois et bouton « Aujourd'hui » ;
  - couleurs distinctes pour travaillé, non travaillé, férié, week-end et aujourd'hui, avec une légende ;
  - un clic inverse le statut d'un jour (les week-ends ne sont pas cliquables) ;
  - les jours passés sont visibles mais grisés et non modifiables.
- **Actions en lot** :
  - « Je ne travaille plus le [jour] à partir d'aujourd'hui » ;
  - « Je travaille de nouveau le [jour] à partir d'aujourd'hui ».
  - Une confirmation en langage simple précède l'application, par exemple : « 245 mercredis seront marqués non travaillés. »
- **Apparence** : coin (4 choix), taille (Petit, Moyen ou Grand), couleur (Clair ou Foncé).
- **Résumé en direct** : « Il reste 1 042 jours travaillés ».
- **Enregistrement automatique** : pas de bouton « Enregistrer » à oublier.
- En bas de la fenêtre, discret : « Désinstaller le compteur ».

### 6.8 Installation et désinstallation (App)

- **Premier lancement** depuis un emplacement quelconque :
  1. l'exe se copie dans `%LocalAppData%\Programs\CompteurRetraite\CompteurRetraite.exe` ;
  2. il écrit la clé `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` ;
  3. il enregistre la date d'installation ;
  4. il ouvre l'écran de réglage en mode « bienvenue » (prénom et date de retraite) ;
  5. il démarre le calque.
- **Désinstaller** : confirmation, retrait de la clé Run, suppression du dossier du programme et des données. L'exe se supprime lui-même via une commande différée, après sa fermeture.
- **Nouvelle version lancée à la main** (seul moyen de livrer tant que GitHub n'est pas branché) *(choix de l'assistant)* :
  - si l'exe lancé est **plus récent** que celui installé, il demande à l'instance en cours de se fermer (via le canal de l'instance unique), remplace l'exe installé, puis relance ;
  - si l'exe lancé est de la même version ou plus ancien, il se contente d'ouvrir les réglages de l'instance en cours.
- **Réinstallation** *(choix de l'assistant)* : il s'agit de relancer l'installeur sur une installation existante, et la date d'installation est conservée. Une désinstallation complète efface tout, date d'installation comprise ; le pourcentage repart alors de 0.

### 6.9 Mise à jour (Core + App)

- **Source** : l'API GitHub des releases (`/repos/{proprietaire}/{depot}/releases/latest`).
  - Le dépôt est défini par une propriété de compilation, vide par défaut, ce qui **désactive** la mise à jour.
  - L'URL de l'API est injectable, pour les tests.
- **Version** : tag `vX.Y.Z`, comparé à la version de l'assembly.
- **Asset** attendu : `CompteurRetraite.exe`.
- **Vérification** au démarrage, puis toutes les 24 h, en silence.
- **Téléchargement** dans `CompteurRetraite.exe.new` :
  - vérification de la taille annoncée et de l'empreinte SHA-256 (champ `digest` de l'asset GitHub) ;
  - en cas d'échec, le fichier est supprimé et on réessaie plus tard.
- **Remplacement**, au moment opportun (au prochain démarrage, ou tout de suite si aucune fenêtre de réglage n'est ouverte) :
  1. renommer l'exe en cours en `.old` ;
  2. placer le nouveau ;
  3. relancer ;
  4. supprimer `.old` au démarrage suivant.
  - En cas d'échec, on restaure l'ancien exe.
- Aucune erreur de réseau n'est jamais montrée à Jean-Philippe.
- **Workflow `release.yml`** : sur un tag `vX.Y.Z`, compile l'exe autonome compressé et le joint à la release.

### 6.10 Tests

**Automatiques (xUnit, sur Core)**
- Jours fériés sur plusieurs années, avec des dates de Pâques connues.
- Priorité entre corrections, règles, fériés et week-ends.
- Application et inversion des règles en lot, avec effacement des corrections.
- Jours restants (bornes incluse et exclue), pourcentage (vacances, bornes, retraite passée).
- Choix des phrases :
  - déterminisme ;
  - pas de répétition avant épuisement ;
  - étapes le bon jour ;
  - saisons ;
  - remplacement du prénom ;
  - le fichier contient bien 100 phrases valides.
- Réglages : aller-retour JSON, fichier corrompu, écriture atomique.
- Mise à jour :
  - comparaison de versions ;
  - lecture de la réponse GitHub (avec un faux serveur HTTP) ;
  - vérification SHA-256 ;
  - séquence de remplacement dans un dossier temporaire, y compris en cas d'échec ;
  - désactivation quand aucun dépôt n'est configuré.

**CI** : `ci.yml` compile la solution complète et lance les tests sur `windows-latest`.

**Manuels** : `docs/test-manuel-windows.md` décrit, sur un vrai Windows 11, les vérifications suivantes :
- installation ;
- calque derrière les icônes ;
- changement de fond d'écran ;
- clics traversants ;
- redémarrage de l'Explorateur ;
- veille ;
- réglages ;
- désinstallation ;
- mise à jour.

## 7. Laissé au développeur

- Le détail visuel : polices (Segoe UI Variable), couleurs, marges et animations, dans l'esprit de la maquette. La maquette est l'artefact « Compte à rebours retraite » (https://claude.ai/artifact/PS72DiXa4ZCQKNRkqKSgTC), onglet « Fond d'écran » : gros chiffre blanc en bas à droite, avec son libellé en majuscules espacées en dessous.
- Le format exact du JSON et des phrases.
- La gestion fine des cas limites d'affichage (DPI, barre des tâches à gauche…), tant que les anti-objectifs sont respectés.
