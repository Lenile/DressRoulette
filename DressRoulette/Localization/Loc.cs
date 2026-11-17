using Dalamud.Game;
using DressRoulette.Core;

namespace DressRoulette.Localization;

/// <summary>
/// Tous les textes de l'interface, en français et en anglais, réunis au même endroit.
/// La langue active est recalculée à chaque image à partir des réglages.
/// </summary>
public static class Loc
{
    public static Lang Current { get; private set; } = Lang.English;

    /// <summary>Met à jour la langue active.</summary>
    public static void Update(LanguageSetting setting, ClientLanguage clientLanguage)
    {
        Current = setting switch
        {
            LanguageSetting.French => Lang.French,
            LanguageSetting.English => Lang.English,
            // Auto : le français suit le client, l'allemand et le japonais passent en anglais.
            _ => clientLanguage == ClientLanguage.French ? Lang.French : Lang.English,
        };
    }

    private static string T(string fr, string en) => Current == Lang.French ? fr : en;

    // Onglets
    public static string TabToday => T("Aujourd'hui", "Today");
    public static string TabWheel => T("Roue libre", "Free wheel");
    public static string TabSettings => T("Réglages", "Settings");

    // Aujourd'hui
    public static string DailyTheme => T("Thème du jour", "Today's theme");
    public static string DailyConstraint => T("Contrainte bonus du jour", "Today's bonus constraint");
    public static string NextThemeIn(int hours, int minutes)
        => T($"Nouveau thème dans {hours} h {minutes:00}", $"New theme in {hours}h {minutes:00}m");
    public static string Days(int n) => Current == Lang.French
        ? $"{n} {(n > 1 ? "jours" : "jour")}"
        : $"{n} {(n == 1 ? "day" : "days")}";
    public static string JokersCount(int n) => $"{n} {(n > 1 || (n == 0 && Current == Lang.English) ? "jokers" : "joker")}";
    public static string Record(int n) => T($"Record : {n}", $"Best: {n}");
    public static string StreakTooltip => T(
        "Jours d'affilée où tu as partagé le thème du jour avec « Partager ma tenue ».",
        "Days in a row you shared today's theme with \"Share my outfit\".");
    public static string JokersTooltip => T(
        "Un joker est gagné tous les 10 jours de série (3 au maximum).\nUn jour raté utilise un joker : ta série est sauvée, mais n'augmente pas ce jour-là.",
        "You earn a joker every 10 days of streak (3 at most).\nA missed day uses a joker: your streak is saved, but doesn't grow that day.");
    public static string ShareMyOutfit => T("Partager ma tenue", "Share my outfit");
    public static string ShareHelp => T(
        "Copie le texte du thème du jour et ouvre le salon de partage Discord. Colle ta capture et le texte !",
        "Copies today's theme text and opens the Discord sharing channel. Paste your screenshot and the text!");
    public static string ShareHelpNoLink => T(
        "Copie le texte du thème du jour. Colle-le avec ta capture où tu veux !",
        "Copies today's theme text. Paste it with your screenshot anywhere you like!");
    public static string AlreadySharedToday => T("Déjà partagé aujourd'hui, ta série est à jour.", "Already shared today, your streak is up to date.");
    public static string Copied => T("Texte copié dans le presse-papiers !", "Text copied to the clipboard!");
    public static string OneJokerSaved => T("Un joker a sauvé ta série !", "A joker saved your streak!");
    public static string JokersSaved(int n) => T($"{n} jokers ont sauvé ta série !", $"{n} jokers saved your streak!");
    public static string JokerEarned => T("Nouveau joker gagné !", "New joker earned!");
    public static string Ok => T("OK", "OK");

    // Calendrier de série
    public static string LastDays => T("Mes derniers jours", "My last days");
    public static string Last7Days => T("7 jours", "7 days");
    public static string Last30Days => T("30 jours", "30 days");
    public static string StatusShared => T("Partagé", "Shared");
    public static string StatusSaved => T("Sauvé par un joker", "Saved by a joker");
    public static string StatusMissed => T("Raté", "Missed");
    public static string StatusPending => T("Aujourd'hui, pas encore partagé", "Today, not shared yet");

