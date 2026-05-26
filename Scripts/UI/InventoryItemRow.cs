using EchoduKarma.Scripts.Entities.Player;
using Godot;

public partial class InventoryItemRow : Button
{
    [Export] ColorRect _accentBar;
    [Export] Label _nameLabel;
    [Export] Label _typeBadge;
    [Export] Label _slotLabel;
    [Export] Label _statsLabel;
    [Export] Label _statusLabel;

    static readonly Color ColorEquipped = new(0.48f, 0.9f, 0.51f);
    static readonly Color ColorAvailable = new(0.55f, 0.75f, 0.92f);
    static readonly Color ColorLocked = new(0.53f, 0.6f, 0.67f);

    public string EquipmentName { get; private set; }
    public bool IsEquippedRow { get; private set; }

    public void Bind(Equipment equipment, bool isEquipped, bool canEquip)
    {
        EquipmentName = equipment.Name;
        IsEquippedRow = isEquipped;
        Text = string.Empty;

        _nameLabel.Text = equipment.Name;
        _typeBadge.Text = equipment.Type;
        _slotLabel.Text = equipment.GetSlotDisplayName();
        _statsLabel.Text = equipment.FormatStatSummary();

        if (isEquipped)
        {
            _accentBar.Color = ColorEquipped;
            _statusLabel.Text = "Équipé";
            _statusLabel.AddThemeColorOverride("font_color", ColorEquipped);
        }
        else if (canEquip)
        {
            _accentBar.Color = ColorAvailable;
            _statusLabel.Text = "Disponible";
            _statusLabel.AddThemeColorOverride("font_color", ColorAvailable);
        }
        else
        {
            _accentBar.Color = ColorLocked;
            _statusLabel.Text = "Incompatible";
            _statusLabel.AddThemeColorOverride("font_color", ColorLocked);
        }

        _typeBadge.AddThemeColorOverride("font_color", isEquipped
            ? new Color(1f, 0.85f, 0.55f)
            : ColorAvailable);
    }
}
