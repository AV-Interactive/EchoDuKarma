using System;
using System.Collections.Generic;
using Godot;

namespace EchoduKarma.Scripts.Data;

public enum DialogueType { TEXT, CHOICE }

public partial class DialogueLine : RefCounted
{
    public string Id { get; set; }
    public DialogueType Type { get; set; }
    public string NpcName { get; set; }
    public string Text { get; set; }
    public string Condition { get; set; }
    public string Action { get; set; }
    public string NextId { get; set; }
    
    public Dictionary<string, string> Choices { get; set; } = new Dictionary<string, string>();
}

public partial class DialogueSystem: Node
{
    public static DialogueSystem Instance { get; private set; }
    readonly Dictionary<string, DialogueLine> _dialogues = new Dictionary<string, DialogueLine>();
    
    [Signal] public delegate void DialogueRequestedEventHandler(DialogueLine line);
    [Signal] public delegate void ChoiceSelectedEventHandler(string nextId);
    [Signal] public delegate void ActionTriggeredEventHandler(string actionName);
    
    public override void _Ready()
    {
        GD.Print("[AUTOLOAD] DialogueSystem Ready - Start");
        Instance = this;
        GD.Print("[AUTOLOAD] DialogueSystem Ready - End");
    }

    public void LoadZoneDialogues(string zoneName)
    {
        GD.Print($"[DialogueSystem] Loading zone dialogues: {zoneName}");
        _dialogues.Clear();
        string filePath = $"res://Datas/Progress/{zoneName}/dialogues.csv";
        LoadFromCSV(filePath);
    }
    
    public void LoadFromCSV(string filePath)
    {
        using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[DialogueSystem] Fichier introuvable: {filePath}");
            return;
        }

        // Saut de l'en-tête
        _ = file.GetLine();
        DialogueLine lastLine = null;
        int count = 0;

        while (!file.EofReached())
        {
            var fields = file.GetCsvLine(",");
            if (fields == null || fields.Length == 0)
                continue;

            // Nouvelle entrée avec ID
            if (!string.IsNullOrWhiteSpace(fields[0]))
            {
                if (fields.Length < 7)
                {
                    GD.PrintErr($"[DialogueSystem] Ligne invalide (colonnes < 7): {string.Join("|", fields)}");
                    continue;
                }

                string rawType = fields[1];
                string typeValue = rawType == "CHOIX" ? "CHOICE" : rawType;

                var line = new DialogueLine
                {
                    Id = fields[0].Trim(),
                    Type = Enum.Parse<DialogueType>(typeValue, true),
                    NpcName = fields[2].Trim(),
                    Text = fields[3],
                    Condition = fields[4],
                    Action = fields[5],
                    NextId = fields[6],
                };

                if (line.Type == DialogueType.CHOICE && !string.IsNullOrWhiteSpace(line.Action) && !string.IsNullOrWhiteSpace(line.NextId))
                {
                    line.Choices[line.Action.Trim()] = line.NextId.Trim();
                }

                // Utiliser set pour éviter les exceptions sur doublon d'ID
                _dialogues[line.Id] = line;
                lastLine = line;
                count++;
            }
            else if (lastLine != null && lastLine.Type == DialogueType.CHOICE && fields.Length >= 7)
            {
                // Ligne de continuation de choix (ID vide)
                if (!string.IsNullOrWhiteSpace(fields[5]) && !string.IsNullOrWhiteSpace(fields[6]))
                {
                    lastLine.Choices[fields[5].Trim()] = fields[6].Trim();
                }
            }
        }

        GD.Print($"[DialogueSystem] {count} lignes (ID) chargées. Total noeuds de dialogue: {_dialogues.Count}");
    }
    
    public void SelectChoice(string nextId)
    {
        EmitSignal(SignalName.ChoiceSelected, nextId);
        var nextLine = GetDialogue(nextId);
        RequestDialogue(nextLine);
    }
    
    public void RequestDialogue(DialogueLine line)
    {
        if (line != null && !string.IsNullOrWhiteSpace(line.Action))
        {
            EmitSignal(SignalName.ActionTriggered, line.Action);
        }
        EmitSignal(SignalName.DialogueRequested, line);
    }
    
    public DialogueLine GetDialogue(string id) => _dialogues.GetValueOrDefault(id);
}
