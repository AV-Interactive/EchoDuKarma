using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;

public partial class Player : CharacterBody3D, IBattler
{ 
	[Export] public float Speed = 5.0f;
	[Export] public Sprite3D Sprite;
	[Export] public NodePath TerrainPath;
	[Export] public float BorderMargin = 2f;
	
	private Vector2 _mapMin = new Vector2(-100f, -100f);
	private Vector2 _mapMax = new Vector2(100f, 100f);
	
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

	public int Strength => _stats.Strength + GetEquipmentBonuses().Strength;
	public int Dexterity => _stats.Dexterity + GetEquipmentBonuses().Dexterity;
	public int Spirit => _stats.Spirit + GetEquipmentBonuses().Spirit;
	public int Defense => _stats.Defense + GetEquipmentBonuses().Defense;

	EquipmentStatBonuses GetEquipmentBonuses() =>
		InventoryManager.Instance?.GetEquipmentBonuses() ?? EquipmentStatBonuses.Zero;

	public List<Skill> LearnedSkills = new List<Skill>();

	ElementType _affinity = ElementType.None;
	public ElementType Affinity => _affinity;

	PlayerVisuals _visuals;
	
	public override void _Ready()
	{
		GameManager.Instance.PlayerLevelUp += OnPlayerLevelUp;
		GameManager.Instance.CurrentPlayer = this;

		_stats = GetNode<StatHandler>("PlayerStats");
		GameManager.Instance.ApplyBattleSnapshotToPlayer(this);

		var hero = HeroManager.GetDefaultHero();
		if (hero != null)
			_affinity = hero.Affinity;
		else
			GD.PrintErr("[Player] Héros par défaut introuvable dans heroes.csv.");

		RefreshLearnedSkills(logNewSkills: true);

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
		
		// Auto-détection des limites terrain
		if (!string.IsNullOrEmpty(TerrainPath.ToString()))
		{
			var terrain = GetNode<MeshInstance3D>(TerrainPath);
			Aabb worldAabb = terrain.GlobalTransform * terrain.GetAabb();
			_mapMin = new Vector2(worldAabb.Position.X + BorderMargin, worldAabb.Position.Z + BorderMargin);
			_mapMax = new Vector2(worldAabb.End.X - BorderMargin, worldAabb.End.Z - BorderMargin);
			GD.Print($"Player limits: {_mapMin} | {_mapMax}");
		}

	}

	public void RefreshLearnedSkills(bool logNewSkills = false)
	{
		var hero = HeroManager.GetDefaultHero();
		if (hero == null || _stats == null)
			return;

		var previous = new HashSet<string>(LearnedSkills.Select(s => s.Name));
		LearnedSkills.Clear();
		foreach (Skill skill in SkillManager.GetUnlockedForClass(hero.ClassName, Level))
		{
			LearnedSkills.Add(skill);
			if (logNewSkills && !previous.Contains(skill.Name))
				GD.Print($"[Player] Compétence débloquée : {skill.Name} (niveau {skill.LevelRequired})");
		}
	}

	void OnPlayerLevelUp(int levelUpAmount)
	{
		for (int i = 0; i < levelUpAmount; i++)
			_stats.LevelUp();

		RefreshLearnedSkills(logNewSkills: true);
		GD.Print($"Le joueur est maintenant niveau {_stats.CurrentLevel} et à {_stats.CurrentPv} PV");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!GameManager.Instance.PlayerMoved) return;

		// 1. Récupération de l'entrée (Vector2)
		Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
	
		var camera = GetViewport().GetCamera3D() as CameraFollow3D;
		if (camera == null)
			return;

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
		
		// Clamp position joueur dans les limites de la map
		if (camera != null)
		{
			GlobalPosition = new Vector3(
				Mathf.Clamp(GlobalPosition.X, camera.MapMin.X + camera.BorderMargin, camera.MapMax.X - camera.BorderMargin),
				GlobalPosition.Y,
				Mathf.Clamp(GlobalPosition.Z, camera.MapMin.Y + camera.BorderMargin, camera.MapMax.Y - camera.BorderMargin)
			);
		}
		
		_visuals.UpdateFrame(Velocity);
	}

	void UpdateSpriteDirection(float moveX)
	{
		if (Sprite == null)
			return;

		// Les sprites LPC ont des lignes LEFT/RIGHT dédiées : pas de flip horizontal.
		Sprite.FlipH = false;
	}
}