    // Rappel dans le chat
    public static string ChatDaily(string theme) => T($"Thème du jour : {theme}", $"Today's theme: {theme}");
    public static string ChatNewDay(string theme) => T($"Nouveau thème du jour : {theme}", $"New theme of the day: {theme}");

    // Roue libre
    public static string WheelModeNamed => T("100 thèmes (avec les noms)", "100 themes (with names)");
    public static string WheelModeFull => T("Roue complète (sans les noms)", "Full wheel (no names)");
    public static string Reshuffle => T("Remélanger", "Reshuffle");
    public static string BonusConstraint => T("Contrainte bonus", "Bonus constraint");
    public static string Spin => T("Lancer la roue", "Spin the wheel");
    public static string Spinning => T("La roue tourne…", "Spinning…");
    public static string ShareThisDraw => T("Partager ce tirage", "Share this draw");
    public static string ShareThisDrawHelp => T(
        "Partage ce tirage libre. Il ne compte pas pour la série.",
        "Shares this free draw. It doesn't count toward your streak.");
    public static string EmptyWheel => T(
        "La roue est vide : active une catégorie ou ajoute des thèmes perso dans les réglages.",
        "The wheel is empty: enable a category or add custom themes in the settings.");
    public static string WheelTooSmall => T(
        "Agrandis la fenêtre pour lire les noms.",
        "Make the window bigger to read the names.");
    public static string ExcludeTheme => T("Ne plus tirer ce thème", "Don't draw this theme again");
    public static string ExcludeThemeHelp => T(
        "Le thème est retiré de la roue libre. Tu peux le remettre dans les réglages. Le thème du jour n'est pas concerné.",
        "The theme is removed from the free wheel. You can bring it back in the settings. Today's theme is not affected.");
    public static string ThemeExcluded => T("Thème retiré de la roue libre.", "Theme removed from the free wheel.");
    public static string ThemesOnWheel(int n) => T($"{n} thèmes sur la roue", $"{n} themes on the wheel");

    // Réglages
    public static string Language => T("Langue", "Language");
    public static string LanguageAuto => T("Automatique (langue du jeu)", "Automatic (game language)");
    public static string LanguageFrench => "Français";
    public static string LanguageEnglish => "English";
    public static string ChatReminder => T(
        "Afficher le thème du jour dans le chat (à la connexion et au reset)",
        "Show today's theme in chat (at login and at reset)");
    public static string ChatReminderHelp => T(
        "Le message n'est visible que par toi.",
        "Only you can see this message.");
    public static string ExcludedThemes => T("Thèmes exclus", "Excluded themes");
    public static string ExcludedThemesHelp => T(
        "Ces thèmes ne sortent plus dans la roue libre. Le thème du jour n'est pas concerné.",
        "These themes no longer come up on the free wheel. Today's theme is not affected.");
    public static string NoExcludedThemes => T("Aucun thème exclu.", "No excluded themes.");
    public static string RestoreTheme => T("Remettre dans la roue", "Put back on the wheel");
    public static string HideDailyConstraint => T("Masquer la contrainte bonus du jour", "Hide today's bonus constraint");
    public static string Categories => T("Catégories de la roue libre", "Free wheel categories");
    public static string CategoriesHelp => T("Au moins une catégorie doit rester active.", "At least one category must stay enabled.");
    public static string MyThemes => T("Mes thèmes", "My themes");
    public static string AddThemeHint => T("Nouveau thème…", "New theme…");
    public static string Add => T("Ajouter", "Add");
    public static string NoCustomThemes => T("Aucun thème perso pour l'instant.", "No custom themes yet.");
    public static string Delete => T("Supprimer", "Delete");
    public static string History => T("Historique", "History");
    public static string HistoryEmpty => T("Aucun tirage pour l'instant.", "No draws yet.");
    public static string ClearHistory => T("Effacer l'historique", "Clear history");
    public static string ConfirmClear => T("Clique encore pour confirmer", "Click again to confirm");
    public static string HistoryHelp => T("Les 30 derniers tirages de la roue libre.", "The last 30 free wheel draws.");
}
