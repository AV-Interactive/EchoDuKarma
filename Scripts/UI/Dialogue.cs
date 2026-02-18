using Godot;
using System;
using EchoduKarma.Scripts.Data;

public partial class Dialogue : Control
{
    [Export] RichTextLabel npcNameLabel;
    [Export] RichTextLabel textLabel;
    [Export] VBoxContainer choicesContainer;
    
    Tween _typewriterTween;
    
    public override void _Ready()
    {
        DialogueSystem.Instance.DialogueRequested += OnDialogueRecevied;
        Visible = false;
        choicesContainer.Visible = false;
        
        MouseFilter = MouseFilterEnum.Ignore;
    }

    void OnDialogueRecevied(DialogueLine line)
    {
        if (!IsInsideTree() || IsQueuedForDeletion()) return;
        
        if (line == null)
        {
            Visible = false;
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
        
        npcNameLabel.Text = line.NpcName;
        Visible = true;
        
        textLabel.Text = line.Text;
        textLabel.VisibleCharacters = 0;

        if (_typewriterTween != null && _typewriterTween.IsRunning())
        {
            _typewriterTween.Kill();
        }
        
        // Utilisation du CreateTween du Node directement
        _typewriterTween = CreateTween();
        
        float duration = line.Text.Length * 0.02f;
        
        _typewriterTween.TweenProperty(textLabel, "visible_characters", textLabel.Text.Length, duration);
        
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
            // Libérer le focus clavier différé
            CallDeferred(MethodName.ReleaseFocus);
        }
    }

    void ShowChoices(DialogueLine line)
    {
        choicesContainer.Visible = true;
    
        foreach (var choice in line.Choices) 
        {
            var btn = new Button();
            btn.AddThemeFontSizeOverride("font_size", 35);
            btn.Text = choice.Key;
            btn.CustomMinimumSize = new Vector2(0, 40); 
            btn.Alignment = HorizontalAlignment.Right;
            btn.FocusMode = FocusModeEnum.All;
            btn.MouseFilter = MouseFilterEnum.Ignore; // Désactiver la souris sur le bouton
            btn.Pressed += () => DialogueSystem.Instance.SelectChoice(choice.Value);
        
            choicesContainer.AddChild(btn);
        }
    
        // Donner le focus au premier bouton après un court délai pour laisser Godot l'ajouter
        CallDeferred(MethodName.FocusFirstButton);
    }

    void ReleaseFocus()
    {
        GetViewport()?.GuiReleaseFocus();
    }
    
    void FocusFirstButton()
    {
        if (choicesContainer.GetChildCount() > 0)
        {
            (choicesContainer.GetChild(0) as Control)?.GrabFocus();
        }
    }
}
