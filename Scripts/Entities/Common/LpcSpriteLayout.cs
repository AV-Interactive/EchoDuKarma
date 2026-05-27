namespace EchoduKarma.Scripts.Entities.Common;

/// <summary>
/// Grille LPC standard : 64×64, 13 colonnes × 4 directions (DOWN, LEFT, RIGHT, UP).
/// </summary>
public static class LpcSpriteLayout
{
    public const int HFrames = 13;
    public const int VFrames = 4;
    public const int FrameSize = 64;

    public static int RowFrame(int row, int col) => row * HFrames + col;

    public static int DirectionRow(string direction) => direction switch
    {
        "DOWN" => 0,
        "LEFT" => 1,
        "RIGHT" => 2,
        "UP" => 3,
        _ => 0
    };

    /// <summary>
    /// Direction LPC pour un PNJ statique face à la caméra isométrique (rotation Y 45°).
    /// Correspond à ToSpriteDirection("DOWN") pour un déplacement vers la caméra.
    /// </summary>
    public const string CameraFacingDirection = "RIGHT";

    /// <summary>
    /// Corrige le décalage de 90° entre l'axe caméra isométrique et les lignes LPC.
    /// </summary>
    public static string ToSpriteDirection(string detectedDirection) => detectedDirection switch
    {
        "UP" => "DOWN",
        "RIGHT" => "UP",
        "DOWN" => "RIGHT",
        "LEFT" => "LEFT",
        _ => "DOWN"
    };
}
