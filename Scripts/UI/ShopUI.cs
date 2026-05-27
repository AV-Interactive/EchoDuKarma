using System.Collections.Generic;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

namespace EchoduKarma.Scripts.UI;

public partial class ShopUI : Control
{
    public const string GroupName = "shop_ui";
    const string CellScenePath = "res://UI/InventorySlotCell.tscn";

    enum ShopMode { Buy, Sell }

    [Export] Label _titleLabel;
    [Export] Label _sectionLabel;
    [Export] Label _goldLabel;
    [Export] Label _hintLabel;
    [Export] Button _buyTabButton;
    [Export] Button _sellTabButton;
    [Export] Control _listView;
    [Export] GridContainer _itemGrid;
    [Export] Control _detailView;
    [Export] InventoryDetailPanel _detailPanel;
    [Export] Button _backButton;
    [Export] Button _actionButton;
    [Export] Button _closeButton;

    readonly List<InventorySlotCell> _cells = new();
    ShopMode _mode = ShopMode.Buy;
    string _shopId;
    float _zoneKarma;
    int _purchasesThisVisit;
    Equipment _selectedEquipment;
    int _selectedPrice;
    bool _selectedIsSell;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        AddToGroup(GroupName);
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;

        _buyTabButton.Pressed += () => SwitchMode(ShopMode.Buy);
        _sellTabButton.Pressed += () => SwitchMode(ShopMode.Sell);
        _backButton.Pressed += ShowList;
        _actionButton.Pressed += OnActionPressed;
        _closeButton.Pressed += Close;

