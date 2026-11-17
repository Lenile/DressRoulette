namespace DressRoulette.Core;

/// <summary>
/// Petit générateur pseudo-aléatoire maison (SplitMix64).
///
/// Pourquoi ne pas utiliser System.Random ou string.GetHashCode() ?
/// Leurs résultats peuvent changer d'une version de .NET à l'autre (et GetHashCode change
/// même à chaque lancement). Ici, le calcul est écrit en entier dans le code :
/// la même entrée donne toujours la même sortie, sur toutes les machines, pour toujours.
/// C'est ce qui garantit que tous les joueurs voient le même thème du jour.
/// </summary>
public static class StableRandom
{
    /// <summary>Mélange un nombre 64 bits (fonction de finalisation de SplitMix64).</summary>
    public static ulong Mix(ulong x)
    {
        unchecked
        {
            x += 0x9E3779B97F4A7C15UL;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }
    }

    /// <summary>Combine plusieurs valeurs en une seule clé stable.</summary>
    public static ulong Combine(ulong salt, long a, long b)
    {
        unchecked
        {
            var h = Mix(salt);
            h = Mix(h ^ (ulong)a);
            h = Mix(h ^ (ulong)b);
            return h;
        }
    }
}
