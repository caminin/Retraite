# Vérifications à faire sur un PC Windows 11

Les tests automatiques couvrent tous les calculs. Cette fiche couvre ce qui ne peut être vérifié que sur un vrai Windows : l'affichage sur le bureau, l'icône, les fenêtres, l'installation et la mise à jour.

Cochez chaque ligne. Si quelque chose ne va pas, notez la version de Windows : Paramètres > Système > Informations système.

## Affichage sur le bureau

- [ ] Le compteur s'affiche **par-dessus le fond d'écran et derrière les icônes**. Une icône placée sur la même zone passe devant le texte.
- [ ] Un clic sur le texte du compteur **sélectionne le bureau ou l'icône dessous** : le compteur ne réagit jamais aux clics.
- [ ] Une fenêtre ouverte (l'Explorateur, par exemple) **passe devant** le compteur.
- [ ] Le compteur **n'apparaît pas** dans la barre des tâches, ni dans Alt+Tab, ni dans Win+Tab.
- [ ] Changez le fond d'écran (clic droit > Personnaliser) : **le compteur reste** et **le nouveau fond est intact**.
- [ ] Essayez un fond d'écran clair et un fond foncé, avec le thème Clair puis Foncé dans les réglages : le texte reste lisible.
- [ ] Les 3 positions (réglages > Apparence) : le texte se place dans le bon coin, aligné du bon côté, sans passer sous la barre des tâches.
- [ ] « Afficher le bureau » (coin en bas à droite de la barre des tâches, ou Win+D) : le compteur reste visible.
- [ ] Les 3 tailles changent bien la taille du texte.
- [ ] Seuls le chiffre, son libellé, une barre de progression et le pourcentage s'affichent (pas de phrase).

## Icône, rafraîchissement, robustesse

- [ ] Une icône bleue (petit sablier) est visible près de l'horloge, éventuellement dans la flèche « ^ ». Survolez-la : le nombre de jours s'affiche.
- [ ] Clic droit sur l'icône : le menu propose « Réglages… », « Masquer le compteur » et « Quitter ». « Masquer » fait disparaître le compteur du bureau, puis le menu propose « Afficher le compteur ».
- [ ] Un double-clic sur l'icône ouvre les réglages.
- [ ] **Passage de minuit** : réglez l'horloge de Windows à 23:59 (Paramètres > Heure et langue), attendez 1 minute : le chiffre change. Remettez ensuite l'heure automatique.
- [ ] **Veille** : mettez le PC en veille le soir et réveillez-le le lendemain : le chiffre est celui du jour.
- [ ] **Redémarrage de l'Explorateur** : Gestionnaire des tâches > « Explorateur Windows » > Redémarrer. Le compteur revient au bout de quelques secondes, et l'icône aussi.
- [ ] **Relancer le programme** alors qu'il tourne déjà : ça ouvre les réglages, sans second compteur.
- [ ] Après un changement de résolution ou le branchement d'un écran, le compteur reste dans son coin sur l'écran principal.

## Écran de réglage

- [ ] La fenêtre est lisible, avec de gros caractères ; sur un petit écran (1366 × 768), elle tient dans l'écran et les boutons « Fermer » / « Désinstaller » restent visibles en bas.
- [ ] Changez le **prénom** : il est repris dans le titre de bienvenue et dans le message « Bonne retraite » du jour J.
- [ ] Choisissez la **date de retraite** avec les trois listes (jour, mois, année) : le résumé « Il reste … jours travaillés » et le bureau se mettent à jour tout de suite.
- [ ] Sans date de retraite, le calendrier et les actions en lot sont grisés, et le bureau invite à ouvrir les réglages.
- [ ] Dans le calendrier, les **week-ends** sont gris et non cliquables, les **jours fériés** orange, les **jours passés** pâles et non cliquables, et **aujourd'hui** est entouré.
- [ ] Un clic sur un jour travaillé le passe en bleu (non travaillé) et le chiffre baisse de 1. Un second clic l'annule.
- [ ] Un clic sur un jour férié le rend travaillé (blanc) et le chiffre augmente de 1.
- [ ] **Action en lot** « Je ne travaille plus le mercredi » : la confirmation annonce le nombre de mercredis, tous les mercredis à venir passent en bleu, et le message « C'est fait » s'affiche.
- [ ] Un mercredi peut ensuite être remis en travaillé d'un clic.
- [ ] « Je travaille de nouveau le mercredi » remet les mercredis à venir en blanc.
- [ ] Les choix **Position**, **Taille** et **Couleur** s'appliquent aussitôt sur le bureau.
- [ ] Fermez puis rouvrez les réglages : tout est conservé. Redémarrez le PC : tout est conservé.

## Installation

- [ ] **Contrôle intelligent des applications** : Sécurité Windows > Contrôle des applications et du navigateur > Contrôle intelligent des applications. S'il est « Activé », Windows bloquera le programme : il faut le passer sur « Désactivé ». Attention, ce choix est définitif.
- [ ] Téléchargez `CompteurRetraite.exe` et double-cliquez dessus depuis Téléchargements. Si l'écran bleu « Windows a protégé votre ordinateur » apparaît, cliquez sur « Informations complémentaires » puis « Exécuter quand même ».
- [ ] La fenêtre **Bienvenue, Jean-Philippe !** s'ouvre, et le compteur apparaît sur le bureau dès que la date est saisie.
- [ ] Le programme est copié dans `%LocalAppData%\Programs\CompteurRetraite\` (collez ce chemin dans la barre d'adresse de l'Explorateur).
- [ ] **Fermez la session puis rouvrez-la** : le compteur revient seul, **sans aucune fenêtre d'avertissement**.
- [ ] Relancez le fichier de Téléchargements : les réglages s'ouvrent, sans second compteur ni réinstallation.

## Nouvelle version donnée à la main

- [ ] Lancez un `CompteurRetraite.exe` de version plus récente : l'ancien compteur se ferme, le nouveau prend sa place, et **les réglages et vacances sont conservés**.

## Mise à jour automatique (une fois le dépôt GitHub en place)

- [ ] Installez une version issue d'une release GitHub, par exemple v0.1.0.
- [ ] Publiez une version plus récente (v0.2.0, voir le README).
- [ ] Relancez la session, ou attendez : 30 secondes après le démarrage, la nouvelle version est téléchargée, puis installée dès que la fenêtre de réglage est fermée. Le compteur disparaît une seconde puis revient.
- [ ] Les réglages et vacances sont conservés.

## Désinstallation

- [ ] Réglages > « Désinstaller le compteur » > Oui : le compteur et l'icône disparaissent.
- [ ] Après quelques secondes, les dossiers `%LocalAppData%\Programs\CompteurRetraite` et `%LocalAppData%\CompteurRetraite` n'existent plus.
- [ ] Au redémarrage de la session, plus rien ne se lance.

## En cas de souci

Le fichier `%LocalAppData%\CompteurRetraite\journal.txt` contient les erreurs rencontrées. Il est utile à joindre à un signalement.
