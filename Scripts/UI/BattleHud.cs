using Godot;
using System;
using System.Collections.Generic;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;
using EchoduKarma.Scripts.UI;

public partial class BattleHud : CanvasLayer
{
    [Signal] public delegate void ActionSelectedEventHandler(string actionName);

    const string CombatantStatScenePath = "res://UI/BattleCombatantStat.tscn";
    const int MaxLogLines = 8;

    Control _uiScene;
    Control _actionMenu;
    Control _playerPanel;
    VBoxContainer _skillsListPanel;
    Sprite2D _targetCursor;
    VBoxContainer _enemyList;
    Tween _cursorTween;

    readonly Dictionary<Enemy, EnemyStatWidgets> _enemyWidgets = new();
    readonly List<string> _logHistory = new();

    [Export] RichTextLabel _playerHpLabel;
    [Export] RichTextLabel _playerMpLabel;
    [Export] RichTextLabel _logs;
    [Export] KarmaBanner _karmaBanner;

    struct EnemyStatWidgets
    {
        public Control Root;
        public Label NameLabel;
        public TextureProgressBar HpBar;
        public Label HpLabel;
    }

    public override void _Ready()
    {
        _uiScene = GetNode<Control>("Scene");
        _actionMenu = GetNode<Control>("Scene/Actions/Panel/ActionMenu");
        _playerPanel = GetNode<Control>("Scene/CombatantsPanel/PlayerPanel");
        _skillsListPanel = GetNode<VBoxContainer>("Scene/Actions/Panel/SkillsList");
        _targetCursor = GetNode<Sprite2D>("Scene/TargetCursor");
        _enemyList = GetNode<VBoxContainer>("Scene/CombatantsPanel/EnemyList");

        _playerHpLabel = GetNodeOrNull<RichTextLabel>("Scene/CombatantsPanel/PlayerPanel/PlayerStat/HBoxContainer/VBoxContainer/HpRow/NB_HP");
        _playerMpLabel = GetNodeOrNull<RichTextLabel>("Scene/CombatantsPanel/PlayerPanel/PlayerStat/HBoxContainer/VBoxContainer/MpRow/NB_MP");
        _karmaBanner = GetNodeOrNull<KarmaBanner>("Scene/KarmaBanner");

        if (_logs != null)
            _logs.Text = "";
        else
            GD.PrintErr("[BattleHud] RichTextLabel de logs introuvable.");

        _actionMenu.Hide();
        _skillsListPanel.Hide();

        var battlePlayer = GameManager.Instance.GetBattleSnapshot()
            ?? GameManager.Instance.CurrentPlayer as IBattler;
        if (battlePlayer != null)
            UpdatePlayerStats(battlePlayer);

        GetNode<Button>("Scene/Actions/Panel/ActionMenu/BtnAttack").Pressed += () => OnButtonPressed("Attack");
        GetNode<Button>("Scene/Actions/Panel/ActionMenu/BtnMagic").Pressed += () => OnButtonPressed("Magic");
        GetNode<Button>("Scene/Actions/Panel/ActionMenu/BtnDefense").Pressed += () => OnButtonPressed("Defense");
        GetNode<Button>("Scene/Actions/Panel/ActionMenu/BtnEscape").Pressed += () => OnButtonPressed("Flee");

        var battleManager = GetTree().Root.FindChild("BattleManager", true, false) as BattleManager;
        if (battleManager != null)
            battleManager.PlayerDamage += OnPlayerDamageReceived;

        StartCursorAnim();
    }

    public void SetupEnemies(IReadOnlyList<Enemy> enemies)
    {
        ClearEnemyWidgets();

        var statScene = GD.Load<PackedScene>(CombatantStatScenePath);
        if (statScene == null)
        {
            GD.PrintErr("[BattleHud] Scène BattleCombatantStat introuvable.");
            return;
        }

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || !GodotObject.IsInstanceValid(enemy))
                continue;

