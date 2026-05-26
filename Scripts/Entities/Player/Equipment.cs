using System.Collections.Generic;
using Godot;

namespace EchoduKarma.Scripts.Entities.Player;

public enum EquipmentSlot
{
    Head,
    OffHand,
    Chest,
    Main,
    Legs,
    Accessory1,
    Accessory2,
}

public readonly struct EquipmentStatBonuses
{
    public int Strength { get; init; }
    public int Dexterity { get; init; }
    public int Spirit { get; init; }
    public int Defense { get; init; }

    public static EquipmentStatBonuses Zero => default;

    public EquipmentStatBonuses Add(EquipmentStatBonuses other) => new()
    {
        Strength = Strength + other.Strength,
        Dexterity = Dexterity + other.Dexterity,
        Spirit = Spirit + other.Spirit,
        Defense = Defense + other.Defense,
    };
}

public partial class Equipment : RefCounted
{
    public string Name { get; set; } = "";
    public EquipmentSlot Slot { get; set; }
    public string Type { get; set; } = "";
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Spirit { get; set; }
    public int Defense { get; set; }
    public string PassiveAbility { get; set; } = "";
    public List<string> Classes { get; set; } = new();
    public int Price { get; set; }

    public EquipmentStatBonuses StatBonuses => new()
    {
        Strength = Strength,
        Dexterity = Dexterity,
        Spirit = Spirit,
        Defense = Defense,
    };

    public bool IsUsableByClass(string playerClass)
    {
        if (Classes == null || Classes.Count == 0)
            return true;

        if (string.IsNullOrWhiteSpace(playerClass))
            return false;

        foreach (string cls in Classes)
        {
            if (cls.Trim().Equals(playerClass, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool TryParseSlot(string raw, out EquipmentSlot slot)
    {
        slot = EquipmentSlot.Main;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        return raw.Trim().ToLowerInvariant() switch
        {
            "head" or "tete" => Assign(EquipmentSlot.Head, out slot),
            "offhand" or "off_hand" or "secondemain" or "mg" => Assign(EquipmentSlot.OffHand, out slot),
            "chest" or "torse" or "haut" or "body" or "corps" => Assign(EquipmentSlot.Chest, out slot),
            "main" or "md" => Assign(EquipmentSlot.Main, out slot),
            "legs" or "jambes" or "bas" => Assign(EquipmentSlot.Legs, out slot),
            "accessory1" or "accessoire1" or "acc1" => Assign(EquipmentSlot.Accessory1, out slot),
            "accessory2" or "accessoire2" or "acc2" => Assign(EquipmentSlot.Accessory2, out slot),
            "accessory" or "accessoire" or "acc" => Assign(EquipmentSlot.Accessory1, out slot),
            _ => false,
        };
    }

    static bool Assign(EquipmentSlot value, out EquipmentSlot slot)
    {
        slot = value;
        return true;
    }

    public string GetSlotDisplayName() => Slot switch
    {
        EquipmentSlot.Head => "Tête",
        EquipmentSlot.OffHand => "Main gauche",
        EquipmentSlot.Chest => "Torse",
        EquipmentSlot.Main => "Main droite",
        EquipmentSlot.Legs => "Jambes",
        EquipmentSlot.Accessory1 => "Accessoire 1",
        EquipmentSlot.Accessory2 => "Accessoire 2",
        _ => Slot.ToString(),
    };

    public static string GetSlotHint(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.Head => "Tête",
        EquipmentSlot.OffHand => "M.G.",
        EquipmentSlot.Chest => "Torse",
        EquipmentSlot.Main => "M.D.",
        EquipmentSlot.Legs => "Jambes",
        EquipmentSlot.Accessory1 => "Acc.",
        EquipmentSlot.Accessory2 => "Acc.",
        _ => "?",
    };

    public string FormatStatSummary()
    {
        var parts = new List<string>();
        if (Strength != 0) parts.Add($"+{Strength} Force");
        if (Spirit != 0) parts.Add($"+{Spirit} Esprit");
        if (Dexterity != 0) parts.Add($"+{Dexterity} Agi");
        if (Defense != 0) parts.Add($"+{Defense} Déf");
        return parts.Count > 0 ? string.Join(" · ", parts) : "Aucun bonus";
    }
}
