using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using DressRoulette.Localization;

namespace DressRoulette.Windows;

/// <summary>
/// Fenêtre principale, avec trois onglets : Aujourd'hui, Roue libre, Réglages.
/// Elle ne s'ouvre jamais toute seule : seulement par une commande ou depuis l'installateur de plugins.
/// </summary>
public sealed class MainWindow : Window, IDisposable
{
    private readonly IFontHandle bigFont;
    private readonly TodayTab todayTab;
    private readonly WheelTab wheelTab;
    private readonly SettingsTab settingsTab;

    private bool selectSettingsTab;

    public MainWindow(Plugin plugin)
        : base("DressRoulette###DressRouletteMain")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 480),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        Size = new Vector2(620, 820);
        SizeCondition = ImGuiCond.FirstUseEver;

        bigFont = Plugin.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(
            new GameFontStyle(GameFontFamilyAndSize.Axis36));

        todayTab = new TodayTab(plugin, bigFont);
        wheelTab = new WheelTab(plugin, bigFont);
        settingsTab = new SettingsTab(plugin, wheelTab);
    }

    public void Dispose()
    {
        bigFont.Dispose();
    }

    /// <summary>Demande l'affichage de l'onglet Réglages à la prochaine image.</summary>
    public void RequestSettingsTab() => selectSettingsTab = true;

    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("##DressRouletteTabs");
        if (!tabBar.Success)
            return;

        // Les identifiants après ### restent fixes quand la langue change.
        using (var tab = ImRaii.TabItem($"{Loc.TabToday}###today"))
        {
            if (tab.Success)
                todayTab.Draw();
        }

        using (var tab = ImRaii.TabItem($"{Loc.TabWheel}###wheel"))
        {
            if (tab.Success)
                wheelTab.Draw();
        }

        var settingsFlags = selectSettingsTab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
        selectSettingsTab = false;
        using (var tab = ImRaii.TabItem($"{Loc.TabSettings}###settings", settingsFlags))
        {
            if (tab.Success)
                settingsTab.Draw();
        }
    }
}
