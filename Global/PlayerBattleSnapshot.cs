using System.Collections.Generic;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

namespace EchoduKarma.Scripts.Data;

/// <summary>
/// Copie des stats joueur pour le combat lorsque la scène monde (et le nœud Player) est déchargée.
/// </summary>
public class PlayerBattleSnapshot : IBattler
{
    public string Name { get; set; } = "Player";
    public Vector3 GlobalPosition { get; set; }
    public int Level { get; set; }
    public int Pv { get; set; }
    public int CurrentPv { get; set; }
    public int Mp { get; set; }
    public int CurrentMp { get; set; }
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Spirit { get; set; }
    public int Defense { get; set; }
    public ElementType Affinity { get; set; }
    public int CurrentExperience { get; set; }
    public List<Skill> LearnedSkills { get; set; } = new();

    public static PlayerBattleSnapshot FromPlayer(Player player)
    {
        var statHandler = player.GetNodeOrNull<StatHandler>("PlayerStats");

        return new PlayerBattleSnapshot
        {
            Name = player.Name,
            GlobalPosition = player.GlobalPosition,
            Level = player.Level,
            Pv = player.Pv,
            CurrentPv = player.CurrentPv,
            Mp = player.Mp,
            CurrentMp = player.CurrentMp,
            Strength = player.Strength,
            Dexterity = player.Dexterity,
            Spirit = player.Spirit,
            Defense = player.Defense,
            Affinity = player.Affinity != ElementType.None
                ? player.Affinity
                : HeroManager.GetDefaultHero()?.Affinity ?? ElementType.None,
            CurrentExperience = statHandler?.CurrentExperience ?? 0,
            LearnedSkills = SkillManager.GetUnlockedForClass(
                HeroManager.GetDefaultHero()?.ClassName ?? string.Empty,
                player.Level),
        };
    }

    /// <summary>
    /// Ajoute de l'XP et applique les montées de niveau selon la table de progression.
    /// </summary>
    public int AddExperience(int amount, IReadOnlyDictionary<int, Stats> progressionByLevel)
    {
        if (amount <= 0 || progressionByLevel == null) return 0;

        CurrentExperience += amount;
        int levelsGained = 0;

        while (TryLevelUp(progressionByLevel))
            levelsGained++;

        if (levelsGained > 0)
            SyncLearnedSkillsWithLevel();

        return levelsGained;
    }

    void SyncLearnedSkillsWithLevel()
    {
        var hero = HeroManager.GetDefaultHero();
        if (hero == null)
            return;

        LearnedSkills = SkillManager.GetUnlockedForClass(hero.ClassName, Level);
    }

    bool TryLevelUp(IReadOnlyDictionary<int, Stats> progressionByLevel)
    {
        int nextLevel = Level + 1;
        if (!progressionByLevel.TryGetValue(nextLevel, out Stats next))
            return false;

        if (CurrentExperience < next.XPForNextLevel)
            return false;

        var bonus = InventoryManager.Instance?.GetEquipmentBonuses() ?? EquipmentStatBonuses.Zero;

        Level = nextLevel;
        Pv = next.Pv;
        Mp = next.Mp;
        Strength = next.Strength + bonus.Strength;
        Dexterity = next.Dexterity + bonus.Dexterity;
        Spirit = next.Spirit + bonus.Spirit;
        Defense = next.Defense + bonus.Defense;
        CurrentPv = Pv;
        CurrentMp = Mp;
        return true;
    }

    public void ApplyToPlayer(Player player)
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;

        player.GlobalPosition = GlobalPosition;
        player.CurrentPv = CurrentPv;
        player.CurrentMp = CurrentMp;

        var stats = player.GetNodeOrNull<StatHandler>("PlayerStats");
        if (stats != null)
        {
            stats.CurrentLevel = Level;
            stats.CurrentExperience = CurrentExperience;
            stats.CurrentPv = CurrentPv;
            stats.CurrentMp = CurrentMp;

            Stats row = stats.GetStatsForLevel(Level);
            if (row != null)
            {
                stats.PvMax = row.Pv;
                stats.MpMax = row.Mp;
                stats.Strength = row.Strength;
                stats.Dexterity = row.Dexterity;
                stats.Spirit = row.Spirit;
                stats.Defense = row.Defense;
            }
            else
            {
                stats.PvMax = Pv;
                stats.MpMax = Mp;
            }
        }
    }
}
