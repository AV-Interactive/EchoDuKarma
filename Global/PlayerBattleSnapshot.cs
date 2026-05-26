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
            CurrentExperience = statHandler?.CurrentExperience ?? 0,
            LearnedSkills = new List<Skill>(player.LearnedSkills),
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

        return levelsGained;
    }

    bool TryLevelUp(IReadOnlyDictionary<int, Stats> progressionByLevel)
    {
        int nextLevel = Level + 1;
        if (!progressionByLevel.TryGetValue(nextLevel, out Stats next))
            return false;

        if (CurrentExperience < next.XPForNextLevel)
            return false;

        Level = nextLevel;
        Pv = next.Pv;
        Mp = next.Mp;
        Strength = next.Strength;
        Dexterity = next.Dexterity;
        Spirit = next.Spirit;
        Defense = next.Defense;
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
            stats.PvMax = Pv;
            stats.MpMax = Mp;
            stats.Strength = Strength;
            stats.Dexterity = Dexterity;
            stats.Spirit = Spirit;
            stats.Defense = Defense;
        }
    }
}
