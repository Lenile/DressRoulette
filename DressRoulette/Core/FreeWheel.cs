using System;
using System.Collections.Generic;
using System.Linq;

namespace DressRoulette.Core;

/// <summary>Un thème ajouté par le joueur.</summary>
[Serializable]
public class CustomTheme
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
}

/// <summary>Un tirage de la roue libre, gardé dans l'historique.</summary>
[Serializable]
public class HistoryEntry
{
    public DateTime DateUtc { get; set; }
    public string ThemeKey { get; set; } = string.Empty;

    // Les textes sont copiés au moment du tirage : l'historique reste lisible
    // même si un thème perso est supprimé ensuite.
    public string ThemeFr { get; set; } = string.Empty;
    public string ThemeEn { get; set; } = string.Empty;
    public int? ConstraintId { get; set; }
    public string? ConstraintFr { get; set; }
    public string? ConstraintEn { get; set; }

    public string GetTheme(Lang lang) => lang == Lang.French ? ThemeFr : ThemeEn;
    public string? GetConstraint(Lang lang) => lang == Lang.French ? ConstraintFr : ConstraintEn;
}

/// <summary>Réglages et mémoire de la roue libre (enregistrés dans la configuration).</summary>
[Serializable]
public class FreeWheelState
{
    /// <summary>Catégories désactivées (les nouvelles catégories sont donc actives par défaut).</summary>
    public HashSet<string> DisabledCategories { get; set; } = new();

    public List<CustomTheme> CustomThemes { get; set; } = new();

    /// <summary>Thèmes que le joueur ne veut plus tirer dans la roue libre (clés).</summary>
    public HashSet<string> ExcludedThemeKeys { get; set; } = new();

    /// <summary>Ajouter une contrainte bonus aux tirages libres.</summary>
    public bool BonusConstraint { get; set; }

    /// <summary>Vrai : roue de 100 thèmes avec les noms. Faux : roue complète sans les noms.</summary>
    public bool ShowNames { get; set; } = true;

    /// <summary>Tirages récents, du plus récent au plus ancien.</summary>
    public List<HistoryEntry> History { get; set; } = new();

    /// <summary>Derniers thèmes tirés (clés), du plus récent au plus ancien.</summary>
    public List<string> RecentThemeKeys { get; set; } = new();

    /// <summary>Dernières contraintes tirées, de la plus récente à la plus ancienne.</summary>
    public List<int> RecentConstraintIds { get; set; } = new();
}

/// <summary>Un segment de la roue.</summary>
public sealed class WheelItem
{
    /// <summary>"b:12" pour un thème intégré, "u:&lt;guid&gt;" pour un thème perso.</summary>
    public required string Key { get; init; }
    public required string Fr { get; init; }
    public required string En { get; init; }
    public required string CategoryId { get; init; }

    public string Get(Lang lang) => lang == Lang.French ? Fr : En;
}

/// <summary>Logique de la roue libre : catégories, tirages, mémoire des derniers résultats.</summary>
public sealed class FreeWheel
{
    public const string CustomCategoryId = "custom";
    public const int AvoidRecentThemes = 15;
    public const int AvoidRecentConstraints = 25;
    public const int HistorySize = 30;
    public const int NamedWheelSize = 100;
    public const int MaxCustomThemeLength = 80;
    public const int MaxCustomThemes = 300;

    private readonly ThemeCatalog catalog;
    private readonly FreeWheelState state;
    private readonly Random random;

    public FreeWheel(ThemeCatalog catalog, FreeWheelState state, Random? random = null)
    {
        this.catalog = catalog;
        this.state = state;
        this.random = random ?? Random.Shared;
    }

    public FreeWheelState State => state;

    public static string BuiltinKey(int id) => $"b:{id}";
    public static string CustomKey(Guid id) => $"u:{id:N}";

    /// <summary>Identifiants de toutes les catégories, « Mes thèmes » compris.</summary>
    public IEnumerable<string> AllCategoryIds()
        => catalog.Categories.Select(c => c.Id).Append(CustomCategoryId);

    public bool IsCategoryEnabled(string id) => !state.DisabledCategories.Contains(id);

    public int EnabledCategoryCount() => AllCategoryIds().Count(IsCategoryEnabled);

    /// <summary>
    /// Active ou désactive une catégorie. Refuse de désactiver la dernière active.
    /// </summary>
    /// <returns>Vrai si quelque chose a changé.</returns>
    public bool SetCategoryEnabled(string id, bool enabled)
    {
        if (enabled)
            return state.DisabledCategories.Remove(id);

        if (!IsCategoryEnabled(id) || EnabledCategoryCount() <= 1)
            return false;

        return state.DisabledCategories.Add(id);
    }

    /// <summary>Tous les thèmes des catégories actives.</summary>
    public List<WheelItem> BuildPool()
    {
        var pool = new List<WheelItem>();
        foreach (var t in catalog.Themes)
        {
            if (IsCategoryEnabled(t.Category) && !IsExcluded(BuiltinKey(t.Id)))
                pool.Add(new WheelItem { Key = BuiltinKey(t.Id), Fr = t.Fr, En = t.En, CategoryId = t.Category });
        }

        if (IsCategoryEnabled(CustomCategoryId))
        {
            foreach (var c in state.CustomThemes)
            {
                if (!string.IsNullOrWhiteSpace(c.Text) && !IsExcluded(CustomKey(c.Id)))
                    pool.Add(new WheelItem { Key = CustomKey(c.Id), Fr = c.Text, En = c.Text, CategoryId = CustomCategoryId });
            }
        }

        return pool;
    }

