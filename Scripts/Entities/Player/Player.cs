using Godot;
using System;
using System.Collections.Generic;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;

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
	public int CurrentMp
	{
		get => _stats.CurrentMp; 
		set => _stats.CurrentMp = value;
	}

	public int Strength => _stats.Strength;
	public int Dexterity => _stats.Dexterity;
	public int Spirit => _stats.Spirit;
	public int Defense => _stats.Defense;

	public List<Skill> LearnedSkills = new List<Skill>();
	
	public override void _Ready()
	{
		GameManager.Instance.PlayerLevelUp += OnPlayerLevelUp;
		GameManager.Instance.CurrentPlayer = this;
		
		_stats = GetNode<StatHandler>("PlayerStats");
		var allSkills = SkillManager.LoadSkills();
		foreach (var skill in allSkills)
		{
			if (skill.Classes.Contains("Magus")) //TODO |-> CHANGER PAR LA CLASSE DE FAÇON DYNAMIQUE
			{
				LearnedSkills.Add(skill);
				GD.Print($"Le joueur a appris la skill {skill.Name}");
			}
		}
		
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
