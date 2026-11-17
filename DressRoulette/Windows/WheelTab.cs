using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using DressRoulette.Core;
using DressRoulette.Localization;

namespace DressRoulette.Windows;

/// <summary>
/// Onglet « Roue libre » : une roue dessinée avec la liste de dessin d'ImGui,
/// qu'on peut relancer autant qu'on veut. Ces tirages ne comptent pas pour la série.
/// </summary>
internal sealed class WheelTab
{
    private const float Tau = MathF.PI * 2f;

    /// <summary>Le pointeur est en haut de la roue (angle ImGui : 0 = droite, sens horaire).</summary>
    private const float PointerAngle = -MathF.PI / 2f;

    private static readonly uint[] Palette =
    {
        Ui.Rgba(244, 167, 185), Ui.Rgba(247, 197, 159), Ui.Rgba(251, 227, 142), Ui.Rgba(184, 224, 160),
        Ui.Rgba(159, 216, 203), Ui.Rgba(160, 196, 242), Ui.Rgba(195, 177, 225), Ui.Rgba(242, 181, 212),
        Ui.Rgba(255, 214, 165), Ui.Rgba(202, 240, 191), Ui.Rgba(155, 226, 240), Ui.Rgba(189, 178, 255),
    };

    private static readonly uint LabelColor = Ui.Rgba(43, 34, 51);
    private static readonly uint RimColor = Ui.Rgba(60, 50, 70);
    private static readonly uint PointerColor = Ui.Rgba(255, 255, 255);
    private static readonly uint PointerOutline = Ui.Rgba(60, 50, 70);

    private readonly Plugin plugin;
    private readonly IFontHandle bigFont;
    private readonly Random random = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();

    private List<WheelItem> segments = new();
    private bool segmentsDirty = true;
    private bool segmentsNamed;

    // Animation
    private float rotation;
    private bool spinning;
    private double spinStart;
    private double spinDuration;
    private float spinFrom;
    private float spinTo;
    private int pendingWinner = -1;
    private ConstraintDef? pendingConstraint;

    // Dernier résultat
    private WheelItem? resultTheme;
    private ConstraintDef? resultConstraint;
    private DateTime copiedMessageUntil = DateTime.MinValue;
    private bool resultExcluded;

    /// <summary>Cache des libellés raccourcis, pour ne pas les recalculer à chaque image.</summary>
    private readonly Dictionary<string, (float Size, float MaxWidth, Lang Lang, string Text)> labelCache = new();

    public WheelTab(Plugin plugin, IFontHandle bigFont)
    {
        this.plugin = plugin;
        this.bigFont = bigFont;
    }

    /// <summary>À appeler quand les catégories ou les thèmes perso changent.</summary>
    public void MarkPoolChanged() => segmentsDirty = true;

    public void Draw()
    {
        var wheelState = plugin.Configuration.Wheel;

        // On ne reconstruit pas la roue pendant qu'elle tourne.
        if (!spinning && (segmentsDirty || segmentsNamed != wheelState.ShowNames))
            RebuildSegments();

        UpdateSpin();
        DrawControls(wheelState);

        if (segments.Count == 0)
        {
            ImGuiHelpers.ScaledDummy(20f);
            Ui.CenteredWrapped(Loc.EmptyWheel, Ui.Gold);
            return;
        }

        DrawWheel(wheelState.ShowNames);
        DrawResult();
    }

    private void DrawControls(FreeWheelState wheelState)
    {
        using (ImRaii.Disabled(spinning))
        {
            var showNames = wheelState.ShowNames;
            if (ImGui.RadioButton(Loc.WheelModeNamed, showNames))
            {
                wheelState.ShowNames = true;
                plugin.Configuration.Save();
            }

            ImGui.SameLine();
            if (ImGui.RadioButton(Loc.WheelModeFull, !showNames))
            {
                wheelState.ShowNames = false;
                plugin.Configuration.Save();
            }

            if (wheelState.ShowNames)
            {
                ImGui.SameLine();
                if (ImGui.SmallButton($"{Loc.Reshuffle}##reshuffle"))
                    segmentsDirty = true;
            }

            var bonus = wheelState.BonusConstraint;
            if (ImGui.Checkbox(Loc.BonusConstraint, ref bonus))
            {
                wheelState.BonusConstraint = bonus;
                plugin.Configuration.Save();
            }
        }

        ImGui.SameLine();
        ImGui.TextColored(Ui.Dim, Loc.ThemesOnWheel(segments.Count));
    }

