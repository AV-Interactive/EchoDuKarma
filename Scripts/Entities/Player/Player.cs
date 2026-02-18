using Godot;
using System;
using EchoduKarma.Scripts.Data;

public partial class Player : CharacterBody2D, IBattler
{ 
	[Export] public float Speed = 250.0f;
	
	StatHandler _stats;
	
	public string Name => "Player";
	public int Level => _stats.CurrentLevel;
	public int Pv => _stats.PvMax;
    public int CurrentPv 
	{ 
		get => _stats.CurrentPv; 
		set => _stats.CurrentPv = value; 
	}
	public int Mp => _stats.MpMax;
	public int CurrentMp  => _stats.CurrentMp;
	public int Strength => _stats.Strength;
	public int Dexterity => _stats.Dexterity;
	public int Spirit => _stats.Spirit;
	public int Defense => _stats.Defense;
	
	public override void _Ready()
	{
		GameManager.Instance.PlayerLevelUp += OnPlayerLevelUp;
		GameManager.Instance.CurrentPlayer = this;
		_stats = GetNode<StatHandler>("PlayerStats");
		GD.Print($"Le joueur est niveau {_stats.CurrentLevel} et à {_stats.CurrentPv} PV");
	}

	void OnPlayerLevelUp(int levelUpAmount)
	{
		_stats.LevelUp();
		GD.Print($"Le joueur est maintenant niveau {_stats.CurrentLevel} et à {_stats.CurrentPv} PV");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!GameManager.Instance.PlayerMoved) return;
		Vector2 inputDirection = Input.GetVector("left", "right", "up", "down");
		
		Velocity = inputDirection * Speed;
		MoveAndSlide();
	}
}
