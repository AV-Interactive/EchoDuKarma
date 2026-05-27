using Godot;
using EchoduKarma.Scripts.Entities.Common;

public partial class NpcVisuals : Sprite3D
{
    [Export] public string SpritesBasePath = "";
    [Export] public bool FaceCamera = true;
    [Export] public string DefaultDirection = "DOWN";
    [Export] AnimationPlayer _animPlayer;

    public override void _Ready()
    {
        if (string.IsNullOrEmpty(SpritesBasePath))
            return;

        Hframes = LpcSpriteLayout.HFrames;
        Vframes = LpcSpriteLayout.VFrames;
        TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;

        string direction = FaceCamera
            ? LpcSpriteLayout.CameraFacingDirection
            : DefaultDirection;

        var idleTexture = GD.Load<Texture2D>(SpritesBasePath + "idle.png");
        Texture = idleTexture;
        Frame = LpcSpriteLayout.RowFrame(LpcSpriteLayout.DirectionRow(direction), 0);

        string idleAnim = "IDLE_" + direction;
        if (_animPlayer != null && _animPlayer.HasAnimation(idleAnim))
            _animPlayer.Play(idleAnim);
    }
}
