using System;
using Godot;

/// <summary>
/// Modificateurs de combat issus du GDD karma (stats héros, dégâts subis, soins).
/// </summary>
public static class KarmaCombatModifiers
{
    public enum StatKind
    {
        Force,
        Spirit,
        Dexterity,
        Defense,
    }

    public readonly struct CombatBonuses
    {
        public int Karma { get; init; }
        public float DamageTakenMultiplier { get; init; }
        public float HealMultiplier { get; init; }
        public string StateLabel { get; init; }
    }

    public static CombatBonuses GetCombatBonuses(int karma)
    {
        karma = KarmaManager.Clamp(karma);
        return new CombatBonuses
        {
            Karma = karma,
            DamageTakenMultiplier = GetDamageTakenMultiplier(karma),
            HealMultiplier = GetHealMultiplier(karma),
            StateLabel = KarmaManager.GetStateLabel(karma),
        };
    }

    /// <summary>StatFinale = StatBase + (StatBase × modificateur).</summary>
    public static int GetEffectiveStat(int baseStat, StatKind kind, int karma)
    {
        if (baseStat <= 0)
            return baseStat;

        float modifier = kind switch
        {
            StatKind.Force => GetForceModifier(karma),
            StatKind.Spirit => GetSpiritModifier(karma),
            StatKind.Dexterity => GetAgilityDefenseModifier(karma),
            StatKind.Defense => GetAgilityDefenseModifier(karma),
            _ => 0f,
        };

        return Mathf.Max(1, Mathf.RoundToInt(baseStat + baseStat * modifier));
    }

    public static int ApplyDamageTaken(int rawDamage, int karma)
    {
        if (rawDamage <= 0)
            return rawDamage;

        float scaled = rawDamage * GetDamageTakenMultiplier(karma);
        return Math.Max(1, Mathf.RoundToInt(scaled));
    }

    public static int ApplyHealAmount(int rawHeal, int karma)
    {
        if (rawHeal <= 0)
            return 0;

        float multiplier = GetHealMultiplier(karma);
        if (multiplier <= 0f)
            return 0;

        return Mathf.Max(0, Mathf.RoundToInt(rawHeal * multiplier));
    }

    /// <summary>Force : +0,25 à -100, -0,2 à +70.</summary>
    public static float GetForceModifier(int karma)
    {
        karma = KarmaManager.Clamp(karma);
        if (karma <= 0)
            return Mathf.Lerp(0f, 0.25f, -karma / 100f);

        return Mathf.Lerp(0f, -0.2f, karma / 70f);
    }

    /// <summary>Esprit : -0,2 à -100, +0,25 à +70.</summary>
    public static float GetSpiritModifier(int karma)
    {
        karma = KarmaManager.Clamp(karma);
        if (karma <= 0)
            return Mathf.Lerp(0f, -0.2f, -karma / 100f);

        return Mathf.Lerp(0f, 0.25f, karma / 70f);
    }

    /// <summary>Agi / Def : +0,25 atteint vers -65 (GDD).</summary>
    public static float GetAgilityDefenseModifier(int karma)
    {
        karma = KarmaManager.Clamp(karma);
        if (karma <= 0)
            return Mathf.Lerp(0f, 0.25f, -karma / 65f);

        return Mathf.Lerp(0f, -0.2f, karma / 70f);
    }

    public static float GetDamageTakenMultiplier(int karma)
    {
        karma = KarmaManager.Clamp(karma);
        if (karma >= 70) return 0.8f;
        if (karma >= 30) return 0.95f;
        if (karma >= -20) return 1f;
        if (karma >= -69) return 1.1f;
        return 1.25f;
    }

    public static float GetHealMultiplier(int karma)
    {
        karma = KarmaManager.Clamp(karma);
        if (karma >= 70) return 1.2f;
        if (karma <= -69) return 0f;
        return 1f;
    }
}
