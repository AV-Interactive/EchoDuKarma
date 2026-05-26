using System;
using System.Collections.Generic;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

public partial class InventoryManager : Node
{
    public static InventoryManager Instance { get; private set; }

    [Signal] public delegate void InventoryChangedEventHandler();
    [Signal] public delegate void GoldChangedEventHandler(int newAmount);
    [Signal] public delegate void EquipmentChangedEventHandler();
    [Signal] public delegate void ItemAcquiredEventHandler(string itemName);

    readonly List<string> _inventory = new();
    readonly Dictionary<EquipmentSlot, string> _equipped = new();

    public int Gold { get; private set; }
    public string PlayerClass { get; set; } = "Magus";

    public override void _Ready()
    {
        Instance = this;
        _ = EquipmentManager.Catalog;
        _ = ResourceItemManager.Catalog;
        GrantStartingLoadout();
        GD.Print("[AUTOLOAD] InventoryManager Ready.");
    }

    void GrantStartingLoadout()
    {
        if (_inventory.Count > 0 || _equipped.Count > 0)
            return;

        AddEquipment("Bâton Commun", notify: false);
        Equip("Bâton Commun");
    }

    public IReadOnlyList<string> GetInventoryItems() => _inventory;

    public IEnumerable<(EquipmentSlot Slot, Equipment Equipment)> GetEquippedItems()
    {
        foreach (var pair in _equipped)
        {
            Equipment equipment = EquipmentManager.GetEquipment(pair.Value);
            if (equipment != null)
                yield return (pair.Key, equipment);
        }
    }

    public Equipment GetEquipped(EquipmentSlot slot)
    {
        if (!_equipped.TryGetValue(slot, out string name))
            return null;

        return EquipmentManager.GetEquipment(name);
    }

    public EquipmentStatBonuses GetEquipmentBonuses()
    {
        var total = EquipmentStatBonuses.Zero;
        foreach (var (_, equipment) in GetEquippedItems())
            total = total.Add(equipment.StatBonuses);
        return total;
    }

    public bool AddGold(int amount)
    {
        if (amount <= 0)
            return false;

        Gold += amount;
        EmitSignal(SignalName.GoldChanged, Gold);
        GD.Print($"[InventoryManager] +{amount} or (total : {Gold}).");
        return true;
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || Gold < amount)
            return false;

        Gold -= amount;
        EmitSignal(SignalName.GoldChanged, Gold);
        return true;
    }

    public bool AddEquipment(string equipmentName, bool notify = true)
    {
        Equipment equipment = EquipmentManager.GetEquipment(equipmentName);
        if (equipment == null)
        {
            GD.PrintErr($"[InventoryManager] Équipement inconnu : '{equipmentName}'.");
            return false;
        }

        if (_inventory.Contains(equipment.Name))
        {
            GD.Print($"[InventoryManager] '{equipment.Name}' déjà dans l'inventaire.");
            return false;
        }

        _inventory.Add(equipment.Name);
        EmitSignal(SignalName.InventoryChanged);
        if (notify)
            EmitSignal(SignalName.ItemAcquired, equipment.Name);
        GD.Print($"[InventoryManager] Objet ajouté : {equipment.Name}.");
        return true;
    }

    public bool TryAddItem(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return false;

        string name = itemName.Trim();
        if (EquipmentManager.GetEquipment(name) != null)
            return AddEquipment(name);

        if (ResourceItemManager.GetResource(name) != null)
            return AddResource(name);

        GD.PrintErr($"[InventoryManager] Objet inconnu : '{name}'.");
        return false;
    }

    public bool AddResource(string resourceName, bool notify = true)
    {
        ResourceItem resource = ResourceItemManager.GetResource(resourceName);
        if (resource == null)
        {
            GD.PrintErr($"[InventoryManager] Ressource inconnue : '{resourceName}'.");
            return false;
        }

        if (_inventory.Contains(resource.Name))
        {
            GD.Print($"[InventoryManager] '{resource.Name}' déjà dans l'inventaire.");
            return false;
        }

        _inventory.Add(resource.Name);
        EmitSignal(SignalName.InventoryChanged);
        if (notify)
            EmitSignal(SignalName.ItemAcquired, resource.Name);
        GD.Print($"[InventoryManager] Ressource ajoutée : {resource.Name}.");
        return true;
    }

    public bool OwnsItem(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return false;

        return _inventory.Contains(itemName.Trim());
    }

    public bool OwnsEquipment(string equipmentName)
    {
        if (string.IsNullOrWhiteSpace(equipmentName))
            return false;

        string name = equipmentName.Trim();
        if (_inventory.Contains(name))
            return true;

        foreach (string equippedName in _equipped.Values)
        {
            if (equippedName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public bool Equip(string equipmentName)
    {
        Equipment equipment = EquipmentManager.GetEquipment(equipmentName);
        if (equipment == null)
            return false;

        if (!_inventory.Contains(equipment.Name))
        {
            GD.PrintErr($"[InventoryManager] '{equipment.Name}' absent de l'inventaire.");
            return false;
        }

        if (!equipment.IsUsableByClass(PlayerClass))
        {
            GD.Print($"[InventoryManager] '{equipment.Name}' incompatible avec la classe {PlayerClass}.");
            return false;
        }

        if (_equipped.TryGetValue(equipment.Slot, out string previousName))
            Unequip(equipment.Slot);

        _inventory.Remove(equipment.Name);
        _equipped[equipment.Slot] = equipment.Name;

        EmitSignal(SignalName.InventoryChanged);
        EmitSignal(SignalName.EquipmentChanged);
        GD.Print($"[InventoryManager] Équipé : {equipment.Name} ({equipment.GetSlotDisplayName()}).");
        return true;
    }

    public bool Unequip(EquipmentSlot slot)
    {
        if (!_equipped.TryGetValue(slot, out string equipmentName))
            return false;

        _equipped.Remove(slot);
        if (!_inventory.Contains(equipmentName))
            _inventory.Add(equipmentName);

        EmitSignal(SignalName.InventoryChanged);
        EmitSignal(SignalName.EquipmentChanged);
        GD.Print($"[InventoryManager] Déséquipé : {equipmentName}.");
        return true;
    }

    public bool IsEquipped(string equipmentName) =>
        _equipped.ContainsValue(equipmentName);

    public bool CanEquip(Equipment equipment)
    {
        if (equipment == null)
            return false;

        return _inventory.Contains(equipment.Name) && equipment.IsUsableByClass(PlayerClass);
    }
}
