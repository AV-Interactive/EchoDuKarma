using Godot;
using System.Threading.Tasks;
using EchoduKarma.Scripts.Entities.Player;

/// <summary>
/// Représentation visuelle du joueur dans la scène de combat.
/// </summary>
public partial class BattleActor : Node3D
{
    const float AttackFrameDuration = 0.07f;
    const float SpellcastFrameDuration = 0.09f;

    [Export] private Sprite3D _sprite;

    Texture2D _walkTexture;
    Texture2D _thrustTexture;
    Texture2D _spellcastTexture;
    Texture2D _hurtTexture;

    public override void _Ready()
    {
        if (_sprite == null)
            _sprite = GetNodeOrNull<Sprite3D>("Sprite3D");

        if (_sprite == null)
        {
            GD.PrintErr("[BattleActor] Sprite3D non assigné ! Vérifie l'inspecteur.");
            return;
        }

        _walkTexture = GD.Load<Texture2D>(LpcSprites.Walk);
        _thrustTexture = GD.Load<Texture2D>(LpcSprites.Thrust);
        _spellcastTexture = GD.Load<Texture2D>(LpcSprites.Spellcast);
        _hurtTexture = GD.Load<Texture2D>(LpcSprites.Hurt);

        _sprite.Hframes = LpcSprites.HFrames;
        ShowCombatStance();
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

    public Task PlayAttackAnimation() =>
        PlayDirectionalFrames(_thrustTexture, LpcSprites.ThrustFrameCount, AttackFrameDuration);

    public Task PlaySpellcastAnimation() =>
        PlayDirectionalFrames(_spellcastTexture, LpcSprites.SpellcastFrameCount, SpellcastFrameDuration);

    async Task PlayDirectionalFrames(Texture2D texture, int frameCount, float frameDuration)
    {
        if (_sprite == null || texture == null)
            return;

        int row = LpcSprites.DirectionRow("LEFT");
        SetSpriteSheet(texture, 4, LpcSprites.RowFrame(row, 0));

        for (int col = 0; col < frameCount; col++)
        {
            _sprite.Frame = LpcSprites.RowFrame(row, col);
            await ToSignal(GetTree().CreateTimer(frameDuration), SceneTreeTimer.SignalName.Timeout);
        }

        ShowCombatStance();
    }

    public void OnCameraChanged(CameraDirector.CameraShot shot)
    {
        switch (shot)
        {
            case CameraDirector.CameraShot.Neutral:
                ShowCombatStance();
                break;
            case CameraDirector.CameraShot.PlayerAttack:
                break;
            case CameraDirector.CameraShot.PlayerMagic:
                ShowSpellcastPose();
                break;
            case CameraDirector.CameraShot.EnemyAttack:
                ShowHurtPose();
                break;
        }
    }

    void ShowCombatStance()
    {
        SetSpriteSheet(_walkTexture, 4, LpcSprites.WalkPoseFrame("LEFT"));
    }

    void ShowSpellcastPose()
    {
        SetSpriteSheet(_spellcastTexture, 4, LpcSprites.RowFrame(0, 3));
    }

    void ShowHurtPose()
    {
        SetSpriteSheet(_hurtTexture, 1, 0);
    }

    void SetSpriteSheet(Texture2D texture, int vframes, int frame)
    {
        if (_sprite == null)
            return;

        _sprite.Texture = texture;
        _sprite.Hframes = LpcSprites.HFrames;
        _sprite.Vframes = vframes;
        _sprite.Frame = frame;
    }
}
