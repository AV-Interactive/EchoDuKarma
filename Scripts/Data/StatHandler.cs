using Godot;
using System;
using System.Collections.Generic;
using EchoduKarma.Scripts.Helpers;

public class Stats
{
    public int XPForNextLevel { get; set; }
    public int Level { get; set; } = 1;
    public float Multiplier { get; set; } = 1.0f;
    public int Experience { get; set; }
    public int Pv { get; set; }
    public int Mp { get; set; }
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Spirit { get; set; }
    public int Defense { get; set; }
}

public partial class StatHandler : Node2D
{
    [Export(PropertyHint.File, "*.csv")]
    public string DataFilePath = "res://Datas/*.csv";
    
    Dictionary<int, Stats> _baseStats = new Dictionary<int, Stats>();

    public int CurrentLevel { get; set; } = 1;
    public int CurrentExperience { get; set; }
    public int PvMax { get; set; }
    public int CurrentPv { get; set;}
    public int MpMax { get; set;}
    public int CurrentMp { get; set;}
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Spirit { get; set; }
    public int Defense { get; set; }
    
    
    public override void _Ready()
    {
        GD.Print($"[StatHandler] _Ready for node {Name} with file {DataFilePath}");
        LoadStats();
        UpdateCurrentStats(CurrentLevel);
        
        Stats startingStats = GetStatsForLevel(CurrentLevel);

        if (startingStats != null)
        {
            CurrentPv = startingStats.Pv;
            CurrentMp = startingStats.Mp;
        }
        else
        {
            GD.PrintErr($"[StatHandler] No stats found for level {CurrentLevel} in {DataFilePath}");
        }
    }
    
    public void LoadStats()
    {
        if (string.IsNullOrWhiteSpace(DataFilePath) || DataFilePath.Contains("*"))
        {
            GD.PrintErr($"[StatHandler] Invalid DataFilePath: {DataFilePath}. Please set it in the inspector.");
            return;
        }

        using var file = FileAccess.Open(DataFilePath, FileAccess.ModeFlags.Read);

        if (file == null)
        {
            GD.PrintErr($"[StatHandler] Impossible de lire le fichier {DataFilePath}.");
            return;
        }
        
        file.GetLine(); // header
        int count = 0;

        while (!file.EofReached())
        {
            string[] columns = file.GetCsvLine(","); 

            if (columns == null || columns.Length == 0) continue;
            if (columns.Length < 9) 
            {
                GD.PrintErr($"[StatHandler] Ligne invalide (colonnes < 9): {string.Join("|", columns)}");
                continue;
            }

            try 
            {
                Stats stats = new Stats();

                for (int i = 0; i < columns.Length; i++) columns[i] = columns[i].Trim();
                
                stats.XPForNextLevel = int.Parse(columns[0]);
                stats.Level = int.Parse(columns[1]);
                float rawMultiplier = float.Parse(columns[2].Replace(',', '.'),
                    System.Globalization.CultureInfo.InvariantCulture);
                stats.Multiplier = MathF.Round(rawMultiplier, 2);
                stats.Pv = int.Parse(columns[3]);
                stats.Mp = int.Parse(columns[4]);
                stats.Strength = int.Parse(columns[5]);
                stats.Spirit = int.Parse(columns[6]);
                stats.Dexterity = int.Parse(columns[7]);
                stats.Defense = int.Parse(columns[8]);
                
                _baseStats[stats.Level] = stats;
                count++;
            }
            catch (Exception e)
            {
                GD.PrintErr($"[StatHandler] Erreur de parsing dans {DataFilePath}: {e.Message}");
            }
        }
        GD.Print($"[StatHandler] {count} niveaux de stats chargés pour {DataFilePath}");
    }

    public Stats GetStatsForLevel(int level)
    {
        if (_baseStats.ContainsKey(level))
        {
            return _baseStats[level];
        }
        return null;
    }
    
    void UpdateCurrentStats(int level)
    {
        Stats s = GetStatsForLevel(level);
        if (s == null) return;

        CurrentLevel = s.Level;
        PvMax = s.Pv;
        MpMax = s.Mp;
        // C'est ici qu'on injecte les valeurs du CSV dans le Handler
        Strength = s.Strength;
        Dexterity = s.Dexterity;
        Spirit = s.Spirit;
        Defense = s.Defense;
    
        CurrentPv = PvMax; 
        CurrentMp = MpMax;
    }

    public Stats LevelUp()
    {
        if (CurrentLevel >= _baseStats.Count) return null;
        
        CurrentLevel++;
        
        Stats newStats = GetStatsForLevel(CurrentLevel);
        if (newStats == null) return null;
        
        CurrentPv = newStats.Pv;
        CurrentMp = newStats.Mp;
        
        UpdateCurrentStats(CurrentLevel);
        
        return newStats;
    }
}
