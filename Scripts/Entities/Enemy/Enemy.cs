using Godot;
using System;

public partial class Enemy : CharacterBody3D, IBattler
{
    [Export] public string EnemyName;
    
    public EnemyStats Stats { get; private set; }

    // Implémentation de IBattler (GlobalPosition est géré par Node3D automatiquement)
    public string Name => EnemyName;
    public int Level => Stats?.Level ?? 1;
    public int Pv => Stats?.Pv ?? 0;
    public int CurrentPv { get; set; }
    public int Mp => Stats?.Mp ?? 0;
    public int CurrentMp { get; set; }
    public int Strength => Stats?.Strength ?? 0;
    public int Dexterity => Stats?.Dexterity ?? 0;
    public int Spirit => Stats?.Spirit ?? 0;
    public int Defense => Stats?.Defense ?? 0;
    
    private Sprite3D _sprite;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite3D>("Node3D/Sprite3D");
        Stats = Bestiary.Instance.GetEnemy(EnemyName);

        if (Stats != null)
        {
            CurrentPv = Stats.Pv;
            LoadTexture();
        }
    }

    private void LoadTexture()
    {
        string path = $"res://Assets/Actors/Enemies/{EnemyName.ToLower()}.png";
        if (FileAccess.FileExists(path))
        {
            _sprite.Texture = GD.Load<Texture2D>(path);
        }
        else
        {
            GD.PrintErr($"[Enemy] Texture manquante : {path}");
        }
    }
    
    public void PlayHitEffect()
    {
        var tween = CreateTween();
        
        // En 3D, "modulate" existe sur Sprite3D, mais attention :
        // Si tu as un matériau d'override, il faut modifier l'albedo du matériau.
        // Ici on reste simple avec le modulate du Sprite3D.
        
        tween.TweenProperty(_sprite, "modulate", Colors.Red, 0.05f);
        
        // Secousse sur l'axe X (3D)
        Vector3 hitPos = _sprite.Position;
        hitPos.X += 0.2f; // Petite valeur car les unités 3D sont grandes !

        tween.Parallel().TweenProperty(_sprite, "position", hitPos, 0.05f);
    
        tween.TweenProperty(_sprite, "modulate", Colors.White, 0.05f);
        tween.Parallel().TweenProperty(_sprite, "position", _sprite.Position, 0.05f);
    }

    public async void PlayAttackAnimation()
    {
        var tween = CreateTween();
        Vector3 originalPos = Position;
        Vector3 targetPos = originalPos + new Vector3(0, 0, -1); // Bond en avant vers le joueur (Z négatif en 3D standard Godot si le joueur est devant)

        // On avance
        tween.TweenProperty(this, "position", targetPos, 0.2f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        
        // On revient
        tween.TweenProperty(this, "position", originalPos, 0.2f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        
        await ToSignal(tween, "finished");
    }
    
    public void LookAtTarget(Vector3 targetPosition)
    {
        var node3D = GetNodeOrNull<Node3D>("Node3D/Sprite3D");
        if (node3D == null) return;

        // On regarde la cible mais on verrouille l'axe Y pour rester vertical
        Vector3 dir = (targetPosition - node3D.GlobalPosition).Normalized();
        dir.Y = 0; // on ignore la hauteur
    
        if (dir != Vector3.Zero)
        {
            node3D.LookAt(node3D.GlobalPosition + dir, Vector3.Up);
            node3D.RotateObjectLocal(Vector3.Up, Mathf.Pi); // 180° car Sprite3D face arrière
        }
    }
}