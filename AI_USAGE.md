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
- README, workflow GitHub, tests de la logique.

## Pour la soumission

- La description de la PR sur DalamudPluginsD17 doit être **écrite par l'auteur lui-même**, et indiquer le niveau d'usage de l'IA.
- Les traductions faites avec l'IA sont signalées dans la description du plugin (visible dans l'installateur).
- Aucune image ni icône générée par IA pour l'instant. Si c'est le cas un jour, le signaler dans la description du plugin.

## Journal des modifications

### 2026-11-17 — Première version

- Demandé par Lénile : écrire le plugin complet à partir de son cahier des charges.
- Fait par l'IA : tout le code, les données JSON, les traductions anglaises, le README, les tests.
- Testé par Lénile en jeu : *à compléter*.

### 2026-11-17 — Vérification des règles Dalamud

- Demandé par Lénile : relire le code pour respecter les conditions de Dalamud ; lien Discord fourni par Lénile.
- Fait par l'IA : lien d'invitation Discord, réparation des configurations abîmées, retrait d'un service inutilisé, emplacement de l'icône corrigé, marche à suivre de soumission dans le README.
- Testé par Lénile en jeu : *à compléter*.

### 2026-11-17 — Contrainte 73 et nouvelles fonctionnalités

- Décidé par Lénile : remplacer la contrainte 73 (qui parlait de la boutique), ajouter le rappel dans le chat, le calendrier de série et l'exclusion de thèmes.
- Fait par l'IA :
    - contrainte 73 : « Au moins une pièce achetée chez un marchand PNJ » / « At least one piece bought from an NPC vendor » (même identifiant, le calcul du jour ne change pas) ;
    - rappel facultatif du thème du jour dans le chat, désactivé par défaut, visible par le joueur seul ;
    - calendrier des 7 ou 30 derniers jours (partagé, sauvé par un joker, raté) ;
    - bouton « Ne plus tirer ce thème » et liste des thèmes exclus dans les réglages ;
    - mention des traductions IA dans la description du plugin.
- Testé par Lénile en jeu : *à compléter*.

### 2026-11-17 — README public en anglais

- Demandé par Lénile : un README en anglais, destiné aux joueurs, sans les consignes internes (compilation, soumission).
- Fait par l'IA : réécriture complète du README.
- Testé par Lénile en jeu : sans objet (documentation seulement).
