using Godot;
using System.Threading.Tasks;

/// <summary>
/// Représentation visuelle du joueur dans la scène de combat.
/// Texture et matériau configurés directement dans l'éditeur Godot.
/// </summary>
public partial class BattleActor : Node3D
{
    [Export] private Sprite3D _sprite;
    
    const int FRAME_NEUTRAL = 24;
    const int FRAME_PLAYER_ATTACK = 51;
    const int FRAME_ENNEMY_ATTACK = 8;
    
    public override void _Ready()
    {
        if (_sprite == null)
            _sprite = GetNodeOrNull<Sprite3D>("Sprite3D");

        if (_sprite == null)
            GD.PrintErr("[BattleActor] Sprite3D non assigné ! Vérifie l'inspecteur.");
        
        SetFrame(FRAME_NEUTRAL);
    }

    public void PlayHitEffect()
    {
        if (_sprite == null) return;

        var tween = CreateTween();
        tween.TweenProperty(_sprite, "modulate", Colors.Red, 0.05f);
        tween.Parallel().TweenProperty(_sprite, "position:x", _sprite.Position.X + 0.15f, 0.05f);
        tween.TweenProperty(_sprite, "modulate", Colors.White, 0.1f);
        tween.Parallel().TweenProperty(_sprite, "position:x", _sprite.Position.X, 0.1f);
    }

    // TODO: implémenter l'animation d'attaque joueur (bond en avant + retour)
    public Task PlayAttackAnimation()
    {
        return Task.CompletedTask;
    }
    
    public void SetFrame(int frame)
    {
        if (_sprite == null) return;
        _sprite.Frame = frame;
    }

    public void OnCameraChanged(CameraDirector.CameraShot shot)
    {
        switch (shot)
        {
            case CameraDirector.CameraShot.Neutral:
                SetFrame(FRAME_NEUTRAL);
                break;
            case CameraDirector.CameraShot.PlayerAttack:
                SetFrame(FRAME_PLAYER_ATTACK);
                break;
            case CameraDirector.CameraShot.EnemyAttack:
                SetFrame(FRAME_ENNEMY_ATTACK);
                break;
        }
    }
}
