namespace DressRoulette.Core;

/// <summary>
/// Textes copiés dans le presse-papiers par les boutons de partage.
/// Ils restent lisibles ailleurs que sur Discord (réseaux sociaux, chat de CL…).
/// Le hashtag est le même dans toutes les langues.
/// </summary>
public static class ShareText
{
    public const string Hashtag = "#DressRoulette";

    /// <summary>Partage du thème du jour, avec la série.</summary>
    /// <param name="constraint">Contrainte bonus à ajouter, ou null.</param>
    public static string Daily(Lang lang, string theme, string? constraint, int streak)
    {
        var themePart = WithConstraint(theme, constraint);
        return lang == Lang.French
            ? $"Thème du jour : {themePart} · Série : {streak} {(streak > 1 ? "jours" : "jour")} 🔥 {Hashtag}"
            : $"Today's theme: {themePart} · Streak: {streak} {(streak == 1 ? "day" : "days")} 🔥 {Hashtag}";
    }

    /// <summary>Partage d'un tirage de la roue libre (sans série).</summary>
    public static string FreeDraw(Lang lang, string theme, string? constraint)
    {
        var themePart = WithConstraint(theme, constraint);
        return lang == Lang.French
            ? $"Ma tenue DressRoulette : {themePart} {Hashtag}"
            : $"My DressRoulette outfit: {themePart} {Hashtag}";
    }

    private static string WithConstraint(string theme, string? constraint)
        => string.IsNullOrWhiteSpace(constraint) ? theme : $"{theme} (+ {constraint})";
}
