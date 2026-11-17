# DressRoulette

A Dalamud plugin for FINAL FANTASY XIV that gives every player the same glamour theme each day.
Build your outfit around it, take a screenshot, and share it with **#DressRoulette**.

## Features

- **Daily theme**: one theme per day, the same for everyone, changing at the daily reset (15:00 UTC).
- **Bonus constraint**: an optional extra challenge for the day, which you can hide.
- **Free wheel**: spin as often as you like.
    - Choose which categories are on the wheel.
    - Add your own themes.
    - Add a bonus constraint to your spins.
    - Recent results are avoided, so you don't get the same theme twice in a row.
    - Remove themes you never want to draw again.
- **Streak**: share today's theme on consecutive days to build your streak.
    - Earn a joker every 10 days (up to 3). A joker saves your streak when you miss a day.
    - See your best streak and a calendar of your last 7 or 30 days.
- **Chat reminder** (off by default): see today's theme in your chat when you log in. Only you can see it.
- **Languages**: English and French.

## Commands

- `/dressroulette`
- `/droulette`
- `/drsr`

All three open the main window.

## Sharing

The **Share my outfit** button copies a short text to your clipboard and opens the DressRoulette sharing channel on Discord in your browser. Paste your screenshot and the text, and you're done.

The text works anywhere else too:

> Today's theme: La vie en rose · Streak: 12 days 🔥 #DressRoulette

Discord: [discord.gg/6NshHb9QzK](https://discord.gg/6NshHb9QzK)

## Privacy

- DressRoulette never changes your outfit and doesn't need any other plugin.
- It sends nothing over the internet. The daily theme is calculated on your computer from the date.
- Your streak, settings and custom themes are stored only in your local plugin configuration.

## Status

DressRoulette is not released yet. It will be submitted to the official Dalamud plugin repository.

To try the test version now:

1. In game, open `/xlsettings` → **Experimental**.
2. Under **Custom Plugin Repositories**, add this URL, tick **Enabled**, then save:

    ```
    https://raw.githubusercontent.com/Lenile/DressRoulette/main/repo.json
    ```

3. Open `/xlplugins` and install DressRoulette.

## AI usage

This plugin is developed with AI assistance. Details are in [AI_USAGE.md](AI_USAGE.md).

## License

AGPL-3.0. See [LICENSE.md](LICENSE.md).

Author: Lénile
