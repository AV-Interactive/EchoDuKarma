using Godot;
using EchoduKarma.Scripts.Data;

/// <summary>
/// Zone ou point d'interaction 3D qui démarre une quête.
/// À placer dans la map : Area3D invisible avec QuestId renseigné dans l'inspecteur.
/// </summary>
public partial class QuestTrigger : Area3D
{
    public enum TriggerMode
    {
        /// <summary>Démarre la quête dès que le joueur entre dans la zone.</summary>
        OnEnter,
        /// <summary>Démarre la quête quand le joueur appuie sur Interaction (E / manette).</summary>
        OnInteract,
    }

    [Export] public string QuestId;
    [Export] public TriggerMode Mode = TriggerMode.OnEnter;
    [Export] public bool OneShot = true;

    bool _playerInRange;
    bool _hasTriggered;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
        CallDeferred(nameof(RefreshPlayerInRange));
    }

    void OnBodyEntered(Node3D body)
    {
        if (!IsPlayer(body))
            return;

        _playerInRange = true;

        if (Mode == TriggerMode.OnEnter)
            TryStartQuest();
    }

    void OnBodyExited(Node3D body)
    {
        if (!IsPlayer(body))
            return;

        _playerInRange = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Mode != TriggerMode.OnInteract)
            return;

        if (!_playerInRange || !@event.IsActionPressed("Interaction"))
            return;

        if (GetViewport().GuiGetFocusOwner() != null)
            return;

        TryStartQuest();
    }

    void TryStartQuest()
    {
        if (OneShot && _hasTriggered)
            return;

        if (GameManager.Instance is not { CanInteractWithWorld: true })
            return;

        if (string.IsNullOrWhiteSpace(QuestId))
        {
            GD.PrintErr("[QuestTrigger] QuestId non renseigné sur le nœud.");
            return;
        }

        if (QuestManager.Instance is null)
            return;

        QuestStatus status = QuestManager.Instance.GetStatus(QuestId);
        if (status != QuestStatus.Inactive)
        {
            if (OneShot)
                DisableTrigger();
            return;
        }

        QuestManager.Instance.StartQuest(QuestId);
        _hasTriggered = true;

        if (OneShot)
            DisableTrigger();
    }

    void DisableTrigger() => SetDeferred(Area3D.PropertyName.Monitoring, false);

    void RefreshPlayerInRange()
    {
        foreach (var body in GetOverlappingBodies())
        {
            if (!IsPlayer(body))
                continue;

            _playerInRange = true;
            if (Mode == TriggerMode.OnEnter)
                TryStartQuest();
            return;
        }
    }

    static bool IsPlayer(Node body)
        => body.Name == "Player" || body.IsInGroup("Player");
}
