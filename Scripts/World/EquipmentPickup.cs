using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

/// <summary>
/// Objet ramassable au sol : le joueur appuie sur Interaction (Espace) dans la zone.
/// </summary>
public partial class EquipmentPickup : Area3D
{
    [Export] public string ItemName = "";
    [Export] public bool OneShot = true;

    bool _playerInRange;
    Sprite3D _sprite;

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<Sprite3D>("Visual");
        ApplyEquipmentVisual();

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
        CallDeferred(nameof(RefreshPlayerInRange));

        if (OneShot && InventoryManager.Instance?.OwnsItem(ItemName) == true)
            DisablePickup();
    }

    void ApplyEquipmentVisual()
    {
        if (_sprite == null)
            return;

        Texture2D icon = UiIcons.GetItemIcon(ItemName);
        if (icon == null)
            return;

        _sprite.Texture = icon;
        _sprite.PixelSize = 0.01f;
        _sprite.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
    }

    void OnBodyEntered(Node3D body)
    {
        if (!IsPlayer(body))
            return;

        _playerInRange = true;
    }

    void OnBodyExited(Node3D body)
    {
        if (!IsPlayer(body))
            return;

        _playerInRange = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_playerInRange || !@event.IsActionPressed("Interaction"))
            return;

        if (GetViewport().GuiGetFocusOwner() != null)
            return;

        if (GameManager.Instance is not { CanInteractWithWorld: true })
            return;

        TryPickup();
    }

    void TryPickup()
    {
        if (string.IsNullOrWhiteSpace(ItemName))
        {
            GD.PrintErr("[EquipmentPickup] ItemName non renseigné.");
            return;
        }

        if (InventoryManager.Instance is null)
            return;

        if (InventoryManager.Instance.OwnsItem(ItemName))
        {
            GD.Print($"[EquipmentPickup] '{ItemName}' déjà possédé.");
            if (OneShot)
                DisablePickup();
            return;
        }

        if (!InventoryManager.Instance.TryAddItem(ItemName))
            return;

        GD.Print($"[EquipmentPickup] Objet ramassé : {ItemName}.");

        if (OneShot)
            DisablePickup();
    }

    void DisablePickup()
    {
        SetDeferred(PropertyName.Monitoring, false);
        Visible = false;
    }

    void RefreshPlayerInRange()
    {
        foreach (var body in GetOverlappingBodies())
        {
            if (!IsPlayer(body))
                continue;

            _playerInRange = true;
            return;
        }
    }

    static bool IsPlayer(Node body)
        => body.Name == "Player" || body.IsInGroup("Player");
}
