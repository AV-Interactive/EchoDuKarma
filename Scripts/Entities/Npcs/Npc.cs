using Godot;
using System;
using EchoduKarma.Scripts.Data;

public partial class Npc : CharacterBody3D
{
    [Export] public string NpcName;
    [Export] public Texture2D SpriteTexture;
    [Export] public string StartDialogueId;
    
    private Sprite3D _sprite;
    private bool _isPlayerInRange = false;
    private string _currentDialogueId;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite3D>("Node3D/Sprite3D");
        _sprite.Texture = SpriteTexture;
        _currentDialogueId = StartDialogueId;
        
        // On récupère l'Area3D pour la détection 3D
        var area = GetNode<Area3D>("InteractionArea");
        
        area.BodyEntered += (body) =>
        {
            // Note: Vérifie si ton joueur possède un script "Player" ou utilise les Groupes
            if (body.Name == "Player" || body.IsInGroup("Player"))
            {
                _isPlayerInRange = true;
                GD.Print($"Toine, le joueur est proche de {NpcName}");
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
        _currentDialogueId = StartDialogueId;
        GameManager.Instance.PlayerMoved = true;
    }
}