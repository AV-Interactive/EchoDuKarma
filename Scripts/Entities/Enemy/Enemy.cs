using Godot;

public partial class Enemy : Node2D, IBattler
{
    [Export] public string EnemyName;
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
        Stats = Bestiary.Instance.GetEnemy(EnemyName);

        if (Stats != null)
        {
            CurrentPv = Stats.Pv;
            
            string path = $"res://Assets/Actors/Enemies/{EnemyName.ToLower()}.png";
            
            if (FileAccess.FileExists(path))
            {
                var text = GD.Load<Texture2D>(path);
                GetNode<Sprite2D>("Sprite2D").Texture = text;
            }
            else
            {
                GD.PrintErr("Pas de sprite pour l'enemi");
            }
        }
        
        GD.Print($"Un {EnemyName} apparait avec {CurrentPv} PV.");
    }
    
    public void PlayHitEffect()
    {
        var sprite = GetNode<Sprite2D>("Sprite2D");
        var tween = CreateTween();
        
        // 1. FLASH ROUGE : On change la couleur (modulate) et on revient au blanc
        tween.TweenProperty(sprite, "modulate", Colors.Red, 0.05f);
    
        // 2. SECOUSSE : On décale un peu à droite pendant le flash
        tween.Parallel().TweenProperty(sprite, "position:x", sprite.Position.X + 8, 0.05f);
    
        // 3. RETOUR : On remet la couleur normale et la position d'origine
        tween.TweenProperty(sprite, "modulate", Colors.White, 0.05f);
        tween.Parallel().TweenProperty(sprite, "position:x", sprite.Position.X, 0.05f);
    }
}
