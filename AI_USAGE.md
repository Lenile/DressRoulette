# Déclaration d'usage de l'IA

Ce fichier suit la [règle d'usage de l'IA de Dalamud](https://dalamud.dev/plugin-publishing/ai-policy).
Il est mis à jour **à chaque modification** du projet faite avec l'aide d'une IA.

## Niveau déclaré

- **Niveau : Copilot** — l'IA écrit la plus grande partie du code ; l'auteur conçoit, décide, relit et teste.
- **Outil** : Claude (Anthropic).

## Répartition du travail

Lénile (auteur) :

- concept du plugin, cahier des charges complet et règles de la série et des jokers ;
- listes françaises des 254 thèmes et 126 contraintes ;
- choix des fonctionnalités et des formulations ;
- compilation sous Windows et tests en jeu.

IA :

- code C# du plugin ;
- fichiers de données JSON, construits à partir des listes françaises de l'auteur ;
- traductions anglaises des thèmes, des contraintes et de l'interface ;
- README, workflow GitHub, tests de la logique ;
- icône du plugin.

## Pour la soumission

- La description de la PR sur DalamudPluginsD17 doit être **écrite par l'auteur lui-même**, et indiquer le niveau d'usage de l'IA.
- Les traductions et l'icône faites avec l'IA sont signalées dans la description du plugin (visible dans l'installateur).

## Journal des modifications

### 2026-11-17 — Première version

- Demandé par Lénile : écrire le plugin complet à partir de son cahier des charges.
- Fait par l'IA : tout le code, les données JSON, les traductions anglaises, le README, les tests.
- Testé par Lénile en jeu : le 17/11/2026, tout fonctionne (version 1.0.0.0, installée depuis le dépôt de test).

### 2026-11-17 — Vérification des règles Dalamud

- Demandé par Lénile : relire le code pour respecter les conditions de Dalamud ; lien Discord fourni par Lénile.
- Fait par l'IA : lien d'invitation Discord, réparation des configurations abîmées, retrait d'un service inutilisé, emplacement de l'icône corrigé, marche à suivre de soumission dans le README.
- Testé par Lénile en jeu : le 17/11/2026, tout fonctionne (version 1.0.0.0, installée depuis le dépôt de test).

### 2026-11-17 — Contrainte 73 et nouvelles fonctionnalités

- Décidé par Lénile : remplacer la contrainte 73 (qui parlait de la boutique), ajouter le rappel dans le chat, le calendrier de série et l'exclusion de thèmes.
- Fait par l'IA :
    - contrainte 73 : « Au moins une pièce achetée chez un marchand PNJ » / « At least one piece bought from an NPC vendor » (même identifiant, le calcul du jour ne change pas) ;
    - rappel facultatif du thème du jour dans le chat, désactivé par défaut, visible par le joueur seul ;
    - calendrier des 7 ou 30 derniers jours (partagé, sauvé par un joker, raté) ;
    - bouton « Ne plus tirer ce thème » et liste des thèmes exclus dans les réglages ;
    - mention des traductions IA dans la description du plugin.
- Testé par Lénile en jeu : le 17/11/2026, tout fonctionne (version 1.0.0.0, installée depuis le dépôt de test).

### 2026-11-17 — README public en anglais

- Demandé par Lénile : un README en anglais, destiné aux joueurs, sans les consignes internes (compilation, soumission).
- Fait par l'IA : réécriture complète du README.
- Testé par Lénile en jeu : sans objet (documentation seulement).

### 2026-11-17 — Dépôt de test non officiel

- Demandé par Lénile : un dépôt public non officiel pour installer le plugin avant la soumission.
- Fait par l'IA : fichier `repo.json` (dépôt personnalisé Dalamud) et section « Status » du README avec l'adresse du dépôt.
- Fait par Lénile : compilation du plugin et publication du zip dans une Release GitHub.
- Testé par Lénile en jeu : le 17/11/2026, tout fonctionne (version 1.0.0.0, installée depuis le dépôt de test).

### 2026-11-17 — Compilation et Release automatiques

- Demandé par Lénile : faire compiler le plugin par GitHub.
- Fait par l'IA : workflow `.github/workflows/main.yml` (compile le plugin et publie la Release avec `latest.zip`, lancé à la main).
- Testé par Lénile : le 17/11/2026, la compilation sur GitHub et la Release v1.0.0.0 fonctionnent.

### 2026-11-17 — Préparation de la soumission

- Demandé par Lénile : tout préparer pour la soumission au dépôt officiel.
- Fait par l'IA :
    - icône du plugin (`images/icon.png`, 512x512), dessinée par un script ;
    - mention de l'icône et des traductions faites avec l'IA dans la description du plugin ;
    - `IconUrl` dans `repo.json` ;
    - dossier de soumission `testing/live/DressRoulette/` (`manifest.toml` et icône), changelog.
- Non fait par l'IA, volontairement : la description de la PR, que la règle Dalamud demande d'écrire soi-même.
- Testé par Lénile en jeu : sans objet (aucun changement de code).
