using System;
using System.IO;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using DressRoulette.Core;
using DressRoulette.Localization;
using DressRoulette.Windows;

namespace DressRoulette;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    private const string MainCommand = "/dressroulette";
    private static readonly string[] AliasCommands = { "/droulette", "/drsr" };

    public Configuration Configuration { get; }
    public ThemeCatalog Catalog { get; }
    public FreeWheel Wheel { get; }

    public readonly WindowSystem WindowSystem = new("DressRoulette");
    private MainWindow MainWindow { get; }

    /// <summary>Dernier jour pour lequel les jours ratés ont été vérifiés.</summary>
    private long lastCheckedDay = long.MinValue;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Normalize();

        Catalog = LoadCatalog();
        Wheel = new FreeWheel(Catalog, Configuration.Wheel);

        UpdateLanguage();

        // Vérifie les jours ratés dès le chargement du plugin.
        CheckDayChange();

        MainWindow = new MainWindow(this);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(MainCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = "Ouvre DressRoulette / Opens DressRoulette (aussi /droulette, /drsr).",
        });
        foreach (var alias in AliasCommands)
        {
            CommandManager.AddHandler(alias, new CommandInfo(OnCommand)
            {
                HelpMessage = "Ouvre DressRoulette / Opens DressRoulette.",
                ShowInHelp = false,
            });
        }

        PluginInterface.UiBuilder.Draw += OnDraw;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += OpenSettings;
        ClientState.Login += OnLogin;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= OnDraw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenSettings;
        ClientState.Login -= OnLogin;

        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(MainCommand);
        foreach (var alias in AliasCommands)
            CommandManager.RemoveHandler(alias);
    }

    /// <summary>Numéro du jour de jeu en cours (change à 15 h UTC).</summary>
    public static long Today => DailyPicker.DayNumber(DateTime.UtcNow);

    /// <summary>
    /// Au changement de jour (ou au chargement), rattrape les jours ratés
    /// et utilise des jokers si besoin.
    /// </summary>
    public void CheckDayChange()
    {
        var today = Today;
        if (today == lastCheckedDay)
            return;

        // Reset quotidien pendant que le joueur est connecté (pas au chargement du plugin).
        var isNewDayWhilePlaying = lastCheckedDay != long.MinValue && ClientState.IsLoggedIn;

        lastCheckedDay = today;
        if (isNewDayWhilePlaying)
            PrintDailyReminder(newDay: true);

        var before = (Configuration.Streak.Current, Configuration.Streak.Jokers, Configuration.Streak.LastCoveredDay);
        StreakRules.CatchUp(Configuration.Streak, today);
        var after = (Configuration.Streak.Current, Configuration.Streak.Jokers, Configuration.Streak.LastCoveredDay);
        if (before != after)
            Configuration.Save();
    }

    /// <summary>
    /// Copie le texte dans le presse-papiers puis ouvre le salon de partage
    /// (uniquement si le lien a été configuré). Le plugin n'envoie rien lui-même.
    /// </summary>
    public static void CopyAndOpenShareChannel(string text)
    {
        ImGui.SetClipboardText(text);
        if (Links.IsShareChannelConfigured)
            Util.OpenLink(Links.ShareChannelUrl);
    }

    /// <summary>Texte du thème du jour, avec la contrainte bonus si elle est affichée.</summary>
    public string DailyThemeText(long day, Lang lang)
    {
        var theme = Catalog.DailyTheme(day).Get(lang);
        var constraint = Catalog.DailyConstraint(day);
        return !Configuration.HideDailyConstraint && !constraint.IsNone
            ? $"{theme} (+ {constraint.Get(lang)})"
            : theme;
    }

    /// <summary>
    /// Rappel facultatif dans le chat (désactivé par défaut). Le message reste local :
    /// il n'est visible que par le joueur et n'est envoyé à personne.
    /// </summary>
    private void PrintDailyReminder(bool newDay)
    {
        if (!Configuration.ChatReminder)
            return;

        UpdateLanguage();
        var text = DailyThemeText(Today, Loc.Current);
        ChatGui.Print(newDay ? Loc.ChatNewDay(text) : Loc.ChatDaily(text), "DressRoulette");
    }

    private void OnLogin() => PrintDailyReminder(newDay: false);

    public void UpdateLanguage() => Loc.Update(Configuration.Language, ClientState.ClientLanguage);

    private void OnDraw()
    {
        UpdateLanguage();
        CheckDayChange();
        WindowSystem.Draw();
    }

    private void OnCommand(string command, string args) => MainWindow.Toggle();

    public void ToggleMainUi() => MainWindow.Toggle();

    public void OpenSettings()
    {
        MainWindow.IsOpen = true;
        MainWindow.RequestSettingsTab();
    }

    private static ThemeCatalog LoadCatalog()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var themes = OpenResource(assembly, "DressRoulette.Data.themes.json");
        using var constraints = OpenResource(assembly, "DressRoulette.Data.constraints.json");
        return ThemeCatalog.Load(themes, constraints);
    }

    private static Stream OpenResource(Assembly assembly, string name)
        => assembly.GetManifestResourceStream(name)
           ?? throw new FileNotFoundException($"Ressource introuvable dans le plugin : {name}");
}
