using System;
using Godot;

namespace EchoduKarma.Scripts.Data;

public enum KarmaEconomyBand
{
    Chaos,
    Instability,
    Balance,
    StableOrder,
    Utopia,
}

/// <summary>
/// Tarification boutique alignée sur le GDD karma (_GDD/GDD_systeme_karma.md).
/// </summary>
public static class ShopPricing
{
    public const float UtopiaThreshold = 70f;
    public const float StableOrderThreshold = 30f;
    public const float BalanceMinThreshold = -20f;
    public const float InstabilityThreshold = -30f;
    public const float ChaosThreshold = -70f;

    public static KarmaEconomyBand GetBand(float zoneKarma)
    {
        float karma = KarmaManager.Clamp(zoneKarma);
        if (karma >= UtopiaThreshold) return KarmaEconomyBand.Utopia;
        if (karma >= StableOrderThreshold) return KarmaEconomyBand.StableOrder;
        if (karma >= BalanceMinThreshold) return KarmaEconomyBand.Balance;
        if (karma >= ChaosThreshold) return KarmaEconomyBand.Instability;
        return KarmaEconomyBand.Chaos;
    }

    public static float GetBuyMultiplier(float zoneKarma) => GetBand(zoneKarma) switch
    {
        KarmaEconomyBand.Utopia => 1.05f,
        KarmaEconomyBand.StableOrder => 0.9f,
        KarmaEconomyBand.Balance => 1f,
        KarmaEconomyBand.Instability => 1.1f,
        KarmaEconomyBand.Chaos => 1.25f,
        _ => 1f,
    };

    public static float GetSellRatio(float zoneKarma) => GetBand(zoneKarma) switch
    {
        KarmaEconomyBand.Utopia => 0.35f,
        KarmaEconomyBand.StableOrder => 0.55f,
        KarmaEconomyBand.Balance => 0.5f,
        KarmaEconomyBand.Instability => 0.45f,
        KarmaEconomyBand.Chaos => 0f,
        _ => 0.5f,
    };

    public static int GetBuyPrice(int basePrice, float zoneKarma)
    {
        if (basePrice <= 0)
            return 0;

        float multiplier = GetBuyMultiplier(zoneKarma);
        return Math.Max(1, (int)Math.Round(basePrice * multiplier));
    }

    public static int GetSellPrice(int basePrice, float zoneKarma)
    {
        if (basePrice <= 0)
            return 0;

        float ratio = GetSellRatio(zoneKarma);
        if (ratio <= 0f)
            return 0;

        return Math.Max(1, (int)Math.Round(basePrice * ratio));
    }

    public static int GetMaxBuyCountPerVisit(float zoneKarma) =>
        GetBand(zoneKarma) == KarmaEconomyBand.Utopia ? 1 : int.MaxValue;

    public static int GetMaxCatalogItemsShown(float zoneKarma, int totalItems)
    {
        if (totalItems <= 0)
            return 0;

        return GetBand(zoneKarma) == KarmaEconomyBand.Utopia
            ? Math.Min(2, totalItems)
            : totalItems;
    }

    public static bool CanMerchantBuyFromPlayer(float zoneKarma) =>
        GetBand(zoneKarma) != KarmaEconomyBand.Chaos;

    public static bool CanPurchaseMore(float zoneKarma, int purchasesThisVisit) =>
        purchasesThisVisit < GetMaxBuyCountPerVisit(zoneKarma);

    public static string GetEconomyHint(float zoneKarma, int purchasesThisVisit)
    {
        string state = KarmaManager.GetStateLabel(zoneKarma);
        var band = GetBand(zoneKarma);

        string buyHint = band switch
        {
            KarmaEconomyBand.Utopia =>
                "Apathie marchande — 1 achat max, stock réduit (2 objets visibles).",
            KarmaEconomyBand.StableOrder => "Ordre stable — achats à −10 %.",
            KarmaEconomyBand.Balance => "Équilibre — tarifs standards.",
            KarmaEconomyBand.Instability => "Instabilité — achats à +10 %.",
            KarmaEconomyBand.Chaos => "Chaos — achats à +25 %, le marchand ne rachète rien.",
            _ => "Tarifs standards.",
        };

        string sellHint = CanMerchantBuyFromPlayer(zoneKarma)
            ? $"Revente à {Math.Round(GetSellRatio(zoneKarma) * 100f)} % de la valeur catalogue."
            : "Revente impossible dans cette zone.";

        if (band == KarmaEconomyBand.Utopia && purchasesThisVisit >= GetMaxBuyCountPerVisit(zoneKarma))
            buyHint += " Limite d'achat atteinte pour cette visite.";

        return $"[{state}] {buyHint} {sellHint}";
    }
}
