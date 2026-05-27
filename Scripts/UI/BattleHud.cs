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
    const int MaxLogLines = 1;

    Control _uiScene;
    Control _actionMenu;
    Control _playerPanel;
    VBoxContainer _skillsListPanel;
    Sprite2D _targetCursor;
    Container _enemyList;
    Control _damageLayer;
    Tween _cursorTween;

    /// <summary>Décalage vertical (en unités design 640×360) pour placer les widgets au-dessus des sprites.</summary>
    [Export] float EnemyStatYOffset = 85f;

    readonly Dictionary<Enemy, EnemyStatWidgets> _enemyWidgets = new();
    readonly List<string> _logHistory = new();

    [Export] RichTextLabel _playerHpLabel;
    [Export] RichTextLabel _playerMpLabel;
    [Export] RichTextLabel _logs;
    [Export] KarmaBanner _karmaBanner;

    struct EnemyStatWidgets
    {
        public Panel Root;
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
        _enemyList = GetNode<Container>("Scene/CombatantsPanel/EnemyList");

        _damageLayer = new Control
        {
            Name = "DamageLayer",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 500,
        };
        _damageLayer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _uiScene.AddChild(_damageLayer);

        _playerHpLabel = GetNodeOrNull<RichTextLabel>("Scene/CombatantsPanel/PlayerPanel/PlayerStat/HBoxContainer/VBoxContainer/HpRow/NB_HP");
        _playerMpLabel = GetNodeOrNull<RichTextLabel>("Scene/CombatantsPanel/PlayerPanel/PlayerStat/HBoxContainer/VBoxContainer/MpRow/NB_MP");
        _karmaBanner = GetNodeOrNull<KarmaBanner>("Scene/KarmaBanner");

        if (_logs != null)
        {
            _logs.Text = "";
            _logs.ScrollActive = false;
        }
        else
            GD.PrintErr("[BattleHud] RichTextLabel de logs introuvable.");

        // EnemyList est vide désormais (les widgets flottent au-dessus des sprites)
        _enemyList.Hide();

        // Panneau de log : une ligne, haut-gauche, marge droite pour la KarmaBanner (qui démarre à 0.76)
        var logPanel = GetNodeOrNull<Control>("Scene/LogPanel");
        if (logPanel != null)
        {
            logPanel.AnchorLeft   = 0.01f;
            logPanel.AnchorTop    = 0.01f;
            logPanel.AnchorRight  = 0.73f;
            logPanel.AnchorBottom = 0.10f;
        }

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

        StartCursorAnim();
    }

    static readonly StyleBoxFlat _enemyPanelStyle = new()
    {
        BgColor = new Color(0f, 0f, 0f, 0.65f),
        CornerRadiusTopLeft     = 3,
        CornerRadiusTopRight    = 3,
        CornerRadiusBottomLeft  = 3,
        CornerRadiusBottomRight = 3,
    };

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

            var root = statScene.Instantiate<Panel>();
            root.AddThemeStyleboxOverride("panel", _enemyPanelStyle);
            root.AnchorLeft   = 0;
            root.AnchorTop    = 0;
            root.AnchorRight  = 0;
            root.AnchorBottom = 0;
            root.Hide();
            _uiScene.AddChild(root);

            var widgets = new EnemyStatWidgets
            {
                Root      = root,
                NameLabel = root.GetNode<Label>("VBox/NameLabel"),
                HpBar     = root.GetNode<TextureProgressBar>("VBox/HpBar"),
                HpLabel   = root.GetNode<Label>("VBox/HpLabel"),
            };

            _enemyWidgets[enemy] = widgets;
            RefreshEnemy(enemy);
        }
    }

    /// <summary>Affiche le widget de l'ennemi ciblé, masque les autres.</summary>
    public void ShowEnemyInfo(Enemy enemy)
    {
        foreach (var pair in _enemyWidgets)
        {
            bool isTarget = pair.Key == enemy && GodotObject.IsInstanceValid(pair.Value.Root);
            if (GodotObject.IsInstanceValid(pair.Value.Root))
                pair.Value.Root.Visible = isTarget;
        }
    }

    /// <summary>Masque tous les widgets ennemis (curseur caché, hors sélection).</summary>
    public void HideAllEnemyInfo()
    {
        foreach (var pair in _enemyWidgets)
        {
            if (GodotObject.IsInstanceValid(pair.Value.Root))
                pair.Value.Root.Hide();
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

    /// <summary>
    /// Appelée depuis BattleManager._Process avec la position viewport déjà projetée
    /// (BattleManager a accès à la Camera3D, pas le CanvasLayer).
    /// </summary>
    public void SetEnemyWidgetPosition(Enemy enemy, Vector2 viewportPos)
    {
        if (!_enemyWidgets.TryGetValue(enemy, out var widgets)) return;
        if (!GodotObject.IsInstanceValid(widgets.Root)) return;

        Vector2 uiPos     = ViewportToUiScene(viewportPos);
        float   halfWidth = Mathf.Max(widgets.Root.Size.X, widgets.Root.GetCombinedMinimumSize().X) / 2f;

        widgets.Root.Position = new Vector2(uiPos.X - halfWidth, uiPos.Y - EnemyStatYOffset);
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

    public void OnPlayerDamageReceived(int damage)
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
        if (_damageLayer == null)
            return;

        Vector2 localPos = ViewportToUiScene(viewportPosition);
        BattleDamagePopup.Spawn(_damageLayer, localPos, amount, color, this);
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

        _logs.Text = _logHistory[^1];
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

    public void HideTargetCursor()
    {
        _targetCursor.Hide();
        HideAllEnemyInfo();
    }
}
