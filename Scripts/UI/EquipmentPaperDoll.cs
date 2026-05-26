using System;
using System.Collections.Generic;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

public partial class EquipmentPaperDoll : PanelContainer
{
    static readonly Vector2 DefaultSlotSize = new(54, 54);
    static readonly Vector2 TorsoSlotSize = new(58, 54);
    static readonly Vector2 DefaultIconSize = new(26, 26);
    static readonly Vector2 TorsoIconSize = new(28, 28);
    [Export] InventorySlotCell _headSlot;
    [Export] InventorySlotCell _offHandSlot;
    [Export] InventorySlotCell _chestSlot;
    [Export] InventorySlotCell _mainSlot;
    [Export] InventorySlotCell _legsSlot;
    [Export] InventorySlotCell _accessory1Slot;
    [Export] InventorySlotCell _accessory2Slot;

    readonly Dictionary<EquipmentSlot, InventorySlotCell> _cells = new();

    public event Action<Equipment, bool> SlotSelected;

    public override void _Ready()
    {
        RegisterCell(EquipmentSlot.Head, _headSlot);
        RegisterCell(EquipmentSlot.OffHand, _offHandSlot);
        RegisterCell(EquipmentSlot.Chest, _chestSlot);
        RegisterCell(EquipmentSlot.Main, _mainSlot);
        RegisterCell(EquipmentSlot.Legs, _legsSlot);
        RegisterCell(EquipmentSlot.Accessory1, _accessory1Slot);
        RegisterCell(EquipmentSlot.Accessory2, _accessory2Slot);
    }

    void RegisterCell(EquipmentSlot slot, InventorySlotCell cell)
    {
        if (cell is null)
            return;

        _cells[slot] = cell;
        ApplyPaperDollDimensions(slot, cell);
        cell.Pressed += () => OnCellPressed(slot, cell);
    }

    void ApplyPaperDollDimensions(EquipmentSlot slot, InventorySlotCell cell)
    {
        bool isTorso = slot is EquipmentSlot.Chest or EquipmentSlot.Main or EquipmentSlot.OffHand;
        Vector2 cellSize = isTorso ? TorsoSlotSize : DefaultSlotSize;
        Vector2 iconSize = isTorso ? TorsoIconSize : DefaultIconSize;
        cell.ApplyDimensions(cellSize, iconSize, hintFontSize: 9);
    }

    void OnCellPressed(EquipmentSlot slot, InventorySlotCell cell)
    {
        if (cell.Equipment is null)
            return;

        SlotSelected?.Invoke(cell.Equipment, true);
    }

    public void Refresh()
    {
        foreach (var (slot, cell) in _cells)
        {
            Equipment equipment = InventoryManager.Instance?.GetEquipped(slot);
            cell.BindPaperDollSlot(slot, equipment);
        }
    }

    public IEnumerable<InventorySlotCell> GetInteractiveCells()
    {
        foreach (var cell in _cells.Values)
        {
            if (cell.Equipment is not null)
                yield return cell;
        }
    }
}
