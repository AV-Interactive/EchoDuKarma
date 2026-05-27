using EchoduKarma.Scripts.Entities.Player;
using Godot;

namespace EchoduKarma.Scripts.Data;

/// <summary>
/// Initiative = Agilité (effective) + bonus d'action (vitesse sort, vitesse attaque ennemi, fuite).
/// </summary>
public static class CombatInitiative
{
    public const int FleeSpeedBonus = 5;

    public static int GetEffectiveAgility(IBattler battler, bool isPlayer, float zoneKarma)
    {
        if (battler == null)
            return 0;

        if (isPlayer)
            return KarmaCombatModifiers.GetEffectiveStat(
                battler.Dexterity, KarmaCombatModifiers.StatKind.Dexterity, zoneKarma);

        return battler.Dexterity;
    }

    public static int ForSkill(IBattler battler, bool isPlayer, float zoneKarma, Skill skill)
    {
        int agi = GetEffectiveAgility(battler, isPlayer, zoneKarma);
        int speed = skill != null ? Mathf.Max(0, skill.Speed) : 0;
        return agi + speed;
    }

    public static int ForPhysical(IBattler battler, bool isPlayer, float zoneKarma) =>
        GetEffectiveAgility(battler, isPlayer, zoneKarma);

    public static int ForDefend(IBattler battler, bool isPlayer, float zoneKarma) =>
        GetEffectiveAgility(battler, isPlayer, zoneKarma);

    public static int ForFlee(IBattler battler, bool isPlayer, float zoneKarma) =>
        GetEffectiveAgility(battler, isPlayer, zoneKarma) + FleeSpeedBonus;

    /// <summary>Initiative sort ennemi : Agi + Vitesse du sort (<c>skills.csv</c>).</summary>
    public static int ForEnemySkill(Enemy enemy, Skill skill) =>
        ForSkill(enemy, isPlayer: false, zoneKarma: 0f, skill);

    /// <summary>Mêlée de secours si aucun sort utilisable.</summary>
    public static int ForEnemyPhysical(Enemy enemy) => enemy?.Dexterity ?? 0;

    public static int ForEnemyDefend(Enemy enemy) => enemy?.Dexterity ?? 0;
}
