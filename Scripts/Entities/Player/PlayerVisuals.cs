using Godot;
using EchoduKarma.Scripts.Entities.Player;

public partial class PlayerVisuals : Sprite3D
{
    [Export] AnimationPlayer _animPlayer;
    [Export] public bool usePixelPerfectAlignement = true;

    Texture2D _walkTexture;
    Texture2D _idleTexture;
    Camera3D _cam;
    string _lastDirection = "DOWN";

    public override void _Ready()
    {
        _cam = GetViewport().GetCamera3D();
        _walkTexture = GD.Load<Texture2D>(LpcSprites.Walk);
        _idleTexture = GD.Load<Texture2D>(LpcSprites.Idle);
        Texture = _idleTexture;
        Hframes = LpcSprites.HFrames;
        Vframes = 4;
        Frame = LpcSprites.RowFrame(LpcSprites.DirectionRow(_lastDirection), 0);

        if (_animPlayer.HasAnimation("IDLE_DOWN"))
            _animPlayer.Play("IDLE_DOWN");
    }

    public override void _Process(double delta)
    {
        if (!usePixelPerfectAlignement)
            return;

        Vector3 globalPosition = GlobalPosition;
        globalPosition.X = Mathf.Round(globalPosition.X * 16) / 16;
        globalPosition.Y = Mathf.Round(globalPosition.Y * 16) / 16;
    }

    public void UpdateFrame(Vector3 velocity)
    {
        if (_animPlayer == null)
            return;

        if (velocity.Length() < 0.1f)
        {
            PlayDirectionalAnimation(_idleTexture, "IDLE_" + _lastDirection);
            return;
        }

        Vector3 camForward = -_cam.GlobalTransform.Basis.Z;
        camForward.Y = 0;
        camForward = camForward.Normalized();

        Vector3 camRight = _cam.GlobalTransform.Basis.X;
        camRight.Y = 0;
        camRight = camRight.Normalized();

        Vector3 moveDir = velocity.Normalized();
        float forwardDot = moveDir.Dot(camForward);
        float rightDot = moveDir.Dot(camRight);

        string detectedDir;
        if (Mathf.Abs(forwardDot) > Mathf.Abs(rightDot))
            detectedDir = forwardDot > 0 ? "UP" : "DOWN";
        else
            detectedDir = rightDot > 0 ? "RIGHT" : "LEFT";

        _lastDirection = LpcSprites.ToSpriteDirection(detectedDir);
        FlipH = false;
        PlayDirectionalAnimation(_walkTexture, "WALK_" + _lastDirection);
    }

    void PlayDirectionalAnimation(Texture2D texture, string animationName)
    {
        if (Texture != texture)
            Texture = texture;

        if (_animPlayer.HasAnimation(animationName) && _animPlayer.CurrentAnimation != animationName)
            _animPlayer.Play(animationName);
    }
}