    /// <summary>Segments à afficher : 100 thèmes au hasard, ou tous les thèmes.</summary>
    public List<WheelItem> BuildSegments(List<WheelItem> pool, bool named)
    {
        var copy = new List<WheelItem>(pool);
        Shuffle(copy);
        if (named && copy.Count > NamedWheelSize)
            copy.RemoveRange(NamedWheelSize, copy.Count - NamedWheelSize);
        return copy;
    }

    /// <summary>
    /// Choisit l'index du segment gagnant en évitant, si possible, les derniers thèmes tirés.
    /// Si tous les segments sont récents, on évite d'abord les plus récents seulement.
    /// </summary>
    public int PickWinnerIndex(IReadOnlyList<WheelItem> segments)
    {
        if (segments.Count == 0)
            throw new InvalidOperationException("La roue est vide.");

        var recent = state.RecentThemeKeys.Take(AvoidRecentThemes).ToList();
        for (var avoid = recent.Count; avoid >= 0; avoid--)
        {
            var blocked = new HashSet<string>(recent.Take(avoid));
            var candidates = new List<int>();
            for (var i = 0; i < segments.Count; i++)
            {
                if (!blocked.Contains(segments[i].Key))
                    candidates.Add(i);
            }

            if (candidates.Count > 0)
                return candidates[random.Next(candidates.Count)];
        }

        return random.Next(segments.Count);
    }

    /// <summary>Tire une contrainte bonus en évitant, si possible, les 25 dernières.</summary>
    public ConstraintDef PickConstraint()
    {
        var all = catalog.Constraints;
        var recent = state.RecentConstraintIds.Take(AvoidRecentConstraints).ToList();
        for (var avoid = recent.Count; avoid >= 0; avoid--)
        {
            var blocked = new HashSet<int>(recent.Take(avoid));
            var candidates = all.Where(c => !blocked.Contains(c.Id)).ToList();
            if (candidates.Count > 0)
                return candidates[random.Next(candidates.Count)];
        }

        return all[random.Next(all.Count)];
    }

    /// <summary>Enregistre un tirage terminé (historique et mémoire anti-répétition).</summary>
    public void Record(WheelItem theme, ConstraintDef? constraint, DateTime utcNow)
    {
        state.History.Insert(0, new HistoryEntry
        {
            DateUtc = utcNow,
            ThemeKey = theme.Key,
            ThemeFr = theme.Fr,
            ThemeEn = theme.En,
            ConstraintId = constraint?.Id,
            ConstraintFr = constraint?.Fr,
            ConstraintEn = constraint?.En,
        });
        Trim(state.History, HistorySize);

        state.RecentThemeKeys.Remove(theme.Key);
        state.RecentThemeKeys.Insert(0, theme.Key);
        Trim(state.RecentThemeKeys, AvoidRecentThemes);

        if (constraint != null)
        {
            state.RecentConstraintIds.Remove(constraint.Id);
            state.RecentConstraintIds.Insert(0, constraint.Id);
            Trim(state.RecentConstraintIds, AvoidRecentConstraints);
        }
    }

    /// <summary>Ajoute un thème perso. Renvoie faux si le texte est vide, en double ou si la liste est pleine.</summary>
    public bool AddCustomTheme(string text)
    {
        text = text.Trim();
        if (text.Length == 0 || state.CustomThemes.Count >= MaxCustomThemes)
            return false;
        if (text.Length > MaxCustomThemeLength)
            text = text[..MaxCustomThemeLength];
        if (state.CustomThemes.Any(c => string.Equals(c.Text, text, StringComparison.OrdinalIgnoreCase)))
            return false;

        state.CustomThemes.Add(new CustomTheme { Text = text });
        return true;
    }

    public bool RemoveCustomTheme(Guid id)
    {
        var removed = state.CustomThemes.RemoveAll(c => c.Id == id) > 0;
        if (removed)
        {
            state.RecentThemeKeys.Remove(CustomKey(id));
            state.ExcludedThemeKeys.Remove(CustomKey(id));
        }

        return removed;
    }

    public bool IsExcluded(string key) => state.ExcludedThemeKeys.Contains(key);

    /// <summary>« Ne plus tirer ce thème ». Le thème du jour n'est pas concerné.</summary>
    public bool Exclude(string key) => state.ExcludedThemeKeys.Add(key);

    /// <summary>Remet un thème exclu dans la roue libre.</summary>
    public bool Restore(string key) => state.ExcludedThemeKeys.Remove(key);

    /// <summary>Thèmes exclus encore existants, dans l'ordre des listes.</summary>
    public List<WheelItem> ExcludedItems()
    {
        var items = new List<WheelItem>();
        foreach (var t in catalog.Themes)
        {
            if (IsExcluded(BuiltinKey(t.Id)))
                items.Add(new WheelItem { Key = BuiltinKey(t.Id), Fr = t.Fr, En = t.En, CategoryId = t.Category });
        }

        foreach (var c in state.CustomThemes)
        {
            if (IsExcluded(CustomKey(c.Id)))
                items.Add(new WheelItem { Key = CustomKey(c.Id), Fr = c.Text, En = c.Text, CategoryId = CustomCategoryId });
        }

        return items;
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static void Trim<T>(List<T> list, int max)
    {
        if (list.Count > max)
            list.RemoveRange(max, list.Count - max);
    }
}
