using System.Collections.Generic;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

namespace EchoduKarma.Scripts.UI;

public partial class SkillsPage : Control, IGameMenuTabPage
{
    const string SkillRowScenePath = "res://UI/skill_stat_row.tscn";

    [Export] Control _listView;
    [Export] VBoxContainer _skillsList;
    [Export] Label _emptyLabel;
    [Export] Label _countLabel;
    [Export] ScrollContainer _scroll;
    [Export] Control _detailView;
    [Export] SkillDetailPanel _detailPanel;
    [Export] Button _backButton;

    readonly List<SkillStatRow> _rows = new();

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;

        _backButton.Pressed += ShowList;

        if (GameManager.Instance != null)
            GameManager.Instance.PlayerLevelUp += OnPlayerLevelUp;
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.PlayerLevelUp -= OnPlayerLevelUp;

        base._ExitTree();
    }

    void OnPlayerLevelUp(int _) => RefreshIfVisible();

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
        if (Visible)
            CallDeferred(MethodName.FocusList);
    }

    void ShowDetail(Skill skill)
    {
        if (skill is null)
            return;

        _detailPanel.SetSkill(skill);
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
        ClearRows();

        Player player = GameManager.Instance?.CurrentPlayer;
        if (player == null)
        {
            _emptyLabel.Visible = true;
            if (_scroll != null)
                _scroll.Visible = false;
            if (_countLabel != null)
                _countLabel.Text = "0";
            return;
        }

        var packed = GD.Load<PackedScene>(SkillRowScenePath);
        int count = player.LearnedSkills.Count;

        if (_countLabel != null)
            _countLabel.Text = count.ToString();

        _emptyLabel.Visible = count == 0;
        if (_scroll != null)
            _scroll.Visible = count > 0;

        foreach (Skill skill in player.LearnedSkills)
        {
            var row = packed.Instantiate<SkillStatRow>();
            row.Bind(skill);
            Skill captured = skill;
            row.Pressed += () => ShowDetail(captured);
            _skillsList.AddChild(row);
            _rows.Add(row);
        }
    }

    void ClearRows()
    {
        _rows.Clear();
        foreach (Node child in _skillsList.GetChildren())
            child.QueueFree();
    }
}
