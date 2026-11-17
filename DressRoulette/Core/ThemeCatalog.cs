using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DressRoulette.Core;

/// <summary>Langues prises en charge par le plugin.</summary>
public enum Lang
{
    French,
    English,
}

/// <summary>Un texte disponible en français et en anglais.</summary>
public sealed class LocalizedText
{
    [JsonPropertyName("fr")]
    public string Fr { get; set; } = string.Empty;

    [JsonPropertyName("en")]
    public string En { get; set; } = string.Empty;

    public string Get(Lang lang) => lang == Lang.French ? Fr : En;
}

public sealed class CategoryDef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("fr")]
    public string Fr { get; set; } = string.Empty;

    [JsonPropertyName("en")]
    public string En { get; set; } = string.Empty;

    public string Get(Lang lang) => lang == Lang.French ? Fr : En;
}

public sealed class ThemeDef
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("fr")]
    public string Fr { get; set; } = string.Empty;

    [JsonPropertyName("en")]
    public string En { get; set; } = string.Empty;

    public string Get(Lang lang) => lang == Lang.French ? Fr : En;
}

public sealed class ConstraintDef
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("fr")]
    public string Fr { get; set; } = string.Empty;

    [JsonPropertyName("en")]
    public string En { get; set; } = string.Empty;

    /// <summary>Vrai pour « Pas de contrainte bonus aujourd'hui ».</summary>
    [JsonPropertyName("isNone")]
    public bool IsNone { get; set; }

    public string Get(Lang lang) => lang == Lang.French ? Fr : En;
}

/// <summary>
/// Liste des thèmes et contraintes intégrés, lue depuis les fichiers JSON du dossier Data.
/// </summary>
public sealed class ThemeCatalog
{
    private sealed class ThemesFile
    {
        [JsonPropertyName("categories")]
        public List<CategoryDef> Categories { get; set; } = new();

        [JsonPropertyName("themes")]
        public List<ThemeDef> Themes { get; set; } = new();
    }

    private sealed class ConstraintsFile
    {
        [JsonPropertyName("constraints")]
        public List<ConstraintDef> Constraints { get; set; } = new();
    }

    public IReadOnlyList<CategoryDef> Categories { get; }
    public IReadOnlyList<ThemeDef> Themes { get; }
    public IReadOnlyList<ConstraintDef> Constraints { get; }

    /// <summary>Identifiants des thèmes intégrés, dans l'ordre du fichier.</summary>
    public IReadOnlyList<int> ThemeIds { get; }

    /// <summary>Identifiants des contraintes, dans l'ordre du fichier.</summary>
    public IReadOnlyList<int> ConstraintIds { get; }

    private readonly Dictionary<int, ThemeDef> themesById;
    private readonly Dictionary<int, ConstraintDef> constraintsById;
    private readonly Dictionary<string, CategoryDef> categoriesById;

    private ThemeCatalog(List<CategoryDef> categories, List<ThemeDef> themes, List<ConstraintDef> constraints)
    {
        Categories = categories;
        Themes = themes;
        Constraints = constraints;
        ThemeIds = themes.Select(t => t.Id).ToArray();
        ConstraintIds = constraints.Select(c => c.Id).ToArray();

        categoriesById = new Dictionary<string, CategoryDef>();
        foreach (var c in categories)
        {
            if (!categoriesById.TryAdd(c.Id, c))
                throw new InvalidDataException($"Catégorie en double : {c.Id}");
        }

        themesById = new Dictionary<int, ThemeDef>();
        foreach (var t in themes)
        {
            if (!themesById.TryAdd(t.Id, t))
                throw new InvalidDataException($"Identifiant de thème en double : {t.Id}");
            if (!categoriesById.ContainsKey(t.Category))
                throw new InvalidDataException($"Le thème {t.Id} a une catégorie inconnue : {t.Category}");
            if (string.IsNullOrWhiteSpace(t.Fr) || string.IsNullOrWhiteSpace(t.En))
                throw new InvalidDataException($"Le thème {t.Id} n'a pas de texte FR ou EN.");
        }

        constraintsById = new Dictionary<int, ConstraintDef>();
        foreach (var c in constraints)
        {
            if (!constraintsById.TryAdd(c.Id, c))
                throw new InvalidDataException($"Identifiant de contrainte en double : {c.Id}");
            if (string.IsNullOrWhiteSpace(c.Fr) || string.IsNullOrWhiteSpace(c.En))
                throw new InvalidDataException($"La contrainte {c.Id} n'a pas de texte FR ou EN.");
        }

        if (themes.Count == 0 || constraints.Count == 0)
            throw new InvalidDataException("Les listes de thèmes et de contraintes ne doivent pas être vides.");
    }

    public static ThemeCatalog Load(Stream themesJson, Stream constraintsJson)
    {
        var options = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip };
        var themes = JsonSerializer.Deserialize<ThemesFile>(themesJson, options)
                     ?? throw new InvalidDataException("themes.json illisible.");
        var constraints = JsonSerializer.Deserialize<ConstraintsFile>(constraintsJson, options)
                          ?? throw new InvalidDataException("constraints.json illisible.");
        return new ThemeCatalog(themes.Categories, themes.Themes, constraints.Constraints);
    }

    public ThemeDef? GetTheme(int id) => themesById.GetValueOrDefault(id);
    public ConstraintDef? GetConstraint(int id) => constraintsById.GetValueOrDefault(id);
    public CategoryDef? GetCategory(string id) => categoriesById.GetValueOrDefault(id);

    // Le calcul est rapide, mais inutile de le refaire à chaque image : on garde les derniers résultats.
    private readonly Dictionary<long, (ThemeDef Theme, ConstraintDef Constraint)> dailyCache = new();

    /// <summary>Thème du jour (toujours un thème intégré, toutes catégories confondues).</summary>
    public ThemeDef DailyTheme(long day) => Daily(day).Theme;

    /// <summary>Contrainte bonus du jour.</summary>
    public ConstraintDef DailyConstraint(long day) => Daily(day).Constraint;

    private (ThemeDef Theme, ConstraintDef Constraint) Daily(long day)
    {
        if (dailyCache.TryGetValue(day, out var cached))
            return cached;

        if (dailyCache.Count > 64)
            dailyCache.Clear();

        var result = (
            themesById[DailyPicker.PickId(ThemeIds, day, DailyPicker.ThemeSalt)],
            constraintsById[DailyPicker.PickId(ConstraintIds, day, DailyPicker.ConstraintSalt)]);
        dailyCache[day] = result;
        return result;
    }
}