            var root = statScene.Instantiate<Control>();
            _enemyList.AddChild(root);

            var widgets = new EnemyStatWidgets
            {
                Root = root,
                NameLabel = root.GetNode<Label>("NameLabel"),
                HpBar = root.GetNode<TextureProgressBar>("HpBar"),
                HpLabel = root.GetNode<Label>("HpLabel"),
            };

            _enemyWidgets[enemy] = widgets;
            RefreshEnemy(enemy);
        }
    }

    public void RefreshEnemy(Enemy enemy)
    {
        if (enemy == null || !_enemyWidgets.TryGetValue(enemy, out EnemyStatWidgets w))
            return;

        w.NameLabel.Text = enemy.EnemyName;
        int maxPv = Mathf.Max(enemy.Pv, 1);
        w.HpBar.MaxValue = maxPv;
        w.HpBar.Value = Mathf.Clamp(enemy.CurrentPv, 0, maxPv);
        w.HpLabel.Text = $"{enemy.CurrentPv}/{enemy.Pv}";
    }

    public void RefreshAllEnemies()
    {
        foreach (var pair in _enemyWidgets)
            RefreshEnemy(pair.Key);
    }

    public void RemoveEnemy(Enemy enemy)
    {
        if (enemy == null || !_enemyWidgets.TryGetValue(enemy, out EnemyStatWidgets w))
            return;

        w.Root.QueueFree();
        _enemyWidgets.Remove(enemy);
    }

    public void SetActivePlayer(bool active)
    {
        ClearActiveHighlight();
        if (active && _playerPanel != null)
            _playerPanel.Modulate = new Color(1.15f, 1.15f, 1.05f);
    }

    public void SetActiveEnemy(Enemy enemy)
    {
        ClearActiveHighlight();
        if (enemy != null && _enemyWidgets.TryGetValue(enemy, out EnemyStatWidgets w))
            w.Root.Modulate = new Color(1.15f, 1.15f, 0.85f);
    }

    public void ClearActiveHighlight()
    {
        if (_playerPanel != null)
            _playerPanel.Modulate = Colors.White;

        foreach (var pair in _enemyWidgets)
            pair.Value.Root.Modulate = Colors.White;
    }

    void ClearEnemyWidgets()
    {
        foreach (var pair in _enemyWidgets)
            pair.Value.Root.QueueFree();

        _enemyWidgets.Clear();
    }

    Vector2 ViewportToUiScene(Vector2 viewportPos)
    {
        if (_uiScene == null)
            return viewportPos;

        return _uiScene.GetGlobalTransformWithCanvas().AffineInverse() * viewportPos;
    }

    public void ShowMenu()
    {
        SetActivePlayer(true);
        _actionMenu.Show();
        var btnAttack = _actionMenu.GetNodeOrNull<Button>("BtnAttack");

        if (btnAttack != null && btnAttack.IsInsideTree())
            btnAttack.GrabFocus();
    }

    public void HideMenu()
    {
        _actionMenu.Hide();
        _skillsListPanel.Hide();
        _targetCursor.Hide();
    }

    void OnButtonPressed(string action)
    {
        HideMenu();
        EmitSignal(nameof(ActionSelected), action);
    }

    void OnPlayerDamageReceived(int damage)
    {
        var player = GameManager.Instance.GetBattleSnapshot()
            ?? GameManager.Instance.CurrentPlayer as IBattler;
        if (player != null)
            UpdatePlayerStats(player);
    }

    public void UpdatePlayerStats(IBattler player)
    {
        if (player == null)
            return;

        var nameLabel = GetNodeOrNull<RichTextLabel>("Scene/CombatantsPanel/PlayerPanel/PlayerStat/HBoxContainer/VBoxContainer/PERSO_NAME");
        if (nameLabel != null)
            nameLabel.Text = $"[b]{player.Name}[/b]";

        if (_playerHpLabel != null)
            _playerHpLabel.Text = $"[color=#F54927]{player.CurrentPv}/{player.Pv} HP[/color]";

        if (_playerMpLabel != null)
            _playerMpLabel.Text = $"[color=#27B0F5]{player.CurrentMp}/{player.Mp} MP[/color]";

        var statPanel = GetNodeOrNull<Control>("Scene/CombatantsPanel/PlayerPanel/PlayerStat");
        if (statPanel == null)
            return;

        var hpBar = statPanel.GetNodeOrNull<TextureProgressBar>("HBoxContainer/VBoxContainer/HpRow/HP");
        var mpBar = statPanel.GetNodeOrNull<TextureProgressBar>("HBoxContainer/VBoxContainer/MpRow/MP");
        if (hpBar != null)
        {
            hpBar.MaxValue = Mathf.Max(player.Pv, 1);
            hpBar.Value = player.CurrentPv;
        }
        if (mpBar != null)
        {
            mpBar.MaxValue = Mathf.Max(player.Mp, 1);
            mpBar.Value = player.CurrentMp;
        }
    }

    public void ShowDamage(Vector2 viewportPosition, int amount, Color color)
    {
        if (_uiScene == null)
            return;

        Vector2 localPos = ViewportToUiScene(viewportPosition);

        var label = new Label
        {
            Text = amount.ToString(),
            Modulate = color,
            Position = localPos,
        };
        label.AddThemeFontSizeOverride("font_size", 22);

        _uiScene.AddChild(label);

        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", localPos.Y - 24f, 0.5f);
        tween.Parallel().TweenProperty(label, "modulate:a", 0, 0.5f);
        tween.Finished += () => label.QueueFree();
    }

    public void ShowLogs(string message)
    {
        if (_logs == null)
        {
            GD.PrintErr("[BattleHud] _logs est nul.");
            return;
        }

        _logHistory.Add(message);
        while (_logHistory.Count > MaxLogLines)
            _logHistory.RemoveAt(0);

        _logs.Text = string.Join("\n", _logHistory);
        CallDeferred(MethodName.ScrollLogToBottom);
    }

    void ScrollLogToBottom()
    {
        if (_logs == null)
            return;

        _logs.ScrollToLine(Mathf.Max(0, _logs.GetLineCount() - 1));
    }

    void StartCursorAnim()
    {
        _cursorTween = CreateTween().SetLoops();
        _cursorTween.TweenProperty(_targetCursor, "offset:y", 6.0f, 0.25f).SetTrans(Tween.TransitionType.Sine);
        _cursorTween.TweenProperty(_targetCursor, "offset:y", 0.0f, 0.25f).SetTrans(Tween.TransitionType.Sine);
    }

    public void UpdateTargetCursor(Vector2 viewportPosition)
    {
        _targetCursor.Show();
        Vector2 localPos = ViewportToUiScene(viewportPosition);
        _targetCursor.Position = new Vector2(localPos.X, localPos.Y - UiPixelRoot.DesignHeight * 0.06f);
    }

    public void ShowMagicMenu(List<Skill> skills)
    {
        _actionMenu.Hide();

        foreach (Node n in _skillsListPanel.GetChildren())
        {
            _skillsListPanel.RemoveChild(n);
            n.QueueFree();
        }

        foreach (var skill in skills)
        {
            var btn = new Button();
            btn.Text = $"{skill.Name} ({skill.Cost} MP)";
            btn.AddThemeFontSizeOverride("font_size", 11);
            btn.Pressed += () => OnButtonPressed($"Magic:{skill.Name}");
            _skillsListPanel.AddChild(btn);
        }

        _skillsListPanel.Show();
        if (_skillsListPanel.GetChildCount() > 0)
            _skillsListPanel.GetChild<Button>(0).GrabFocus();
    }

    public void HideTargetCursor() => _targetCursor.Hide();
}
