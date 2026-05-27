using EchoduKarma.Scripts.Data;
using Godot;

namespace EchoduKarma.Scripts.UI;

public enum GameMenuTab
{
    Stats,
    Skills,
    Inventory,
    Quests,
    System,
}

public partial class GameMenuShell : Control
{
    [Export] Label _titleLabel;
    [Export] Button _statsTabButton;
    [Export] Button _skillsTabButton;
    [Export] Button _inventoryTabButton;
    [Export] Button _questsTabButton;
    [Export] Button _systemTabButton;
    [Export] Button _closeButton;
    [Export] PlayerStatsPage _statsPage;
    [Export] SkillsPage _skillsPage;
    [Export] InventoryPage _inventoryPage;
    [Export] QuestJournalPage _questsPage;
    [Export] SavePage _systemPage;

    Control _dialogueUi;
    GameMenuTab _currentTab = GameMenuTab.Stats;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        SetProcess(true);

        _dialogueUi = GetParent()?.GetNodeOrNull<Control>("DialogueUI");

        _statsTabButton.Pressed += () => OpenTab(GameMenuTab.Stats);
        _skillsTabButton.Pressed += () => OpenTab(GameMenuTab.Skills);
        _inventoryTabButton.Pressed += () => OpenTab(GameMenuTab.Inventory);
        _questsTabButton.Pressed += () => OpenTab(GameMenuTab.Quests);
        _systemTabButton.Pressed += () => OpenTab(GameMenuTab.System);
        _closeButton.Pressed += Close;

