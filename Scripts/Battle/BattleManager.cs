using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;

/// <summary>
/// Lead Developer Refactor: Orchestrates the turn-based battle logic.
/// Handles State transitions, turn ordering, and execution of player/enemy actions.
/// </summary>
public partial class BattleManager : Node
{
    #region --- Enums & Signals ---

    public enum BattleState
    {
        Setup,      // Initializing units and turn order
        Selection,  // Waiting for player input or starting AI turn
        Action,     // Executing animations and damage
        Evaluation, // Checking win/loss conditions
        Victory,    // Player won
        Defeat      // Player lost
    }
    
    public enum BattleEndReason
    {
        Victory,
        Defeat,
        Flee
    }

    [Signal] public delegate void PlayerDamageEventHandler(int damage);
    [Signal] public delegate void BattleEndedEventHandler(BattleEndReason reason);

    #endregion

    #region --- Fields & Properties ---

    [ExportGroup("Nodes & Scenes")]
    [Export] public BattleHud _hud;
    [Export] public PackedScene EnemyScene { get; set; }
    [Export] CameraDirector _cameraDirector;
    [Export] PackedScene BattleActorScene { get; set; }

    [ExportGroup("Combatants")]
    private Player _player;                          // source de vérité des stats
    private BattleActor _playerActor;               // coquille visuelle joueur
    private Node3D _playerAnchor;                   // point de spawn joueur
    private Node3D _enemiesAnchor;                  // point de spawn ennemis
    private readonly List<Enemy> _enemies = new List<Enemy>();
    private List<EnemyStats> _enemyStatsSource = new List<EnemyStats>();

    [ExportGroup("Turn Management")]
    private BattleState _currentState;
    private List<IBattler> _turnOrder = new List<IBattler>();
    private int _currentTurnIndex = 0;

    [ExportGroup("Action Selection State")]
    private bool _isPlayerDefending = false;
    private int _targetIndex = 0;
    private bool _isSelectingTarget = false;
    private Skill _selectedSkill;
    private bool _isActionRunning = false;
    
    private int _retryCount = 0;
    private bool _isReady = false;

    #endregion

    #region --- Lifecycle & Initialization ---

    public override void _Ready()
    {
        CallDeferred(nameof(InitializeBattle));
    }

    private void InitializeBattle()
    {
        _player = GameManager.Instance.CurrentPlayer;

        if (_player == null)
        {
            GD.PrintErr("[BattleManager] CRITICAL ERROR: Player not found in GameManager.");
            return;
        }

        _enemyStatsSource = GameManager.Instance.ListEnemiesBattle;

        // Auto-link HUD if not assigned
        if (_hud != null)
        {
            _hud.ActionSelected += OnPlayerActionSelected;
        }
        else
        {
            GD.PrintErr("[BattleManager] WARNING: BattleHud not found.");
        }
        
        // AUTO LINK des acteurs
        _playerAnchor  = GetTree().Root.FindChild("PlayerAnchor",  true, false) as Node3D;
        _enemiesAnchor = GetTree().Root.FindChild("EnemiesAnchor", true, false) as Node3D;

        if (_playerAnchor == null)
            GD.PrintErr("[BattleManager] WARNING: PlayerAnchor not found.");
        if (_enemiesAnchor == null)
            GD.PrintErr("[BattleManager] WARNING: EnemiesAnchor not found.");

        _isReady = true;
        ChangeState(BattleState.Setup);
    }

    #endregion

    #region --- State Machine Core ---

    /// <summary>
    /// Centralized state switcher to ensure consistent logic flow.
    /// </summary>
    public void ChangeState(BattleState newState)
    {
        _currentState = newState;
        GD.Print($"[BattleState] Entering: {newState}");

        switch (newState)
        {
            case BattleState.Setup:      HandleSetupState(); break;
            case BattleState.Selection:  HandleSelectionState(); break;
            case BattleState.Action:     HandleActionState(); break;
            case BattleState.Evaluation: HandleEvaluationState(); break;
            case BattleState.Victory:    HandleVictoryState(); break;
            case BattleState.Defeat:     HandleDefeatState(); break;
        }
    }

    private void HandleSetupState()
    {
        SpawnPlayer();
        SpawnEnemies();
        DetermineTurnOrder();
    }

    private void HandleSelectionState()
    {
        if (_turnOrder.Count == 0)
        {
            GD.PushError("[BattleManager] Turn order empty. Resetting to Setup.");
            ChangeState(BattleState.Setup);
            return;
        }

        // Loop turn index if out of bounds
        if (_currentTurnIndex >= _turnOrder.Count)
            _currentTurnIndex = 0;

        var activeUnit = _turnOrder[_currentTurnIndex];

        if (activeUnit is Player)
        {
            _isPlayerDefending = false;
            _hud?.ShowMenu();
        }
        else
        {
            // Auto-transition to Action for AI units
            ChangeState(BattleState.Action);
        }
    }

