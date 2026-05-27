using Godot;
using System;
using System.Collections.Generic;

public enum AiPattern
{
    Normal,      // Attaque physique basique chaque tour
    Aggressive,  // Toujours attaque ; bonus Force quand PV < 30 %
    Defensive,   // Attaque si PV > 50 % ; sinon 60 % de chances de passer en posture défensive
}

public class EnemyStats : Stats
{
    public string EnemyName { get; set; }
    public int XpValue { get; set; }
    public string Loot { get; set; }
    public AiPattern AiPattern { get; set; }

    static readonly Dictionary<string, AiPattern> _patternMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Normal",      AiPattern.Normal },
        { "Aggressive",  AiPattern.Aggressive },
        { "Defensive",   AiPattern.Defensive },
    };

    public static AiPattern ParsePattern(string raw)
    {
        if (!string.IsNullOrWhiteSpace(raw) && _patternMap.TryGetValue(raw.Trim(), out var p))
            return p;
        GD.PrintErr($"[Bestiary] AiPattern inconnu : '{raw}' → Normal par défaut.");
        return AiPattern.Normal;
    }

    /// <summary>
    /// Parse la colonne LOOT du bestiaire (ex. "Gelée, Fleur de gobi" ou "Peau de rat").
    /// </summary>
    public static IReadOnlyList<string> ParseLoot(string lootRaw)
    {
        if (string.IsNullOrWhiteSpace(lootRaw))
            return Array.Empty<string>();

        return lootRaw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}

public partial class Bestiary : Node
{
    [Export(PropertyHint.File, "*.csv")]
    public string BestiaryPath = "res://Datas/Bestiary/bestiary.csv";

    Dictionary<string, EnemyStats> _bestiary = new Dictionary<string, EnemyStats>();
    
    public static Bestiary Instance { get; private set; }
    
    public override void _Ready()
    {
        GD.Print("[AUTOLOAD] Bestiary Ready - Start");
        Instance = this;
        
        LoadBestiary();
        GD.Print("[AUTOLOAD] Bestiary Ready - End");
    }

    private void LoadBestiary()
    {
        GD.Print($"[Bestiary] Loading bestiary from: {BestiaryPath}");
        using var file = FileAccess.Open(BestiaryPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[Bestiary] CSV file not found: {BestiaryPath}");
            return;
        }

        file.GetLine(); // Skip header
        int count = 0;

        while (!file.EofReached())
        {
            string[] columns = file.GetCsvLine(",");
            if (columns == null || columns.Length == 0) continue;
            if (columns.Length < 10)
            {
                GD.PrintErr($"[Bestiary] Invalid line (columns < 10): {string.Join("|", columns)}");
                continue;
            }

            try
            {
                EnemyStats enemy = new EnemyStats {
                    EnemyName = columns[0].Trim(),
                    Level = int.Parse(columns[1]),
                    Pv = int.Parse(columns[2]),
                    XpValue = int.Parse(columns[3]),
                    Loot = columns[9].Trim(),
                    Strength = int.Parse(columns[4]),
                    Spirit = int.Parse(columns[5]),
                    Dexterity = int.Parse(columns[6]),
                    Defense = int.Parse(columns[7]),
                    AiPattern = EnemyStats.ParsePattern(columns[8])
                };
                _bestiary[enemy.EnemyName] = enemy;
                count++;
            }
            catch (Exception e)
            {
                GD.PrintErr($"[Bestiary] Error parsing line: {string.Join("|", columns)}. Error: {e.Message}");
            }
        }
        GD.Print($"[Bestiary] {count} enemies loaded.");
    }

    public EnemyStats GetEnemy(string name) => _bestiary.GetValueOrDefault(name);
}
