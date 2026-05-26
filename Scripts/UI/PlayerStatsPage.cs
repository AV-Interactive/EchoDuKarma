using Godot;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;

public partial class PlayerStatsPage : Control
{
    [Export] RichTextLabel _nameLabel;
    [Export] RichTextLabel _classLabel;
    [Export] RichTextLabel _levelLabel;
    [Export] RichTextLabel _hpLabel;
    [Export] RichTextLabel _mpLabel;
    [Export] RichTextLabel _xpLabel;
    [Export] ProgressBar _hpBar;
    [Export] ProgressBar _mpBar;
    [Export] ProgressBar _xpBar;
    [Export] RichTextLabel _forceLabel;
    [Export] RichTextLabel _espritLabel;
    [Export] RichTextLabel _agiLabel;
    [Export] RichTextLabel _defLabel;
    [Export] VBoxContainer _skillsContainer;
    [Export] Button _closeButton;
    [Export] TextureRect _avatar;
    [Export] string _playerClassName = "Magus";

    Control _dialogueUi;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        SetProcess(true);
        _dialogueUi = GetParent()?.GetNodeOrNull<Control>("DialogueUI");

        if (_closeButton != null)
            _closeButton.Pressed += Close;
        else
            GD.PrintErr("[PlayerStatsPage] CloseButton introuvable — vérifiez les exports de la scène.");

        if (GameManager.Instance != null)
            GameManager.Instance.PlayerLevelUp += OnPlayerLevelUp;
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.PlayerLevelUp -= OnPlayerLevelUp;

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("stats"))
        {
            if (!Visible && IsDialogueOpen())
                return;

            Toggle();
            return;
        }

        if (Visible && (Input.IsActionJustPressed("menu") || Input.IsActionJustPressed("ui_cancel")))
            Close();
    }

    void OnPlayerLevelUp(int _)
    {
        if (Visible)
            Refresh();
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
        GetParent()?.GetNodeOrNull<QuestJournalPage>("QuestJournalPage")?.Close();
        Refresh();
        Visible = true;
        ZIndex = 10;
        MoveToFront();
        GameManager.Instance.PlayerMoved = false;
        _closeButton.GrabFocus();
    }

    public void Close()
    {
        Visible = false;
        GameManager.Instance.PlayerMoved = true;
        GetViewport()?.GuiReleaseFocus();
    }

    void Refresh()
    {
        Player player = GameManager.Instance?.CurrentPlayer;
        if (player == null)
            return;

        StatHandler statHandler = player.GetNodeOrNull<StatHandler>("PlayerStats");

        _nameLabel.Text = $"[b]{player.Name}[/b]";
        _classLabel.Text = $"[color=#58B4C6]{_playerClassName}[/color]";
        _levelLabel.Text = $"Niveau {player.Level}";

        _hpLabel.Text = $"[color=#F54927]{player.CurrentPv} / {player.Pv}[/color]";
        _mpLabel.Text = $"[color=#27B0F5]{player.CurrentMp} / {player.Mp}[/color]";

        _hpBar.MaxValue = player.Pv;
        _hpBar.Value = player.CurrentPv;
        _mpBar.MaxValue = player.Mp;
        _mpBar.Value = player.CurrentMp;

        RefreshExperience(statHandler, player.Level);
        RefreshAttributes(player);
        RefreshSkills(player);
    }

    void RefreshExperience(StatHandler statHandler, int level)
    {
        if (statHandler == null)
        {
            _xpLabel.Text = "—";
            _xpBar.MaxValue = 1;
            _xpBar.Value = 0;
            return;
        }

        Stats currentRow = statHandler.GetStatsForLevel(level);
        Stats nextRow = statHandler.GetStatsForLevel(level + 1);

        if (nextRow == null)
        {
            _xpLabel.Text = "[color=#FFD166]XP MAX[/color]";
            _xpBar.MaxValue = 1;
            _xpBar.Value = 1;
            return;
        }

        int currentXp = statHandler.CurrentExperience;
        int minXp = currentRow?.XPForNextLevel ?? 0;
        int maxXp = nextRow.XPForNextLevel;
        int span = Mathf.Max(maxXp - minXp, 1);

        _xpLabel.Text = $"[color=#FFD166]{currentXp} / {maxXp} XP[/color]";
        _xpBar.MaxValue = span;
        _xpBar.Value = Mathf.Clamp(currentXp - minXp, 0, span);
    }

    void RefreshAttributes(Player player)
    {
        _forceLabel.Text = player.Strength.ToString();
        _espritLabel.Text = player.Spirit.ToString();
        _agiLabel.Text = player.Dexterity.ToString();
        _defLabel.Text = player.Defense.ToString();
    }

    void RefreshSkills(Player player)
    {
        foreach (Node child in _skillsContainer.GetChildren())
            child.QueueFree();

        if (player.LearnedSkills.Count == 0)
        {
            var emptyLabel = new Label { Text = "Aucune compétence apprise." };
            emptyLabel.AddThemeFontSizeOverride("font_size", 12);
            _skillsContainer.AddChild(emptyLabel);
            return;
        }

        foreach (Skill skill in player.LearnedSkills)
        {
            var line = new Label
            {
                Text = $"{skill.Name}  —  {skill.Cost} PM · {skill.Power} puiss.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            line.AddThemeFontSizeOverride("font_size", 11);
            line.Modulate = new Color(0.82f, 0.88f, 0.95f);
            _skillsContainer.AddChild(line);
        }
    }
}