    private void HandleActionState() => ExecuteCurrentTurn();
    private void HandleEvaluationState() => CheckBattleStatus();
    private void HandleVictoryState() => HandleVictory();

    private void HandleDefeatState()
    {
        _hud?.ShowLogs($"Défaite... {_player.Name} a succombé.");
        EndBattle(BattleEndReason.Defeat);
    }

    #endregion

    #region --- Player Input & Selection ---

    /// <summary>
    /// Callback from BattleHud when a button is pressed.
    /// </summary>
    private void OnPlayerActionSelected(string actionName)
    {
        if (_currentState != BattleState.Selection) return;

        // Reset context
        _isPlayerDefending = false;
        _selectedSkill = null;

        if (actionName.StartsWith("Magic:"))
        {
            ProcessMagicSelection(actionName);
            return;
        }

        switch (actionName)
        {
            case "Attack":
                StartTargetSelection();
                break;
            case "Magic":
                _hud.ShowMagicMenu(_player.LearnedSkills);
                break;
            case "Defense":
                ExecuteDefense();
                break;
            case "Flee":
                ExecuteFlee();
                break;
        }
    }

    private void ProcessMagicSelection(string actionName)
    {
        string skillName = actionName.Split(':')[1];
        _selectedSkill = _player.LearnedSkills.Find(s => s.Name == skillName);

        if (_selectedSkill == null) return;

        // Support skills (Heal/Buff) are self-targeted for now
        if (_selectedSkill.Type == SkillType.Support)
        {
            ExecuteMagicAction(_player, _selectedSkill);
        }
        else
        {
            StartTargetSelection();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if(!_isReady) return;
        if (!_isSelectingTarget) return;

        if (@event.IsActionPressed("ui_right"))
        {
            _targetIndex = GetNextEnemyIndex(1);
            UpdateTargetCursor();
        }
        else if (@event.IsActionPressed("ui_left"))
        {
            _targetIndex = GetNextEnemyIndex(-1);
            UpdateTargetCursor();
        }
        else if (@event.IsActionPressed("ui_accept"))
        {
            ConfirmTargetSelection();
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            CancelTargetSelection();
        }
    }
    
    private int GetNextEnemyIndex(int direction)
    {
        var camera = GetViewport().GetCamera3D();
        if (camera == null)
            return (_targetIndex + direction + _enemies.Count) % _enemies.Count;

        // On projette chaque ennemi en espace écran et on trie par X écran
        var sorted = _enemies
            .Select((e, i) => new { Index = i, ScreenX = camera.UnprojectPosition(e.GlobalPosition).X })
            .OrderBy(e => e.ScreenX)
            .ToList();

        // Position du currentTarget dans la liste triée
        int sortedPos = sorted.FindIndex(e => e.Index == _targetIndex);
        int nextSortedPos = (sortedPos + direction + sorted.Count) % sorted.Count;

        return sorted[nextSortedPos].Index;
    }

    private void StartTargetSelection()
    {
        if (_enemies == null || _enemies.Count == 0) 
        {
            GD.PrintErr("[BattleManager] StartTargetSelection: No enemies to target!");
            return;
        }

        _isSelectingTarget = true;
        _targetIndex = 0;
        UpdateTargetCursor();
    }

    private void UpdateTargetCursor()
    {
        if (_enemies == null || _targetIndex < 0 || _targetIndex >= _enemies.Count)
        {
            _hud?.HideTargetCursor();
            return;
        }
        
        // Lead Dev Tip: On utilise GetScreenPositionOfNode car le curseur est un Sprite2D dans l'UI
        _hud?.UpdateTargetCursor(GetScreenPositionOfNode(_enemies[_targetIndex]));
    }

    private void ConfirmTargetSelection()
    {
        _isSelectingTarget = false;
        _hud?.HideTargetCursor();

        if (_selectedSkill != null)
            ExecuteMagicAction(_enemies[_targetIndex], _selectedSkill);
        else
            ExecutePhysicalAttack(_enemies[_targetIndex]);
    }

    private void CancelTargetSelection()
    {
        _isSelectingTarget = false;
        _hud?.HideTargetCursor();
        _hud?.ShowMenu();
    }

    #endregion

    #region --- Combat Execution: Player ---

    private async void ExecutePhysicalAttack(Enemy target)
    {
        if (_isActionRunning) return;
        _isActionRunning = true;

        if (_player == null || target == null)
        {
            ChangeState(BattleState.Evaluation);
            return;
        }

        ChangeState(BattleState.Action);

        // CHANGEMENT ANGLE CAMERA
        await _cameraDirector.CutTo(CameraDirector.CameraShot.PlayerAttack);
        _playerActor?.OnCameraChanged(CameraDirector.CameraShot.PlayerAttack);

        int damage = CalculatePhysicalDamage(_player.Strength, target.Defense);
        target.CurrentPv -= damage;

        _hud?.ShowLogs($"{_player.Name} attaque {target.EnemyName} pour {damage} dégâts !");
        
        // Lead Dev Tip: On utilise GetScreenPositionOfNode pour projeter la position 3D en 2D pour l'UI
        Vector2 screenPos = GetScreenPositionOfNode(target);
        _hud?.ShowDamage(new Vector2(screenPos.X, screenPos.Y - 50), damage, Colors.Red);
        
        target.PlayHitEffect();

        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
        
        // RETOUR PLAN NEUTRE
        await _cameraDirector.CutTo(CameraDirector.CameraShot.Neutral);
        _playerActor?.OnCameraChanged(CameraDirector.CameraShot.Neutral);
        
        _isActionRunning = false;
        
        ChangeState(BattleState.Evaluation);
    }

    private async void ExecuteMagicAction(IBattler target, Skill skill)
    {
        if (_isActionRunning) return;
        _isActionRunning = true;

        if (_player.CurrentMp < skill.Cost)
        {
            _hud?.ShowLogs($"{_player.Name} n'a pas assez de MP pour utiliser {skill.Name} !");
            await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
            _hud?.ShowMenu();
            return;
        }

        ChangeState(BattleState.Action);
        _player.CurrentMp -= skill.Cost;
        _hud?.UpdatePlayerStats(_player);

        if (skill.Type == SkillType.Support)
            ApplyHealEffect(skill);
        else
            ApplyMagicDamage(target, skill);

        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");

        _isActionRunning = false;
        
        ChangeState(BattleState.Evaluation);
    }

    private void ApplyHealEffect(Skill skill)
    {
        _hud?.ShowLogs($"{_player.Name} utilise {skill.Name} !");
        int healAmount = CalculateHealAmount(skill);

        _player.CurrentPv = Math.Min(_player.Pv, _player.CurrentPv + healAmount);
        _hud?.UpdatePlayerStats(_player);
        _hud?.ShowDamage(GetPlayerUIPosition(), healAmount, Colors.Green);
    }

    private void ApplyMagicDamage(IBattler target, Skill skill)
    {
        _hud?.ShowLogs($"{_player.Name} lance {skill.Name} sur {target.Name} !");
        int damage = CalculateMagicDamage(_player, target, skill);

        if (target is Enemy e)
        {
            e.CurrentPv -= damage;
            e.PlayHitEffect();
        }

        Vector2 screenPos = GetScreenPositionOfNode(target as Node3D);
        _hud?.ShowDamage(new Vector2(screenPos.X, screenPos.Y - 50), damage, Colors.Red);
    }

    private async void ExecuteDefense()
    {
        ChangeState(BattleState.Action);
        _isPlayerDefending = true;
        _hud?.ShowLogs($"{_player.Name} se prépare à encaisser !");
        await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
        ChangeState(BattleState.Evaluation);
    }

    async void ExecuteFlee()
    {
        ChangeState(BattleState.Action);
        _hud?.ShowLogs($"{_player.Name} tente de fuir...");
        await ToSignal(GetTree().CreateTimer(1.5f), "timeout");

        if (GD.Randf() > 0.5f)
        {
            _hud?.ShowLogs("Fuite réussie !");
            await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
            EndBattle(BattleEndReason.Flee);
        }
        else
        {
            _hud?.ShowLogs("L'ennemi vous barre la route !");
            await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
            ChangeState(BattleState.Evaluation);
        }
    }

    #endregion

    #region --- Combat Execution: Enemy ---

    private void ExecuteCurrentTurn()
    {
        if (_currentTurnIndex >= _turnOrder.Count) return;
        var activeUnit = _turnOrder[_currentTurnIndex];

        if (activeUnit is Enemy enemy)
            ProcessEnemyTurn(enemy);
    }

    private async void ProcessEnemyTurn(Enemy enemy)
    {
        if (enemy == null || enemy.CurrentPv <= 0)
        {
            ChangeState(BattleState.Evaluation);
            return;
        }

        // CHANGEMENT ANGLE CAMERA
        await _cameraDirector.CutTo(CameraDirector.CameraShot.EnemyAttack);
        _playerActor?.OnCameraChanged(CameraDirector.CameraShot.EnemyAttack);
        
        _hud?.ShowLogs($"{enemy.EnemyName} prépare son attaque...");
        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");

        int damage = CalculatePhysicalDamage(enemy.Stats.Strength, _player.Defense);

        if (_isPlayerDefending)
        {
            damage = Math.Max(1, damage / 2);
            _hud?.ShowLogs($"{_player.Name} bloque une partie de l'attaque !");
        }

        ShakeScreen();
        _player.CurrentPv -= damage;
        _hud?.UpdatePlayerStats(_player);
        _hud?.ShowDamage(GetPlayerUIPosition(), damage, Colors.Red);
        
        _hud?.ShowLogs($"{enemy.EnemyName} inflige {damage} dégâts !");
        EmitSignal(SignalName.PlayerDamage, damage);

        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
        
        // RETOUR PLAN NEUTRE
        await _cameraDirector.CutTo(CameraDirector.CameraShot.Neutral);
        _playerActor?.OnCameraChanged(CameraDirector.CameraShot.Neutral);
        
        ChangeState(BattleState.Evaluation);
    }

    #endregion

    #region --- Turn Flow & Status Checks ---

    private void DetermineTurnOrder()
    {
        _turnOrder.Clear();
        if (_player != null) _turnOrder.Add(_player);
        _turnOrder.AddRange(_enemies);

        // Turn order based on Dexterity
        _turnOrder = _turnOrder.OrderByDescending(x => x.Dexterity).ToList();
        _currentTurnIndex = 0;
        
        ChangeState(BattleState.Selection);
    }

    private void CheckBattleStatus()
    {
        // 1. Defeat Check
        if (_player != null && _player.CurrentPv <= 0)
        {
            ChangeState(BattleState.Defeat);
            return;
        }

        // 2. Victory Check (after cleaning up dead enemies)
        UpdateActiveEnemies();

        if (_enemies.Count == 0)
        {
            ChangeState(BattleState.Victory);
            return;
        }

        // 3. Increment turn and continue
        _currentTurnIndex = (_currentTurnIndex + 1) % _turnOrder.Count;
        ChangeState(BattleState.Selection);
    }

    private void UpdateActiveEnemies()
    {
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            if (_enemies[i].CurrentPv <= 0)
            {
                var dead = _enemies[i];
                _hud?.ShowLogs($"{dead.EnemyName} est vaincu !");

                var tween = CreateTween();
                tween.TweenProperty(dead, "modulate:a", 0, 0.5f);
                tween.Parallel().TweenProperty(dead, "scale", Vector3.Zero, 0.5f);

                _enemies.RemoveAt(i);
                _turnOrder.Remove(dead);

                tween.Finished += () => dead.QueueFree();
            }
        }
        
        // Safety: index adjustment if units were removed
        if (_currentTurnIndex >= _turnOrder.Count) 
            _currentTurnIndex = 0;
    }

