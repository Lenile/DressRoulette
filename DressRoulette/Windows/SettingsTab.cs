using System;
using System.Globalization;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using DressRoulette.Core;
using DressRoulette.Localization;

namespace DressRoulette.Windows;

/// <summary>Onglet « Réglages » : langue, contrainte du jour, catégories, thèmes perso, historique.</summary>
internal sealed class SettingsTab
{
    private readonly Plugin plugin;
    private readonly WheelTab wheelTab;

    private string newThemeText = string.Empty;
    private DateTime confirmClearUntil = DateTime.MinValue;

    public SettingsTab(Plugin plugin, WheelTab wheelTab)
    {
        this.plugin = plugin;
        this.wheelTab = wheelTab;
    }

    public void Draw()
    {
        DrawGeneral();
        ImGuiHelpers.ScaledDummy(6f);
        DrawCategories();
        DrawCustomThemes();
        DrawExcludedThemes();
        DrawHistory();
    }

    private void DrawGeneral()
    {
        var config = plugin.Configuration;

        ImGui.SetNextItemWidth(260f * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo(Loc.Language, LanguageLabel(config.Language)))
        {
            if (combo.Success)
            {
                foreach (var option in Enum.GetValues<LanguageSetting>())
                {
                    if (ImGui.Selectable(LanguageLabel(option), option == config.Language))
                    {
                        config.Language = option;
                        config.Save();
                        plugin.UpdateLanguage();
                    }
                }
            }
        }

        var hide = config.HideDailyConstraint;
        if (ImGui.Checkbox(Loc.HideDailyConstraint, ref hide))
        {
            config.HideDailyConstraint = hide;
            config.Save();
        }

        var reminder = config.ChatReminder;
        if (ImGui.Checkbox(Loc.ChatReminder, ref reminder))
        {
            config.ChatReminder = reminder;
            config.Save();
        }

        Ui.Tooltip(Loc.ChatReminderHelp);
    }

    private static string LanguageLabel(LanguageSetting setting) => setting switch
    {
        LanguageSetting.French => Loc.LanguageFrench,
        LanguageSetting.English => Loc.LanguageEnglish,
        _ => Loc.LanguageAuto,
    };

    private void DrawCategories()
    {
        if (!ImGui.CollapsingHeader($"{Loc.Categories}###categories", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        var lang = Loc.Current;
        var wheel = plugin.Wheel;
        var catalog = plugin.Catalog;
        var lastOne = wheel.EnabledCategoryCount() <= 1;

        ImGui.TextColored(Ui.Dim, Loc.CategoriesHelp);

        foreach (var category in catalog.Categories)
        {
            var count = 0;
            foreach (var t in catalog.Themes)
            {
                if (t.Category == category.Id)
                    count++;
            }

            DrawCategoryCheckbox(category.Id, $"{category.Get(lang)} ({count})", lastOne);
        }

        DrawCategoryCheckbox(
            FreeWheel.CustomCategoryId,
            $"{Loc.MyThemes} ({plugin.Configuration.Wheel.CustomThemes.Count})",
            lastOne);
    }

    private void DrawCategoryCheckbox(string id, string label, bool lastOne)
    {
        var enabled = plugin.Wheel.IsCategoryEnabled(id);

        // La dernière catégorie active ne peut pas être décochée.
        using (ImRaii.Disabled(enabled && lastOne))
        {
            if (ImGui.Checkbox($"{label}##cat_{id}", ref enabled) && plugin.Wheel.SetCategoryEnabled(id, enabled))
            {
                plugin.Configuration.Save();
                wheelTab.MarkPoolChanged();
            }
        }
    }

    private void DrawCustomThemes()
    {
        if (!ImGui.CollapsingHeader($"{Loc.MyThemes}###customThemes"))
            return;

        var wheel = plugin.Wheel;
        var themes = plugin.Configuration.Wheel.CustomThemes;

        var buttonWidth = ImGui.CalcTextSize(Loc.Add).X + (ImGui.GetStyle().FramePadding.X * 2);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - buttonWidth - ImGui.GetStyle().ItemSpacing.X);
        var submitted = ImGui.InputTextWithHint(
            "##newTheme",
            Loc.AddThemeHint,
            ref newThemeText,
            FreeWheel.MaxCustomThemeLength,
            ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.SameLine();
        if (ImGui.Button($"{Loc.Add}##addTheme") || submitted)
        {
            if (wheel.AddCustomTheme(newThemeText))
            {
                plugin.Configuration.Save();
                wheelTab.MarkPoolChanged();
            }

            newThemeText = string.Empty;
        }

        if (themes.Count == 0)
        {
            ImGui.TextColored(Ui.Dim, Loc.NoCustomThemes);
            return;
        }

        Guid? toRemove = null;
        foreach (var theme in themes)
        {
            using (ImRaii.PushId(theme.Id.ToString()))
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
                    toRemove = theme.Id;
                Ui.Tooltip(Loc.Delete);
            }

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(theme.Text);
        }

        if (toRemove is { } id && wheel.RemoveCustomTheme(id))
        {
            plugin.Configuration.Save();
            wheelTab.MarkPoolChanged();
        }
    }

    private void DrawExcludedThemes()
    {
        var wheel = plugin.Wheel;
        var excluded = wheel.ExcludedItems();
        if (!ImGui.CollapsingHeader($"{Loc.ExcludedThemes} ({excluded.Count})###excludedThemes"))
            return;

        ImGui.TextColored(Ui.Dim, Loc.ExcludedThemesHelp);
        if (excluded.Count == 0)
        {
            ImGui.TextColored(Ui.Dim, Loc.NoExcludedThemes);
            return;
        }

        var lang = Loc.Current;
        string? toRestore = null;
        foreach (var item in excluded)
        {
            using (ImRaii.PushId(item.Key))
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Undo))
                    toRestore = item.Key;
                Ui.Tooltip(Loc.RestoreTheme);
            }

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(item.Get(lang));
        }

        if (toRestore != null && wheel.Restore(toRestore))
        {
            plugin.Configuration.Save();
            wheelTab.MarkPoolChanged();
        }
    }

    private void DrawHistory()
    {
        if (!ImGui.CollapsingHeader($"{Loc.History}###history"))
            return;

        var lang = Loc.Current;
        var history = plugin.Configuration.Wheel.History;
        ImGui.TextColored(Ui.Dim, Loc.HistoryHelp);

        if (history.Count == 0)
        {
            ImGui.TextColored(Ui.Dim, Loc.HistoryEmpty);
            return;
        }

        var format = lang == Lang.French ? "dd/MM/yyyy HH:mm" : "yyyy-MM-dd HH:mm";
        foreach (var entry in history)
        {
            var date = entry.DateUtc.ToLocalTime().ToString(format, CultureInfo.InvariantCulture);
            ImGui.TextColored(Ui.Dim, date);
            ImGui.SameLine();
            var constraint = entry.GetConstraint(lang);
            ImGui.TextWrapped(constraint is null ? entry.GetTheme(lang) : $"{entry.GetTheme(lang)} (+ {constraint})");
        }

        ImGuiHelpers.ScaledDummy(4f);
        var confirming = DateTime.UtcNow < confirmClearUntil;
        if (ImGui.Button(confirming ? $"{Loc.ConfirmClear}###clearHistory" : $"{Loc.ClearHistory}###clearHistory"))
        {
            if (confirming)
            {
                history.Clear();
                plugin.Configuration.Save();
                confirmClearUntil = DateTime.MinValue;
            }
            else
            {
                confirmClearUntil = DateTime.UtcNow.AddSeconds(4);
            }
        }
    }
}