    private void RebuildSegments()
    {
        var wheelState = plugin.Configuration.Wheel;
        var pool = plugin.Wheel.BuildPool();
        segments = plugin.Wheel.BuildSegments(pool, wheelState.ShowNames);
        segmentsNamed = wheelState.ShowNames;
        segmentsDirty = false;
        labelCache.Clear();
    }

    // ---------------------------------------------------------------- Animation

    private void StartSpin()
    {
        if (spinning || segments.Count == 0)
            return;

        var wheel = plugin.Wheel;
        pendingWinner = wheel.PickWinnerIndex(segments);
        pendingConstraint = plugin.Configuration.Wheel.BonusConstraint ? wheel.PickConstraint() : null;

        // Angle final : le milieu du segment gagnant (avec un petit décalage au hasard) sous le pointeur.
        var width = Tau / segments.Count;
        var jitter = ((float)random.NextDouble() - 0.5f) * 0.7f * width;
        var target = PointerAngle - ((pendingWinner + 0.5f) * width) + jitter;
        var delta = Mod(target - rotation, Tau);
        var turns = 5 + random.Next(2);

        spinFrom = rotation;
        spinTo = rotation + (turns * Tau) + delta;
        spinDuration = 5.0 + random.NextDouble();
        spinStart = clock.Elapsed.TotalSeconds;
        spinning = true;

        resultTheme = null;
        resultConstraint = null;
        resultExcluded = false;
    }

    private void UpdateSpin()
    {
        if (!spinning)
            return;

        var t = (clock.Elapsed.TotalSeconds - spinStart) / spinDuration;
        if (t >= 1.0)
        {
            rotation = Mod(spinTo, Tau);
            spinning = false;
            FinishSpin();
            return;
        }

        // Ralentissement progressif (courbe « ease-out » d'ordre 4).
        var eased = 1.0 - Math.Pow(1.0 - t, 4);
        rotation = (float)(spinFrom + ((spinTo - spinFrom) * eased));
    }

    private void FinishSpin()
    {
        if (pendingWinner < 0 || pendingWinner >= segments.Count)
            return;

        resultTheme = segments[pendingWinner];
        resultConstraint = pendingConstraint;
        plugin.Wheel.Record(resultTheme, resultConstraint, DateTime.UtcNow);
        plugin.Configuration.Save();
        pendingWinner = -1;
        pendingConstraint = null;
    }

    // ---------------------------------------------------------------- Dessin

    private void DrawWheel(bool named)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var avail = ImGui.GetContentRegionAvail();
        var reservedBelow = 170f * scale;
        var size = MathF.Min(avail.X, avail.Y - reservedBelow);
        size = MathF.Max(size, 220f * scale);

        ImGuiHelpers.ScaledDummy(4f);
        var start = ImGui.GetCursorScreenPos();
        var left = start.X + ((avail.X - size) * 0.5f);
        var center = new Vector2(left + (size * 0.5f), start.Y + (size * 0.5f));
        var radius = (size * 0.5f) - (14f * scale);

        // Zone cliquable : un clic sur la roue la lance.
        ImGui.SetCursorScreenPos(new Vector2(left, start.Y));
        if (ImGui.InvisibleButton("##wheel", new Vector2(size, size)) && !spinning)
            StartSpin();

        var drawList = ImGui.GetWindowDrawList();
        var count = segments.Count;
        var width = Tau / count;

        // Segments
        if (count == 1)
        {
            drawList.AddCircleFilled(center, radius, Palette[0], 64);
        }
        else
        {
            var arcSteps = Math.Max(2, (int)MathF.Ceiling(width / (Tau / 128f)));
            for (var i = 0; i < count; i++)
            {
                var a0 = rotation + (i * width);
                drawList.PathClear();
                drawList.PathLineTo(center);
                drawList.PathArcTo(center, radius, a0, a0 + width, arcSteps);
                drawList.PathFillConvex(SegmentColor(i, count));
            }
        }

