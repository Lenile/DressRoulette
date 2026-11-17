using System;
using Dalamud.Configuration;
using DressRoulette.Core;

namespace DressRoulette;

/// <summary>Choix de langue dans les réglages.</summary>
public enum LanguageSetting
{
    /// <summary>Langue du client du jeu (français → français, sinon anglais).</summary>
    Auto,
    French,
    English,
}

/// <summary>
/// Configuration locale du plugin. Dalamud l'enregistre dans un fichier sur le PC du joueur ;
/// rien n'est envoyé ailleurs.
/// </summary>
[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public LanguageSetting Language { get; set; } = LanguageSetting.Auto;

    /// <summary>Masquer la contrainte bonus du jour.</summary>
    public bool HideDailyConstraint { get; set; }

    /// <summary>Afficher le thème du jour dans le chat à la connexion et au reset (désactivé par défaut).</summary>
    public bool ChatReminder { get; set; }

    /// <summary>Calendrier de série : vrai = 30 derniers jours, faux = 7 derniers jours.</summary>
    public bool CalendarShowMonth { get; set; }

    public StreakState Streak { get; set; } = new();

    public FreeWheelState Wheel { get; set; } = new();

    /// <summary>
    /// Répare une configuration abîmée ou modifiée à la main (listes manquantes, valeurs hors limites),
    /// pour que le plugin ne plante jamais au chargement.
    /// </summary>
    public void Normalize()
    {
        Streak ??= new StreakState();
        Wheel ??= new FreeWheelState();
        Wheel.DisabledCategories ??= new();
        Wheel.CustomThemes ??= new();
        Wheel.History ??= new();
        Wheel.RecentThemeKeys ??= new();
        Wheel.RecentConstraintIds ??= new();
        Wheel.ExcludedThemeKeys ??= new();
        Streak.SharedDays ??= new();
        Streak.SavedDays ??= new();

        Wheel.CustomThemes.RemoveAll(t => t is null || string.IsNullOrWhiteSpace(t.Text));
        Wheel.History.RemoveAll(h => h is null);

        Streak.Current = Math.Max(0, Streak.Current);
        Streak.Best = Math.Max(Streak.Best, Streak.Current);
        Streak.Jokers = Math.Clamp(Streak.Jokers, 0, StreakRules.MaxJokers);
        Streak.UnseenJokersUsed = Math.Max(0, Streak.UnseenJokersUsed);

        if (!Enum.IsDefined(Language))
            Language = LanguageSetting.Auto;
    }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
