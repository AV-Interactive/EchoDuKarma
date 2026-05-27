using System.Collections.Generic;
using EchoduKarma.Scripts.Data;
using Godot;

namespace EchoduKarma.Scripts.UI;

public partial class QuestJournalPage : Control, IGameMenuTabPage
{
    const string QuestRowScenePath = "res://UI/QuestStat.tscn";

    [Export] Control _listView;
    [Export] VBoxContainer _questList;
    [Export] Label _emptyLabel;
    [Export] Label _questCountLabel;
    [Export] ScrollContainer _scroll;
    [Export] Control _detailView;
    [Export] QuestDetailPanel _detailPanel;
    [Export] Button _backButton;

    readonly List<QuestStatRow> _rows = new();
    string _viewingQuestId;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;

        _detailPanel ??= GetNodeOrNull<QuestDetailPanel>("MarginContainer/VBoxContainer/DetailView/QuestDetail");
        if (_detailPanel == null)
            GD.PrintErr("[QuestJournalPage] QuestDetailPanel introuvable — vérifiez l'export _detailPanel.");

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
        _viewingQuestId = null;
        if (Visible)
            CallDeferred(MethodName.FocusList);
    }

    void ShowDetail(string questId)
    {
        if (string.IsNullOrEmpty(questId) || QuestManager.Instance is null)
            return;

        var quest = QuestManager.Instance.GetQuest(questId);
        if (quest is null)
            return;

        if (_detailPanel == null)
        {
            GD.PrintErr("[QuestJournalPage] Impossible d'afficher le détail : panneau absent.");
            return;
        }

        _viewingQuestId = questId;
        _detailPanel.SetQuest(quest, QuestManager.Instance.GetRuntime(questId));

        _listView.Visible = false;
        _detailView.Visible = true;
        _backButton.GrabFocus();
    }

    void FocusList()
    {
        if (_rows.Count > 0)
            _rows[0].GrabFocus();
        else if (_scroll != null)
            _scroll.GrabFocus();
    }

    void Refresh()
    {
        string restoreQuestId = _viewingQuestId;
        bool restoreDetail = _detailView.Visible && !string.IsNullOrEmpty(restoreQuestId);

        ClearRows();

        if (QuestManager.Instance is null)
        {
            _emptyLabel.Visible = true;
            if (_scroll != null)
                _scroll.Visible = false;
            if (_questCountLabel != null)
                _questCountLabel.Text = "0";
            if (restoreDetail)
                ShowList();
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
            string questId = data.Id;
            row.Pressed += () => ShowDetail(questId);
            _questList.AddChild(row);
            _rows.Add(row);
        }

        ConfigureRowFocus();

        _emptyLabel.Visible = !hasQuest;
        if (_questCountLabel != null)
            _questCountLabel.Text = count.ToString();

        if (_scroll != null)
            _scroll.Visible = hasQuest;

        if (restoreDetail && QuestManager.Instance.GetQuest(restoreQuestId) is not null)
            ShowDetail(restoreQuestId);
        else if (restoreDetail)
            ShowList();
    }

    void ConfigureRowFocus()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            QuestStatRow row = _rows[i];
            if (i > 0)
                row.FocusNeighborTop = row.GetPathTo(_rows[i - 1]);
            if (i < _rows.Count - 1)
                row.FocusNeighborBottom = row.GetPathTo(_rows[i + 1]);
        }
    }

    void ClearRows()
    {
        _rows.Clear();
        foreach (Node child in _questList.GetChildren())
            child.QueueFree();
    }
}
