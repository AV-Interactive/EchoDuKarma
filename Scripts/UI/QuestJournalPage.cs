using System.Collections.Generic;
using EchoduKarma.Scripts.Data;
using Godot;

public partial class QuestJournalPage : Control
{
    const string QuestRowScenePath = "res://UI/QuestStat.tscn";

    [Export] Control _listView;
    [Export] VBoxContainer _questList;
    [Export] Label _emptyLabel;
    [Export] Button _closeButton;
    [Export] Control _detailView;
    [Export] QuestDetailPanel _detailPanel;
    [Export] Button _backButton;
    [Export] Label _questCountLabel;

    Control _dialogueUi;
    readonly List<QuestStatRow> _rows = new();

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        SetProcess(true);

        _dialogueUi = GetParent()?.GetNodeOrNull<Control>("DialogueUI");

        _closeButton.Pressed += Close;
        _backButton.Pressed += ShowList;

        if (QuestManager.Instance is not null)
        {
            QuestManager.Instance.QuestStarted += OnQuestChanged;
            QuestManager.Instance.QuestCompleted += OnQuestChanged;
            QuestManager.Instance.QuestStepAdvanced += OnQuestStepAdvanced;
        }
    }

    public override void _ExitTree()
    {
        if (QuestManager.Instance is not null)
        {
            QuestManager.Instance.QuestStarted -= OnQuestChanged;
            QuestManager.Instance.QuestCompleted -= OnQuestChanged;
            QuestManager.Instance.QuestStepAdvanced -= OnQuestStepAdvanced;
        }

        base._ExitTree();
    }

    void OnQuestChanged(string _) => RefreshIfVisible();

    void OnQuestStepAdvanced(string _, int __) => RefreshIfVisible();

    void RefreshIfVisible()
    {
        if (Visible)
            Refresh();
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("quests"))
        {
            if (!Visible && IsDialogueOpen())
                return;

            Toggle();
            return;
        }

        if (!Visible)
            return;

        if (_detailView.Visible && Input.IsActionJustPressed("ui_cancel"))
        {
            ShowList();
            return;
        }

        if (_listView.Visible && (Input.IsActionJustPressed("menu") || Input.IsActionJustPressed("ui_cancel")))
            Close();
    }

    bool IsDialogueOpen() => _dialogueUi != null && _dialogueUi.Visible;

    public void Toggle()
    {
        if (Visible)
            Close();
        else
            Open();
    }

    public void Open()
    {
        var statsPage = GetParent()?.GetNodeOrNull<PlayerStatsPage>("PlayerStatsPage");
        if (statsPage != null && statsPage.Visible)
            statsPage.Close();

        GetParent()?.GetNodeOrNull<InventoryPage>("InventoryPage")?.Close();

        ShowList();
        Refresh();
        Visible = true;
        ZIndex = 10;
        MoveToFront();
        GameManager.Instance.SetMenuBlockingWorld(true);
        GameManager.Instance.PlayerMoved = false;
        FocusList();
    }

    public void Close()
    {
        Visible = false;
        ShowList();
        GameManager.Instance.SetMenuBlockingWorld(false);
        GameManager.Instance.PlayerMoved = true;
        GetViewport()?.GuiReleaseFocus();
    }

    void ShowList()
    {
        _listView.Visible = true;
        _detailView.Visible = false;
        CallDeferred(MethodName.FocusList);
    }

    void ShowDetail(string questId)
    {
        var quest = QuestManager.Instance?.GetQuest(questId);
        if (quest is null)
            return;

        var runtime = QuestManager.Instance.GetRuntime(questId);
        _detailPanel.SetQuest(quest, runtime);

        _listView.Visible = false;
        _detailView.Visible = true;
        _backButton.GrabFocus();
    }

    void FocusList()
    {
        if (_rows.Count > 0)
            _rows[0].GrabFocus();
        else
            _closeButton.GrabFocus();
    }

    void Refresh()
    {
        ClearRows();

        if (QuestManager.Instance is null)
        {
            _emptyLabel.Visible = true;
            if (_questCountLabel != null)
                _questCountLabel.Text = "0";
            return;
        }

        var packed = GD.Load<PackedScene>(QuestRowScenePath);
        bool hasQuest = false;
        int count = 0;

        foreach (var (data, runtime) in QuestManager.Instance.GetTrackedQuests())
        {
            hasQuest = true;
            count++;
            var row = packed.Instantiate<QuestStatRow>();
            row.Bind(data, runtime);
            row.Pressed += () => ShowDetail(data.Id);
            _questList.AddChild(row);
            _rows.Add(row);
        }

        _emptyLabel.Visible = !hasQuest;
        if (_questCountLabel != null)
            _questCountLabel.Text = count.ToString();

        if (_questList.GetParent() is ScrollContainer scroll)
            scroll.Visible = hasQuest;
    }

    void ClearRows()
    {
        _rows.Clear();
        foreach (Node child in _questList.GetChildren())
            child.QueueFree();
    }
}
