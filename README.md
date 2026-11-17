# DressRoulette

Un plugin Dalamud pour FINAL FANTASY XIV qui donne chaque jour **un thème de glamour**, le même pour tous les joueurs.
Fais ta tenue sur le thème, puis partage-la avec **#DressRoulette**.

*A Dalamud plugin that gives everyone the same glamour theme each day. Dress up, then share with #DressRoulette. French and English.*

## Fonctionnalités

- Thème du jour, identique pour tout le monde, qui change au reset quotidien (15 h UTC).
- Contrainte bonus du jour (masquable dans les réglages).
- Roue libre à relancer autant qu'on veut :
    - catégories à activer ou désactiver ;
    - thèmes perso (« Mes thèmes ») ;
    - contrainte bonus en option ;
    - évite les 15 derniers thèmes et les 25 dernières contraintes ;
    - roue de 100 thèmes avec les noms, ou roue complète sans les noms.
    - bouton « Ne plus tirer ce thème », et liste des thèmes exclus dans les réglages.
- Historique des 30 derniers tirages.
- Série, record et jokers (un joker tous les 10 jours, 3 au maximum).
- Calendrier des 7 ou 30 derniers jours : partagé, sauvé par un joker ou raté.
- Rappel facultatif du thème du jour dans le chat (à la connexion et au reset), visible par toi seul. Désactivé par défaut.
- Français et anglais.
- Commandes : `/dressroulette`, `/droulette`, `/drsr`.

Le plugin **n'applique aucune tenue**, ne dépend d'aucun autre plugin et **n'envoie rien sur internet**.
Le bouton de partage copie un texte dans le presse-papiers et ouvre le salon Discord dans le navigateur, sur un clic du joueur.

## Compiler

Prérequis :

- XIVLauncher, le jeu et Dalamud installés, et le jeu lancé au moins une fois avec Dalamud.
- Le SDK .NET 10 (avec Visual Studio 2026 ou plus récent, Rider, ou `dotnet` en ligne de commande).

Compilation :

1. Ouvrir `DressRoulette.slnx` dans Visual Studio ou Rider, puis compiler.
2. Ou en ligne de commande, à la racine : `dotnet build -c Release`
3. Le plugin se trouve dans `DressRoulette/bin/x64/Release/DressRoulette/` (ou `Debug`).

## Tester en jeu

1. En jeu : `/xlsettings` → **Experimental** → ajouter le chemin complet de `DressRoulette.dll` dans **Dev Plugin Locations**.
2. `/xlplugins` → **Dev Tools** → **Installed Dev Plugins** → activer DressRoulette.
3. Taper `/drsr`.

## Structure du projet

- `DressRoulette/Plugin.cs` : point d'entrée, commandes, chargement des données.
- `DressRoulette/Configuration.cs` : réglages enregistrés en local par Dalamud.
- `DressRoulette/Links.cs` : le lien d'invitation vers le salon de partage Discord.
- `DressRoulette/Core/` : la logique, sans interface.
    - `DailyPicker.cs` : numéro du jour et choix du thème du jour.
    - `StableRandom.cs` : générateur pseudo-aléatoire maison (SplitMix64).
    - `StreakRules.cs` : série, record et jokers.
    - `FreeWheel.cs` : roue libre, thèmes perso, historique.
    - `ShareText.cs` : textes copiés par les boutons de partage.
    - `ThemeCatalog.cs` : lecture des fichiers de thèmes.
- `DressRoulette/Data/` : `themes.json` (254 thèmes, 11 catégories) et `constraints.json` (126 contraintes), intégrés à la DLL.
- `DressRoulette/Localization/Loc.cs` : tous les textes de l'interface en français et en anglais.
- `DressRoulette/Windows/` : la fenêtre et ses trois onglets (Aujourd'hui, Roue libre, Réglages).

## Ajouter des thèmes ou des contraintes

- Toujours ajouter **à la fin** du fichier, avec un **nouvel identifiant** (le plus grand + 1).
- Ne jamais réordonner, renuméroter ni supprimer une entrée existante.
- Chaque entrée a un texte `fr` et un texte `en`.
- Les noms officiels du jeu suivent la langue du client (« Pampa » en français, « Cactuar » en anglais).

## Comment le thème du jour est calculé

- Le jour 0 commence le 1er janvier 2026 à 15 h UTC. Chaque jour de jeu commence à 15 h UTC.
- Les thèmes passent dans un ordre mélangé, différent à chaque cycle (un cycle = autant de jours que de thèmes). Chaque thème sort une fois par cycle, et jamais deux jours de suite.
- Le mélange ne dépend que des identifiants des thèmes et du numéro du cycle, via un générateur maison. On n'utilise ni `System.Random` ni `string.GetHashCode()`, dont le résultat peut changer selon la version de .NET.
- Le thème du jour est tiré parmi les thèmes intégrés seulement, sans tenir compte des catégories désactivées ni des thèmes perso.
- Après une mise à jour qui ajoute des thèmes, tous les joueurs à jour ont de nouveau le même thème du jour.

## Publier sur le dépôt officiel

1. Pousser le code sur un dépôt GitHub public, avec `packages.lock.json` (le serveur de compilation n'a pas accès à internet).
2. Faire une PR sur [DalamudPluginsD17](https://github.com/goatcorp/DalamudPluginsD17), dans `testing/live/DressRoulette/` (obligatoire pour un nouveau plugin).
3. Dans ce dossier :
    - `manifest.toml` (voir ci-dessous) ;
    - `images/icon.png` : carré, entre 64x64 et 512x512 ;
    - `images/image1.png` à `image5.png` (facultatif) : captures de 730x380 au maximum.
4. Dans la description de la PR, écrite à la main : déclarer le niveau d'usage de l'IA ([règle](https://dalamud.dev/plugin-publishing/ai-policy)). Le détail est tenu à jour dans `AI_USAGE.md`.

```toml
[plugin]
repository = "https://github.com/<ton-compte>/DressRoulette.git"
commit = "<hash du commit à compiler>"
owners = ["<ton-compte>"]
project_path = "DressRoulette"
changelog = "Première version."
```

## Usage de l'IA

Ce plugin est développé avec l'aide d'une IA (niveau **Copilot**). Le détail de qui fait quoi, et le journal de chaque modification, sont dans [`AI_USAGE.md`](AI_USAGE.md).

## Licence

AGPL-3.0, comme le modèle [SamplePlugin](https://github.com/goatcorp/SamplePlugin). Voir `LICENSE.md`.

Auteur : Lénile.
