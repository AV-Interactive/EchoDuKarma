using Godot;

public partial class Enemy : Node2D, IBattler
{
    [Export] public string EnemyName;
    [Export] public Texture2D SpritePath;
    [Export] public Bestiary StatsHandler;
    
    public EnemyStats Stats { get; private set; }

    public string Name => Stats.EnemyName;
    public int Level => Stats.Level;
    public int Pv => Stats.Pv;
    public int CurrentPv { get; set; }
    public int Mp => Stats.Mp;
    public int Strength => Stats.Strength;
    public int Dexterity => Stats.Dexterity;
    public int Spirit => Stats.Spirit;
    public int Defense => Stats.Defense;
    
    public override void _Ready()
    {
        if (SpritePath != null) GetNode<Sprite2D>("Sprite2D").Texture = SpritePath;
        
        Stats = Bestiary.Instance.GetEnemy(EnemyName);

        if (Stats != null)
        {
            CurrentPv = Stats.Pv;
            GD.Print($"Un {EnemyName} apparait avec {CurrentPv} PV.");
        }
    }
}