    #endregion

    #region --- Formulas & Math ---
    
    Vector2 GetScreenPositionOfNode(Node3D node)
    {
        if (node == null || !IsInstanceValid(node)) return Vector2.Zero;
        
        var camera = GetViewport().GetCamera3D();
        if (camera == null)
        {
            GD.PrintErr("[BattleManager] GetScreenPositionOfNode: No active Camera3D found!");
            // Fallback: position centrale par défaut ou position arbitraire
            var size = GetViewport().GetVisibleRect().Size;
            return size / 2;
        }

        // Projette le point 3D sur l'espace 2D de l'écran
        try 
        {
            return camera.UnprojectPosition(node.GlobalPosition);
        }
        catch (ObjectDisposedException)
        {
            GD.PrintErr($"[BattleManager] Tentative d'accès à un objet libéré : {node.Name}");
            return GetViewport().GetVisibleRect().Size / 2;
        }
    }

    private int CalculatePhysicalDamage(int attackerAtk, int defenderDef)
    {
        float baseDamage = (attackerAtk / 2.0f) - (defenderDef / 4.0f);
        float variance = (float)GD.RandRange(0.9, 1.1);
        return Math.Max(1, Mathf.RoundToInt(baseDamage * variance));
    }

    private int CalculateMagicDamage(IBattler attacker, IBattler target, Skill skill)
    {
        // Formula: (Power * (Spirit / 5)) - (Target Spirit / 4)
        float baseDamage = (skill.Power * (attacker.Spirit / 5.0f)) - (target.Spirit / 4.0f);
        float variance = (float)GD.RandRange(0.9, 1.1);
        return Math.Max(1, Mathf.RoundToInt(baseDamage * variance));
    }

