using System;
using System.Collections.Generic;

namespace DressRoulette.Core;

/// <summary>
/// État de la série, enregistré dans la configuration locale du plugin.
/// </summary>
[Serializable]
public class StreakState
{
    /// <summary>Série en cours (jours d'affilée avec un partage du thème du jour).</summary>
    public int Current { get; set; }

    /// <summary>Meilleure série jamais atteinte.</summary>
    public int Best { get; set; }

    /// <summary>Jokers en réserve (0 à <see cref="StreakRules.MaxJokers"/>).</summary>
    public int Jokers { get; set; }

    /// <summary>
    /// Dernier jour « couvert » : un jour de partage, ou un jour raté sauvé par un joker.
    /// Null si aucune série n'est en cours.
    /// </summary>
    public long? LastCoveredDay { get; set; }

    /// <summary>Dernier jour où le joueur a vraiment partagé (sert à afficher « déjà partagé »).</summary>
    public long? LastShareDay { get; set; }

    /// <summary>Nombre de jokers utilisés dont le joueur n'a pas encore vu le message.</summary>
    public int UnseenJokersUsed { get; set; }

    /// <summary>Vrai si un joker vient d'être gagné et que le joueur n'a pas encore vu le message.</summary>
    public bool UnseenJokerEarned { get; set; }

    /// <summary>Derniers jours de partage (pour le calendrier), du plus ancien au plus récent.</summary>
    public List<long> SharedDays { get; set; } = new();

    /// <summary>Derniers jours sauvés par un joker (pour le calendrier), du plus ancien au plus récent.</summary>
    public List<long> SavedDays { get; set; } = new();
}

/// <summary>État d'un jour dans le calendrier de série.</summary>
public enum DayStatus
{
    /// <summary>Thème du jour partagé.</summary>
    Shared,

    /// <summary>Jour raté, sauvé par un joker.</summary>
    Saved,

    /// <summary>Jour raté.</summary>
    Missed,

    /// <summary>Aujourd'hui, pas encore partagé.</summary>
    Pending,
}

/// <summary>Résultat d'un partage du thème du jour.</summary>
public readonly record struct ShareOutcome(bool Counted, int JokersUsed, bool JokerEarned);

/// <summary>
/// Règles de la série et des jokers.
///
/// - Un partage par jour au maximum compte (les suivants sont ignorés).
/// - Un joker est gagné à 10, 20, 30… jours de série (3 au maximum en réserve).
/// - Un jour raté coûte un joker : la série ne baisse pas, mais n'augmente pas ce jour-là.
/// - Pas assez de jokers pour couvrir tous les jours ratés : la série retombe à zéro
///   et les jokers restants disparaissent.
/// - Le calcul des jours ratés se fait au partage suivant, à l'ouverture du plugin
///   et au passage du reset quotidien.
/// </summary>
public static class StreakRules
{
    public const int JokerEvery = 10;
    public const int MaxJokers = 3;

    /// <summary>Nombre de jours gardés pour le calendrier.</summary>
    public const int CalendarDays = 60;

    /// <summary>
    /// Rattrape les jours ratés depuis le dernier jour couvert.
    /// Le jour en cours n'est jamais compté comme raté : le joueur peut encore partager.
    /// </summary>
    /// <returns>Le nombre de jokers utilisés.</returns>
    public static int CatchUp(StreakState state, long today)
    {
        if (state.LastCoveredDay is not long last || state.Current <= 0)
            return 0;

        var missed = today - last - 1;
        if (missed <= 0)
            return 0;

        if (state.Jokers >= missed)
        {
            for (var day = last + 1; day < today; day++)
                Remember(state.SavedDays, day);

            state.Jokers -= (int)missed;
            state.LastCoveredDay = today - 1;
            state.UnseenJokersUsed += (int)missed;
            return (int)missed;
        }

        // Pas assez de jokers : la série est perdue.
        state.Current = 0;
        state.Jokers = 0;
        state.LastCoveredDay = null;
        return 0;
    }

    /// <summary>Enregistre un partage du thème du jour.</summary>
    public static ShareOutcome RegisterShare(StreakState state, long today)
    {
        var used = CatchUp(state, today);

        // Déjà partagé aujourd'hui (ou horloge revenue en arrière) : rien ne change.
        if (state.LastCoveredDay is long last && today <= last)
            return new ShareOutcome(false, used, false);

        state.Current++;
        state.LastCoveredDay = today;
        state.LastShareDay = today;
        Remember(state.SharedDays, today);
        state.Best = Math.Max(state.Best, state.Current);

        var earned = false;
        if (state.Current % JokerEvery == 0 && state.Jokers < MaxJokers)
        {
            state.Jokers++;
            state.UnseenJokerEarned = true;
            earned = true;
        }

        return new ShareOutcome(true, used, earned);
    }

    /// <summary>État d'un jour pour le calendrier de série.</summary>
    public static DayStatus StatusOf(StreakState state, long day, long today)
    {
        if (state.SharedDays.Contains(day))
            return DayStatus.Shared;
        if (state.SavedDays.Contains(day))
            return DayStatus.Saved;
        return day >= today ? DayStatus.Pending : DayStatus.Missed;
    }

    /// <summary>Ajoute un jour à une liste du calendrier en gardant seulement les plus récents.</summary>
    private static void Remember(List<long> days, long day)
    {
        if (days.Contains(day))
            return;

        days.Add(day);
        days.Sort();
        if (days.Count > CalendarDays)
            days.RemoveRange(0, days.Count - CalendarDays);
    }

    /// <summary>Vrai si le joueur a déjà partagé le thème de ce jour.</summary>
    public static bool HasSharedToday(StreakState state, long today)
        => state.LastShareDay == today;
}
