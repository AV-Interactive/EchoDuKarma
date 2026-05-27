using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EchoduKarma.Scripts.Data;

/// <summary>
/// Table de spawn par zone (<c>Datas/Progress/{zone}/enemies.csv</c>).
/// Colonne Spawn Rate ignorée pour l'instant.
/// </summary>
public static class ZoneEnemyCatalog
{
    readonly struct LevelRange
    {
        public readonly int Min;
        public readonly int Max;

        public LevelRange(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public int RollLevel() => Min == Max ? Min : Min + (int)(GD.Randi() % (uint)(Max - Min + 1));
    }

    static readonly Dictionary<string, Dictionary<string, LevelRange>> _zoneTables = new(StringComparer.OrdinalIgnoreCase);

    public static void LoadZone(string zoneName)
    {
        if (string.IsNullOrWhiteSpace(zoneName))
            return;

        string path = $"res://Datas/Progress/{zoneName.Trim()}/enemies.csv";
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.Print($"[ZoneEnemyCatalog] Pas de table ennemis pour la zone '{zoneName}' ({path}).");
            _zoneTables[zoneName.Trim()] = new Dictionary<string, LevelRange>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var entries = new Dictionary<string, LevelRange>(StringComparer.OrdinalIgnoreCase);
        file.GetLine(); // header

        while (!file.EofReached())
        {
            string[] columns = file.GetCsvLine(";");
            if (columns == null || columns.Length < 2)
                continue;

            string name = columns[0].Trim();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (!TryParseLevelRange(columns[1], out int minLevel, out int maxLevel))
            {
                GD.PrintErr($"[ZoneEnemyCatalog] Plage de niveaux invalide pour '{name}' : '{columns[1]}'");
                continue;
            }

            entries[name] = new LevelRange(minLevel, maxLevel);
        }

        _zoneTables[zoneName.Trim()] = entries;
        GD.Print($"[ZoneEnemyCatalog] {entries.Count} entrée(s) chargée(s) pour '{zoneName}'.");
    }

    /// <summary>Tire un niveau selon la zone courante, ou 1 si l'ennemi n'y figure pas.</summary>
    public static int RollEnemyLevel(string zoneName, string enemyName)
    {
        if (string.IsNullOrWhiteSpace(zoneName) || string.IsNullOrWhiteSpace(enemyName))
            return 1;

        if (!_zoneTables.TryGetValue(zoneName.Trim(), out var entries))
            return 1;

        return entries.TryGetValue(enemyName.Trim(), out var range)
            ? range.RollLevel()
            : 1;
    }

    public static bool TryParseLevelRange(string raw, out int minLevel, out int maxLevel)
    {
        minLevel = 1;
        maxLevel = 1;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        raw = raw.Trim();
        int dash = raw.IndexOf('-');
        if (dash >= 0)
        {
            if (!int.TryParse(raw[..dash].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out minLevel))
                return false;
            if (!int.TryParse(raw[(dash + 1)..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out maxLevel))
                return false;
        }
        else if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out minLevel))
        {
            return false;
        }

        if (minLevel > maxLevel)
            (minLevel, maxLevel) = (maxLevel, minLevel);

        minLevel = Math.Max(1, minLevel);
        maxLevel = Math.Max(minLevel, maxLevel);
        return true;
    }
}