    private int CalculateHealAmount(Skill skill)
    {
        // Formula: Power + (Spirit * 1.5)
        float baseHeal = skill.Power + (_player.Spirit * 1.5f);
        float variance = (float)GD.RandRange(0.9, 1.1);
        return Mathf.RoundToInt(baseHeal * variance);
    }

    #endregion

    #region --- Helpers: Spawning & VFX ---

    private void SpawnPlayer()
    {
        if (BattleActorScene == null)
        {
            GD.PrintErr("[BattleManager] BattleActor non assigné");
            return;
        }
        
        _playerActor = BattleActorScene.Instantiate<BattleActor>();

        if (_playerActor != null)
        {
            _playerAnchor.AddChild(_playerActor);
            _playerActor.Position = Vector3.Zero;
        }
        else
        {
            AddChild(_playerAnchor);
            _playerAnchor.Position = new Vector3(2.6f, .05f, 0);
        }
    }

    private void SpawnEnemies()
    {
        if (_enemyStatsSource == null || _enemyStatsSource.Count == 0)
        {
            GD.PrintErr("[BattleManager] No enemies provided by GameManager.");
            return;
        }

        if (EnemyScene == null)
        {
            GD.PrintErr("[BattleManager] EnemyScene is not assigned!");
            return;
        }

        _enemies.Clear();
        
        float spacing = 1.2f;
        float totalWidth = (_enemyStatsSource.Count -1) * spacing;
        float startX = -totalWidth / 2.0f;
        
        Vector3 anchorPos = _enemiesAnchor?.GlobalPosition ?? Vector3.Zero;

        for (int i = 0; i < _enemyStatsSource.Count; i++)
        {
            var stats = _enemyStatsSource[i];
            var enemy = EnemyScene.Instantiate<Enemy>();

            enemy.EnemyName = stats.EnemyName;
            
            if (_enemiesAnchor != null)
            {
                GD.Print("[BattleManager] On utilise l'anchor pour les ennemis:");
                _enemiesAnchor.AddChild(enemy);
                enemy.Position = new Vector3(2, 0, startX + i * spacing);
                enemy.LookAtTarget(_playerAnchor.GlobalPosition);
            }
            else
            {
                GD.Print("[BattleManager] On créer une ancre pour les ennemis:");
                AddChild(enemy);
                enemy.GlobalPosition = new Vector3(anchorPos.X + startX + i * spacing,
                    anchorPos.Y,
                    anchorPos.Z);
            }
            
            
            _enemies.Add(enemy);
        }
    }

