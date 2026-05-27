using System.Collections.Generic;
using EchoduKarma.Scripts.Entities.Common;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

namespace EchoduKarma.Scripts.UI;

/// <summary>Miniatures 2D (idle LPC joueur, sprite ennemi) pour le panneau d'initiative.</summary>
public static class CombatantPortrait
{
    const string PlayerCacheKey = "__player__";
    static readonly Dictionary<string, Texture2D> Cache = new();

    public static Texture2D GetPlayerPortrait() =>
        GetOrCreate(PlayerCacheKey, CreatePlayerIdlePortrait);

    public static Texture2D GetEnemyPortrait(string enemyName)
    {
        if (string.IsNullOrWhiteSpace(enemyName))
            return null;

        string key = enemyName.Trim().ToLowerInvariant();
        return GetOrCreate(key, () => LoadEnemyPortrait(key));
    }

    static Texture2D GetOrCreate(string key, System.Func<Texture2D> factory)
    {
        if (Cache.TryGetValue(key, out Texture2D cached) && cached != null)
            return cached;

        Texture2D created = factory();
        if (created != null)
            Cache[key] = created;
        return created;
    }

    static Texture2D CreatePlayerIdlePortrait() =>
        CreateAtlasFrame(GD.Load<Texture2D>(LpcSprites.Idle), LpcSpriteLayout.CameraFacingDirection, 0);

    static Texture2D LoadEnemyPortrait(string enemyKey)
    {
        string path = $"res://Assets/Actors/Enemies/{enemyKey}.png";
        if (!FileAccess.FileExists(path))
            return null;

        var sheet = GD.Load<Texture2D>(path);
        if (sheet == null)
            return null;

        if (sheet.GetWidth() >= LpcSpriteLayout.FrameSize * 2
            && sheet.GetHeight() >= LpcSpriteLayout.FrameSize)
        {
            return CreateAtlasFrame(sheet, "DOWN", 0);
        }

        return sheet;
    }

    static AtlasTexture CreateAtlasFrame(Texture2D atlas, string direction, int column)
    {
        int frame = LpcSpriteLayout.RowFrame(LpcSpriteLayout.DirectionRow(direction), column);
        int col = frame % LpcSpriteLayout.HFrames;
        int row = frame / LpcSpriteLayout.HFrames;
        float size = LpcSpriteLayout.FrameSize;

        return new AtlasTexture
        {
            Atlas = atlas,
            Region = new Rect2(col * size, row * size, size, size),
        };
    }
}
