using Godot;
using System;
using EchoduKarma.Scripts.Data;

public partial class Npc : CharacterBody3D
{
    [Export] public string NpcName;
    [Export] public Texture2D SpriteTexture;
    [Export] public string StartDialogueId;
    /// <summary>
    /// IDs de dialogues alternatifs évalués en priorité (dans l'ordre du tableau).
    /// Le premier dont la CONDITION ACCES est remplie remplace StartDialogueId.
    /// Exemple pour le Marchand : ["MARCHAND_DONE_01", "MARCHAND_EN_COURS_01"]
    /// </summary>
    [Export] public string[] ConditionalStartIds = Array.Empty<string>();

    private Sprite3D _sprite;
    private bool _isPlayerInRange = false;
    private string _currentDialogueId;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite3D>("Node3D/Sprite3D");
        _sprite.Texture = SpriteTexture;
        _currentDialogueId = GetActiveStartDialogueId();
        
        // On récupère l'Area3D pour la détection 3D
        var area = GetNode<Area3D>("InteractionArea");
        
        area.BodyEntered += (body) =>
        {
            if (body.Name == "Player" || body.IsInGroup("Player"))
            {
                _isPlayerInRange = true;
                // Réévalue le dialogue de départ au cas où la quête aurait progressé
                _currentDialogueId = GetActiveStartDialogueId();
                GD.Print($"[{NpcName}] Dialogue de départ : {_currentDialogueId}");
            }
        };

        area.BodyExited += (body) =>
        {
            if (body.Name == "Player" || body.IsInGroup("Player"))
            {
                _isPlayerInRange = false;
                ResetDialogue();
            }
        };

        CallDeferred(nameof(RefreshPlayerInRange));
        
        // Ton système de dialogue reste identique (logique pure)
        DialogueSystem.Instance.ChoiceSelected += (nextId) => 
        {
            if (_isPlayerInRange)
            {
                var line = DialogueSystem.Instance.GetDialogue(nextId);
                
                if (line != null && line.Type != DialogueType.CHOICE)
                {
                    // Comme le DialogueSystem affiche déjà la réponse du choix,
                    // on prépare directement l'ID suivant pour le NPC.
                    _currentDialogueId = line.NextId;
                }
                else
                {
                    // Si c'est un autre choix ou si la ligne n'existe pas, on garde l'ID actuel
                    _currentDialogueId = nextId;
                }
            }
        };
    }

    // On utilise UnhandledInput pour ne pas déclencher le dialogue si on clique sur un bouton d'UI
    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isPlayerInRange && @event.IsActionPressed("Interaction"))
        {
            if (GetViewport().GuiGetFocusOwner() != null) return;
            
            GD.Print($"On tente une interaction avec {NpcName}");
            AdvanceDialogue();
        }
    }

    public void AdvanceDialogue()
    {
        // Si l'ID est null (on a fini le dialogue précédemment), on ferme
        if (string.IsNullOrWhiteSpace(_currentDialogueId))
        {
            FinishInteraction();
            return;
        }

        DialogueLine line = DialogueSystem.Instance.GetDialogue(_currentDialogueId);

        if (line != null)
        {
            GameManager.Instance.PlayerMoved = false;
            
            // On prépare le texte et on l'envoie à l'UI
            DialogueSystem.Instance.RequestDialogue(line);

            // Si c'est un choix, on s'arrête là (l'UI prend le relais)
            if (line.Type == DialogueType.CHOICE) return;
            
            // On prépare l'ID pour le PROCHAIN appui sur Interaction
            if (!string.IsNullOrWhiteSpace(line.NextId))
            {
                _currentDialogueId = line.NextId;
            }
            else
            {
                // Si pas de suite, on marque que le prochain appui fermera le dialogue
                _currentDialogueId = null; 
            }
        }
        else
        {
            FinishInteraction();
        }
    }
    
    private void FinishInteraction()
    {
        ResetDialogue();
        DialogueSystem.Instance.RequestDialogue(null); // Ferme l'UI
    }

    public void ResetDialogue()
    {
        _currentDialogueId = GetActiveStartDialogueId();
        GameManager.Instance.PlayerMoved = true;
    }

    /// <summary>
    /// Retourne le premier dialogue de ConditionalStartIds dont la condition est remplie.
    /// Retombe sur StartDialogueId si aucune condition ne passe.
    /// </summary>
    private string GetActiveStartDialogueId()
    {
        if (ConditionalStartIds is { Length: > 0 })
        {
            foreach (string condId in ConditionalStartIds)
            {
                var line = DialogueSystem.Instance?.GetDialogue(condId);
                if (line is null) continue;
                if (DialogueConditions.EvaluateAll(line.Condition))
                    return condId;
            }
        }
        return StartDialogueId;
    }

    void RefreshPlayerInRange()
    {
        var area = GetNodeOrNull<Area3D>("InteractionArea");
        if (area == null) return;

        foreach (var body in area.GetOverlappingBodies())
        {
            if (body.Name == "Player" || body.IsInGroup("Player"))
            {
                _isPlayerInRange = true;
                return;
            }
        }
    }
}