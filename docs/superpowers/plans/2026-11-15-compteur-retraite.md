# Compteur retraite — plan de développement

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal :** un exe Windows 11 autonome qui affiche sur le bureau, derrière les icônes, les jours travaillés restants avant la retraite de Jean-Philippe, un pourcentage et une phrase du jour, avec un écran de réglage et une mise à jour prête pour GitHub.

**Architecture :** toute la logique (calendrier, fériés, compteur, phrases, données, décisions d'installation, mise à jour) vit dans `CompteurRetraite.Core` (net10.0, testable sous Linux). `CompteurRetraite.App` (net10.0-windows, WPF + NotifyIcon WinForms) n'est qu'une couche d'affichage et d'appels Win32 au-dessus.

**Tech Stack :** .NET 10 SDK (`$HOME/.dotnet`), C#, WPF, System.Text.Json, xUnit, GitHub Actions.

**Spec :** `docs/superpowers/specs/2026-11-15-compteur-retraite-design.md`

## Global Constraints

- Cibles : Core et tests en `net10.0` ; App en `net10.0-windows` avec `UseWPF` et `UseWindowsForms` à `true` ; `EnableWindowsTargeting=true` dans `Directory.Build.props` pour compiler sous Linux.
- Publication : `-r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`. Nom de l'exe : `CompteurRetraite.exe`.
- Aucune dépendance NuGet dans Core et App. Les tests utilisent xUnit et Microsoft.NET.Test.Sdk.
- Tous les textes visibles sont en français, au **vouvoiement**, sans jargon. Prénom par défaut : `Jean-Philippe`.
- Données dans `%LocalAppData%\CompteurRetraite\donnees.json` ; exe installé dans `%LocalAppData%\Programs\CompteurRetraite\CompteurRetraite.exe`.
- Clé de démarrage : `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, valeur `CompteurRetraite`.
- Dépôt GitHub : propriété MSBuild `DepotGitHub` (format `proprietaire/depot`), vide par défaut, ce qui désactive la mise à jour.
- Toujours utiliser `DateOnly` pour les jours. L'heure courante est injectée (jamais `DateTime.Now` dans Core).
- Commandes : préfixer par `export PATH="$HOME/.dotnet:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1`.

## Review Focus

1. **Date de retraite absente ou passée** (premier lancement, faute de frappe) : aucun plantage, et le bureau affiche une invitation à régler la date ou « Bonne retraite ». Test dans la tâche 7.
2. **PC laissé allumé après minuit, ou sortie de veille** : le chiffre change sans redémarrage. Test de `Horloge.ProchainMinuit` dans la tâche 7, plus le guide manuel.
3. **Fichier de données corrompu ou écriture interrompue** : on repart sans perdre la date d'installation. Tests dans la tâche 5.
4. **Action en lot appliquée deux fois, ou inversée puis réappliquée** : le compte reste juste et les confirmations annoncent le bon nombre. Tests dans la tâche 3.
5. **Exe relancé depuis le dossier d'installation, ou relancé en double** : pas de copie sur lui-même ni de seconde instance. Tests de `DecisionLancement` dans la tâche 7.

---

### Task 1 : Squelette de la solution, git et CI

**Files :**
- Create : `.gitignore`, `Directory.Build.props`, `CompteurRetraite.sln`
- Create : `src/CompteurRetraite.Core/CompteurRetraite.Core.csproj`
- Create : `src/CompteurRetraite.App/CompteurRetraite.App.csproj`, `App.xaml`, `App.xaml.cs`
- Create : `tests/CompteurRetraite.Core.Tests/CompteurRetraite.Core.Tests.csproj`, `FumeeTests.cs`
- Create : `.github/workflows/ci.yml`

**Interfaces :**
- Produces : les espaces de noms `CompteurRetraite.Core` et `CompteurRetraite.App`, et la propriété MSBuild `DepotGitHub`, exposée par `[AssemblyMetadata("DepotGitHub", ...)]` dans l'App.

- [ ] **Step 1 :** `git init`. `.gitignore` couvre `bin/`, `obj/`, `publish/`, `*.user` et `.vs/`.
- [ ] **Step 2 :** `Directory.Build.props` contient :
  - `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable` ;
  - `EnableWindowsTargeting=true` ;
  - `Version=0.1.0` ;
  - `DepotGitHub` vide.
- [ ] **Step 3 :** créer les trois projets.
  - Les tests référencent Core. L'App référence Core.
  - L'App déclare `<AssemblyMetadata Include="DepotGitHub" Value="$(DepotGitHub)" />`, `OutputType=WinExe` et `AssemblyName=CompteurRetraite`.
  - Core déclare `InternalsVisibleTo` pour les tests.
- [ ] **Step 4 :** écrire `FumeeTests.Solution_compile()` (`Assert.True(true)`), puis lancer `dotnet test tests/CompteurRetraite.Core.Tests`. Attendu : 1 test réussi.
- [ ] **Step 5 :** lancer `dotnet build CompteurRetraite.sln -c Release`. Attendu : 0 erreur, App comprise.
- [ ] **Step 6 :** écrire `ci.yml` :
  - déclenchement sur `push` et `pull_request` ;
  - runner `windows-latest` ;
  - `actions/setup-dotnet` en `10.0.x` ;
  - `dotnet test -c Release` sur la solution ;
  - `dotnet publish` de l'App avec les options de publication, puis upload de l'exe en artefact.
- [ ] **Step 7 :** commit `chore: squelette de la solution, CI`.

### Task 2 : Jours fériés

**Files :** Create `src/CompteurRetraite.Core/JoursFeries.cs` · Test `tests/CompteurRetraite.Core.Tests/JoursFeriesTests.cs`

**Interfaces :**
- Produces : `public static class JoursFeries`, avec :
  - `DateOnly Paques(int annee)` ;
  - `IReadOnlySet<DateOnly> PourAnnee(int annee)` ;
  - `bool EstFerie(DateOnly jour)`, avec un cache par année.

- [ ] **Step 1 : tests.**
  - `Paques` : 2024 → 31/03, 2025 → 20/04, 2026 → 05/04, 2027 → 28/03, 2030 → 21/04, 2031 → 13/04.
  - `PourAnnee(2026)` contient exactement 11 dates : 01/01, 06/04, 01/05, 08/05, 14/05 (Ascension), 25/05 (lundi de Pentecôte), 14/07, 15/08, 01/11, 11/11, 25/12.
  - `EstFerie(2026-12-25)` est vrai ; `EstFerie(2026-12-24)` est faux.
- [ ] **Step 2 :** lancer les tests. Attendu : échec, la classe n'existe pas.
- [ ] **Step 3 :** implémenter. Pâques par l'algorithme grégorien anonyme (Meeus/Jones/Butcher). Ascension = Pâques + 39, Pentecôte (lundi) = Pâques + 50.
- [ ] **Step 4 :** lancer les tests. Attendu : réussite.
- [ ] **Step 5 :** commit `feat(core): jours fériés français`.

### Task 3 : Calendrier de travail et actions en lot

**Files :** Create `src/CompteurRetraite.Core/CalendrierTravail.cs`, `RegleLot.cs`, `TypeJour.cs` · Test `CalendrierTravailTests.cs`

**Interfaces :**
- Consumes : `JoursFeries.EstFerie`.
- Produces :
  - `public enum TypeJour { Travaille, WeekEnd, Ferie, NonTravailleLot, NonTravailleManuel, TravailleManuel }` ;
  - `public sealed record RegleLot(DayOfWeek Jour, DateOnly Debut, DateOnly? Fin)`, qui signifie « non travaillé » du `Debut` au `Fin` inclus, avec `null` pour « sans fin » ;
  - `public sealed class CalendrierTravail(List<RegleLot> regles, Dictionary<DateOnly, bool> corrections)`, qui partage les deux collections passées, avec :
    - `TypeJour Statut(DateOnly jour)` ;
    - `bool EstTravaille(DateOnly jour)`, vrai pour `Travaille` et `TravailleManuel` ;
    - `bool Basculer(DateOnly jour)`, qui renvoie `false` sans rien changer pour un week-end ;
    - `int NeTravaillePlus(DayOfWeek jour, DateOnly aujourdhui, DateOnly retraite)` ;
    - `int TravailleDeNouveau(DayOfWeek jour, DateOnly aujourdhui, DateOnly retraite)` ;
    - `int CompterTravailles(DateOnly debutInclus, DateOnly finExclue)` ;
    - `int CompterJoursSemaine(DayOfWeek jour, DateOnly aujourdhui, DateOnly retraite)`, qui compte les jours de ce type entre aujourd'hui inclus et la retraite exclue, quel que soit leur statut actuel. Il sert à annoncer le nombre de jours concernés avant de confirmer.

**Règles (spec 6.2) :**
- Ordre de priorité : correction manuelle > règle en lot > férié > week-end > travaillé.
  - Les week-ends ignorent les corrections et les règles, car ils ne sont pas cliquables.
- `Basculer` inverse `EstTravaille`. Si le nouveau statut est égal au statut qu'aurait le jour sans correction, on retire la correction au lieu d'en ajouter une.
- `NeTravaillePlus` ajoute `RegleLot(jour, aujourdhui, null)` et efface les corrections de ce jour de semaine à partir d'aujourd'hui.
  - Ne crée pas de doublon si une règle sans fin couvre déjà la date.
  - Renvoie le nombre de jours de ce type, entre aujourd'hui inclus et la retraite exclue, qui sont maintenant non travaillés et ne sont ni des week-ends ni des fériés.
- `TravailleDeNouveau` traite les règles de ce jour de semaine :
  - une règle avec `Debut >= aujourdhui` est supprimée ;
  - une règle encore active aujourd'hui reçoit `Fin = aujourdhui - 1`.
  - Il efface aussi les corrections futures de ce jour de semaine, et renvoie le nombre de jours redevenus travaillés.

- [ ] **Step 1 : tests.** Pour chacun, partir d'un calendrier vide, « aujourd'hui » = lundi 16/11/2026 et retraite = 30/06/2031.
  - `Statut(2026-11-21)` (samedi) donne `WeekEnd` ; `Statut(2026-12-25)` donne `Ferie` ; `Statut(2026-11-18)` donne `Travaille`.
  - `Basculer(mercredi 18/11)` le rend `NonTravailleManuel`. Un second `Basculer` le remet à `Travaille`, et les corrections sont vides.
  - `Basculer(samedi)` renvoie `false`.
  - `Basculer(25/12)` le rend `TravailleManuel`.
  - `NeTravaillePlus(Wednesday)` met mercredi 18/11 en `NonTravailleLot`, mais laisse mercredi 11/11 (passé et férié) en `Ferie`.
  - Après `NeTravaillePlus`, `Basculer(mercredi 25/11)` le rend `TravailleManuel`.
  - Un second `NeTravaillePlus(Wednesday)` laisse une seule règle et efface la correction du 25/11.
  - `NeTravaillePlus` puis `TravailleDeNouveau(Wednesday)` le même jour laissent zéro règle, et `CompterTravailles` revient à sa valeur initiale.
  - `TravailleDeNouveau` le 02/12 d'une règle commencée le 16/11 donne `Fin = 01/12`, et le 25/11 reste `NonTravailleLot`.
  - `CompterTravailles(lundi 16/11, lundi 23/11)` = 5 ; sur la semaine du 09/11 (qui contient le 11/11 férié) = 4.
  - Le nombre renvoyé par `NeTravaillePlus` est égal à la baisse de `CompterTravailles(aujourdhui, retraite)`.
- [ ] **Step 2 :** lancer les tests. Attendu : échec.
- [ ] **Step 3 :** implémenter.
- [ ] **Step 4 :** lancer les tests. Attendu : réussite.
- [ ] **Step 5 :** commit `feat(core): calendrier de travail et actions en lot`.

### Task 4 : Compteur (jours restants et pourcentage)

**Files :** Create `src/CompteurRetraite.Core/Compteur.cs` · Test `CompteurTests.cs`

**Interfaces :**
- Consumes : `CalendrierTravail.CompterTravailles`.
- Produces : `public static class Compteur`, avec :
  - `int JoursRestants(CalendrierTravail c, DateOnly aujourdhui, DateOnly retraite)`, qui renvoie 0 si `aujourdhui >= retraite` ;
  - `int Pourcentage(CalendrierTravail c, DateOnly installation, DateOnly aujourdhui, DateOnly retraite)`, qui calcule `travaillés[installation, aujourdhui) / travaillés[installation, retraite)`, arrondi au plus proche (`MidpointRounding.AwayFromZero`) et borné entre 0 et 100. Il renvoie 100 si le dénominateur vaut 0.

- [ ] **Step 1 : tests.**
  - Restants du lundi 16/11 au samedi 21/11 (exclu) = 5 ; le vendredi 20/11 = 1.
  - Le jour de la retraite = 0, le lendemain = 0.
  - Pourcentage le jour de l'installation = 0.
  - Avec installation au 16/11 et retraite au 30/11, le lundi 23/11 donne 50.
  - Marquer non travaillés les 16 au 20/11, puis évaluer le 23/11 : 0, car les vacances ne font pas avancer la barre.
  - Retraite avant l'installation : 100.
- [ ] **Step 2 :** lancer les tests. Attendu : échec. **Step 3 :** implémenter. **Step 4 :** lancer les tests. Attendu : réussite.
- [ ] **Step 5 :** commit `feat(core): compteur et pourcentage`.

### Task 5 : Données et stockage

**Files :** Create `src/CompteurRetraite.Core/Donnees.cs`, `Apparence.cs`, `EtatPhrases.cs`, `StockageDonnees.cs` · Test `StockageDonneesTests.cs`

**Interfaces :**
- Produces :
  - `public enum Coin { BasDroite, BasGauche, HautDroite, HautGauche }` ;
  - `public enum Taille { Petit, Moyen, Grand }` ;
  - `public enum Theme { Clair, Fonce }` ;
  - `public sealed class Apparence { Coin Coin = BasDroite; Taille Taille = Moyen; Theme Theme = Clair; }` ;
  - `public sealed class EtatPhrases`, avec :
    - `List<string> OrdreGeneral = []` ;
    - `int Position` ;
    - `Dictionary<string, string> Historique = []`, qui associe une date `yyyy-MM-dd` à l'identifiant d'une phrase ;
    - `HashSet<string> EtapesVues = []` ;
  - `public sealed class Donnees`, avec :
    - `int Schema = 1` ;
    - `string Prenom = "Jean-Philippe"` ;
    - `DateOnly? Retraite` ;
    - `DateOnly Installation` ;
    - `List<RegleLot> Regles = []` ;
    - `Dictionary<DateOnly, bool> Corrections = []` ;
    - `Apparence Apparence = new()` ;
    - `EtatPhrases Phrases = new()` ;
    - `bool CompteurMasque` ;
    - `CalendrierTravail Calendrier()`, qui renvoie `new(Regles, Corrections)` ;
  - `public sealed class StockageDonnees(string dossier)`, avec :
    - `string Chemin` (`dossier/donnees.json`) ;
    - `Donnees Charger(DateOnly aujourdhui)` ;
    - `void Enregistrer(Donnees d)` ;
    - `static string DossierParDefaut()`, qui renvoie `%LocalAppData%\CompteurRetraite`.

**Règles :**
- JSON indenté, enums sous forme de chaînes.
- Fichier absent : `new Donnees { Installation = aujourdhui }`, enregistré tout de suite.
- Fichier illisible :
  - le renommer en `donnees.json.bak`, en écrasant un éventuel `.bak` existant ;
  - repartir des valeurs par défaut ;
  - reprendre `Installation` si le nœud JSON `Installation` se lit encore (via `JsonNode`) ;
  - sinon prendre `aujourdhui`.
- `Enregistrer` écrit dans `donnees.json.tmp`, puis appelle `File.Move(tmp, chemin, overwrite: true)`.

- [ ] **Step 1 : tests**, dans un dossier temporaire par test.
  - Aller-retour complet : règles, corrections, apparence, état des phrases et `Retraite` sont identiques après `Charger`.
  - Fichier absent : `Installation == aujourdhui`, et le fichier existe.
  - Contenu `"{ pas du json"` : `.bak` créé, valeurs par défaut.
  - Contenu `{"Installation":"2025-01-02","Regles":42}` : `Installation == 2025-01-02`.
  - Un fichier `.tmp` resté d'une écriture interrompue n'empêche pas `Charger`.
- [ ] **Step 2 :** lancer les tests. Attendu : échec. **Step 3 :** implémenter. **Step 4 :** lancer les tests. Attendu : réussite.
- [ ] **Step 5 :** commit `feat(core): données et stockage atomique`.

### Task 6 : Phrases (banque de 100 et choix du jour)

**Files :** Create `src/CompteurRetraite.Core/Phrases/Phrase.cs`, `BanquePhrases.cs`, `SelecteurPhrase.cs`, `phrases.json` (ressource intégrée) · Test `PhrasesTests.cs`

**Interfaces :**
- Consumes : `EtatPhrases`.
- Produces :
  - `public enum CategoriePhrase { General, Saison, Etape }` ;
  - `public sealed record Phrase(string Id, string Texte, CategoriePhrase Categorie, string? Debut, string? Fin, string? Etape)`, où `Debut` et `Fin` sont au format `"MM-dd"`, peuvent chevaucher le changement d'année et sont inclusifs ;
  - `public static class BanquePhrases`, avec `IReadOnlyList<Phrase> Charger()` ;
  - `public sealed class SelecteurPhrase(IReadOnlyList<Phrase> phrases)`, avec `Phrase Choisir(DateOnly aujourdhui, int joursRestants, int pourcentage, int joursRestantsInstallation, EtatPhrases etat)`, qui modifie `etat` ;
  - `public static string Personnaliser(string texte, string prenom)`, qui remplace `{prenom}`.

**Codes d'étape :** `jours-1000`, `jours-500`, `jours-365`, `jours-200`, `jours-100`, `jours-50`, `jours-20`, `jours-5`, `jours-1`, `mi-parcours`, avec 2 phrases par code (20 au total).

**Algorithme de `Choisir` :**
1. Si `etat.Historique` contient déjà la date, renvoyer cette phrase.
2. Sinon, chercher les étapes non vues dans `EtapesVues`, et ne garder que celles qui sont franchies aujourd'hui :
   - pour `jours-N` : `joursRestants <= N` et `joursRestantsInstallation > N` ;
   - pour `mi-parcours` : `pourcentage >= 50`.
   - Prendre la plus petite étape, la marquer vue, ainsi que toutes les autres étapes franchies.
   - Choisir l'une des 2 phrases de ce code avec `aujourdhui.DayNumber % 2`.
3. Sinon, si `aujourdhui.DayNumber % 3 == 0` et qu'il existe des phrases de saison valables : prendre `saisons[aujourdhui.DayNumber / 3 % saisons.Count]`.
4. Sinon, prendre une phrase générale :
   - si `OrdreGeneral` est vide ou si `Position` est au bout, remélanger les identifiants avec `new Random(aujourdhui.DayNumber)` (Fisher-Yates) et remettre `Position = 0` ;
   - prendre `OrdreGeneral[Position++]`.
5. Enregistrer la phrase dans `Historique`, en ne gardant que les 60 dernières entrées.

**Contenu de `phrases.json` :**
- 60 phrases `General`, 20 `Saison` et 20 `Etape`, soit 100 au total.
- Positives et bienveillantes, au vouvoiement, 90 caractères au plus.
- Au moins 15 utilisent `{prenom}`.
- Les saisons couvrent : nouvel an, hiver, printemps, ponts de mai, été, rentrée, automne et fêtes de fin d'année. Aucun jour de l'année n'est sans phrase de saison.

- [ ] **Step 1 : tests.**
  - `Charger()` renvoie 100 phrases, avec des identifiants uniques et des textes non vides de 90 caractères au plus. La répartition est 60, 20 et 20. Les 10 codes d'étape ont chacun 2 phrases.
  - Tous les jours de 2027 ont au moins une phrase de saison valable.
  - Aucune phrase ne contient les mots « tu », « te », « ton », « ta », « tes » ou « toi » (vérifié mot par mot).
  - Même date et même état : même phrase, et l'état reste inchangé au second appel.
  - 60 jours générés à la suite, en forçant `DayNumber % 3 != 0`, sans étape (restants = installation = 5000) : 60 identifiants distincts.
  - Restants 999 avec installation à 1200 donne une phrase `jours-1000`. Le lendemain, avec 998 restants, ce n'est plus une étape.
  - Restants 999 avec installation à 999 ne donne pas d'étape.
  - Saison « 12-15 → 01-05 » valable le 02/01.
  - `Personnaliser("Bravo {prenom} !", "Jean-Philippe")` donne `"Bravo Jean-Philippe !"`.
- [ ] **Step 2 :** lancer les tests. Attendu : échec. **Step 3 :** implémenter, puis écrire les 100 phrases. **Step 4 :** lancer les tests. Attendu : réussite.
- [ ] **Step 5 :** commit `feat(core): 100 phrases et choix de la phrase du jour`.

### Task 7 : État d'affichage, horloge et décision de lancement

**Files :** Create `src/CompteurRetraite.Core/Affichage.cs`, `Horloge.cs`, `DecisionLancement.cs` · Test `AffichageTests.cs`, `HorlogeTests.cs`, `DecisionLancementTests.cs`

**Interfaces :**
- Consumes : `Donnees`, `Compteur`, `SelecteurPhrase`.
- Produces :
  - `public sealed record EtatAffichage(Mode Mode, string Phrase, string Chiffre, string Libelle, int Pourcentage)`, avec `public enum Mode { AConfigurer, Compte, Retraite }` ;
  - `public static class Affichage`, avec `EtatAffichage Calculer(Donnees d, SelecteurPhrase s, DateOnly aujourdhui)` ;
  - `public static class Horloge`, avec `TimeSpan JusquAuProchainMinuit(DateTime maintenant)`, qui ajoute 2 s de marge ;
  - `public enum ActionLancement { Installer, RemplacerEtRelancer, SignalerInstanceEnCours, Demarrer }` ;
  - `public static class DecisionLancement`, avec `ActionLancement Decider(string exeLance, string exeInstalle, bool installeExiste, Version versionLancee, Version? versionInstallee, bool instanceEnCours)`.

**Règles :**
- `Calculer` :
  - `Retraite` nulle : `Mode.AConfigurer`, phrase « Ouvrez les réglages pour indiquer votre date de retraite. », chiffre vide.
  - `aujourdhui >= Retraite` : `Mode.Retraite`, phrase « Bonne retraite, {prenom} ! », pourcentage 100.
  - Sinon :
    - le chiffre est formaté en `fr-FR`, par exemple « 1 042 » ;
    - le libellé est « jours travaillés avant la retraite », ou « jour travaillé avant la retraite » pour 1 ;
    - `joursRestantsInstallation` = `CompterTravailles(Installation, Retraite)`.
- `Decider` :
  - si les chemins de `exeLance` et `exeInstalle` sont égaux (sans tenir compte de la casse) : `SignalerInstanceEnCours` si une instance tourne, sinon `Demarrer` ;
  - sinon, si aucune installation n'existe : `Installer` ;
  - sinon, si `versionLancee > versionInstallee`, ou si cette dernière est inconnue : `RemplacerEtRelancer` ;
  - sinon : `SignalerInstanceEnCours` si une instance tourne, sinon `Demarrer` (en démarrant l'exe installé).

- [ ] **Step 1 : tests.**
  - Les trois modes de `Calculer`, dont `"1 042"` (avec le séparateur de milliers `fr-FR`) et le singulier pour 1 jour.
  - Pas de plantage si `Retraite` est avant `Installation`.
  - `JusquAuProchainMinuit` : 23:59:00 donne 62 s ; 00:00:00 donne 24 h + 2 s.
  - `Decider` : une table de cas couvrant chaque branche, dont la casse différente du chemin et une version installée inconnue.
- [ ] **Step 2 :** lancer les tests. Attendu : échec. **Step 3 :** implémenter. **Step 4 :** lancer les tests. Attendu : réussite.
- [ ] **Step 5 :** commit `feat(core): état d'affichage, horloge, décision de lancement`.

### Task 8 : Mise à jour (Core)

**Files :** Create `src/CompteurRetraite.Core/MiseAJour/VersionDisponible.cs`, `VerificateurMaj.cs`, `TelechargeurMaj.cs`, `RemplacementExe.cs` · Test `MiseAJourTests.cs` (avec un `HttpMessageHandler` factice)

**Interfaces :**
- Produces :
  - `public sealed record VersionDisponible(Version Version, Uri Url, long Taille, string? Sha256)` ;
  - `public sealed class VerificateurMaj(HttpClient http, string? depot, Uri? baseApi = null)`, où `baseApi` vaut `https://api.github.com/` par défaut, avec :
    - `bool Active`, vrai si `depot` n'est pas vide ;
    - `Task<VersionDisponible?> ChercherAsync(Version actuelle, CancellationToken ct)` ;
    - `static Version? LireTag(string tag)` ;
  - `public sealed class TelechargeurMaj(HttpClient http)`, avec `Task<bool> TelechargerAsync(VersionDisponible v, string destination, CancellationToken ct)` ;
  - `public static class RemplacementExe`, avec :
    - `bool Remplacer(string exeActuel, string exeNouveau)` ;
    - `void Nettoyer(string exeActuel)`, qui supprime `exeActuel + ".old"` sans lever d'erreur.

**Règles :**
- `ChercherAsync` :
  - interroge `GET repos/{depot}/releases/latest` avec un `User-Agent` générique `CompteurRetraite` ;
  - prend l'asset nommé `CompteurRetraite.exe` ;
  - lit le champ `digest` au format `sha256:<hex>` ;
  - renvoie `null` s'il n'y a pas de dépôt, si la version n'est pas plus récente, s'il n'y a pas d'asset, en cas d'erreur HTTP ou de JSON, ou en cas d'exception. Il ne lève jamais d'exception.
- `LireTag` accepte `v1.2.3` et `1.2.3`, et renvoie `null` sinon.
- `TelechargerAsync` écrit `destination`, vérifie la taille, puis le SHA-256 s'il est fourni. Si une vérification échoue, il supprime le fichier et renvoie `false`.
- `Remplacer` :
  1. déplace `actuel` vers `actuel.old`, en écrasant un éventuel ancien ;
  2. déplace `nouveau` vers `actuel` ;
  3. en cas d'échec à l'étape 2, remet `.old` à sa place et renvoie `false`.

- [ ] **Step 1 : tests.**
  - `LireTag("v1.2.3")` donne `1.2.3` ; `LireTag("abc")` donne `null`.
  - Dépôt vide : `Active == false`, `ChercherAsync` renvoie `null` sans aucun appel HTTP.
  - Réponse factice avec le tag `v0.2.0` et la version actuelle `0.1.0` : la version et l'URL de l'asset sont lues, et le SHA est sans le préfixe `sha256:`.
  - Tag `v0.1.0` : `null`. Réponse 404 : `null`. Corps `"<html>"` : `null`.
  - Téléchargement correct : `true`, et le contenu est identique. Mauvais SHA : `false`, et le fichier est absent. Mauvaise taille : `false`.
  - `Remplacer` dans un dossier temporaire : le contenu de `actuel` devient celui de `nouveau` et `.old` contient l'ancien. Si `nouveau` n'existe pas : `false`, et `actuel` reste intact.
- [ ] **Step 2 :** lancer les tests. Attendu : échec. **Step 3 :** implémenter. **Step 4 :** lancer les tests. Attendu : réussite.
- [ ] **Step 5 :** commit `feat(core): vérification, téléchargement et remplacement pour la mise à jour`.

### Task 9 : Calque sur le bureau (App)

**Files :** Create `src/CompteurRetraite.App/Bureau/Win32.cs`, `AttacheBureau.cs`, `FenetreCompteur.xaml`, `FenetreCompteur.xaml.cs`

**Interfaces :**
- Consumes : `EtatAffichage`, `Apparence`.
- Produces :
  - `internal static class AttacheBureau`, avec `bool Attacher(IntPtr hwnd)` ;
  - `public partial class FenetreCompteur : Window`, avec :
    - `void Afficher(EtatAffichage e, Apparence a)` ;
    - `void Rattacher()`.

**Détails :**
- **Fenêtre** : `WindowStyle=None`, `AllowsTransparency=True`, `Background=Transparent`, `ShowInTaskbar=False`, `ShowActivated=False`, `Focusable=False`.
- **Dans `SourceInitialized`** : ajouter `WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE` via `SetWindowLongPtr`.
- **`Attacher`** :
  1. `FindWindow("Progman")`, puis `SendMessageTimeout(progman, 0x052C, 0xD, 1, SMTO_NORMAL, 1000)` ;
  2. si `Progman` a un enfant `WorkerW` (cas 24H2 et suivantes), faire `SetParent(hwnd, progman)` et placer la fenêtre juste au-dessus de cet enfant `WorkerW` ;
  3. sinon, parcourir les fenêtres avec `EnumWindows` pour trouver la fenêtre de premier niveau qui contient `SHELLDLL_DefView`, prendre le `WorkerW` suivant et faire `SetParent` dessus ;
  4. en cas d'échec : `SetWindowPos(HWND_BOTTOM)` et renvoyer `false`.
- **Position** : dans la zone de travail de l'écran principal (`SystemParameters.WorkArea`), au coin demandé, avec une marge de 48 px.
- **Tailles** du chiffre : 72, 110 et 150 px. Phrase à 0,22 × la taille du chiffre, libellé à 0,16 ×.
- **Contenu** :
  - empilés : la phrase (italique), le chiffre (gras, `Segoe UI Variable Display`), le libellé (majuscules, espacement 0,12 em), puis une barre de 4 px avec « 78 % » ;
  - alignés à droite pour les coins droits, à gauche pour les coins gauches ;
  - thème clair : texte blanc à 90 % avec `DropShadowEffect` noir à 35 % ; thème foncé : texte `#1E2633` avec ombre blanche à 25 %.
- En `Mode.AConfigurer`, seule la phrase s'affiche ; en `Mode.Retraite`, seule la phrase s'affiche, en taille du chiffre.

- [ ] **Step 1 :** implémenter.
- [ ] **Step 2 :** lancer `dotnet build -c Release`. Attendu : 0 erreur et 0 avertissement nouveau.
- [ ] **Step 3 :** ajouter les vérifications correspondantes à `docs/test-manuel-windows.md` :
  - visible derrière les icônes ;
  - clic à travers ;
  - changement de fond d'écran ;
  - les 4 coins ;
  - les 2 thèmes.
- [ ] **Step 4 :** commit `feat(app): calque derrière les icônes du bureau`.

### Task 10 : Démarrage, instance unique, icône et rafraîchissement (App)

**Files :** Create `src/CompteurRetraite.App/Systeme/InstanceUnique.cs`, `IconeNotification.cs`, `Rafraichisseur.cs`, `Ressources/icone.ico` · Modify `App.xaml.cs`

**Interfaces :**
- Consumes : `StockageDonnees`, `BanquePhrases`, `SelecteurPhrase`, `Affichage`, `Horloge`, `FenetreCompteur`.
- Produces :
  - `internal sealed class InstanceUnique : IDisposable`, avec :
    - `bool EstPremiere` (mutex `Local\CompteurRetraite`) ;
    - `event Action<string> CommandeRecue` (canal nommé `CompteurRetraite`) ;
    - `static bool Envoyer(string commande)`, avec les commandes `REGLAGES` et `QUITTER` ;
  - `App.Rafraichir()` (public), appelée par les réglages après chaque changement ;
  - `App.Donnees` et `App.Stockage` (propriétés).

**Détails :**
- **Ordre d'`OnStartup`** : `DecisionLancement` (branché dans la tâche 12 ; ici, toujours `Demarrer`), instance unique, chargement des données, `RemplacementExe.Nettoyer`, fenêtre du compteur, icône, rafraîchissement.
- **Icône** :
  - info-bulle « Retraite : 1 042 jours travaillés » ;
  - menu « Réglages… », « Masquer le compteur » ou « Afficher le compteur » (enregistré dans `Donnees.CompteurMasque`), « Quitter » ;
  - double-clic : ouvre les réglages.
- **Rafraîchissement** :
  - un `DispatcherTimer` réarmé sur `Horloge.JusquAuProchainMinuit` ;
  - `SystemEvents.PowerModeChanged` (sortie de veille), `TimeChanged` et `DisplaySettingsChanged` déclenchent `Rafraichir()` ;
  - un hook `HwndSource` sur le message enregistré `TaskbarCreated` appelle `Rattacher()`.
- **`Rafraichir`** : calcule `Affichage.Calculer`, enregistre les données (l'état des phrases a pu changer), met à jour la fenêtre et l'info-bulle.

- [ ] **Step 1 :** implémenter. **Step 2 :** lancer `dotnet build -c Release`. Attendu : 0 erreur.
- [ ] **Step 3 :** compléter le guide manuel : changement à minuit (en changeant l'heure système), sortie de veille, redémarrage de l'Explorateur, relance du programme qui ouvre les réglages.
- [ ] **Step 4 :** commit `feat(app): démarrage, icône de notification, rafraîchissement`.

### Task 11 : Écran de réglage et calendrier (App)

**Files :** Create `src/CompteurRetraite.App/Reglages/FenetreReglages.xaml(.cs)`, `CalendrierMois.xaml(.cs)`, `Styles.xaml`

**Interfaces :**
- Consumes : `Donnees`, `CalendrierTravail` (`Statut`, `Basculer`, `NeTravaillePlus`, `TravailleDeNouveau`, `CompterJoursSemaine`), `Compteur.JoursRestants`, `App.Rafraichir()`.
- Produces : `public partial class FenetreReglages : Window`, avec le constructeur `FenetreReglages(bool bienvenue)` ; une seule fenêtre ouverte à la fois, gérée par `App.OuvrirReglages()`.

**Détails :**
- Police de 16 px minimum, boutons de 40 px de haut minimum, fenêtre de 900 × 720 redimensionnable.
- **Mode bienvenue** : un titre « Bienvenue, {prenom} ! » et une courte phrase d'explication.
- **Champs** : « Prénom » ; « Premier jour de retraite » (`DatePicker`, culture `fr-FR`).
- **`CalendrierMois`** :
  - en-tête « novembre 2026 », boutons ◀ ▶ et « Aujourd'hui » ;
  - grille de 7 colonnes L M M J V S D ;
  - couleurs par `TypeJour` :
    - travaillé : fond blanc ;
    - non travaillé (manuel ou en lot) : bleu clair avec un motif ;
    - férié : orange clair ;
    - week-end : gris clair, non cliquable ;
    - aujourd'hui : bordure épaisse ;
    - jours passés : opacité à 0,45, non cliquables ;
  - une légende sous la grille ;
  - un clic appelle `Basculer`, puis enregistre et rafraîchit.
- **Actions en lot** :
  - « Je ne travaille plus le [lundi…vendredi ▾] à partir d'aujourd'hui » [Appliquer] ;
  - « Je travaille de nouveau le [▾] à partir d'aujourd'hui » [Appliquer] ;
  - avant d'appliquer, demander confirmation avec un `MessageBox` en langage simple :
    - « {n} mercredis seront marqués comme non travaillés. Vous pourrez ensuite modifier un jour en cliquant dessus. » ;
    - `n` est calculé avec `CompterJoursSemaine` sur la période allant d'aujourd'hui à la retraite exclue ;
  - après application, afficher le nombre renvoyé par l'action.
  - Nom des jours avec `CultureInfo("fr-FR").DateTimeFormat.GetDayName`, au pluriel par ajout d'un « s ».
- **Apparence** : 3 groupes de boutons radio (Coin, Taille, Couleur).
- **Résumé en direct**, en gros : « Il reste 1 042 jours travaillés ».
- **Enregistrement automatique** à chaque modification, suivi de `App.Rafraichir()`.
- **Lien discret** « Désinstaller le compteur », branché dans la tâche 12.
- Sans date de retraite, le calendrier et les actions en lot sont désactivés, avec la mention « Indiquez d'abord votre date de retraite. »

- [ ] **Step 1 :** implémenter. **Step 2 :** lancer `dotnet build -c Release`. Attendu : 0 erreur.
- [ ] **Step 3 :** compléter le guide manuel : saisir une date, basculer des jours, une action en lot et son inverse, changer l'apparence et la voir sur le bureau.
- [ ] **Step 4 :** commit `feat(app): écran de réglage et calendrier`.

### Task 12 : Installation, désinstallation et service de mise à jour (App)

**Files :** Create `src/CompteurRetraite.App/Systeme/Installeur.cs`, `ServiceMiseAJour.cs` · Modify `App.xaml.cs`, `FenetreReglages.xaml.cs`

**Interfaces :**
- Consumes : `DecisionLancement`, `InstanceUnique.Envoyer`, `VerificateurMaj`, `TelechargeurMaj`, `RemplacementExe`, `StockageDonnees.DossierParDefaut`.
- Produces : `internal static class Installeur`, avec :
  - `string ExeInstalle` ;
  - `void Installer(string exeSource)`, qui copie l'exe et écrit la clé `Run` avec le chemin entre guillemets ;
  - `void Desinstaller()` ;
  - `Version? VersionInstallee()`, via `FileVersionInfo`.

**Détails :**
- **Branchement des actions de `DecisionLancement`** :
  - `Installer` : copier l'exe, lancer l'exe installé avec l'argument `--bienvenue`, puis quitter ;
  - `RemplacerEtRelancer` :
    1. `Envoyer("QUITTER")` et attendre jusqu'à 5 s que le mutex soit libéré ;
    2. copier l'exe lancé dans `ExeInstalle.new`, puis `RemplacementExe.Remplacer` ;
    3. relancer l'exe installé, puis quitter ;
  - `SignalerInstanceEnCours` : `Envoyer("REGLAGES")`, puis quitter.
- **`Desinstaller`** :
  1. confirmation « Le compteur va être retiré de cet ordinateur. Continuer ? » ;
  2. retrait de la clé `Run` ;
  3. lancement de `cmd /c timeout /t 2 & rmdir /s /q "<dossier programme>" & rmdir /s /q "<dossier données>"`, fenêtre masquée ;
  4. arrêt du programme.
- **`ServiceMiseAJour`** :
  - inactif si `DepotGitHub` est vide (lu via `AssemblyMetadataAttribute`) ;
  - sinon, vérification 30 s après le démarrage puis toutes les 24 h ;
  - si une version est trouvée, téléchargement dans `ExeInstalle + ".new"` ;
  - si aucune fenêtre de réglage n'est ouverte, `Remplacer`, puis relancer et quitter ; sinon, au prochain démarrage (`OnStartup` applique d'abord un `.new` valide s'il est présent) ;
  - aucune erreur n'est affichée ; les erreurs sont écrites dans `%LocalAppData%\CompteurRetraite\journal.txt`, limité à 100 Ko.

- [ ] **Step 1 :** implémenter. **Step 2 :** lancer `dotnet build -c Release`. Attendu : 0 erreur.
- [ ] **Step 3 :** compléter le guide manuel :
  - premier lancement depuis Téléchargements ;
  - avertissement SmartScreen et vérification du Contrôle intelligent des applications ;
  - redémarrage de session ;
  - lancement d'un exe plus récent ;
  - désinstallation (et vérification que la clé et les dossiers ont disparu).
- [ ] **Step 4 :** commit `feat(app): installation, désinstallation, service de mise à jour`.

### Task 13 : Publication, release GitHub et documentation

**Files :** Create `.github/workflows/release.yml`, `README.md`, `scripts/publier.sh` · Finalize `docs/test-manuel-windows.md`

- [ ] **Step 1 :** `scripts/publier.sh` lance `dotnet publish src/CompteurRetraite.App -c Release` avec les options de publication et `-o publish/`.
- [ ] **Step 2 :** lancer `bash scripts/publier.sh`. Attendu : `publish/CompteurRetraite.exe` existe et pèse entre 40 et 90 Mo.
- [ ] **Step 3 :** écrire `release.yml` :
  - déclenchement sur un tag `v*.*.*`, runner `windows-latest` ;
  - lancer les tests ;
  - publier avec `-p:Version=${tag sans v}` et `-p:DepotGitHub=${{ github.repository }}` ;
  - créer la release avec l'asset `CompteurRetraite.exe` (`softprops/action-gh-release@v2`, `permissions: contents: write`).
- [ ] **Step 4 :** écrire le `README.md` en français. Il explique :
  - ce que fait le programme ;
  - comment compiler sous Linux ;
  - comment publier une version : pousser le dépôt public sur GitHub, puis `git tag v0.2.0 && git push --tags`.
  - Il précise aussi que seules les versions compilées par `release.yml` savent se mettre à jour. Le premier exe à donner à Jean-Philippe doit donc venir d'une release GitHub, ou être compilé localement avec `-p:DepotGitHub=proprietaire/depot`.
- [ ] **Step 5 :** lancer tous les tests (`dotnet test`). Attendu : tous réussis.
- [ ] **Step 6 :** commit `chore: publication, release GitHub, documentation`.
