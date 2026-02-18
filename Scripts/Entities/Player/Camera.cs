using Godot;
using System;

public partial class Camera : Camera2D
{
    public override void _Ready()
    {
        CallDeferred(nameof(SetupCameraLimit));
    }

    void SetupCameraLimit()
    {
        TileMapLayer groundLayer = GetTree().CurrentScene.FindChild("Ground", true, false) as TileMapLayer;

        if (groundLayer != null)
        {
            Rect2I userRect = groundLayer.GetUsedRect();
            int tileSize = groundLayer.TileSet.TileSize.X;
            
            LimitLeft = userRect.Position.X * tileSize + tileSize;
            LimitRight = (userRect.Position.X + userRect.Size.X) * tileSize;
            LimitTop = userRect.Position.Y * tileSize;
            LimitBottom = (userRect.Position.Y + userRect.Size.Y) * tileSize;
            
            GD.Print($"[Camera] Verrouillée sur la map {LimitRight}x{LimitBottom} pixels.");
        }
        else
        {
            GD.PrintErr("[Camera] Impossible de trouver la couche de sol.");
        }
    }
}
