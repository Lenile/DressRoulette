using System;
using System.Collections.Generic;

namespace DressRoulette.Core;

/// <summary>
/// Calcul du thème et de la contrainte du jour, sans serveur.
///
/// 1. On compte les jours écoulés depuis une date fixe, un « jour » commençant à 15 h UTC
///    (le reset quotidien du jeu).
/// 2. Les éléments sont rangés dans un ordre mélangé, différent à chaque « cycle »
///    (un cycle = autant de jours qu'il y a d'éléments). Chaque élément sort donc
///    une fois par cycle, sans doublon.
/// 3. Le mélange ne dépend que des identifiants fixes des éléments et du numéro de cycle,
///    via <see cref="StableRandom"/>. Tous les joueurs qui ont la même liste obtiennent
///    exactement le même résultat.
/// </summary>
public static class DailyPicker
{
    /// <summary>Jour 0 : le 1er janvier 2026 à 15 h UTC. Ne jamais modifier.</summary>
    public static readonly DateTime Epoch = new(2026, 1, 1, 15, 0, 0, DateTimeKind.Utc);

    /// <summary>Graine des thèmes du jour. Ne jamais modifier.</summary>
    public const ulong ThemeSalt = 0x4452_5468_656D_6531UL;

    /// <summary>Graine des contraintes du jour. Ne jamais modifier.</summary>
    public const ulong ConstraintSalt = 0x4452_436F_6E73_7431UL;

    /// <summary>Numéro du jour de jeu pour un instant donné (en UTC).</summary>
    public static long DayNumber(DateTime utcNow)
    {
        if (utcNow.Kind == DateTimeKind.Local)
            utcNow = utcNow.ToUniversalTime();

        var ticks = utcNow.Ticks - Epoch.Ticks;
        return FloorDiv(ticks, TimeSpan.TicksPerDay);
    }

    /// <summary>Début (en UTC) d'un jour de jeu.</summary>
    public static DateTime DayStartUtc(long day)
        => new(Epoch.Ticks + (day * TimeSpan.TicksPerDay), DateTimeKind.Utc);

    /// <summary>
    /// Donne l'identifiant choisi pour un jour.
    /// </summary>
    /// <param name="ids">Identifiants fixes des éléments (thèmes intégrés ou contraintes).</param>
    /// <param name="day">Numéro du jour.</param>
    /// <param name="salt">Graine propre à la liste (thèmes ou contraintes).</param>
    public static int PickId(IReadOnlyList<int> ids, long day, ulong salt)
    {
        if (ids.Count == 0)
            throw new ArgumentException("La liste est vide.", nameof(ids));

        var n = ids.Count;
        var cycle = FloorDiv(day, n);
        var position = (int)(day - (cycle * n));
        return CycleOrder(ids, cycle, salt)[position];
    }

    /// <summary>
    /// Ordre de passage des éléments pendant un cycle.
    /// Si le premier élément d'un cycle est le même que le dernier du cycle précédent,
    /// on échange les deux premiers pour éviter le même élément deux jours de suite.
    /// </summary>
    public static int[] CycleOrder(IReadOnlyList<int> ids, long cycle, ulong salt)
    {
        var order = RawOrder(ids, cycle, salt);

        // Avec 2 éléments ou moins, l'échange n'a pas de sens.
        if (order.Length > 2)
        {
            var previous = RawOrder(ids, cycle - 1, salt);
            if (order[0] == previous[^1])
                (order[0], order[1]) = (order[1], order[0]);
        }

        return order;
    }

    private static int[] RawOrder(IReadOnlyList<int> ids, long cycle, ulong salt)
    {
        var keyed = new (ulong Key, int Id)[ids.Count];
        for (var i = 0; i < ids.Count; i++)
            keyed[i] = (StableRandom.Combine(salt, cycle, ids[i]), ids[i]);

        // Tri par clé, puis par identifiant en cas d'égalité : ordre total, donc stable partout.
        Array.Sort(keyed, static (a, b) =>
        {
            var c = a.Key.CompareTo(b.Key);
            return c != 0 ? c : a.Id.CompareTo(b.Id);
        });

        var result = new int[keyed.Length];
        for (var i = 0; i < keyed.Length; i++)
            result[i] = keyed[i].Id;
        return result;
    }

    /// <summary>Division entière arrondie vers le bas (aussi pour les nombres négatifs).</summary>
    public static long FloorDiv(long a, long b)
    {
        var q = a / b;
        if ((a % b != 0) && ((a < 0) != (b < 0)))
            q--;
        return q;
    }
}
