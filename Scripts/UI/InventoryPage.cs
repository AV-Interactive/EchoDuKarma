using System.Collections.Generic;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

namespace EchoduKarma.Scripts.UI;

public partial class InventoryPage : Control, IGameMenuTabPage
{
    const string CellScenePath = "res://UI/InventorySlotCell.tscn";

    [Export] Control _listView;
    [Export] EquipmentPaperDoll _paperDoll;
    [Export] GridContainer _inventoryGrid;
    [Export] Label _emptyLabel;
    [Export] Label _goldLabel;
    [Export] Label _itemCountLabel;
    [Export] Control _detailView;
    [Export] InventoryDetailPanel _detailPanel;
    [Export] Button _backButton;
    [Export] Button _actionButton;

    readonly List<InventorySlotCell> _cells = new();
    Equipment _selectedEquipment;
    ResourceItem _selectedResource;
    bool _selectedIsEquipped;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;

        _backButton.Pressed += ShowList;
        _actionButton.Pressed += OnActionPressed;

        if (_paperDoll is not null)
            _paperDoll.SlotSelected += OnPaperDollSlotSelected;

        if (InventoryManager.Instance is not null)
        {
            InventoryManager.Instance.InventoryChanged += OnInventoryChanged;
            InventoryManager.Instance.GoldChanged += OnGoldChanged;
            InventoryManager.Instance.EquipmentChanged += OnInventoryChanged;
        }
    }

    public override void _ExitTree()
    {
        if (_paperDoll is not null)
            _paperDoll.SlotSelected -= OnPaperDollSlotSelected;

        if (InventoryManager.Instance is not null)
        {
            InventoryManager.Instance.InventoryChanged -= OnInventoryChanged;
            InventoryManager.Instance.GoldChanged -= OnGoldChanged;
            InventoryManager.Instance.EquipmentChanged -= OnInventoryChanged;
        }

        base._ExitTree();
    }

    void OnPaperDollSlotSelected(Equipment equipment, bool isEquipped) =>
        ShowDetail(equipment, isEquipped);

    void OnInventoryChanged() => RefreshIfVisible();
    void OnGoldChanged(int _) => RefreshIfVisible();

    void RefreshIfVisible()
    {
        if (Visible)
            Refresh();
    }

    public void OnTabShown()
    {
        Visible = true;
        ShowList();
        Refresh();
        CallDeferred(MethodName.FocusDefault);
    }

    public void OnTabHidden()
    {
        Visible = false;
        ShowList();
    }

    public void FocusDefault() => FocusList();

    public bool TryHandleCancel()
    {
        if (_detailView.Visible)
        {
            ShowList();
            return true;
        }

        return false;
    }

    void ShowList()
    {
        _listView.Visible = true;
        _detailView.Visible = false;
        _selectedEquipment = null;
        _selectedResource = null;
        if (Visible)
            CallDeferred(MethodName.FocusList);
    }

    void ShowDetail(Equipment equipment, bool isEquipped)
    {
        if (equipment is null)
            return;

        _selectedEquipment = equipment;
        _selectedResource = null;
        _selectedIsEquipped = isEquipped;

        bool canEquip = !isEquipped && InventoryManager.Instance?.CanEquip(equipment) == true;
        _detailPanel.SetEquipment(equipment, isEquipped, canEquip);

        if (isEquipped)
        {
            _actionButton.Text = "Déséquiper";
            _actionButton.Disabled = false;
        }
        else if (canEquip)
        {
            _actionButton.Text = "Équiper";
            _actionButton.Disabled = false;
        }
        else
        {
            _actionButton.Text = "Incompatible";
            _actionButton.Disabled = true;
        }

        _listView.Visible = false;
        _detailView.Visible = true;
        _actionButton.GrabFocus();
    }

    void ShowResourceDetail(ResourceItem resource)
    {
        if (resource is null)
            return;

        _selectedEquipment = null;
        _selectedResource = resource;
        _selectedIsEquipped = false;

        _detailPanel.SetResource(resource);
        _actionButton.Text = "Matériau";
        _actionButton.Disabled = true;

        _listView.Visible = false;
        _detailView.Visible = true;
        _backButton.GrabFocus();
    }

    void OnActionPressed()
    {
        if (_selectedEquipment is null || InventoryManager.Instance is null)
            return;

        if (_selectedIsEquipped)
            InventoryManager.Instance.Unequip(_selectedEquipment.Slot);
        else
            InventoryManager.Instance.Equip(_selectedEquipment.Name);

        ShowList();
        Refresh();
    }

    void FocusList()
    {
        if (_cells.Count > 0)
        {
            _cells[0].GrabFocus();
            return;
        }

        if (_paperDoll is not null)
        {
            foreach (InventorySlotCell cell in _paperDoll.GetInteractiveCells())
            {
                cell.GrabFocus();
                return;
            }
        }
    }

    void Refresh()
    {
        ClearCells();
        _paperDoll?.Refresh();

        if (InventoryManager.Instance is null)
        {
            _emptyLabel.Visible = true;
            _goldLabel.Text = "0 or";
            if (_itemCountLabel != null)
                _itemCountLabel.Text = "0";
            return;
        }

        _goldLabel.Text = $"{InventoryManager.Instance.Gold} or";

        var packed = GD.Load<PackedScene>(CellScenePath);
        int bagCount = 0;

        foreach (string itemName in InventoryManager.Instance.GetInventoryItems())
        {
            Equipment equipment = EquipmentManager.GetEquipment(itemName);
            if (equipment is not null)
            {
                bagCount++;
                bool canEquip = InventoryManager.Instance.CanEquip(equipment);
                var cell = packed.Instantiate<InventorySlotCell>();
                cell.BindInventoryItem(equipment, canEquip);
                cell.Pressed += () => ShowDetail(equipment, false);
                _inventoryGrid.AddChild(cell);
                _cells.Add(cell);
                continue;
            }

            ResourceItem resource = ResourceItemManager.GetResource(itemName);
            if (resource is null)
                continue;

            bagCount++;
            var resourceCell = packed.Instantiate<InventorySlotCell>();
            resourceCell.BindResourceItem(resource);
            resourceCell.Pressed += () => ShowResourceDetail(resource);
            _inventoryGrid.AddChild(resourceCell);
            _cells.Add(resourceCell);
        }

        int equippedCount = 0;
        foreach (var _ in InventoryManager.Instance.GetEquippedItems())
            equippedCount++;

        _emptyLabel.Visible = bagCount == 0;
        if (_itemCountLabel != null)
            _itemCountLabel.Text = (bagCount + equippedCount).ToString();
    }

    void ClearCells()
    {
        _cells.Clear();
        foreach (Node child in _inventoryGrid.GetChildren())
            child.QueueFree();
    }
}
