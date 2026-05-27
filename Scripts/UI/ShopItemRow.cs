using EchoduKarma.Scripts.Entities.Player;
using Godot;

namespace EchoduKarma.Scripts.UI;

public partial class ShopItemRow : Button
{
    [Export] ColorRect _accentBar;
    [Export] Label _nameLabel;
    [Export] Label _typeBadge;
    [Export] Label _slotLabel;
    [Export] Label _statsLabel;
    [Export] Label _priceLabel;

    static readonly Color ColorAvailable = new(0.55f, 0.75f, 0.92f);
    static readonly Color ColorOwned = new(0.48f, 0.9f, 0.51f);
    static readonly Color ColorBlocked = new(0.53f, 0.6f, 0.67f);
    static readonly Color ColorTooExpensive = new(0.91f, 0.36f, 0.36f);

    public Equipment Equipment { get; private set; }
    public int Price { get; private set; }

    public void Bind(Equipment equipment, int price, bool canBuy, bool alreadyOwned, bool tooExpensive)
    {
        Equipment = equipment;
        Price = price;
        Text = string.Empty;

        _nameLabel.Text = equipment.Name;
        _typeBadge.Text = equipment.Type;
        _slotLabel.Text = equipment.GetSlotDisplayName();
        _statsLabel.Text = equipment.FormatStatSummary();
        _priceLabel.Text = price > 0 ? $"{price} or" : "Gratuit";

        if (alreadyOwned)
        {
            _accentBar.Color = ColorOwned;
            _priceLabel.Text = "Possédé";
            _priceLabel.AddThemeColorOverride("font_color", ColorOwned);
        }
        else if (!canBuy)
        {
            _accentBar.Color = ColorBlocked;
            _priceLabel.AddThemeColorOverride("font_color", ColorBlocked);
        }
        else if (tooExpensive)
        {
            _accentBar.Color = ColorTooExpensive;
            _priceLabel.AddThemeColorOverride("font_color", ColorTooExpensive);
        }
        else
        {
            _accentBar.Color = ColorAvailable;
            _priceLabel.AddThemeColorOverride("font_color", new Color(1f, 0.82f, 0.4f));
        }

        _typeBadge.AddThemeColorOverride("font_color", ColorAvailable);
    }
}