        if (InventoryManager.Instance is not null)
        {
            InventoryManager.Instance.GoldChanged += OnGoldChanged;
            InventoryManager.Instance.InventoryChanged += OnInventoryChanged;
        }
    }

    public override void _ExitTree()
    {
        if (InventoryManager.Instance is not null)
        {
            InventoryManager.Instance.GoldChanged -= OnGoldChanged;
            InventoryManager.Instance.InventoryChanged -= OnInventoryChanged;
        }

        base._ExitTree();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible)
            return;

        if (!Input.IsActionJustPressed("menu") && !Input.IsActionJustPressed("ui_cancel"))
            return;

        if (_detailView.Visible)
        {
            ShowList();
            GetViewport().SetInputAsHandled();
            return;
        }

        Close();
        GetViewport().SetInputAsHandled();
    }

    public void Open(string shopId)
    {
        _shopId = shopId?.Trim() ?? "";
        _zoneKarma = KarmaManager.Instance?.GetZoneKarma(GameManager.Instance?.ReturnZoneName ?? "Introduction") ?? 0f;
        _purchasesThisVisit = 0;

        string merchantName = ShopCatalog.GetMerchantName(_shopId);
        _titleLabel.Text = $"Boutique — {merchantName}";
        UpdateHint();

        Visible = true;
        ZIndex = 12;
        MoveToFront();

        GameManager.Instance?.SetMenuBlockingWorld(true);
        GameManager.Instance.PlayerMoved = false;

        SwitchMode(ShopMode.Buy);
        ShowList();
        Refresh();
        CallDeferred(MethodName.FocusDefault);
    }

    public void Close()
    {
        if (!Visible)
            return;

        Visible = false;
        ShowList();
        GameManager.Instance?.SetMenuBlockingWorld(false);
        GameManager.Instance.PlayerMoved = true;
        GetViewport()?.GuiReleaseFocus();
    }

    void OnGoldChanged(int _) => RefreshIfVisible();
    void OnInventoryChanged() => RefreshIfVisible();

    void RefreshIfVisible()
    {
        if (Visible)
            Refresh();
    }

    void SwitchMode(ShopMode mode)
    {
        if (mode == ShopMode.Sell && !ShopPricing.CanMerchantBuyFromPlayer(_zoneKarma))
            return;

        _mode = mode;
        _selectedEquipment = null;

        bool buyActive = mode == ShopMode.Buy;
        _buyTabButton.ButtonPressed = buyActive;
        _buyTabButton.Modulate = buyActive ? new Color(0.55f, 0.88f, 1f) : Colors.White;
        _sellTabButton.ButtonPressed = !buyActive;
        _sellTabButton.Modulate = !buyActive ? new Color(0.55f, 0.88f, 1f) : Colors.White;

        _sectionLabel.Text = buyActive ? "Marchandise" : "Ton sac";
        _sellTabButton.Disabled = !ShopPricing.CanMerchantBuyFromPlayer(_zoneKarma);

        ShowList();
        Refresh();
    }

    void UpdateHint() =>
        _hintLabel.Text = $"{ShopPricing.GetEconomyHint(_zoneKarma, _purchasesThisVisit)} · Échap fermer / retour";

    void ShowList()
    {
        _listView.Visible = true;
        _detailView.Visible = false;
        _selectedEquipment = null;
        if (Visible)
            CallDeferred(MethodName.FocusDefault);
    }

    void ShowBuyDetail(Equipment equipment, int price)
    {
        _selectedEquipment = equipment;
        _selectedPrice = price;
        _selectedIsSell = false;
        UpdateDetailView();
    }

    void ShowSellDetail(Equipment equipment, int sellPrice)
    {
        _selectedEquipment = equipment;
        _selectedPrice = sellPrice;
        _selectedIsSell = true;
        UpdateDetailView();
    }

    void UpdateDetailView()
    {
        if (_selectedEquipment is null)
            return;

        string reason = "";
        bool canTransact = _selectedIsSell
            ? CanSell(_selectedEquipment, _selectedPrice, out reason)
            : CanBuy(_selectedEquipment, _selectedPrice, out reason);

        _detailPanel.SetShopTransaction(_selectedEquipment, _selectedPrice, _selectedIsSell, canTransact, reason);
        _actionButton.Text = canTransact
            ? (_selectedIsSell ? "Vendre" : "Acheter")
            : "Indisponible";
        _actionButton.Disabled = !canTransact;

        _listView.Visible = false;
        _detailView.Visible = true;
        _actionButton.GrabFocus();
    }

    bool CanBuy(Equipment equipment, int price, out string reason)
    {
        reason = "";
        if (InventoryManager.Instance == null)
            return false;

        if (!InventoryManager.Instance.CanBuyEquipment(equipment, price, out reason))
            return false;

        if (!ShopPricing.CanPurchaseMore(_zoneKarma, _purchasesThisVisit))
        {
            reason = "Le marchand n'accepte plus d'achats (apathie du Karma).";
            return false;
        }

        return true;
    }

    bool CanSell(Equipment equipment, int sellPrice, out string reason)
    {
        reason = "";
        if (InventoryManager.Instance == null)
            return false;

        if (!ShopPricing.CanMerchantBuyFromPlayer(_zoneKarma))
        {
            reason = "Le marchand ne rachète rien en période de Chaos.";
            return false;
        }

        if (sellPrice <= 0)
        {
            reason = "Cet objet n'a pas de valeur de revente.";
            return false;
        }

        return InventoryManager.Instance.CanSellEquipment(equipment.Name, out reason);
    }

    void OnActionPressed()
    {
        if (_selectedEquipment is null || InventoryManager.Instance is null)
            return;

        if (_selectedIsSell)
        {
            if (!InventoryManager.Instance.TrySellEquipment(_selectedEquipment.Name, _selectedPrice))
                return;
        }
        else if (!InventoryManager.Instance.TryBuyEquipment(_selectedEquipment.Name, _selectedPrice))
        {
            return;
        }
        else
        {
            _purchasesThisVisit++;
            UpdateHint();
        }

        ShowList();
        Refresh();
    }

    void FocusDefault()
    {
        if (_detailView.Visible)
        {
            _actionButton.GrabFocus();
            return;
        }

        if (_mode == ShopMode.Buy)
            _buyTabButton.GrabFocus();
        else if (_cells.Count > 0)
            _cells[0].GrabFocus();
        else
            _closeButton.GrabFocus();
    }

    void Refresh()
    {
        ClearCells();

        if (InventoryManager.Instance is null)
        {
            _goldLabel.Text = "0 or";
            return;
        }

        _goldLabel.Text = $"{InventoryManager.Instance.Gold} or";

        if (_mode == ShopMode.Buy)
            RefreshBuyGrid();
        else
            RefreshSellGrid();

        if (_selectedEquipment is not null && _detailView.Visible)
            UpdateDetailView();
    }

    void RefreshBuyGrid()
    {
        var packed = GD.Load<PackedScene>(CellScenePath);
        var offers = new List<ShopOffer>(ShopCatalog.GetShopOffers(_shopId));
        int maxItems = ShopPricing.GetMaxCatalogItemsShown(_zoneKarma, offers.Count);

        for (int i = 0; i < maxItems; i++)
        {
            ShopOffer offer = offers[i];
            Equipment equipment = offer.Equipment;
            int price = ShopPricing.GetBuyPrice(equipment.Price, _zoneKarma);
            int ownedCount = InventoryManager.Instance.CountInBag(equipment.Name);
            if (InventoryManager.Instance.IsEquipped(equipment.Name))
                ownedCount++;

            bool classOk = equipment.IsUsableByClass(InventoryManager.Instance.PlayerClass);
            bool canAfford = price <= 0 || InventoryManager.Instance.Gold >= price;
            bool karmaOk = ShopPricing.CanPurchaseMore(_zoneKarma, _purchasesThisVisit);
            bool canBuy = classOk && canAfford && karmaOk;

            var cell = packed.Instantiate<InventorySlotCell>();
            cell.BindShopItem(equipment, price, ownedCount, canBuy, !canAfford);
            cell.Pressed += () => ShowBuyDetail(equipment, price);
            _itemGrid.AddChild(cell);
            _cells.Add(cell);
        }
    }

    void RefreshSellGrid()
    {
        if (!ShopPricing.CanMerchantBuyFromPlayer(_zoneKarma))
            return;

        var packed = GD.Load<PackedScene>(CellScenePath);
        foreach (string itemName in InventoryManager.Instance.GetInventoryItems())
        {
            Equipment equipment = EquipmentManager.GetEquipment(itemName);
            if (equipment is null)
                continue;

            int sellPrice = ShopPricing.GetSellPrice(equipment.Price, _zoneKarma);
            bool canSell = CanSell(equipment, sellPrice, out _);

            var cell = packed.Instantiate<InventorySlotCell>();
            cell.BindShopSellItem(equipment, sellPrice, canSell);
            cell.Pressed += () => ShowSellDetail(equipment, sellPrice);
            _itemGrid.AddChild(cell);
            _cells.Add(cell);
        }
    }

    void ClearCells()
    {
        _cells.Clear();
        foreach (Node child in _itemGrid.GetChildren())
            child.QueueFree();
    }
}
