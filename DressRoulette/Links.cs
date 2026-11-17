namespace DressRoulette;

/// <summary>Liens externes du plugin. Ils ne s'ouvrent que sur un clic du joueur.</summary>
public static class Links
{
    /// <summary>
    /// Invitation Discord qui mène directement au salon de partage DressRoulette.
    /// Une invitation (et non un lien de salon) permet aux joueurs qui ne sont pas encore
    /// sur le serveur de le rejoindre.
    /// </summary>
    public const string ShareChannelUrl = "https://discord.gg/6NshHb9QzK";

    /// <summary>Si le lien est vidé, le bouton copie seulement le texte, sans ouvrir de page.</summary>
    public static bool IsShareChannelConfigured => ShareChannelUrl.Length > 0;
}
