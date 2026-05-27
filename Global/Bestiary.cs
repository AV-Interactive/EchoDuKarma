using EchoduKarma.Scripts.Data;
using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public enum AiPattern
{
    /// <summary>Mix ~50 % sort / ~50 % mêlée (selon sorts utilisables).</summary>
    Normal,
    /// <summary>Privilégie les sorts d'attaque (~70 %) ; mêlée si PM bas ; bonus Force si PV &lt; 30 %.</summary>
    Aggressive,
    /// <summary>PV &gt; 50 % : plutôt mêlée ; PV bas : défense ou soin ; sorts d'attaque occasionnels.</summary>
    Defensive,
}

public class EnemyStats : Stats
{
    public string EnemyName { get; set; }
    public int XpValue { get; set; }
    public string Loot { get; set; }
    public AiPattern AiPattern { get; set; }
    public ElementType Affinity { get; set; }
    /// <summary>Clés de sorts (<c>skills.csv</c>), séparées par <c>|</c> dans le CSV.</summary>
    public List<string> SkillKeys { get; set; } = new();

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

    /// <summary>Parse la colonne Skills (ex. <c>Stalagtite|Renforcement</c>).</summary>
    public static List<string> ParseSkills(string skillsRaw)
    {
        if (string.IsNullOrWhiteSpace(skillsRaw))
            return new List<string>();

        return skillsRaw
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    public EnemyStats Clone() => new()
    {
        EnemyName = EnemyName,
        Level = Level,
        Multiplier = Multiplier,
        Pv = Pv,
        Mp = Mp,
        Strength = Strength,
        Spirit = Spirit,
        Dexterity = Dexterity,
        Defense = Defense,
        XpValue = XpValue,
        Loot = Loot,
        AiPattern = AiPattern,
        Affinity = Affinity,
        SkillKeys = new List<string>(SkillKeys),
    };
}

sealed class EnemyDefinition
{
    public string Name { get; init; }
    public int XpValue { get; init; }
    public AiPattern AiPattern { get; init; }
    public string Loot { get; init; }
    public ElementType Affinity { get; init; }
    public List<string> SkillKeys { get; init; } = new();
}

public partial class Bestiary : Node
{
    const string ProgressionFolder = "res://Datas/Bestiary/";

    [Export(PropertyHint.File, "*.csv")]
    public string BestiaryPath = "res://Datas/Bestiary/bestiary.csv";

    readonly Dictionary<string, EnemyDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Dictionary<int, Stats>> _progressions = new(StringComparer.OrdinalIgnoreCase);

    public static Bestiary Instance { get; private set; }

    public override void _Ready()
    {
        GD.Print("[AUTOLOAD] Bestiary Ready - Start");
        Instance = this;

        LoadBestiary();
        GD.Print("[AUTOLOAD] Bestiary Ready - End");
    }

    void LoadBestiary()
    {
        GD.Print($"[Bestiary] Loading bestiary from: {BestiaryPath}");
        using var file = FileAccess.Open(BestiaryPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[Bestiary] CSV file not found: {BestiaryPath}");
            return;
        }

        file.GetLine(); // header
        int count = 0;

        while (!file.EofReached())
        {
            string[] columns = file.GetCsvLine(";");
            if (columns == null || columns.Length == 0)
                continue;

            if (columns.Length < 6)
            {
                GD.PrintErr($"[Bestiary] Invalid line (columns < 6): {string.Join("|", columns)}");
                continue;
            }

            try
            {
                for (int i = 0; i < columns.Length; i++)
                    columns[i] = columns[i].Trim();

                string name = columns[0];
                var definition = new EnemyDefinition
                {
                    Name = name,
                    XpValue = Math.Max(0, int.Parse(columns[1], CultureInfo.InvariantCulture)),
                    AiPattern = EnemyStats.ParsePattern(columns[2]),
                    Loot = columns[3],
                    Affinity = ElementCombat.Parse(columns[4]),
                    SkillKeys = EnemyStats.ParseSkills(columns[5]),
                };

                _definitions[name] = definition;
                LoadProgression(name);
                count++;
            }
            catch (Exception e)
            {
                GD.PrintErr($"[Bestiary] Error parsing line: {string.Join("|", columns)}. Error: {e.Message}");
            }
        }

        GD.Print($"[Bestiary] {count} enemies loaded.");
    }

