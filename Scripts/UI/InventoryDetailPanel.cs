using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

public partial class InventoryDetailPanel : PanelContainer
{
    [Export] TextureRect _icon;
    [Export] RichTextLabel _nameLabel;
    [Export] RichTextLabel _metaLabel;
    [Export] RichTextLabel _statsLabel;
    [Export] RichTextLabel _passiveLabel;
    [Export] RichTextLabel _priceLabel;

    public void SetResource(ResourceItem resource)
    {
        UiIcons.Apply(_icon, UiIcons.GetItemIcon(resource.Name));

        _nameLabel.Text = $"[b]{resource.Name}[/b]";
        _metaLabel.Text = $"[color=#58B4C6]{resource.Type}[/color]  ·  [color=#8899AA]Matériau[/color]";
        _statsLabel.Text = "[color=#8899AA]Non équipable[/color]";
        _passiveLabel.Text = string.IsNullOrWhiteSpace(resource.Description)
            ? "[color=#8899AA]Aucune description[/color]"
            : resource.Description;
        _priceLabel.Text = "[color=#8899AA]Valeur : —[/color]";
    }

    public void SetEquipment(Equipment equipment, bool isEquipped, bool canEquip)
    {
        UiIcons.Apply(_icon, UiIcons.GetEquipmentIcon(equipment));

        _nameLabel.Text = $"[b]{equipment.Name}[/b]";
        _metaLabel.Text =
            $"[color=#58B4C6]{equipment.Type}[/color]  ·  {equipment.GetSlotDisplayName()}  ·  " +
            (isEquipped
                ? "[color=#7AE582]Équipé[/color]"
                : canEquip
                    ? "[color=#8CBFEA]Disponible[/color]"
                    : "[color=#8899AA]Incompatible[/color]");

        _statsLabel.Text = FormatStats(equipment);
        _passiveLabel.Text = string.IsNullOrWhiteSpace(equipment.PassiveAbility)
            ? "[color=#8899AA]Capacité passive : aucune[/color]"
            : $"[color=#FFD166]Passif :[/color] {equipment.PassiveAbility}";

        _priceLabel.Text = equipment.Price > 0
            ? $"[color=#FFD166]Valeur :[/color] {equipment.Price} or"
            : "[color=#8899AA]Valeur : —[/color]";
    }

    public void SetShopOffer(Equipment equipment, int price, bool canBuy, string blockReason)
    {
        UiIcons.Apply(_icon, UiIcons.GetEquipmentIcon(equipment));

        _nameLabel.Text = $"[b]{equipment.Name}[/b]";
        _metaLabel.Text =
            $"[color=#58B4C6]{equipment.Type}[/color]  ·  {equipment.GetSlotDisplayName()}  ·  " +
            (canBuy
                ? "[color=#7AE582]Disponible[/color]"
                : "[color=#E85D5D]Indisponible[/color]");

        _statsLabel.Text = FormatStats(equipment);
        _passiveLabel.Text = string.IsNullOrWhiteSpace(equipment.PassiveAbility)
            ? "[color=#8899AA]Capacité passive : aucune[/color]"
            : $"[color=#FFD166]Passif :[/color] {equipment.PassiveAbility}";

        string priceText = price > 0
            ? $"[color=#FFD166]Prix :[/color] {price} or"
            : "[color=#7AE582]Gratuit[/color]";

        if (!canBuy && !string.IsNullOrWhiteSpace(blockReason))
            priceText += $"\n[color=#E85D5D]{blockReason}[/color]";

        _priceLabel.Text = priceText;
    }

    static string FormatStats(Equipment equipment)
    {
        string summary = equipment.FormatStatSummary();
        return summary == "Aucun bonus"
            ? "[color=#8899AA]Aucun bonus de stats[/color]"
            : $"[color=#FFD166]Bonus[/color]  ·  {summary}";
    }
}
