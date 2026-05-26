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
    public List<Skill> LearnedSkills { get; set; } = new();

    public static PlayerBattleSnapshot FromPlayer(Player player)
    {
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
            LearnedSkills = new List<Skill>(player.LearnedSkills),
        };
    }

    public void ApplyToPlayer(Player player)
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;

        player.CurrentPv = CurrentPv;
        player.CurrentMp = CurrentMp;

        var stats = player.GetNodeOrNull<StatHandler>("PlayerStats");
        if (stats != null)
        {
            stats.CurrentLevel = Level;
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
