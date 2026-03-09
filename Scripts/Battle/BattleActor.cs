using Godot;

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

    public async void PlayAttackAnimation()
    {
        if (_sprite == null) return;

        var tween = CreateTween();
        Vector3 originalPos = Position;
        Vector3 forward = originalPos + new Vector3(-1f, 0, 0);

        tween.TweenProperty(this, "position", forward, 0.15f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "position", originalPos, 0.15f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);

        await ToSignal(tween, Tween.SignalName.Finished);
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