        // Noms (roue de 100 seulement)
        var showLabels = false;
        if (named)
            showLabels = DrawLabels(drawList, center, radius, width);

        // Bord, moyeu et pointeur
        drawList.AddCircle(center, radius, RimColor, 0, 3f * scale);
        drawList.AddCircleFilled(center, radius * 0.1f, RimColor, 32);
        drawList.AddCircleFilled(center, radius * 0.06f, PointerColor, 32);

        var tip = new Vector2(center.X, center.Y - radius + (14f * scale));
        var baseLeft = new Vector2(center.X - (11f * scale), center.Y - radius - (10f * scale));
        var baseRight = new Vector2(center.X + (11f * scale), center.Y - radius - (10f * scale));
        drawList.AddTriangleFilled(baseLeft, baseRight, tip, PointerColor);
        drawList.AddTriangle(baseLeft, baseRight, tip, PointerOutline, 2f * scale);

        if (named && !showLabels)
            Ui.CenteredWrapped(Loc.WheelTooSmall, Ui.Dim);
    }

    /// <summary>Dessine les noms le long des rayons. Renvoie faux si la roue est trop petite pour les lire.</summary>
    private bool DrawLabels(ImDrawListPtr drawList, Vector2 center, float radius, float width)
    {
        var lang = Loc.Current;
        var font = ImGui.GetFont();

        // Taille du texte : ce qui tient dans un segment aux trois quarts du rayon.
        var fontSize = MathF.Min(radius * 0.75f * width * 0.9f, 18f * ImGuiHelpers.GlobalScale);
        if (fontSize < 8f)
            return false;

        var outer = radius * 0.94f;
        var inner = radius * 0.16f;
        var maxWidth = outer - inner;

        for (var i = 0; i < segments.Count; i++)
        {
            var label = FitLabel(segments[i], lang, font, fontSize, maxWidth);
            var textSize = ImGui.CalcTextSizeA(font, fontSize, float.MaxValue, 0f, label, out _);

            var angle = Mod(rotation + ((i + 0.5f) * width), Tau);

            // Sur la moitié gauche, on retourne le texte pour qu'il ne soit pas à l'envers.
            var flip = MathF.Cos(angle) < 0f;
            var position = flip
                ? new Vector2(center.X - outer, center.Y - (textSize.Y * 0.5f))
                : new Vector2(center.X + outer - textSize.X, center.Y - (textSize.Y * 0.5f));

            var firstVertex = drawList.VtxBuffer.Size;
            drawList.AddText(font, fontSize, position, LabelColor, label);
            RotateVertices(drawList, firstVertex, center, flip ? angle - MathF.PI : angle);
        }

        return true;
    }

    /// <summary>
    /// ImGui ne sait pas écrire du texte en biais : on écrit le texte à l'horizontale,
    /// puis on fait tourner ses sommets autour du centre de la roue.
    /// </summary>
    private static void RotateVertices(ImDrawListPtr drawList, int firstVertex, Vector2 center, float angle)
    {
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        ref var vertices = ref drawList.VtxBuffer;
        for (var v = firstVertex; v < vertices.Size; v++)
        {
            var vertex = vertices[v];
            var d = vertex.Pos - center;
            vertex.Pos = new Vector2(center.X + (d.X * cos) - (d.Y * sin), center.Y + (d.X * sin) + (d.Y * cos));
            vertices[v] = vertex;
        }
    }

    /// <summary>Raccourcit un nom avec « … » s'il est trop long pour le segment.</summary>
    private string FitLabel(WheelItem item, Lang lang, ImFontPtr font, float fontSize, float maxWidth)
    {
        if (labelCache.TryGetValue(item.Key, out var cached)
            && cached.Lang == lang
            && MathF.Abs(cached.Size - fontSize) < 0.5f
            && MathF.Abs(cached.MaxWidth - maxWidth) < 2f)
            return cached.Text;

        var text = item.Get(lang);
        if (ImGui.CalcTextSizeA(font, fontSize, float.MaxValue, 0f, text, out _).X > maxWidth)
        {
            // Recherche du plus long début qui tient avec les points de suspension.
            int low = 0, high = text.Length;
            while (low < high)
            {
                var mid = (low + high + 1) / 2;
                var candidate = text[..mid].TrimEnd() + "…";
                if (ImGui.CalcTextSizeA(font, fontSize, float.MaxValue, 0f, candidate, out _).X <= maxWidth)
                    low = mid;
                else
                    high = mid - 1;
            }

            text = text[..low].TrimEnd() + "…";
        }

        labelCache[item.Key] = (fontSize, maxWidth, lang, text);
        return text;
    }

    private static uint SegmentColor(int index, int count)
    {
        var colorIndex = index % Palette.Length;

        // Évite que le dernier segment ait la même couleur que le premier, son voisin.
        if (index == count - 1 && count > 1 && colorIndex == 0)
            colorIndex = 1;

        return Palette[colorIndex];
    }

    // ---------------------------------------------------------------- Résultat

    private void DrawResult()
    {
        var lang = Loc.Current;
        ImGuiHelpers.ScaledDummy(6f);

        if (spinning)
        {
            // Pendant la rotation : le nom du segment qui passe sous le pointeur.
            var under = SegmentUnderPointer();
            using (bigFont.Push())
                Ui.CenteredWrapped(under?.Get(lang) ?? string.Empty, Ui.Dim);
            Ui.CenteredWrapped(Loc.Spinning, Ui.Dim);
            return;
        }

        if (resultTheme != null)
        {
            using (bigFont.Push())
                Ui.CenteredWrapped(resultTheme.Get(lang), Ui.Pink);

            if (resultConstraint != null)
                Ui.CenteredWrapped($"+ {resultConstraint.Get(lang)}", Ui.Gold);

            DrawExcludeButton(resultTheme);
        }

        ImGuiHelpers.ScaledDummy(6f);
        var scale = ImGuiHelpers.GlobalScale;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var buttonWidth = (ImGui.GetContentRegionAvail().X - spacing) * 0.5f;
        var height = 34f * scale;

        if (ImGui.Button($"{Loc.Spin}##spin", new Vector2(buttonWidth, height)))
            StartSpin();

        ImGui.SameLine();
        using (ImRaii.Disabled(resultTheme == null))
        {
            if (ImGui.Button($"{Loc.ShareThisDraw}##shareDraw", new Vector2(buttonWidth, height)) && resultTheme != null)
            {
                // Ce partage ne compte pas pour la série.
                var constraintText = resultConstraint is { IsNone: false } c ? c.Get(lang) : null;
                Plugin.CopyAndOpenShareChannel(ShareText.FreeDraw(lang, resultTheme.Get(lang), constraintText));
                copiedMessageUntil = DateTime.UtcNow.AddSeconds(6);
            }
        }

        Ui.Tooltip(Loc.ShareThisDrawHelp);

        if (DateTime.UtcNow < copiedMessageUntil)
            Ui.CenteredWrapped(Loc.Copied, Ui.Good);
    }

    /// <summary>Petit bouton « Ne plus tirer ce thème » sous le résultat.</summary>
    private void DrawExcludeButton(WheelItem theme)
    {
        if (resultExcluded)
        {
            Ui.CenteredWrapped(Loc.ThemeExcluded, Ui.Dim);
            return;
        }

        var label = $"{Loc.ExcludeTheme}##exclude";
        var width = ImGui.CalcTextSize(Loc.ExcludeTheme).X + (ImGui.GetStyle().FramePadding.X * 2);
        var offset = (ImGui.GetContentRegionAvail().X - width) * 0.5f;
        if (offset > 0)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        if (ImGui.SmallButton(label) && plugin.Wheel.Exclude(theme.Key))
        {
            plugin.Configuration.Save();
            resultExcluded = true;
            segmentsDirty = true;
        }

        Ui.Tooltip(Loc.ExcludeThemeHelp);
    }

    private WheelItem? SegmentUnderPointer()
    {
        if (segments.Count == 0)
            return null;

        var width = Tau / segments.Count;
        var relative = Mod(PointerAngle - rotation, Tau);
        var index = Math.Clamp((int)(relative / width), 0, segments.Count - 1);
        return segments[index];
    }

    private static float Mod(float value, float modulus)
    {
        var r = value % modulus;
        return r < 0 ? r + modulus : r;
    }
}
