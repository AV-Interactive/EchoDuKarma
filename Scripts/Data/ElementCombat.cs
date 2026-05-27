using System.Collections.Generic;
using Godot;

namespace EchoduKarma.Scripts.Data;

/// <summary>
/// Affinité du lanceur (synergie avec l'élément du sort), cycle sort→cible et cycle affinité lanceur→cible.
/// Fire bat Earth, Earth bat Air, Air bat Water, Water bat Fire.
/// </summary>
public static class ElementCombat
{
    public const float AffinityMatchMultiplier = 1.25f;
    public const float StrongAgainstMultiplier = 1.5f;
    public const float WeakAgainstMultiplier = 0.75f;

    static readonly Dictionary<ElementType, ElementType> StrongAgainst = new()
    {
        { ElementType.Fire, ElementType.Earth },
        { ElementType.Earth, ElementType.Air },
        { ElementType.Air, ElementType.Water },
        { ElementType.Water, ElementType.Fire },
    };

    public static ElementType Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return ElementType.None;

        return raw.Trim().ToLowerInvariant() switch
        {
            "fire" => ElementType.Fire,
            "earth" => ElementType.Earth,
            "air" => ElementType.Air,
            "water" => ElementType.Water,
            _ => ElementType.None,
        };
    }

    public static string ToDisplayName(ElementType element) => element switch
    {
        ElementType.Fire => "Feu",
        ElementType.Earth => "Terre",
        ElementType.Air => "Air",
        ElementType.Water => "Eau",
        _ => "",
    };

    /// <summary>Bonus lorsque l'affinité du lanceur correspond à l'élément de la compétence.</summary>
    public static float GetAffinityPowerMultiplier(ElementType battlerAffinity, string skillElement)
    {
        ElementType skill = Parse(skillElement);
        if (battlerAffinity == ElementType.None || skill == ElementType.None)
            return 1f;

        return skill == battlerAffinity ? AffinityMatchMultiplier : 1f;
    }

    /// <summary>Cycle élémentaire : élément offensif (sort ou affinité) vs affinité de la cible.</summary>
    public static float GetCycleMultiplier(ElementType offensiveElement, ElementType defenderAffinity)
    {
        if (offensiveElement == ElementType.None || defenderAffinity == ElementType.None)
            return 1f;

        if (StrongAgainst.TryGetValue(offensiveElement, out ElementType weakTarget) && weakTarget == defenderAffinity)
            return StrongAgainstMultiplier;

        if (StrongAgainst.TryGetValue(defenderAffinity, out ElementType weakTarget2) && weakTarget2 == offensiveElement)
            return WeakAgainstMultiplier;

        return 1f;
    }

    public static float GetCycleMultiplier(string skillElement, ElementType defenderAffinity) =>
        GetCycleMultiplier(Parse(skillElement), defenderAffinity);

    /// <summary>
    /// Synergie affinité/sort + cycle du sort + cycle de l'affinité du lanceur (joueur ou ennemi).
    /// </summary>
    public static float GetCombinedPowerMultiplier(
        ElementType attackerAffinity,
        string skillElement,
        ElementType defenderAffinity)
    {
        ElementType skill = Parse(skillElement);

        return GetAffinityPowerMultiplier(attackerAffinity, skillElement)
            * GetCycleMultiplier(skill, defenderAffinity)
            * GetCycleMultiplier(attackerAffinity, defenderAffinity);
    }

    /// <summary>Messages de combat (affinité / cycles sort et lanceur).</summary>
    public static IReadOnlyList<string> GetCombatLogLines(
        ElementType attackerAffinity,
        string skillElement,
        ElementType defenderAffinity)
    {
        var lines = new List<string>();
        ElementType skill = Parse(skillElement);

        if (skill == ElementType.None && attackerAffinity == ElementType.None)
            return lines;

        if (skill != ElementType.None
            && attackerAffinity != ElementType.None
            && skill == attackerAffinity)
        {
            lines.Add($"Synergie avec l'affinité {ToDisplayName(attackerAffinity)} !");
        }

        float skillCycle = GetCycleMultiplier(skill, defenderAffinity);
        float affinityCycle = GetCycleMultiplier(attackerAffinity, defenderAffinity);

        if (defenderAffinity == ElementType.None)
            return lines;

        bool sameCycle = skill != ElementType.None
            && attackerAffinity != ElementType.None
            && skill == attackerAffinity
            && Mathf.IsEqualApprox(skillCycle, affinityCycle);

        if (sameCycle && skillCycle != 1f)
        {
            lines.Add(DescribeCycleCombined(skill, defenderAffinity, skillCycle));
            return lines;
        }

        if (skill != ElementType.None && skillCycle != 1f)
            lines.Add(DescribeCycle(skill, defenderAffinity, skillCycle, isSkill: true));

        if (attackerAffinity != ElementType.None && affinityCycle != 1f)
            lines.Add(DescribeCycle(attackerAffinity, defenderAffinity, affinityCycle, isSkill: false));

        return lines;
    }

    static string DescribeCycleCombined(ElementType element, ElementType defender, float multiplier)
    {
        string el = ToDisplayName(element);
        string def = ToDisplayName(defender);
        return multiplier > 1f
            ? $"L'affinité et le sort {el} déferlent sur {def} !"
            : $"L'affinité {el} et le sort du même élément peinent contre {def}…";
    }

    static string DescribeCycle(ElementType offensive, ElementType defender, float multiplier, bool isSkill)
    {
        string off = ToDisplayName(offensive);
        string def = ToDisplayName(defender);
        if (isSkill)
        {
            return multiplier > 1f
                ? $"Le sort {off} est efficace contre {def} !"
                : $"Le sort {off} est peu efficace contre {def}…";
        }

        return multiplier > 1f
            ? $"L'affinité {off} du lanceur domine {def} !"
            : $"L'affinité {off} du lanceur est freinée par {def}…";
    }

    /// <summary>Compatibilité : premier message pertinent ou null.</summary>
    public static string DescribeCombatBonus(
        ElementType attackerAffinity,
        string skillElement,
        ElementType defenderAffinity)
    {
        var lines = GetCombatLogLines(attackerAffinity, skillElement, defenderAffinity);
        return lines.Count > 0 ? lines[^1] : null;
    }
}
