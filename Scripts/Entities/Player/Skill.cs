using System.Collections.Generic;
using Godot;

namespace EchoduKarma.Scripts.Entities.Player;

public enum SkillType
{
    Attack,
    Support,
}

public partial class Skill : RefCounted
{
    public string Name { get; set; }
    public SkillType Type { get; set; }
    public List<string> Classes { get; set; }
    public int LevelRequired { get; set; }
    public int Cost { get; set; }
    public int Power { get; set; }
    public string Element { get; set; }
    public int Speed { get; set; }
    public string Description { get; set; }
    public string Effect { get; set; }
    public string TargetType { get; set; }
}