    void LoadProgression(string enemyName)
    {
        string path = $"{ProgressionFolder}{enemyName.ToLowerInvariant()}.csv";
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[Bestiary] Progression CSV not found for '{enemyName}': {path}");
            _progressions[enemyName] = new Dictionary<int, Stats>();
            return;
        }

        var levels = new Dictionary<int, Stats>();
        file.GetLine(); // header

        while (!file.EofReached())
        {
            string[] columns = file.GetCsvLine(";");
            if (columns == null || columns.Length == 0)
                continue;

            if (columns.Length < 8)
            {
                GD.PrintErr($"[Bestiary] Invalid progression line for '{enemyName}' (columns < 8): {string.Join("|", columns)}");
                continue;
            }

            try
            {
                for (int i = 0; i < columns.Length; i++)
                    columns[i] = columns[i].Trim();

                int level = int.Parse(columns[0], CultureInfo.InvariantCulture);
                float multiplier = float.Parse(columns[1].Replace(',', '.'), CultureInfo.InvariantCulture);

                var stats = new Stats
                {
                    Level = level,
                    Multiplier = MathF.Round(multiplier, 3),
                    Pv = int.Parse(columns[2], CultureInfo.InvariantCulture),
                    Mp = int.Parse(columns[3], CultureInfo.InvariantCulture),
                    Strength = int.Parse(columns[4], CultureInfo.InvariantCulture),
                    Spirit = int.Parse(columns[5], CultureInfo.InvariantCulture),
                    Dexterity = int.Parse(columns[6], CultureInfo.InvariantCulture),
                    Defense = int.Parse(columns[7], CultureInfo.InvariantCulture),
                };

                levels[level] = stats;
            }
            catch (Exception e)
            {
                GD.PrintErr($"[Bestiary] Error parsing progression for '{enemyName}': {string.Join("|", columns)} — {e.Message}");
            }
        }

        _progressions[enemyName] = levels;
        GD.Print($"[Bestiary] {levels.Count} niveau(x) chargé(s) pour '{enemyName}'.");
    }

    /// <summary>Métadonnées + stats au niveau demandé. Null si l'ennemi est inconnu.</summary>
    public EnemyStats GetEnemyAtLevel(string name, int level)
    {
        if (string.IsNullOrWhiteSpace(name) || !_definitions.TryGetValue(name.Trim(), out var definition))
            return null;

        level = Math.Max(1, level);
        Stats levelStats = ResolveLevelStats(name.Trim(), level);
        if (levelStats == null)
        {
            GD.PrintErr($"[Bestiary] Aucune progression pour '{name}' niveau {level}.");
            return null;
        }

        return BuildEnemyStats(definition, levelStats);
    }

    /// <summary>Compatibilité — niveau 1 par défaut.</summary>
    public EnemyStats GetEnemy(string name) => GetEnemyAtLevel(name, 1);

    static EnemyStats BuildEnemyStats(EnemyDefinition definition, Stats levelStats) => new()
    {
        EnemyName = definition.Name,
        Level = levelStats.Level,
        Multiplier = levelStats.Multiplier,
        Pv = levelStats.Pv,
        Mp = levelStats.Mp,
        Strength = levelStats.Strength,
        Spirit = levelStats.Spirit,
        Dexterity = levelStats.Dexterity,
        Defense = levelStats.Defense,
        XpValue = definition.XpValue,
        Loot = definition.Loot,
        AiPattern = definition.AiPattern,
        Affinity = definition.Affinity,
        SkillKeys = new List<string>(definition.SkillKeys),
    };

    Stats ResolveLevelStats(string enemyName, int level)
    {
        if (!_progressions.TryGetValue(enemyName, out var levels) || levels.Count == 0)
            return null;

        if (levels.TryGetValue(level, out var exact))
            return exact;

        int closest = levels.Keys.Where(l => l <= level).DefaultIfEmpty(levels.Keys.Min()).Max();
        GD.Print($"[Bestiary] Niveau {level} absent pour '{enemyName}' — fallback niveau {closest}.");
        return levels[closest];
    }
}
