using Godot;
using System;
using EchoduKarma.Scripts.Data;

public partial class BattleHud : CanvasLayer
{
    [Signal] public delegate void ActionSelectedEventHandler(string actionName);

    Control _actionMenu;
    [Export] RichTextLabel _playerHpLabel;
    [Export] RichTextLabel _playerMpLabel;
    [Export] RichTextLabel _logs;

    Tween _logTween;

    public override void _Ready()
    {
        _actionMenu = GetNode<Control>("Scene/ActionMenu");
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
        
        // Stats Player
        _playerHpLabel.Text = $"{GameManager.Instance.CurrentPlayer.CurrentPv.ToString()}/{GameManager.Instance.CurrentPlayer.Pv.ToString()} HP";
        
        GetNode<Button>("Scene/ActionMenu/Panel/VBoxContainer/BtnAttack").Pressed += () => OnButtonPressed("Attack");
        GetNode<Button>("Scene/ActionMenu/Panel/VBoxContainer/BtnMagic").Pressed += () => OnButtonPressed("Magic");
        GetNode<Button>("Scene/ActionMenu/Panel/VBoxContainer/BtnDefense").Pressed += () => OnButtonPressed("Defense");
        GetNode<Button>("Scene/ActionMenu/Panel/VBoxContainer/BtnEscape").Pressed += () => OnButtonPressed("Flee");

        var battleManager = GetTree().Root.FindChild("BattleManager", true, false) as BattleManager;
        if (battleManager != null) battleManager.PlayerDamage += OnPlayerDamageReceived;
        
        // On empêche la perte du focus
        GetViewport().GuiFocusChanged += (node) =>
        {
            if (node == null)
            {
                _actionMenu.GetNode<Button>("Scene/ActionMenu/Panel/VBoxContainer/BtnAttack").GrabFocus();
            }
        };
    }

    public void ShowMenu()
    {
        _actionMenu.Show();
        var btnAttack = _actionMenu.GetNode<Button>("Panel/VBoxContainer/BtnAttack");
    
        btnAttack?.GrabFocus();
    }

    public void HideMenu() => _actionMenu.Hide();
    
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
}
