using System;
using EchoduKarma.Scripts.Entities.Player;

namespace EchoduKarma.Scripts.Data;

/// <summary>Interprète la colonne « Effet special » des sorts Support (skills.csv).</summary>
public static class SkillSupportEffect
{
    public enum Kind
    {
        Heal,
        BuffForce,
    }

    public static Kind GetKind(Skill skill)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.Effect))
            return Kind.Heal;

        if (skill.Effect.StartsWith("BuffForce", StringComparison.OrdinalIgnoreCase))
            return Kind.BuffForce;

        return Kind.Heal;
    }

    /// <summary>Durée aléatoire pour BuffForce (ex. <c>BuffForce:2-5</c>).</summary>
    public static int RollDuration(Skill skill, int defaultMin = 2, int defaultMax = 5)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.Effect))
            return Godot.GD.RandRange(defaultMin, defaultMax);

        string payload = skill.Effect;
        int colon = payload.IndexOf(':');
        if (colon < 0)
            return Godot.GD.RandRange(defaultMin, defaultMax);

        string range = payload[(colon + 1)..].Trim();
        int dash = range.IndexOf('-');
        if (dash < 0)
        {
            if (int.TryParse(range, out int single))
                return single;

            return Godot.GD.RandRange(defaultMin, defaultMax);
        }

        if (int.TryParse(range[..dash].Trim(), out int min)
            && int.TryParse(range[(dash + 1)..].Trim(), out int max))
        {
            if (min > max)
                (min, max) = (max, min);

            return Godot.GD.RandRange(min, max);
        }

        return Godot.GD.RandRange(defaultMin, defaultMax);
    }
}
