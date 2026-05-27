using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

public partial class InventorySlotCell : Button
{
    static readonly Color ColorEquipped = new(0.48f, 0.9f, 0.51f);
    static readonly Color ColorAvailable = new(0.55f, 0.75f, 0.92f);
    static readonly Color ColorLocked = new(0.53f, 0.6f, 0.67f);
    static readonly Color ColorEmpty = new(0.4f, 0.46f, 0.54f);

    [Export] TextureRect _icon;
    [Export] Label _hintLabel;
    [Export] Label _itemLabel;

    public EquipmentSlot? PaperDollSlot { get; private set; }
    public Equipment Equipment { get; private set; }
    public bool IsEquippedCell { get; private set; }

    public override void _Ready()
    {
        if (_itemLabel != null)
        {
            _itemLabel.AddThemeFontSizeOverride("font_size", 7);
            _itemLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            _itemLabel.AutowrapMode = TextServer.AutowrapMode.Off;
            _itemLabel.ClipText = true;
        }
    }

    public void ApplyDimensions(Vector2 cellSize, Vector2 iconSize, int hintFontSize = 8, int itemFontSize = 7)
    {
        CustomMinimumSize = cellSize;
        if (_icon != null)
            _icon.CustomMinimumSize = iconSize;
        if (_hintLabel != null)
            _hintLabel.AddThemeFontSizeOverride("font_size", hintFontSize);
        if (_itemLabel != null)
        {
            _itemLabel.AddThemeFontSizeOverride("font_size", itemFontSize);
            _itemLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            _itemLabel.AutowrapMode = TextServer.AutowrapMode.Off;
            _itemLabel.ClipText = true;
        }
    }

    public void BindPaperDollSlot(EquipmentSlot slot, Equipment equipment)
    {
        PaperDollSlot = slot;
        Equipment = equipment;
        IsEquippedCell = true;
        Text = string.Empty;
        _itemLabel.Visible = false;

        _hintLabel.Text = Equipment.GetSlotHint(slot);
        _hintLabel.AddThemeColorOverride("font_color", ColorEmpty);

        if (equipment is null)
        {
            Disabled = true;
            UiIcons.Apply(_icon, UiIcons.GetEquipmentSlotIcon(slot));
            _icon.Modulate = new Color(1f, 1f, 1f, 0.45f);
            _itemLabel.Text = string.Empty;
            _itemLabel.AddThemeColorOverride("font_color", ColorEmpty);
            Modulate = new Color(0.85f, 0.85f, 0.85f, 0.65f);
            return;
        }

        Disabled = false;
        UiIcons.Apply(_icon, UiIcons.GetEquipmentIcon(equipment));
        _icon.Modulate = Colors.White;
        _itemLabel.Text = string.Empty;
        _itemLabel.AddThemeColorOverride("font_color", ColorEquipped);
        Modulate = Colors.White;
    }

    public void BindResourceItem(ResourceItem resource)
    {
        PaperDollSlot = null;
        Equipment = null;
        IsEquippedCell = false;
        Text = string.Empty;
        Disabled = false;
        _itemLabel.Visible = true;

        UiIcons.Apply(_icon, UiIcons.GetItemIcon(resource.Name));
        _icon.Modulate = Colors.White;

        _hintLabel.Text = resource.Type;
        _hintLabel.AddThemeColorOverride("font_color", ColorAvailable);

        _itemLabel.Text = Truncate(resource.Name, 9);
        _itemLabel.AddThemeColorOverride("font_color", Colors.White);
        Modulate = Colors.White;
    }
    public void BindInventoryItem(Equipment equipment, bool canEquip)
    {
        PaperDollSlot = null;
        Equipment = equipment;
        IsEquippedCell = false;
        Text = string.Empty;
        Disabled = false;
        _itemLabel.Visible = true;

        UiIcons.Apply(_icon, UiIcons.GetEquipmentIcon(equipment));
        _icon.Modulate = canEquip ? Colors.White : new Color(0.7f, 0.7f, 0.7f, 0.85f);

        _hintLabel.Text = equipment.Type;
        _hintLabel.AddThemeColorOverride("font_color", canEquip ? ColorAvailable : ColorLocked);

        _itemLabel.Text = Truncate(equipment.Name, 9);
        _itemLabel.AddThemeColorOverride("font_color", canEquip ? Colors.White : ColorLocked);
        Modulate = Colors.White;
    }

    public void BindShopItem(Equipment equipment, int price, int ownedCount, bool canBuy, bool tooExpensive)
    {
        PaperDollSlot = null;
        Equipment = equipment;
        IsEquippedCell = false;
        Text = string.Empty;
        Disabled = false;
        _itemLabel.Visible = true;

        UiIcons.Apply(_icon, UiIcons.GetEquipmentIcon(equipment));
        Modulate = Colors.White;

        string priceText = price > 0 ? $"{price} or" : "Gratuit";
        if (ownedCount > 0)
            _hintLabel.Text = ownedCount > 1 ? $"x{ownedCount} · {priceText}" : $"x1 · {priceText}";
        else
            _hintLabel.Text = priceText;

        _itemLabel.Text = Truncate(equipment.Name, 9);

        if (!canBuy && !tooExpensive)
        {
            _hintLabel.AddThemeColorOverride("font_color", ColorLocked);
            _itemLabel.AddThemeColorOverride("font_color", ColorLocked);
            _icon.Modulate = new Color(0.7f, 0.7f, 0.7f, 0.85f);
        }
        else if (tooExpensive)
        {
            _hintLabel.AddThemeColorOverride("font_color", new Color(0.91f, 0.36f, 0.36f));
            _itemLabel.AddThemeColorOverride("font_color", new Color(0.91f, 0.36f, 0.36f));
            _icon.Modulate = Colors.White;
        }
        else
        {
            _hintLabel.AddThemeColorOverride("font_color", new Color(1f, 0.82f, 0.4f));
            _itemLabel.AddThemeColorOverride("font_color", Colors.White);
            _icon.Modulate = Colors.White;
        }
    }

    public void BindShopSellItem(Equipment equipment, int sellPrice, bool canSell)
    {
        PaperDollSlot = null;
        Equipment = equipment;
        IsEquippedCell = false;
        Text = string.Empty;
        Disabled = false;
        _itemLabel.Visible = true;

        UiIcons.Apply(_icon, UiIcons.GetEquipmentIcon(equipment));
        Modulate = Colors.White;
        _itemLabel.Text = Truncate(equipment.Name, 9);

        if (!canSell || sellPrice <= 0)
        {
            _hintLabel.Text = "—";
            _hintLabel.AddThemeColorOverride("font_color", ColorLocked);
            _itemLabel.AddThemeColorOverride("font_color", ColorLocked);
            _icon.Modulate = new Color(0.7f, 0.7f, 0.7f, 0.85f);
            return;
        }

        _hintLabel.Text = $"+{sellPrice} or";
        _hintLabel.AddThemeColorOverride("font_color", ColorEquipped);
        _itemLabel.AddThemeColorOverride("font_color", Colors.White);
        _icon.Modulate = Colors.White;
    }

    static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..(maxLength - 1)] + "…";
    }
}
