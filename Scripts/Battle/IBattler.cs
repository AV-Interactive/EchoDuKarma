using Godot;

public interface IBattler
{
    string Name { get; }
    Vector2 GlobalPosition { get; }
    int Level { get; }
    int Pv { get; }
    int CurrentPv { get; set; }
    int Mp { get; }
    int Strength { get; }
    int Dexterity { get; }
    int Spirit { get; }
    int Defense { get; }
}
