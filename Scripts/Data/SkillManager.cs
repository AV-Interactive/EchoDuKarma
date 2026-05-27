using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoduKarma.Scripts.Entities.Player;
using FileAccess = Godot.FileAccess;

public partial class SkillManager : Node
{
    static List<Skill> _catalog;

    public static bool IsUnlockedAtLevel(Skill skill, int playerLevel) =>
        skill != null && playerLevel >= skill.LevelRequired;

    public static bool MatchesClass(Skill skill, string className)
    {
        if (skill?.Classes == null || string.IsNullOrWhiteSpace(className))
            return false;

        foreach (string c in skill.Classes)
        {
            if (string.Equals(c.Trim(), className.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>Compétences de la classe accessibles au niveau donné (Level requis inclus).</summary>
    public static List<Skill> GetUnlockedForClass(string className, int playerLevel) =>
        LoadSkills()
            .Where(s => MatchesClass(s, className) && IsUnlockedAtLevel(s, playerLevel))
            .ToList();

    public static List<Skill> LoadSkills()
    {
        if (_catalog != null)
            return _catalog;

        List<Skill> skillList = new List<Skill>();
        string path = "res://Datas/Persos/skills.csv";
        
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr("Impossible de trouver le fichier CSV des skills");
            return skillList;
        }

        file.GetLine();

        while (!file.EofReached())
        {
            string[] cols = file.GetCsvLine(";");
            if (cols == null || cols.Length < 11)
                continue;

            for (int i = 0; i < cols.Length; i++)
                cols[i] = cols[i].Trim();

            Skill s = new Skill();
            s.Name = cols[0];
            s.Type = Enum.Parse<SkillType>(cols[1]);
            s.Description = cols[2];
            s.Cost = cols[3].ToInt();
            s.Power = cols[4].ToInt();
            s.Element = cols[5];
            s.Speed = cols[6].ToInt();
            s.Effect = cols[7];
            s.TargetType = cols[8];
            s.Classes = cols[9]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            s.LevelRequired = cols[10].ToInt();
            
            skillList.Add(s);
            
            GD.Print("Skill loaded: " + s.Name);
        }

        _catalog = skillList;
        return skillList;
    }
}
