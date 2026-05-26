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
                DialogueSystem.Instance.SelectChoice(_choiceValues[_selectedChoiceIndex]);
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
        _choiceValues.Clear();
        _selectedChoiceIndex = 0;
        choicesContainer.Visible = true;
        _choicesInputLocked = true;

        foreach (var choice in line.Choices)
        {
            _choiceValues.Add(choice.Value);

            var btn = new Button();
            btn.AddThemeFontSizeOverride("font_size", 16);
            btn.CustomMinimumSize = new Vector2(0, 40);
            btn.Text = choice.Key;
            btn.Alignment = HorizontalAlignment.Right;
            // FocusMode = All pour le highlight visuel, mais l'input clavier
            // est entièrement géré par _Input (pas via ui_accept du bouton)
            btn.FocusMode = FocusModeEnum.All;

            string capturedValue = choice.Value;
            btn.Pressed += () => DialogueSystem.Instance.SelectChoice(capturedValue); // souris uniquement

            choicesContainer.AddChild(btn);
        }

        GetTree().CreateTimer(0.5f).Timeout += () =>
        {
            _choicesInputLocked = false;
            HighlightChoice(0);
        };
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
