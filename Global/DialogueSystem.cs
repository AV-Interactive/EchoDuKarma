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

    /// <summary>Condition d'accès par libellé de choix (CONDITION ACCES, tokens séparés par |).</summary>
    public Dictionary<string, string> ChoiceConditions { get; set; } = new Dictionary<string, string>();
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
            var fields = file.GetCsvLine(";");
            if (fields == null || fields.Length == 0)
                continue;

            // Nouvelle entrée avec ID
            if (!string.IsNullOrWhiteSpace(fields[0]))
            {
                if (!TryParseDialogueFields(fields, out string id, out string rawType, out string npcName,
                        out string text, out string condition, out string action, out string nextId))
                {
                    GD.PrintErr($"[DialogueSystem] Ligne invalide: {string.Join("|", fields)}");
                    continue;
                }

                string typeValue = rawType == "CHOIX" ? "CHOICE" : rawType;

                var line = new DialogueLine
                {
                    Id = id,
                    Type = Enum.Parse<DialogueType>(typeValue, true),
                    NpcName = npcName,
                    Text = text,
                    Condition = condition,
                    Action = action,
                    NextId = nextId,
                };

                if (line.Type == DialogueType.CHOICE && !string.IsNullOrWhiteSpace(line.Action) && !string.IsNullOrWhiteSpace(line.NextId))
                {
                    string label = line.Action.Trim();
                    line.Choices[label] = line.NextId.Trim();
                    if (!string.IsNullOrWhiteSpace(line.Condition))
                        line.ChoiceConditions[label] = line.Condition.Trim();
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
                    string label = fields[5].Trim();
                    lastLine.Choices[label] = fields[6].Trim();
                    if (!string.IsNullOrWhiteSpace(fields[4]))
                        lastLine.ChoiceConditions[label] = fields[4].Trim();
                }
            }
        }

        GD.Print($"[DialogueSystem] {count} lignes (ID) chargées. Total noeuds de dialogue: {_dialogues.Count}");
    }

    /// <summary>
    /// Parse une ligne dialogue (7 colonnes fixes ; le TEXTE peut contenir des ';').
    /// Colonnes finales : … TEXTE | CONDITION | ACTION | LIEN SUIVANT.
    /// </summary>
    static bool TryParseDialogueFields(string[] fields, out string id, out string rawType, out string npcName,
        out string text, out string condition, out string action, out string nextId)
    {
        id = rawType = npcName = text = condition = action = nextId = "";

        if (fields == null || fields.Length < 4)
            return false;

        var cols = new List<string>(fields);
        while (cols.Count > 0 && string.IsNullOrWhiteSpace(cols[^1]))
            cols.RemoveAt(cols.Count - 1);

        while (cols.Count < 7)
            cols.Add("");

        if (string.IsNullOrWhiteSpace(cols[0]))
            return false;

        int n = cols.Count;
        id = cols[0].Trim();
        rawType = cols[1].Trim();
        npcName = cols[2].Trim();
        nextId = cols[n - 1].Trim();
        action = cols[n - 2].Trim();
        condition = cols[n - 3].Trim();
        text = string.Join(";", cols.GetRange(3, n - 6)).Trim();

        return true;
    }
    
    public void SelectChoice(string nextId)
    {
        EmitSignal(SignalName.ChoiceSelected, nextId);
        var nextLine = GetDialogue(nextId);
        RequestDialogue(nextLine);
    }
    
    public void RequestDialogue(DialogueLine line)
    {
        // UI d'abord, puis action (ex. BATTLE) pour que le texte s'affiche avant changement de scène.
        EmitSignal(SignalName.DialogueRequested, line);

        if (line != null && line.Type == DialogueType.TEXT && !string.IsNullOrWhiteSpace(line.Action))
            EmitSignal(SignalName.ActionTriggered, line.Action);
    }
    
    public DialogueLine GetDialogue(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return _dialogues.GetValueOrDefault(id);
    }
}
