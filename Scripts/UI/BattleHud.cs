using Godot;
using System;
using System.Collections.Generic;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;

public partial class BattleHud : CanvasLayer
{
    [Signal] public delegate void ActionSelectedEventHandler(string actionName);

    Control _actionMenu;
    VBoxContainer _skillsListPanel;
    Sprite2D _targetCursor;
    Tween _cursorTween;
    
    [Export] RichTextLabel _playerHpLabel;
    [Export] RichTextLabel _playerMpLabel;
    [Export] RichTextLabel _logs;

    Tween _logTween;

    public override void _Ready()
    {
        _actionMenu = GetNode<Control>("Scene/Actions/Panel/ActionMenu");
        _skillsListPanel = GetNode<VBoxContainer>("Scene/Actions/Panel/SkillsList");
        
        _targetCursor = GetNode<Sprite2D>("Scene/TargetCursor");
        
        _playerHpLabel = GetNodeOrNull<RichTextLabel>("Scene/StatPanel/Panel/VBoxContainer/Control/HBoxContainer/VBoxContainer/HBoxContainer/NB_HP");
        _playerMpLabel = GetNodeOrNull<RichTextLabel>("Scene/StatPanel/Panel/VBoxContainer/Control/HBoxContainer/VBoxContainer/HBoxContainer/NB_MP");

        if (_logs != null)
        {
            _logs.Text = "";
        }
        else
        {
            GD.PrintErr("ERREUR CRITIQUE : Le RichTextLabel de logs est introuvable dans le BattleHud.");
        }
        
        _actionMenu.Hide();
        _skillsListPanel.Hide();
        
        // Stats Player
        _playerHpLabel.Text = $"{GameManager.Instance.CurrentPlayer.CurrentPv.ToString()}/{GameManager.Instance.CurrentPlayer.Pv.ToString()} HP";
        
        GetNode<Button>("Scene/Actions/Panel/ActionMenu/BtnAttack").Pressed += () => OnButtonPressed("Attack");
        GetNode<Button>("Scene/Actions/Panel/ActionMenu/BtnMagic").Pressed += () => OnButtonPressed("Magic");
        GetNode<Button>("Scene/Actions/Panel/ActionMenu/BtnDefense").Pressed += () => OnButtonPressed("Defense");
        GetNode<Button>("Scene/Actions/Panel/ActionMenu/BtnEscape").Pressed += () => OnButtonPressed("Flee");

        var battleManager = GetTree().Root.FindChild("BattleManager", true, false) as BattleManager;
        if (battleManager != null) battleManager.PlayerDamage += OnPlayerDamageReceived;
        
        // On empêche la perte du focus
        GetViewport().GuiFocusChanged += (node) =>
        {
            if (node == null)
            {
                _actionMenu.GetNode<Button>("Scene/Actions/Panel/ActionMenu/BtnAttack").GrabFocus();
            }
        };
        
        StartCursorAnim();
    }

    public void ShowMenu()
    {
        _actionMenu.Show();
        var btnAttack = _actionMenu.GetNode<Button>("BtnAttack");
    
        btnAttack?.GrabFocus();
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
        GD.Print($"Player HP : {damage}");
        _playerHpLabel.Text = $"{(GameManager.Instance.CurrentPlayer.CurrentPv - damage).ToString()}/{GameManager.Instance.CurrentPlayer.Pv.ToString()} HP";
    }
    
    public void UpdatePlayerStats(Player player)
    {
        // Mise à jour des PV
        if (_playerHpLabel != null)
        {
            _playerHpLabel.Text = $"{player.CurrentPv}/{player.Pv} HP";
        }

        // Mise à jour des PM
        if (_playerMpLabel != null)
        {
            _playerMpLabel.Text = $"{player.CurrentMp}/{player.Mp} MP";
        }
    }

    public void ShowDamage(Vector2 position, int amount, Color color)
    {
        Label label = new Label();
        label.Text = amount.ToString();
        label.AddThemeFontSizeOverride("font_size", 65);
        label.Modulate = color;
        label.Position = position;

        AddChild(label);
        
        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", position.Y - 50, .5f);
        tween.Parallel().TweenProperty(label, "modulate:a", 0, .5f);
        tween.Finished += () => label.QueueFree();
    }

    public async void ShowLogs(string message)
    {
        // 1. On nettoie l'ancien Tween s'il existe encore
        if (_logTween != null && _logTween.IsRunning())
        {
            _logTween.Kill();
        }

        // Sécurité : Vérifier si le label existe bien
        if (_logs == null)
        {
            GD.PrintErr("BattleHud : _logs est nul ! Vérifie l'assignation dans l'éditeur.");
            return;
        }

        _logs.Text = message;
        _logs.VisibleRatio = 0;

        // 2. CRITIQUE : Il faut créer le Tween avant de l'utiliser !
        _logTween = CreateTween();
    
        // 3. On configure l'animation
        _logTween.TweenProperty(_logs, "visible_ratio", 1, 0.5f);
    
        // On attend la fin de l'animation
        await ToSignal(_logTween, "finished");
    }

    void StartCursorAnim()
    {
        _cursorTween = CreateTween().SetLoops();
        
        _cursorTween.TweenProperty(_targetCursor, "offset:y", 10.0f, 0.25f).SetTrans(Tween.TransitionType.Sine);
        _cursorTween.TweenProperty(_targetCursor, "offset:y", 0.0f, 0.25f).SetTrans(Tween.TransitionType.Sine);
    }
    
    public void UpdateTargetCursor(Vector2 targetPos)
    {
        _targetCursor.Show();
        targetPos = new Vector2(targetPos.X, targetPos.Y - 100);
        _targetCursor.GlobalPosition = targetPos;
    }

    public void ShowMagicMenu(List<Skill> skills)
    {
        GD.Print("ShowMagicMenu");
        _actionMenu.Hide();

        foreach (Node n in _skillsListPanel.GetChildren())
        {
            _skillsListPanel.RemoveChild(n);
            n.QueueFree();
        }

        foreach (var skill in skills)
        {
            Button btn = new Button();
            btn.Text = $"{skill.Name} ({skill.Cost} MP)";
            btn.AddThemeFontSizeOverride("font_size", 35);
            btn.Pressed += () => OnButtonPressed($"Magic:{skill.Name}");
            _skillsListPanel.AddChild(btn);
            GD.Print($"Ajout de la skill {skill.Name} au menu");
        }
        
        _skillsListPanel.Show();
        if (_skillsListPanel.GetChildCount() > 0)
        {
            var firstBtn = _skillsListPanel.GetChild<Button>(0);
            firstBtn.GrabFocus();
        }
    }
    
    public void HideTargetCursor() => _targetCursor.Hide();
}