        ConfigureNavFocus();
    }

    void ConfigureNavFocus()
    {
        _statsTabButton.FocusNeighborBottom = _statsTabButton.GetPathTo(_skillsTabButton);
        _statsTabButton.FocusNeighborTop = _statsTabButton.GetPathTo(_closeButton);

        _skillsTabButton.FocusNeighborTop = _skillsTabButton.GetPathTo(_statsTabButton);
        _skillsTabButton.FocusNeighborBottom = _skillsTabButton.GetPathTo(_inventoryTabButton);

        _inventoryTabButton.FocusNeighborTop = _inventoryTabButton.GetPathTo(_skillsTabButton);
        _inventoryTabButton.FocusNeighborBottom = _inventoryTabButton.GetPathTo(_questsTabButton);

        _questsTabButton.FocusNeighborTop = _questsTabButton.GetPathTo(_inventoryTabButton);
        _questsTabButton.FocusNeighborBottom = _questsTabButton.GetPathTo(_systemTabButton);

        _systemTabButton.FocusNeighborTop = _systemTabButton.GetPathTo(_questsTabButton);
        _systemTabButton.FocusNeighborBottom = _systemTabButton.GetPathTo(_closeButton);

        _closeButton.FocusNeighborTop = _closeButton.GetPathTo(_systemTabButton);
        _closeButton.FocusNeighborBottom = _closeButton.GetPathTo(_statsTabButton);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible || !@event.IsActionPressed("ui_accept"))
            return;

        if (GetViewport().GuiGetFocusOwner() is not Button focused || !focused.IsVisibleInTree())
            return;

        if (focused != _statsTabButton && focused != _skillsTabButton &&
            focused != _inventoryTabButton && focused != _questsTabButton &&
            focused != _systemTabButton && focused != _closeButton)
            return;

        focused.EmitSignal(Button.SignalName.Pressed);
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("stats"))
        {
            HandleShortcut(GameMenuTab.Stats);
            return;
        }

        if (Input.IsActionJustPressed("skills"))
        {
            HandleShortcut(GameMenuTab.Skills);
            return;
        }

        if (Input.IsActionJustPressed("inventory"))
        {
            HandleShortcut(GameMenuTab.Inventory);
            return;
        }

        if (Input.IsActionJustPressed("quests"))
        {
            HandleShortcut(GameMenuTab.Quests);
            return;
        }

        if (!Visible)
        {
            if (!Input.IsActionJustPressed("menu") && !Input.IsActionJustPressed("ui_cancel"))
                return;

            if (IsDialogueOpen() || IsShopOpen())
                return;

            OpenTab(GameMenuTab.System);
            return;
        }

        if (!Input.IsActionJustPressed("menu") && !Input.IsActionJustPressed("ui_cancel"))
            return;

        if (GetActiveTabPage()?.TryHandleCancel() == true)
            return;

        Close();
    }

    void HandleShortcut(GameMenuTab tab)
    {
        if (!Visible)
        {
            if (IsDialogueOpen())
                return;

            OpenTab(tab);
            return;
        }

        if (_currentTab == tab)
            Close();
        else
            OpenTab(tab);
    }

    bool IsDialogueOpen() => _dialogueUi != null && _dialogueUi.Visible;

    bool IsShopOpen()
    {
        var shop = GetTree().GetFirstNodeInGroup(ShopUI.GroupName) as ShopUI;
        return shop != null && shop.IsOpen;
    }

    public void OpenTab(GameMenuTab tab)
    {
        if (!Visible && IsDialogueOpen())
            return;

        bool wasClosed = !Visible;
        Visible = true;
        ZIndex = 10;
        MoveToFront();

        if (wasClosed)
        {
            GameManager.Instance.SetMenuBlockingWorld(true);
            GameManager.Instance.PlayerMoved = false;
        }

        SwitchTab(tab);
        if (wasClosed)
            CallDeferred(tab == GameMenuTab.System ? MethodName.FocusSystemNav : MethodName.FocusNav);
    }

    public void Close()
    {
        if (!Visible)
            return;

        _statsPage.OnTabHidden();
        _skillsPage.OnTabHidden();
        _inventoryPage.OnTabHidden();
        _questsPage.OnTabHidden();
        _systemPage.OnTabHidden();

        Visible = false;
        GameManager.Instance.SetMenuBlockingWorld(false);
        GameManager.Instance.PlayerMoved = true;
        GetViewport()?.GuiReleaseFocus();
    }

    void SwitchTab(GameMenuTab tab)
    {
        if (Visible)
            GetActiveTabPage()?.OnTabHidden();

        _currentTab = tab;

        _statsPage.Visible = tab == GameMenuTab.Stats;
        _skillsPage.Visible = tab == GameMenuTab.Skills;
        _inventoryPage.Visible = tab == GameMenuTab.Inventory;
        _questsPage.Visible = tab == GameMenuTab.Quests;
        _systemPage.Visible = tab == GameMenuTab.System;

        UpdateTitle();
        UpdateTabButtons();

        GetActiveTabPage()?.OnTabShown();
        CallDeferred(MethodName.FocusContent);
    }

    IGameMenuTabPage GetActiveTabPage() => _currentTab switch
    {
        GameMenuTab.Stats => _statsPage,
        GameMenuTab.Skills => _skillsPage,
        GameMenuTab.Inventory => _inventoryPage,
        GameMenuTab.Quests => _questsPage,
        GameMenuTab.System => _systemPage,
        _ => null,
    };

    void UpdateTitle()
    {
        if (_titleLabel == null)
            return;

        _titleLabel.Text = _currentTab switch
        {
            GameMenuTab.Stats => "Statistiques",
            GameMenuTab.Skills => "Compétences",
            GameMenuTab.Inventory => "Inventaire",
            GameMenuTab.Quests => "Journal des quêtes",
            GameMenuTab.System => "Système",
            _ => "",
        };
    }

    void UpdateTabButtons()
    {
        SetTabPressed(_statsTabButton, _currentTab == GameMenuTab.Stats);
        SetTabPressed(_skillsTabButton, _currentTab == GameMenuTab.Skills);
        SetTabPressed(_inventoryTabButton, _currentTab == GameMenuTab.Inventory);
        SetTabPressed(_questsTabButton, _currentTab == GameMenuTab.Quests);
        SetTabPressed(_systemTabButton, _currentTab == GameMenuTab.System);
    }

    static void SetTabPressed(Button button, bool active)
    {
        if (button == null)
            return;

        button.ButtonPressed = active;
        button.Modulate = active
            ? new Color(0.55f, 0.88f, 1f)
            : Colors.White;
    }

    void FocusNav() => _statsTabButton?.GrabFocus();

    void FocusSystemNav() => _systemTabButton?.GrabFocus();

    void FocusContent() => GetActiveTabPage()?.FocusDefault();
}
