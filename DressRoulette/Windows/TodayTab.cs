using System;
using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using DressRoulette.Core;
using DressRoulette.Localization;

namespace DressRoulette.Windows;

/// <summary>Onglet « Aujourd'hui » : thème du jour, contrainte bonus, série et bouton de partage.</summary>
internal sealed class TodayTab
{
    private readonly Plugin plugin;
    private readonly IFontHandle bigFont;

    /// <summary>Jusqu'à quand afficher « Texte copié ».</summary>
    private DateTime copiedMessageUntil = DateTime.MinValue;

    public TodayTab(Plugin plugin, IFontHandle bigFont)
    {
        this.plugin = plugin;
        this.bigFont = bigFont;
    }

    public void Draw()
    {
        var lang = Loc.Current;
        var today = Plugin.Today;
        var config = plugin.Configuration;
        var streak = config.Streak;
        var theme = plugin.Catalog.DailyTheme(today);
        var constraint = plugin.Catalog.DailyConstraint(today);

        DrawStreakMessages(streak);

        ImGuiHelpers.ScaledDummy(6f);
        Ui.CenteredWrapped(Loc.DailyTheme, Ui.Dim);

        using (bigFont.Push())
            Ui.CenteredWrapped(theme.Get(lang), Ui.Pink);

        var category = plugin.Catalog.GetCategory(theme.Category);
        if (category != null)
            Ui.CenteredWrapped(category.Get(lang), Ui.Dim);

        if (!config.HideDailyConstraint)
        {
            ImGuiHelpers.ScaledDummy(10f);
            Ui.CenteredWrapped(Loc.DailyConstraint, Ui.Dim);
            Ui.CenteredWrapped(constraint.Get(lang), Ui.Gold);
        }

        ImGuiHelpers.ScaledDummy(8f);
        var remaining = DailyPicker.DayStartUtc(today + 1) - DateTime.UtcNow;
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;
        Ui.CenteredWrapped(Loc.NextThemeIn((int)remaining.TotalHours, remaining.Minutes), Ui.Dim);

        ImGuiHelpers.ScaledDummy(8f);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(8f);

        DrawStreakRow(streak);

        ImGuiHelpers.ScaledDummy(12f);
        DrawShareButton(today, theme, constraint);

        ImGuiHelpers.ScaledDummy(10f);
        DrawCalendar(today, streak);
    }

    /// <summary>Calendrier de série : les 7 ou 30 derniers jours, avec leur thème et leur état.</summary>
    private void DrawCalendar(long today, StreakState streak)
    {
        if (!ImGui.CollapsingHeader($"{Loc.LastDays}###calendar"))
            return;

        var config = plugin.Configuration;
        if (ImGui.RadioButton(Loc.Last7Days, !config.CalendarShowMonth))
        {
            config.CalendarShowMonth = false;
            config.Save();
        }

        ImGui.SameLine();
        if (ImGui.RadioButton(Loc.Last30Days, config.CalendarShowMonth))
        {
            config.CalendarShowMonth = true;
            config.Save();
        }

        var lang = Loc.Current;
        var culture = CultureInfo.GetCultureInfo(lang == Lang.French ? "fr-FR" : "en-US");
        var dateFormat = lang == Lang.French ? "ddd dd/MM" : "ddd MM/dd";
        var count = config.CalendarShowMonth ? 30 : 7;

        for (var i = 0; i < count; i++)
        {
            var day = today - i;
            var status = StreakRules.StatusOf(streak, day, today);
            var (icon, color, label) = status switch
            {
                DayStatus.Shared => (FontAwesomeIcon.Check, Ui.Good, Loc.StatusShared),
                DayStatus.Saved => (FontAwesomeIcon.Clone, Ui.Gold, Loc.StatusSaved),
                DayStatus.Pending => (FontAwesomeIcon.HourglassHalf, Ui.Dim, Loc.StatusPending),
                _ => (FontAwesomeIcon.Times, Ui.Dim, Loc.StatusMissed),
            };

            using (ImRaii.PushFont(UiBuilder.IconFont))
                ImGui.TextColored(color, icon.ToIconString());
            Ui.Tooltip(label);

            ImGui.SameLine(32f * ImGuiHelpers.GlobalScale);
            var date = DailyPicker.DayStartUtc(day).ToLocalTime().ToString(dateFormat, culture);
            ImGui.TextColored(Ui.Dim, date);

            ImGui.SameLine(130f * ImGuiHelpers.GlobalScale);
            ImGui.TextUnformatted(plugin.Catalog.DailyTheme(day).Get(lang));
        }
    }