    private void ShakeScreen(float intensity = 0.2f)
    {
        var camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        var tween = CreateTween();
        Vector3 originalPos = camera.Position;
        
        // Secousse en 3D sur les axes X et Y relatifs à la caméra
        tween.TweenProperty(camera, "position", originalPos + new Vector3(intensity, intensity, 0), 0.05f);
        tween.TweenProperty(camera, "position", originalPos + new Vector3(-intensity, -intensity, 0), 0.05f);
        tween.TweenProperty(camera, "position", originalPos, 0.05f);
    }

    private Vector2 GetPlayerUIPosition()
    {
        // On récupère la position écran du joueur (qui est en 3D)
        if (_player != null && IsInstanceValid(_player))
        {
            return GetScreenPositionOfNode(_player as Node3D);
        }
        
        var size = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
        return new Vector2(size.X / 2.0f, 980);
    }

    #endregion

    #region --- Victory / Reward Logic ---

    private async void HandleVictory()
    {
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");

        int totalXp = _enemyStatsSource.Sum(e => e.XpValue);
        _hud?.ShowLogs($"{totalXp} XP Gagnés !");
        
        ExitBattleSequence();
    }

    private async void ExitBattleSequence()
    {
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
        GD.Print("[BattleManager] Battle finished. Returning to map...");
        EndBattle(BattleEndReason.Victory);
    }

    #endregion
    
    void EndBattle(BattleEndReason reason)
    {
        GD.Print($"[BattleManager] Battle ended with reason: {reason}");
        // Nettoyage des abonnements pour éviter des callbacks fantômes après la scene change
        if (_hud != null)
            _hud.ActionSelected -= OnPlayerActionSelected;

        _isSelectingTarget = false; // stoppe la capture d’input locale

        EmitSignal(SignalName.BattleEnded, (int)reason);
        
        // Laisse l’orchestrateur changer de scène; le combat se libère proprement
        CallDeferred(MethodName.QueueFree);
    }
}
