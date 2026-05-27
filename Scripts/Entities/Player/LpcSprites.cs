using EchoduKarma.Scripts.Entities.Common;

namespace EchoduKarma.Scripts.Entities.Player;

public static class LpcSprites
{
    public const string MagusBasePath = "res://Assets/Actors/Heroes/magus/standard/";

    public const int HFrames = LpcSpriteLayout.HFrames;
    public const int FrameSize = LpcSpriteLayout.FrameSize;

    public const string Walk = MagusBasePath + "walk.png";
    public const string Idle = MagusBasePath + "idle.png";
    public const string Run = MagusBasePath + "run.png";
    public const string Combat = MagusBasePath + "combat.png";
    public const string Thrust = MagusBasePath + "thrust.png";
    public const string Spellcast = MagusBasePath + "spellcast.png";
    public const string Hurt = MagusBasePath + "hurt.png";

    public const int ThrustFrameCount = 8;
    public const int SpellcastFrameCount = 7;

    public static int RowFrame(int row, int col) => LpcSpriteLayout.RowFrame(row, col);
    public static int DirectionRow(string direction) => LpcSpriteLayout.DirectionRow(direction);
    public static int WalkPoseFrame(string direction, int column = 0) =>
        RowFrame(DirectionRow(direction), column);
    public static string ToSpriteDirection(string detectedDirection) =>
        LpcSpriteLayout.ToSpriteDirection(detectedDirection);
}