    private void DrawStreakRow(StreakState streak)
    {
        var streakText = Loc.Days(streak.Current);
        var jokersText = Loc.JokersCount(streak.Jokers);
        var recordText = Loc.Record(streak.Best);

        var gap = 24f * ImGuiHelpers.GlobalScale;
        var total = Ui.IconTextWidth(FontAwesomeIcon.Fire, streakText)
                    + gap + Ui.IconTextWidth(FontAwesomeIcon.Clone, jokersText)
                    + gap + Ui.IconTextWidth(FontAwesomeIcon.Trophy, recordText);
        var offset = (ImGui.GetContentRegionAvail().X - total) * 0.5f;
        if (offset > 0)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        ImGui.BeginGroup();
        Ui.IconText(FontAwesomeIcon.Fire, Ui.Flame, streakText);
        ImGui.EndGroup();
        Ui.Tooltip(Loc.StreakTooltip);

        ImGui.SameLine(0, gap);
        ImGui.BeginGroup();
        Ui.IconText(FontAwesomeIcon.Clone, Ui.Gold, jokersText);
        ImGui.EndGroup();
        Ui.Tooltip(Loc.JokersTooltip);

        ImGui.SameLine(0, gap);
        Ui.IconText(FontAwesomeIcon.Trophy, Ui.Dim, recordText);
    }

    /// <summary>Messages « Un joker a sauvé ta série ! » et « Nouveau joker gagné ! ».</summary>
    private void DrawStreakMessages(StreakState streak)
    {
        if (streak.UnseenJokersUsed <= 0 && !streak.UnseenJokerEarned)
            return;

        ImGuiHelpers.ScaledDummy(4f);
        if (streak.UnseenJokersUsed > 0)
        {
            var text = streak.UnseenJokersUsed == 1 ? Loc.OneJokerSaved : Loc.JokersSaved(streak.UnseenJokersUsed);
            Ui.CenteredWrapped(text, Ui.Gold);
        }

        if (streak.UnseenJokerEarned)
            Ui.CenteredWrapped(Loc.JokerEarned, Ui.Good);

        var buttonWidth = 80f * ImGuiHelpers.GlobalScale;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((ImGui.GetContentRegionAvail().X - buttonWidth) * 0.5f));
        if (ImGui.Button($"{Loc.Ok}##jokerMessage", new Vector2(buttonWidth, 0)))
        {
            streak.UnseenJokersUsed = 0;
            streak.UnseenJokerEarned = false;
            plugin.Configuration.Save();
        }

        ImGui.Separator();
    }

    private void DrawShareButton(long today, ThemeDef theme, ConstraintDef constraint)
    {
        var config = plugin.Configuration;
        var height = 40f * ImGuiHelpers.GlobalScale;

        if (ImGui.Button($"{Loc.ShareMyOutfit}##shareDaily", new Vector2(-1, height)))
        {
            // Seul ce bouton compte pour la série. Plusieurs partages le même jour comptent pour un.
            StreakRules.RegisterShare(config.Streak, today);
            config.Save();

            var lang = Loc.Current;
            var constraintText = !config.HideDailyConstraint && !constraint.IsNone ? constraint.Get(lang) : null;
            var text = ShareText.Daily(lang, theme.Get(lang), constraintText, config.Streak.Current);
            Plugin.CopyAndOpenShareChannel(text);
            copiedMessageUntil = DateTime.UtcNow.AddSeconds(6);
        }

        if (DateTime.UtcNow < copiedMessageUntil)
            Ui.CenteredWrapped(Loc.Copied, Ui.Good);
        else if (StreakRules.HasSharedToday(config.Streak, today))
            Ui.CenteredWrapped(Loc.AlreadySharedToday, Ui.Good);

        ImGuiHelpers.ScaledDummy(4f);
        using (ImRaii.PushColor(ImGuiCol.Text, Ui.Dim))
            ImGui.TextWrapped(Links.IsShareChannelConfigured ? Loc.ShareHelp : Loc.ShareHelpNoLink);
    }
}
