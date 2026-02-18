using Godot;
using System;
using EchoduKarma.Scripts.Data;

public partial class Npc : CharacterBody2D
{
    [Export] public string Name;
    [Export] public string StartDialogueId;
    [Export] public Texture2D SpritePath;
    
    Sprite2D _sprite;
    bool _isPlayerInRange = false;
    string _currentDialogueId;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
        if (SpritePath != null) _sprite.Texture = SpritePath;
        _currentDialogueId = StartDialogueId;
        
        var area = GetNode<Area2D>("InteractionArea");
        
        area.BodyEntered += (body) =>
        {
            if (body.Name == "Player")
            {
                _isPlayerInRange = true;
            }
        };
        area.BodyExited += (body) =>
        {
            if (body.Name == "Player")
            {
                _isPlayerInRange = false;
                ResetDialogue();
            }
        };
        
        DialogueSystem.Instance.ChoiceSelected += (nextId) => 
        {
            if (_isPlayerInRange)
            {
                GD.Print($"[DialogueSystem] Choix de dialogue pour {Name} : {nextId}");
                _currentDialogueId = nextId;
            }
        };
    }

    public override void _Input(InputEvent @event)
    {
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isPlayerInRange && @event.IsActionPressed("Interaction"))
        {
            // Si l'UI a le focus, on la laisse gérer l'input (Espace)
            if (GetViewport().GuiGetFocusOwner() != null) return;
            
            AdvanceDialogue();
        }
    }

    public void AdvanceDialogue()
    {
        DialogueLine line = DialogueSystem.Instance.GetDialogue(_currentDialogueId);

        if (line != null)
        {
            GameManager.Instance.PlayerMoved = false;
            // Dans Npc.cs
            DialogueSystem.Instance.RequestDialogue(line);

            if (line.Type == DialogueType.CHOICE)
            {
                // On ne change pas _currentDialogueId ici, 
                // il sera mis à jour via le signal ChoiceSelected
                return;
            }
            
            if (!string.IsNullOrWhiteSpace(line.NextId))
            {
                _currentDialogueId = line.NextId;
            }
            else
            {
                _currentDialogueId = "FINISH";
                ResetDialogue();
            }
        }
        else
        {
            GameManager.Instance.PlayerMoved = true;
            ResetDialogue();
        }
    }
    
    public void ResetDialogue()
    {
        _currentDialogueId = StartDialogueId;
        GameManager.Instance.PlayerMoved = true;
        DialogueSystem.Instance.EmitSignal(DialogueSystem.SignalName.DialogueRequested, (DialogueLine)null);
    }
}
