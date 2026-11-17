using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace DressRoulette.Windows;

/// <summary>Petits outils d'affichage partagés par les onglets.</summary>
internal static class Ui
{
    public static readonly Vector4 Pink = new(0.97f, 0.62f, 0.76f, 1f);
    public static readonly Vector4 Gold = new(0.98f, 0.82f, 0.42f, 1f);
    public static readonly Vector4 Flame = new(1f, 0.55f, 0.2f, 1f);
    public static readonly Vector4 Dim = new(0.65f, 0.65f, 0.7f, 1f);
    public static readonly Vector4 Good = new(0.55f, 0.85f, 0.55f, 1f);

    /// <summary>Couleur ImGui (format ABGR) à partir de composantes 0-255.</summary>
    public static uint Rgba(byte r, byte g, byte b, byte a = 255)
        => ((uint)a << 24) | ((uint)b << 16) | ((uint)g << 8) | r;

    /// <summary>Texte coupé en lignes et centré horizontalement dans la zone disponible.</summary>
    public static void CenteredWrapped(string text, Vector4? color = null)
    {
        var width = ImGui.GetContentRegionAvail().X;
        foreach (var line in WrapLines(text, width))
        {
            var lineWidth = ImGui.CalcTextSize(line).X;
            var offset = (width - lineWidth) * 0.5f;
            if (offset > 0)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

            if (color is { } c)
                ImGui.TextColored(c, line);
            else
                ImGui.TextUnformatted(line);
        }
    }

    /// <summary>Découpe un texte en lignes qui tiennent dans la largeur donnée (police actuelle).</summary>
    public static List<string> WrapLines(string text, float width)
    {
        var lines = new List<string>();
        var current = string.Empty;
        foreach (var word in text.Split(' '))
        {
            var candidate = current.Length == 0 ? word : $"{current} {word}";
            if (current.Length > 0 && ImGui.CalcTextSize(candidate).X > width)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = candidate;
            }
        }

        if (current.Length > 0)
            lines.Add(current);
        return lines;
    }

    /// <summary>Icône Font Awesome en couleur, suivie d'un texte sur la même ligne.</summary>
    public static void IconText(FontAwesomeIcon icon, Vector4 iconColor, string text)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
            ImGui.TextColored(iconColor, icon.ToIconString());
        ImGui.SameLine();
        ImGui.TextUnformatted(text);
    }

    /// <summary>Largeur d'une icône suivie d'un texte (pour centrer une ligne).</summary>
    public static float IconTextWidth(FontAwesomeIcon icon, string text)
    {
        float iconWidth;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            iconWidth = ImGui.CalcTextSize(icon.ToIconString()).X;
        return iconWidth + ImGui.GetStyle().ItemSpacing.X + ImGui.CalcTextSize(text).X;
    }

    /// <summary>Affiche une info-bulle quand la souris survole l'élément précédent.</summary>
    public static void Tooltip(string text)
    {
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(text);
    }
}
