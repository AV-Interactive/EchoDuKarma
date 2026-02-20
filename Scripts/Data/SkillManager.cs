using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using EchoduKarma.Scripts.Entities.Player;
using FileAccess = Godot.FileAccess;

public partial class SkillManager : Node
{
    public static List<Skill> LoadSkills()
    {
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
            string[] cols = file.GetCsvLine(",");

            if (cols.Length < 8) continue;

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
            s.Classes = new List<string>(cols[9].Split(','));
            s.LevelRequired = cols[10].ToInt();
            
            skillList.Add(s);
            
            GD.Print("Skill loaded: " + s.Name);
        }
        
        return skillList;
    }
}
