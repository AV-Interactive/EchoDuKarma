using Godot;
using System;
using System.Collections.Generic;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;

public partial class Player : CharacterBody3D, IBattler
{ 
	[Export] public float Speed = 5.0f;
	[Export] public Sprite3D Sprite;
	
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

	PlayerVisuals _visuals;
	
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

		if (Sprite != null)
		{
			// On s'assure d'avoir un matériau unique pour ne pas impacter les autres sprites
			StandardMaterial3D material = new StandardMaterial3D();
			Sprite.MaterialOverride = material;

			// Activer le mode Billboard
			// Y-Billboard permet au sprite de pivoter sans pencher en avant/arrière
			material.BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled;
			material.BillboardKeepScale = true;
	
			// Si tu veux que le sprite ne soit pas affecté par les ombres (look rétro HD-2D)
			material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		}
		
		_visuals = GetNode<PlayerVisuals>("Node3D/Sprite3D");
	}

	void OnPlayerLevelUp(int levelUpAmount)
	{
		_stats.LevelUp();
		GD.Print($"Le joueur est maintenant niveau {_stats.CurrentLevel} et à {_stats.CurrentPv} PV");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!GameManager.Instance.PlayerMoved) return;

		// 1. Récupération de l'entrée (Vector2)
		Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
	
		// 2. Récupération de la caméra active
		var camera = GetViewport().GetCamera3D();
	
		// 3. Calcul des vecteurs de direction relatifs à la caméra
		// On récupère la base de la caméra (son orientation)
		Vector3 forward = camera.GlobalTransform.Basis.Z;
		Vector3 right = camera.GlobalTransform.Basis.X;

		// IMPORTANT : On annule le Y pour rester au sol et on normalise
		forward.Y = 0;
		right.Y = 0;
		forward = forward.Normalized();
		right = right.Normalized();

		// 4. Calcul de la direction de mouvement finale
		// Note : en Godot, Forward est -Z, donc on inverse inputDir.Y
		Vector3 direction = (forward * inputDir.Y) + (right * inputDir.X);

		Vector3 velocity = Velocity;
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * 5.0f;
			velocity.Z = direction.Z * 5.0f;
		
			// Gestion du Flip du sprite selon le mouvement relatif
			UpdateSpriteDirection(direction.X);
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, 5.0f);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, 5.0f);
		}

		Velocity = velocity;
		MoveAndSlide();
		_visuals.UpdateFrame(Velocity);
	}

	void UpdateSpriteDirection(float moveX)
	{
		if(Sprite == null) return;
		if (Mathf.Abs(moveX) > .1f)
		{
			Sprite.FlipH = moveX < 0;
		}
	}
}
