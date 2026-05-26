using Godot;
using System;
using System.Collections.Generic;
using EchoduKarma.Scripts.Data;

public partial class Dialogue : Control
{
    [Export] RichTextLabel npcNameLabel;
    [Export] RichTextLabel textLabel;
    [Export] VBoxContainer choicesContainer;
    
    Tween _typewriterTween;

    bool _isTyping = false;
    bool _choicesInputLocked = false;
    readonly List<string> _choiceValues = new();
    readonly List<string> _choiceLabels = new();
    readonly List<bool> _choiceAvailable = new();
    DialogueLine _currentChoiceLine;
    int _selectedChoiceIndex = 0;

    public override void _Ready()
    {
        DialogueSystem.Instance.DialogueRequested += OnDialogueRecevied;
        Visible = false;
        choicesContainer.Visible = false;

        if (npcNameLabel != null)
        {
            npcNameLabel.ScrollActive = false;
            npcNameLabel.ScrollFollowing = false;
            npcNameLabel.FitContent = true;
        }
        
        MouseFilter = MouseFilterEnum.Stop; // Changé de Ignore à Stop pour bloquer les clics
    }

    public override void _ExitTree()
    {
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.DialogueRequested -= OnDialogueRecevied;

        if (_typewriterTween != null && _typewriterTween.IsValid())
            _typewriterTween.Kill();

        base._ExitTree();
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        if (@event.IsActionPressed("Interaction"))
        {
            // Période de verrouillage après apparition des choix : bloquer tout
            if (_choicesInputLocked)
            {
                GetViewport().SetInputAsHandled();
                return;
            }

            // Typewriter en cours : compléter le texte instantanément
            if (_isTyping)
            {
                SkipTypewriter();
                GetViewport().SetInputAsHandled();
                return;
            }

            // Choix visible : confirmer la sélection courante (clavier / manette)
            if (choicesContainer.Visible && _choiceValues.Count > 0)
            {
                TryConfirmChoice(_selectedChoiceIndex);
                GetViewport().SetInputAsHandled();
                return;
            }

            // Cas TEXT normal : on laisse passer → Npc._UnhandledInput avance le dialogue
        }

        // Navigation dans les choix (flèches / stick)
        if (choicesContainer.Visible && !_choicesInputLocked && _choiceValues.Count > 1)
        {
            if (@event.IsActionPressed("ui_down"))
            {
                HighlightChoice((_selectedChoiceIndex + 1) % _choiceValues.Count);
                GetViewport().SetInputAsHandled();
            }
            else if (@event.IsActionPressed("ui_up"))
            {
                HighlightChoice((_selectedChoiceIndex - 1 + _choiceValues.Count) % _choiceValues.Count);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    void SkipTypewriter()
    {
        if (_typewriterTween != null && _typewriterTween.IsRunning())
        {
            _typewriterTween.Kill();
            textLabel.VisibleCharacters = textLabel.Text.Length;
        }
        _isTyping = false;
    }

    void OnDialogueRecevied(DialogueLine line)
    {
        if (!GodotObject.IsInstanceValid(this))
            return;

        if (!IsInsideTree() || IsQueuedForDeletion())
            return;
        
        if (line == null)
        {
            Visible = false;
            _isTyping = false;
            _choicesInputLocked = false;
            _currentChoiceLine = null;
            CallDeferred(MethodName.ReleaseFocus);
            return;
        }

        if (line.Type == DialogueType.TEXT
            && !string.IsNullOrWhiteSpace(line.Condition)
            && !DialogueConditions.EvaluateAll(line.Condition))
        {
            Visible = true;
            npcNameLabel.Text = $"[color=#3F9DD9]{line.NpcName}[/color]";
            textLabel.Text = DialogueConditions.GetFailureReason(line.Condition)
                             ?? "[color=#E85D5D]Tu ne peux pas poursuivre ce dialogue pour l'instant.[/color]";
            textLabel.VisibleCharacters = textLabel.Text.Length;
            choicesContainer.Visible = false;
            _isTyping = false;
            CallDeferred(MethodName.ReleaseFocus);
            return;
        }

        if (!IsInsideTree())
        {
            CallDeferred(nameof(OnDialogueRecevied), line);
            return;
        }
        
        foreach (Node child in choicesContainer.GetChildren()) 
        {
            choicesContainer.RemoveChild(child);
            child.QueueFree();
        }
        
        npcNameLabel.Text = $"[color=#3F9DD9]{line.NpcName}[/color]";
        npcNameLabel.ScrollActive = false;
        ZIndex = 5;
        MoveToFront();
        Visible = true;
        
        textLabel.Text = line.Text;
        textLabel.VisibleCharacters = 0;

        if (_typewriterTween != null && _typewriterTween.IsRunning())
        {
            _typewriterTween.Kill();
        }
        
        _isTyping = true;
        _typewriterTween = CreateTween();
        
        float duration = line.Text.Length * 0.02f;
        
        _typewriterTween.TweenProperty(textLabel, "visible_characters", textLabel.Text.Length, duration);
        _typewriterTween.Finished += () => _isTyping = false;

        if (line.Type == DialogueType.CHOICE)
        {
            choicesContainer.Visible = false;
            _typewriterTween.Finished += () =>
            {
                ShowChoices(line);
            };
        }
        else
        {
            choicesContainer.Visible = false;
            // Libérer le focus clavier différé pour permettre à l'input de remonter au NPC après le texte
            CallDeferred(MethodName.ReleaseFocus);
        }
    }

    void ShowChoices(DialogueLine line)
    {
        _currentChoiceLine = line;
        _choiceValues.Clear();
        _choiceLabels.Clear();
        _choiceAvailable.Clear();
        _selectedChoiceIndex = 0;
        choicesContainer.Visible = true;
        _choicesInputLocked = true;

        foreach (var choice in line.Choices)
        {
            string label = choice.Key;
            string nextId = choice.Value;
            string condition = line.ChoiceConditions.GetValueOrDefault(label, "");
            bool available = DialogueConditions.EvaluateAll(condition);

            _choiceLabels.Add(label);
            _choiceValues.Add(nextId);
            _choiceAvailable.Add(available);

            var btn = new Button();
            btn.AddThemeFontSizeOverride("font_size", 16);
            btn.CustomMinimumSize = new Vector2(0, 40);
            btn.Text = BuildChoiceButtonText(label, condition, available);
            btn.Alignment = HorizontalAlignment.Right;
            btn.FocusMode = FocusModeEnum.All;

            if (!available)
                btn.Modulate = new Color(0.55f, 0.55f, 0.6f);

            int capturedIndex = _choiceValues.Count - 1;
            btn.Pressed += () => TryConfirmChoice(capturedIndex);

            choicesContainer.AddChild(btn);
        }

        GetTree().CreateTimer(0.5f).Timeout += () =>
        {
            _choicesInputLocked = false;
            HighlightChoice(FindFirstAvailableChoiceIndex());
        };
    }

    static string BuildChoiceButtonText(string label, string condition, bool available)
    {
        if (available || string.IsNullOrWhiteSpace(condition))
            return label;

        string shortHint = ExtractKarmaShortHint(condition);
        return string.IsNullOrEmpty(shortHint) ? $"{label} (bloqué)" : $"{label} ({shortHint})";
    }

    static string ExtractKarmaShortHint(string condition)
    {
        foreach (string raw in condition.Split('|'))
        {
            string token = raw.Trim();
            if (!token.StartsWith("KARMA:", StringComparison.OrdinalIgnoreCase))
                continue;

            int colon = token.LastIndexOf(':');
            if (colon >= 0 && colon < token.Length - 1)
                return $"Karma {token[(colon + 1)..]}";
        }

        return null;
    }

    void TryConfirmChoice(int index)
    {
        if (index < 0 || index >= _choiceValues.Count)
            return;

        if (!_choiceAvailable[index])
        {
            string condition = _currentChoiceLine?.ChoiceConditions.GetValueOrDefault(_choiceLabels[index], "") ?? "";
            ShowConditionFailure(DialogueConditions.GetFailureReason(condition));
            return;
        }

        DialogueSystem.Instance.SelectChoice(_choiceValues[index]);
    }

    void ShowConditionFailure(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            message = "[color=#E85D5D]Tu ne remplis pas encore les conditions pour ce choix.[/color]";

        SkipTypewriter();
        textLabel.Text = message;
        textLabel.VisibleCharacters = message.Length;
        _isTyping = false;
    }

    int FindFirstAvailableChoiceIndex()
    {
        for (int i = 0; i < _choiceAvailable.Count; i++)
        {
            if (_choiceAvailable[i])
                return i;
        }

        return 0;
    }

    /// <summary>Met le highlight visuel sur le choix à l'index donné.</summary>
    void HighlightChoice(int index)
    {
        _selectedChoiceIndex = index;
        for (int i = 0; i < choicesContainer.GetChildCount(); i++)
        {
            if (choicesContainer.GetChild(i) is Button btn && i == index)
                btn.GrabFocus();
        }
    }

    void ReleaseFocus()
    {
        GetViewport()?.GuiReleaseFocus();
    }
}
