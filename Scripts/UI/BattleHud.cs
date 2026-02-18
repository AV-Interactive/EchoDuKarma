using Godot;
using System;
using EchoduKarma.Scripts.Data;

public partial class BattleHud : CanvasLayer
{
    [Signal] public delegate void ActionSelectedEventHandler(string actionName);

    Control _actionMenu;
    [Export] public RichTextLabel playerHpLabel;
    [Export] public RichTextLabel _playerMpLabel;


    public override void _Ready()
    {
        _actionMenu = GetNode<Control>("Scene/ActionMenu");
        playerHpLabel = GetNodeOrNull<RichTextLabel>("Scene/StatPanel/Panel/VBoxContainer/Control/HBoxContainer/VBoxContainer/HBoxContainer/NB_HP");
        
        _actionMenu.Hide();
        
        // Stats Player
        playerHpLabel.Text = $"{GameManager.Instance.CurrentPlayer.CurrentPv.ToString()}/{GameManager.Instance.CurrentPlayer.Pv.ToString()} HP";
        
        GetNode<Button>("Scene/ActionMenu/Panel/VBoxContainer/BtnAttack").Pressed += () => OnButtonPressed("Attack");
        GetNode<Button>("Scene/ActionMenu/Panel/VBoxContainer/BtnMagic").Pressed += () => OnButtonPressed("Magic");

        var battleManager = GetTree().Root.FindChild("BattleManager", true, false) as BattleManager;
        if (battleManager != null) battleManager.PlayerDamage += OnPlayerDamageReceived;
    }

    public void ShowMenu() => _actionMenu.Show();
    public void HideMenu() => _actionMenu.Hide();
    
    void OnButtonPressed(string action)
    {
        HideMenu();
        EmitSignal(nameof(ActionSelected), action);
    }

    void OnPlayerDamageReceived(int damage)
    {
        GD.Print($"Player HP : {damage}");
        playerHpLabel.Text = $"{(GameManager.Instance.CurrentPlayer.CurrentPv - damage).ToString()}/{GameManager.Instance.CurrentPlayer.Pv.ToString()} HP";
    }
}
