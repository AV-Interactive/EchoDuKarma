using System;
using System.Collections.Generic;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

public static class UiIcons
{
    static readonly Dictionary<string, Texture2D> Cache = new();

    public const string Strength = "res://Assets/UI/stats/strength.png";
    public const string Spirit = "res://Assets/UI/stats/spirit.png";
    public const string Agility = "res://Assets/UI/stats/agility.png";
    public const string Defense = "res://Assets/UI/stats/defense.png";
    public const string Karma = "res://Assets/UI/stats/karma.png";
    public const string Pocket = "res://Assets/UI/pocket.png";
    public const string Arrow = "res://Assets/UI/arrow.png";
    public const string MagusStaff = "res://Assets/UI/weapons/staff.png";
    public const string HealStaff = "res://Assets/UI/weapons/heal_staff.png";
    public const string Sword = "res://Assets/UI/weapons/sword.png";
    public const string MagusHat = "res://Assets/UI/armors/magus_hat.png";
    public const string MagusDress = "res://Assets/UI/armors/magus_dress.png";
    public const string MagusCape = "res://Assets/UI/armors/magus_cape.png";
    public const string Hand = "res://Assets/UI/armors/hand.png";
    public const string Ring = "res://Assets/UI/accessories/ring.png";
    public const string Skin = "res://Assets/UI/resources/skin.png";

    public static Texture2D GetItemIcon(string itemName)
    {
        Equipment equipment = EquipmentManager.GetEquipment(itemName);
        if (equipment != null)
            return GetEquipmentIcon(equipment);

        ResourceItem resource = ResourceItemManager.GetResource(itemName);
        if (resource != null)
            return Load(ResourceItemManager.ResolveIconPath(resource));

        return Load(Pocket);
    }

    public static Texture2D Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (Cache.TryGetValue(path, out Texture2D cached))
            return cached;

        Texture2D texture = GD.Load<Texture2D>(path);
        if (texture != null)
            Cache[path] = texture;

        return texture;
    }

    public static Texture2D GetEquipmentSlotIcon(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.Head => Load(MagusHat),
        EquipmentSlot.OffHand => Load(Hand),
        EquipmentSlot.Chest => Load(MagusCape),
        EquipmentSlot.Main => Load(Hand),
        EquipmentSlot.Legs => Load(MagusDress),
        EquipmentSlot.Accessory1 => Load(Ring),
        EquipmentSlot.Accessory2 => Load(Ring),
        _ => null,
    };

    public static Texture2D GetEquipmentIcon(Equipment equipment)
    {
        if (equipment == null)
            return null;

        if (equipment.Type.Contains("Arme", StringComparison.OrdinalIgnoreCase))
        {
            if (equipment.Spirit >= equipment.Strength)
                return Load(MagusStaff);

            return Load(Sword);
        }

        return GetEquipmentSlotIcon(equipment.Slot);
    }

    public static void Apply(TextureRect target, Texture2D texture)
    {
        if (target == null)
            return;

        target.Texture = texture;
        target.Visible = texture != null;
    }
}
