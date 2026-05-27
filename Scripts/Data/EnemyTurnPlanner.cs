using System.Collections.Generic;
using System.Linq;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

namespace EchoduKarma.Scripts.Data;

/// <summary>
/// Planifie l'action d'un ennemi : défense, sort (bestiaire) ou attaque physique.
/// Le tirage skill / mêlée dépend du profil IA et du contexte (PV, PM).
/// </summary>
public static class EnemyTurnPlanner
{
    const float DefensiveLowHpThreshold = 0.5f;
    const float DefensiveHealThreshold = 0.45f;
    const float DefendChanceDefensive = 0.6f;

    public static WaveActionEntry Plan(Enemy enemy)
    {
        if (enemy?.Stats == null)
            return PhysicalEntry(enemy);

        List<Skill> usable = GetUsableSkills(enemy);
        float hpRatio = GetHpRatio(enemy);

        if (ShouldDefend(enemy, hpRatio))
            return DefendEntry(enemy);

        if (DecideUseSkill(enemy, usable, hpRatio))
        {
            Skill skill = PickSkill(enemy, usable, hpRatio);
            if (skill != null)
                return SkillEntry(enemy, skill);
        }

        return PhysicalEntry(enemy);
    }

    static List<Skill> GetUsableSkills(Enemy enemy) =>
        SkillManager.ResolveByKeys(enemy.Stats.SkillKeys)
            .Where(s => enemy.CurrentMp >= s.Cost)
            .ToList();

    static float GetHpRatio(Enemy enemy) =>
        enemy.Stats.Pv > 0 ? (float)enemy.CurrentPv / enemy.Stats.Pv : 0f;

    static bool ShouldDefend(Enemy enemy, float hpRatio) =>
        enemy.Stats.AiPattern == AiPattern.Defensive
        && hpRatio <= DefensiveLowHpThreshold
        && GD.Randf() < DefendChanceDefensive;

    static List<Skill> HealSupports(List<Skill> supports) =>
        supports.Where(s => SkillSupportEffect.GetKind(s) == SkillSupportEffect.Kind.Heal).ToList();

    static List<Skill> BuffSupports(List<Skill> supports) =>
        supports.Where(s => SkillSupportEffect.GetKind(s) == SkillSupportEffect.Kind.BuffForce).ToList();

    /// <summary>True = utiliser un sort ce tour (sinon attaque physique).</summary>
    static bool DecideUseSkill(Enemy enemy, List<Skill> usable, float hpRatio)
    {
        if (usable.Count == 0)
            return false;

        var attacks = usable.Where(s => s.Type == SkillType.Attack).ToList();
        var supports = usable.Where(s => s.Type == SkillType.Support).ToList();
        var heals = HealSupports(supports);
        var buffs = BuffSupports(supports);

        return enemy.Stats.AiPattern switch
        {
            AiPattern.Aggressive => DecideAggressiveSkill(enemy, attacks, buffs),
            AiPattern.Defensive => DecideDefensiveSkill(hpRatio, attacks, heals, supports),
            _ => DecideNormalSkill(attacks, supports),
        };
    }

    static bool DecideAggressiveSkill(Enemy enemy, List<Skill> attacks, List<Skill> buffs)
    {
        if (attacks.Count == 0)
            return false;

        if (buffs.Count > 0 && attacks.Count > 0 && GD.Randf() < 0.08f)
            return true;

        int cheapestAttackMp = attacks.Min(s => s.Cost);
        if (enemy.CurrentMp < cheapestAttackMp + 2)
            return GD.Randf() < 0.25f;

        return GD.Randf() < 0.7f;
    }

    static bool DecideDefensiveSkill(float hpRatio, List<Skill> attacks, List<Skill> heals, List<Skill> supports)
    {
        if (hpRatio <= DefensiveHealThreshold && heals.Count > 0)
            return true;

        if (hpRatio > DefensiveLowHpThreshold)
        {
            if (attacks.Count == 0)
                return false;

            return GD.Randf() < 0.35f;
        }

        if (attacks.Count > 0 && supports.Count > 0)
            return GD.Randf() < 0.4f;

        return supports.Count > 0 && attacks.Count == 0;
    }

    static bool DecideNormalSkill(List<Skill> attacks, List<Skill> supports)
    {
        if (attacks.Count == 0)
            return supports.Count > 0;

        if (supports.Count == 0)
            return true;

        return GD.Randf() < 0.5f;
    }

    static Skill PickSkill(Enemy enemy, List<Skill> usable, float hpRatio)
    {
        var attacks = usable.Where(s => s.Type == SkillType.Attack).ToList();
        var supports = usable.Where(s => s.Type == SkillType.Support).ToList();
        var heals = HealSupports(supports);
        var buffs = BuffSupports(supports);

        return enemy.Stats.AiPattern switch
        {
            AiPattern.Aggressive => PickAggressiveSkill(attacks, buffs),
            AiPattern.Defensive => PickDefensiveSkill(hpRatio, attacks, heals, supports),
            _ => PickFromList(usable),
        };
    }

    static Skill PickAggressiveSkill(List<Skill> attacks, List<Skill> buffs)
    {
        if (attacks.Count == 0)
            return null;

        if (buffs.Count > 0 && GD.Randf() < 0.08f)
            return PickFromList(buffs);

        return PickFromList(attacks);
    }

    static Skill PickDefensiveSkill(float hpRatio, List<Skill> attacks, List<Skill> heals, List<Skill> supports)
    {
        if (hpRatio <= DefensiveHealThreshold && heals.Count > 0)
            return PickFromList(heals);

        if (hpRatio > DefensiveLowHpThreshold)
            return attacks.Count > 0 ? PickFromList(attacks) : null;

        if (supports.Count > 0 && (attacks.Count == 0 || GD.Randf() < 0.55f))
            return PickFromList(supports);

        return attacks.Count > 0 ? PickFromList(attacks) : PickFromList(supports);
    }

    static Skill PickFromList(List<Skill> skills)
    {
        if (skills == null || skills.Count == 0)
            return null;

        return skills[(int)(GD.Randi() % (uint)skills.Count)];
    }

    static WaveActionEntry DefendEntry(Enemy enemy) => new()
    {
        Battler = enemy,
        Enemy = enemy,
        Kind = WaveActionEntry.ActionKind.EnemyDefend,
        Initiative = CombatInitiative.ForEnemyDefend(enemy),
    };

    static WaveActionEntry SkillEntry(Enemy enemy, Skill skill) => new()
    {
        Battler = enemy,
        Enemy = enemy,
        Kind = ResolveEnemySkillKind(skill),
        Skill = skill,
        Initiative = CombatInitiative.ForEnemySkill(enemy, skill),
    };

    static WaveActionEntry.ActionKind ResolveEnemySkillKind(Skill skill)
    {
        if (skill.Type != SkillType.Support)
            return WaveActionEntry.ActionKind.EnemyMagic;

        return SkillSupportEffect.GetKind(skill) == SkillSupportEffect.Kind.BuffForce
            ? WaveActionEntry.ActionKind.EnemyBuff
            : WaveActionEntry.ActionKind.EnemyHeal;
    }

    static WaveActionEntry PhysicalEntry(Enemy enemy) => new()
    {
        Battler = enemy,
        Enemy = enemy,
        Kind = WaveActionEntry.ActionKind.EnemyAttack,
        Initiative = CombatInitiative.ForEnemyPhysical(enemy),
    };
}
