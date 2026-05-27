using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;
using EchoduKarma.Scripts.UI;

/// <summary>
/// Lead Developer Refactor: Orchestrates the turn-based battle logic.
/// Handles State transitions, turn ordering, and execution of player/enemy actions.
/// </summary>
public partial class BattleManager : Node
{
    public const string GroupName = "battle_manager";

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
    private IBattler _playerBattler;
    private List<Skill> _playerSkills = new();
    private BattleActor _playerActor;               // coquille visuelle joueur
    private Node3D _playerAnchor;                   // point de spawn joueur
    private Node3D _enemiesAnchor;                  // point de spawn ennemis
    private readonly List<Enemy> _enemies = new List<Enemy>();
    private List<EnemyStats> _enemyStatsSource = new List<EnemyStats>();

    [ExportGroup("Timing")]
    [Export] float _actionResultDelay  = 1.5f; // pause après un résultat d'action (dégâts, log)
    [Export] float _enemyPreAttackDelay = 1.0f; // anticipation avant le bond ennemi
    [Export] float _defenseDelay       = 2.0f; // durée affichage posture défensive
    [Export] float _fleeDelay          = 2.0f; // attente pendant tentative de fuite
    [Export] float _victoryDelay       = 3.0f; // pause avant affichage XP
    [Export] float _xpDisplayDelay     = 1.5f; // durée affichage message XP
    [Export] float _levelUpDelay       = 2.0f; // durée affichage niveau gagné
    [Export] float _exitBattleDelay    = 2.5f; // pause finale avant retour map
    [Export] float _lootDisplayDelay     = 1.2f; // durée affichage message butin
    [Export] float _defeatDelay        = 3.0f; // durée affichage message de défaite

    [ExportGroup("Turn Management")]
    private BattleState _currentState;
    private readonly List<WaveActionEntry> _roundQueue = new();
    private int _roundTurnIndex;
    private bool _roundExecutionStarted;
    private int? _previewPlayerInitiative;
    private string _previewPlayerActionLabel;

    [ExportGroup("Action Selection State")]
    private bool _isPlayerDefending = false;
    private int _targetIndex = 0;
    private bool _isSelectingTarget = false;
    private Skill _selectedSkill;
    private bool _isActionRunning = false;

    private readonly HashSet<Enemy> _defendingEnemies = new();
    private readonly CombatBuffTracker _buffTracker = new();

    private bool _isReady = false;
    private float _zoneKarma;
    private KarmaCombatModifiers.CombatBonuses _karmaBonuses;

    #endregion

    #region --- Lifecycle & Initialization ---

    public override void _Ready()
    {
        AddToGroup(GroupName);
        CallDeferred(nameof(InitializeBattle));
    }

    private void InitializeBattle()
    {
        var snapshot = GameManager.Instance.GetBattleSnapshot();
        if (snapshot == null)
        {
            GD.PrintErr("[BattleManager] CRITICAL ERROR: aucun snapshot joueur (PersistPlayerForBattle manquant).");
            return;
        }

        _playerBattler = snapshot;
        _playerSkills = snapshot.LearnedSkills
            .Where(s => SkillManager.IsUnlockedAtLevel(s, snapshot.Level))
            .ToList();

        if (snapshot.Affinity == ElementType.None)
        {
            var hero = HeroManager.GetDefaultHero();
            if (hero != null)
                snapshot.Affinity = hero.Affinity;
        }

        InitializeKarmaForBattle();

        _enemyStatsSource = GameManager.Instance.ListEnemiesBattle;

        // Auto-link HUD if not assigned
        if (_hud != null)
        {
            _hud.ActionSelected += OnPlayerActionSelected;
            PlayerDamage += _hud.OnPlayerDamageReceived;
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

    public override void _Process(double delta)
    {
        if (!_isReady || _hud == null || _enemies.Count == 0) return;

        foreach (Enemy enemy in _enemies)
        {
            if (!IsInstanceValid(enemy)) continue;
            _hud.SetEnemyWidgetPosition(enemy, GetScreenPositionOfNode(enemy));
        }
    }

    void InitializeKarmaForBattle()
    {
        string zone = GameManager.Instance.ReturnZoneName;
        _zoneKarma = KarmaManager.Instance?.GetZoneKarma(zone) ?? 0;
        _karmaBonuses = KarmaCombatModifiers.GetCombatBonuses(_zoneKarma);

        KarmaManager.Instance?.SetCurrentZone(zone);

        GD.Print($"[BattleManager] Karma zone '{zone}' : {_zoneKarma} ({_karmaBonuses.StateLabel}) — " +
                 $"dégâts subis ×{_karmaBonuses.DamageTakenMultiplier:0.##}, soins ×{_karmaBonuses.HealMultiplier:0.##}");
    }

    int GetPlayerEffectiveStat(int baseStat, KarmaCombatModifiers.StatKind kind)
        => KarmaCombatModifiers.GetEffectiveStat(baseStat, kind, _zoneKarma);

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
        RegisterCameraBattleFocus();
        _hud?.SetupEnemies(_enemies);
        LogKarmaCombatStart();
        BeginRound();
    }

    void RegisterCameraBattleFocus()
    {
        if (_cameraDirector == null || _enemies.Count == 0)
            return;

        Vector3 sum = Vector3.Zero;
        foreach (Enemy enemy in _enemies)
            sum += enemy.GlobalPosition;

        _cameraDirector.RegisterBattleFocus(sum / _enemies.Count);
    }

    private void HandleSelectionState()
    {
        if (_roundExecutionStarted)
        {
            AdvanceRoundExecution();
            return;
        }

        _isPlayerDefending = false;
        _hud?.ClearActiveHighlight();
        _hud?.SetActivePlayer(true);
        _hud?.ShowMenu();
    }

    private void HandleActionState() => ExecuteCurrentTurn();
    private void HandleEvaluationState() => CheckBattleStatus();
    private void HandleVictoryState() => HandleVictory();

    private async void HandleDefeatState()
    {
        _hud?.HideMenu();
        _hud?.ShowLogs($"Défaite... {_playerBattler.Name} a succombé.");
        await ToSignal(GetTree().CreateTimer(_defeatDelay), "timeout");
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
                _hud.ShowMagicMenu(_playerSkills);
                break;
            case "Defense":
                CommitAndStartRoundExecution(BuildPlayerActionEntry("Defense"));
                break;
            case "Flee":
                CommitAndStartRoundExecution(BuildPlayerActionEntry("Flee"));
                break;
        }
    }

    private void ProcessMagicSelection(string actionName)
    {
        string skillName = actionName.Split(':')[1];
        _selectedSkill = _playerSkills.Find(s => s.Name == skillName);

        if (_selectedSkill == null)
        {
            _hud?.ShowMenu();
            return;
        }

        // Support skills (Heal/Buff) are self-targeted for now
        if (_selectedSkill.Type == SkillType.Support)
        {
            if (_playerBattler.CurrentMp < _selectedSkill.Cost)
            {
                _hud?.ShowLogs($"{_playerBattler.Name} n'a pas assez de MP pour utiliser {_selectedSkill.Name} !");
                _hud?.ShowMenu();
                return;
            }

            CommitAndStartRoundExecution(
                BuildPlayerActionEntry($"Magic:{_selectedSkill.Name}", _selectedSkill));
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
            _hud?.HideAllEnemyInfo();
            return;
        }

        var target = _enemies[_targetIndex];
        _hud?.UpdateTargetCursor(GetScreenPositionOfNode(target));
        _hud?.ShowEnemyInfo(target);
    }

    private void ConfirmTargetSelection()
    {
        _isSelectingTarget = false;
        _hud?.HideTargetCursor();

        if (_selectedSkill != null)
        {
            if (_playerBattler.CurrentMp < _selectedSkill.Cost)
            {
                _hud?.ShowLogs($"{_playerBattler.Name} n'a pas assez de MP pour utiliser {_selectedSkill.Name} !");
                _selectedSkill = null;
                _hud?.ShowMenu();
                return;
            }

            CommitAndStartRoundExecution(
                BuildPlayerActionEntry($"Magic:{_selectedSkill.Name}", _selectedSkill, _targetIndex));
        }
        else
        {
            CommitAndStartRoundExecution(
                BuildPlayerActionEntry("Attack", targetIndex: _targetIndex));
        }
    }

    private void CancelTargetSelection()
    {
        _isSelectingTarget = false;
        _hud?.HideTargetCursor();
        _hud?.ShowMenu();
    }

    /// <summary>
    /// Annule une action joueur en cours et réaffiche le menu (ex. MP insuffisants).
    /// </summary>
    private void CancelPlayerActionAndShowMenu()
    {
        _isActionRunning = false;
        _isSelectingTarget = false;
        _selectedSkill = null;
        _hud?.HideTargetCursor();
        ChangeState(BattleState.Selection);
    }

    #endregion

    #region --- Combat Execution: Player ---

    private async Task RunPlayerPhysicalAttackAsync(Enemy target)
    {
        if (_playerBattler == null || target == null)
            return;

        await _cameraDirector.CutTo(CameraDirector.CameraShot.PlayerAttack, target);
        _playerActor?.OnCameraChanged(CameraDirector.CameraShot.PlayerAttack);

        if (_playerActor != null)
            await _playerActor.PlayAttackAnimation();

        int playerStr = GetPlayerAttackStrength();
        LogElementCombat(_playerBattler.Affinity, null, target.Affinity);
        int damage = CalculatePhysicalDamage(_playerBattler, target, playerStr, target.Defense);

        if (_defendingEnemies.Remove(target))
        {
            damage = Math.Max(1, damage / 2);
            _hud?.ShowLogs($"{target.EnemyName} bloque partiellement l'attaque !");
        }

        target.CurrentPv -= damage;
        _hud?.RefreshEnemy(target);
        _hud?.ShowLogs($"{_playerBattler.Name} attaque {target.EnemyName} pour {damage} dégâts !");

        Vector2 screenPos = GetScreenPositionOfNode(target);
        _hud?.ShowDamage(new Vector2(screenPos.X, screenPos.Y - 24f), damage, Colors.Red);
        
        target.PlayHitEffect();

        await ToSignal(GetTree().CreateTimer(_actionResultDelay), "timeout");
        
        // RETOUR PLAN NEUTRE
        await _cameraDirector.CutTo(CameraDirector.CameraShot.Neutral);
        _playerActor?.OnCameraChanged(CameraDirector.CameraShot.Neutral);
    }

    private async Task RunPlayerMagicAsync(IBattler target, Skill skill)
    {
        if (_playerBattler == null || skill == null)
            return;

        _playerBattler.CurrentMp -= skill.Cost;
        _hud?.UpdatePlayerStats(_playerBattler);

        bool isOffensive = skill.Type != SkillType.Support;
        var magicShot = CameraDirector.CameraShot.PlayerMagic;

        if (isOffensive && target is Node3D offensiveTarget)
            await _cameraDirector.CutTo(magicShot, offensiveTarget);
        else
            await _cameraDirector.CutTo(magicShot);
        _playerActor?.OnCameraChanged(magicShot);

        if (_playerActor != null)
            await _playerActor.PlaySpellcastAnimation();

        if (isOffensive)
            ApplyMagicDamage(target, skill);
        else if (SkillSupportEffect.GetKind(skill) == SkillSupportEffect.Kind.BuffForce)
            ApplyForceBuffEffect(target, skill);
        else
            ApplyHealEffect(skill);

        await ToSignal(GetTree().CreateTimer(_actionResultDelay), "timeout");

        await _cameraDirector.CutTo(CameraDirector.CameraShot.Neutral);
        _playerActor?.OnCameraChanged(CameraDirector.CameraShot.Neutral);
    }

    private async Task RunPlayerDefenseAsync()
    {
        _isPlayerDefending = true;
        _hud?.ShowLogs($"{_playerBattler.Name} se prépare à encaisser !");
        await ToSignal(GetTree().CreateTimer(_defenseDelay), "timeout");
    }

    private async Task<bool> RunPlayerFleeAsync()
    {
        _hud?.ShowLogs($"{_playerBattler.Name} tente de fuir...");
        await ToSignal(GetTree().CreateTimer(_fleeDelay), "timeout");

        if (GD.Randf() > 0.5f)
        {
            _hud?.ShowLogs("Fuite réussie !");
            await ToSignal(GetTree().CreateTimer(_actionResultDelay), "timeout");
            EndBattle(BattleEndReason.Flee);
            return true;
        }

        _hud?.ShowLogs("L'ennemi vous barre la route !");
        await ToSignal(GetTree().CreateTimer(_actionResultDelay), "timeout");
        return false;
    }

    private void ApplyForceBuffEffect(IBattler target, Skill skill)
    {
        if (target == null || skill == null)
            return;

        int turns = SkillSupportEffect.RollDuration(skill);
        int amount = skill.Power;

        _buffTracker.ApplyForceBuff(target, amount, turns, skill.Name);
        _hud?.ShowLogs($"{target.Name} gagne +{amount} Force pour {turns} tour{(turns > 1 ? "s" : "")} !");

        if (target == _playerBattler)
            _hud?.UpdatePlayerStats(_playerBattler);

        SyncInitiativeHud();
    }

    private void ApplyHealEffect(Skill skill)
    {
        _hud?.ShowLogs($"{_playerBattler.Name} utilise {skill.Name} !");

        LogElementCombat(_playerBattler.Affinity, skill.Element, ElementType.None);

        int healAmount = CalculateHealAmount(skill);

        if (healAmount <= 0)
        {
            _hud?.ShowLogs("Le Karma du monde neutralise les soins !");
            return;
        }

        _playerBattler.CurrentPv = Math.Min(_playerBattler.Pv, _playerBattler.CurrentPv + healAmount);
        _hud?.UpdatePlayerStats(_playerBattler);
        _hud?.ShowDamage(GetPlayerUIPosition(), healAmount, Colors.Green);
    }

    private void ApplyMagicDamage(IBattler target, Skill skill)
    {
        _hud?.ShowLogs($"{_playerBattler.Name} lance {skill.Name} sur {target.Name} !");

        LogElementCombat(_playerBattler.Affinity, skill.Element, target.Affinity);

        int damage = CalculateMagicDamage(_playerBattler, target, skill);

        if (target is Enemy e)
        {
            if (_defendingEnemies.Remove(e))
            {
                damage = Math.Max(1, damage / 2);
                _hud?.ShowLogs($"{e.EnemyName} bloque partiellement le sort !");
            }

            e.CurrentPv -= damage;
            e.PlayHitEffect();
            _hud?.RefreshEnemy(e);
        }

        Vector2 screenPos = GetScreenPositionOfNode(target as Node3D);
        _hud?.ShowDamage(new Vector2(screenPos.X, screenPos.Y - 24f), damage, Colors.Red);
    }

    #endregion

    #region --- Combat Execution: Enemy ---

    private async Task ExecuteEnemyAttack(Enemy enemy, bool aggressiveBonus = false)
    {
        if (enemy.Stats == null)
        {
            GD.PrintErr($"[BattleManager] {enemy.EnemyName} a des stats nulles — tour ennemi ignoré.");
            ChangeState(BattleState.Evaluation);
            return;
        }

        await _cameraDirector.CutTo(CameraDirector.CameraShot.EnemyAttack);
        _playerActor?.OnCameraChanged(CameraDirector.CameraShot.EnemyAttack);

        _hud?.SetActiveEnemy(enemy);
        enemy.PlayTurnHighlight();
        _hud?.ShowLogs($"{enemy.EnemyName} prépare son attaque...");
        await ToSignal(GetTree().CreateTimer(_enemyPreAttackDelay), "timeout");
        await enemy.PlayAttackAnimation();

        int baseStrength = GetEnemyAttackStrength(enemy, aggressiveBonus);
        int playerDef = GetPlayerEffectiveStat(_playerBattler.Defense, KarmaCombatModifiers.StatKind.Defense);
        LogElementCombat(enemy.Affinity, null, _playerBattler.Affinity);
        int damage = CalculatePhysicalDamage(enemy, _playerBattler, baseStrength, playerDef);
        damage = KarmaCombatModifiers.ApplyDamageTaken(damage, _zoneKarma);

        if (_isPlayerDefending)
        {
            damage = Math.Max(1, damage / 2);
            _hud?.ShowLogs($"{_playerBattler.Name} bloque une partie de l'attaque !");
        }

        if (aggressiveBonus)
            _hud?.ShowLogs($"{enemy.EnemyName} attaque avec rage !");

        ShakeScreen();
        _playerBattler.CurrentPv -= damage;
        _hud?.UpdatePlayerStats(_playerBattler);
        _hud?.ShowDamage(GetPlayerUIPosition(), damage, Colors.Red);

        _hud?.ShowLogs($"{enemy.EnemyName} inflige {damage} dégâts !");
        EmitSignal(SignalName.PlayerDamage, damage);

        await ToSignal(GetTree().CreateTimer(_actionResultDelay), "timeout");

        enemy.StopTurnHighlight();
        _hud?.ClearActiveHighlight();

        await _cameraDirector.CutTo(CameraDirector.CameraShot.Neutral);
        _playerActor?.OnCameraChanged(CameraDirector.CameraShot.Neutral);

        ChangeState(BattleState.Evaluation);
    }

    private async Task ExecuteEnemySkillAsync(Enemy enemy, Skill skill, WaveActionEntry.ActionKind kind)
    {
        if (enemy == null || skill == null || _playerBattler == null)
        {
            ChangeState(BattleState.Evaluation);
            return;
        }

        if (enemy.CurrentMp < skill.Cost)
        {
            _hud?.ShowLogs($"{enemy.EnemyName} manque de PM pour {skill.Name} — attaque physique !");
            bool rage = enemy.Stats?.AiPattern == AiPattern.Aggressive
                && enemy.CurrentPv <= enemy.Stats.Pv * 0.3f;
            await ExecuteEnemyAttack(enemy, rage);
            return;
        }

        enemy.CurrentMp -= skill.Cost;

        await _cameraDirector.CutTo(CameraDirector.CameraShot.EnemyAttack);
        _playerActor?.OnCameraChanged(CameraDirector.CameraShot.EnemyAttack);

        _hud?.SetActiveEnemy(enemy);
        enemy.PlayTurnHighlight();
        _hud?.ShowLogs($"{enemy.EnemyName} utilise {skill.Name} !");
        await ToSignal(GetTree().CreateTimer(_enemyPreAttackDelay), "timeout");
        await enemy.PlayAttackAnimation();

        if (kind == WaveActionEntry.ActionKind.EnemyHeal)
        {
            int heal = Mathf.Max(1, skill.Power + enemy.Spirit / 2);
            enemy.CurrentPv = Math.Min(enemy.Pv, enemy.CurrentPv + heal);
            _hud?.ShowLogs($"{enemy.EnemyName} récupère {heal} PV.");
            Vector2 screenPos = GetScreenPositionOfNode(enemy);
            _hud?.ShowDamage(screenPos, heal, Colors.Green);
        }
        else if (kind == WaveActionEntry.ActionKind.EnemyBuff)
        {
            ApplyForceBuffEffect(enemy, skill);
        }
        else
        {
            LogElementCombat(enemy.Affinity, skill.Element, _playerBattler.Affinity);
            int damage = CalculateMagicDamage(enemy, _playerBattler, skill);
            damage = KarmaCombatModifiers.ApplyDamageTaken(damage, _zoneKarma);

            if (_isPlayerDefending)
            {
                damage = Math.Max(1, damage / 2);
                _hud?.ShowLogs($"{_playerBattler.Name} bloque une partie du sort !");
            }

            ShakeScreen();
            _playerBattler.CurrentPv -= damage;
            _hud?.UpdatePlayerStats(_playerBattler);
            _hud?.ShowDamage(GetPlayerUIPosition(), damage, Colors.Red);
            _hud?.ShowLogs($"{enemy.EnemyName} inflige {damage} dégâts magiques !");
            EmitSignal(SignalName.PlayerDamage, damage);
        }

        await ToSignal(GetTree().CreateTimer(_actionResultDelay), "timeout");

        enemy.StopTurnHighlight();
        _hud?.ClearActiveHighlight();

        await _cameraDirector.CutTo(CameraDirector.CameraShot.Neutral);
        _playerActor?.OnCameraChanged(CameraDirector.CameraShot.Neutral);

        ChangeState(BattleState.Evaluation);
    }

    private async Task ExecuteEnemyDefend(Enemy enemy)
    {
        await _cameraDirector.CutTo(CameraDirector.CameraShot.Neutral);
        _playerActor?.OnCameraChanged(CameraDirector.CameraShot.Neutral);

        _hud?.SetActiveEnemy(enemy);
        enemy.PlayTurnHighlight();
        _defendingEnemies.Add(enemy);
        _hud?.ShowLogs($"{enemy.EnemyName} adopte une posture défensive !");
        await ToSignal(GetTree().CreateTimer(_defenseDelay), "timeout");

        enemy.StopTurnHighlight();
        _hud?.ClearActiveHighlight();

        ChangeState(BattleState.Evaluation);
    }

    #endregion

    #region --- Round, initiative & turns ---

    void BeginRound()
    {
        if (_playerBattler == null || _playerBattler.CurrentPv <= 0)
        {
            ChangeState(BattleState.Defeat);
            return;
        }

        if (_enemies.Count == 0)
        {
            ChangeState(BattleState.Victory);
            return;
        }

        _roundQueue.Clear();
        _roundTurnIndex = 0;
        _roundExecutionStarted = false;
        _isPlayerDefending = false;
        ClearInitiativePreview();
        TickCombatBuffs();

        foreach (Enemy enemy in _enemies)
        {
            if (enemy.CurrentPv > 0)
                _roundQueue.Add(PlanEnemyTurn(enemy));
        }

        _hud?.ShowLogs("Choisissez votre action.");
        SyncInitiativeHud();
        ChangeState(BattleState.Selection);
    }

    void CommitAndStartRoundExecution(WaveActionEntry playerAction)
    {
        if (playerAction == null || _roundExecutionStarted)
            return;

        _roundQueue.RemoveAll(e => e.Battler == _playerBattler);
        _roundQueue.Add(playerAction);
        SortRoundByInitiative();

        _roundExecutionStarted = true;
        _roundTurnIndex = 0;
        ClearInitiativePreview();
        LogRoundOrder();

        AdvanceRoundExecution();
    }

    WaveActionEntry BuildPlayerActionEntry(string actionKey, Skill skill = null, int targetIndex = -1)
    {
        var kind = ResolvePlayerActionKind(actionKey, skill);
        return new WaveActionEntry
        {
            Battler = _playerBattler,
            Kind = kind,
            Initiative = ComputePreviewInitiative(actionKey, skill),
            Skill = skill,
            TargetIndex = targetIndex,
        };
    }

    static WaveActionEntry.ActionKind ResolvePlayerActionKind(string actionKey, Skill skill)
    {
        if (actionKey == "Attack")
            return WaveActionEntry.ActionKind.PlayerPhysical;
        if (actionKey == "Defense")
            return WaveActionEntry.ActionKind.PlayerDefend;
        if (actionKey == "Flee")
            return WaveActionEntry.ActionKind.PlayerFlee;

        if (actionKey.StartsWith("Magic:", StringComparison.Ordinal) && skill != null)
        {
            if (skill.Type != SkillType.Support)
                return WaveActionEntry.ActionKind.PlayerMagic;

            return SkillSupportEffect.GetKind(skill) == SkillSupportEffect.Kind.BuffForce
                ? WaveActionEntry.ActionKind.PlayerBuff
                : WaveActionEntry.ActionKind.PlayerHeal;
        }

        return WaveActionEntry.ActionKind.PlayerPhysical;
    }

    bool IsPlayerSelectionPhase() =>
        !_roundExecutionStarted && _currentState == BattleState.Selection;

    /// <summary>Aperçu initiative joueur + tri HUD temps réel (phase de choix).</summary>
    public void PreviewPlayerInitiative(string actionKey, Skill skill = null)
    {
        if (!IsPlayerSelectionPhase() || _playerBattler == null)
            return;

        _previewPlayerInitiative = ComputePreviewInitiative(actionKey, skill);
        _previewPlayerActionLabel = DescribePreviewAction(actionKey, skill);
        SyncInitiativeHud();
    }

    public void ClearInitiativePreview()
    {
        _previewPlayerInitiative = null;
        _previewPlayerActionLabel = null;
    }

    void SyncInitiativeHud()
    {
        if (_hud == null)
            return;

        _hud.UpdateInitiativeTrack(BuildInitiativeDisplayRows());
    }

    List<InitiativeDisplayEntry> BuildInitiativeDisplayRows()
    {
        var rows = new List<InitiativeDisplayEntry>();

        if (!_roundExecutionStarted)
        {
            foreach (WaveActionEntry entry in _roundQueue)
            {
                if (!IsRoundEntryAlive(entry))
                    continue;

                rows.Add(ToDisplayEntry(entry, isActive: false, isCompleted: false));
            }

            rows.Add(new InitiativeDisplayEntry
            {
                DisplayName = _playerBattler.Name,
                Portrait = CombatantPortrait.GetPlayerPortrait(),
                Initiative = _previewPlayerInitiative
                    ?? ComputePreviewInitiative("Attack"),
                ActionLabel = _previewPlayerActionLabel ?? "Choisir…",
                IsPlayer = true,
                IsPending = false,
                IsActive = _currentState == BattleState.Selection,
                IsCompleted = false,
                Buffs = BuildBuffDisplays(_playerBattler),
            });

            rows.Sort(CompareDisplayEntries);
            return rows;
        }

        for (int i = 0; i < _roundQueue.Count; i++)
        {
            WaveActionEntry entry = _roundQueue[i];
            if (!IsRoundEntryAlive(entry))
                continue;

            bool completed = i < _roundTurnIndex;
            bool active = i == _roundTurnIndex && _currentState == BattleState.Action;
            rows.Add(ToDisplayEntry(entry, active, completed));
        }

        return rows;
    }

    static int CompareDisplayEntries(InitiativeDisplayEntry a, InitiativeDisplayEntry b)
    {
        if (a.Initiative < 0 && b.Initiative >= 0)
            return 1;
        if (b.Initiative < 0 && a.Initiative >= 0)
            return -1;

        int byInit = b.Initiative.CompareTo(a.Initiative);
        if (byInit != 0)
            return byInit;

        if (a.IsPlayer)
            return -1;
        if (b.IsPlayer)
            return 1;

        return string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
    }

    InitiativeDisplayEntry ToDisplayEntry(WaveActionEntry entry, bool isActive, bool isCompleted)
    {
        bool isPlayer = entry.Battler == _playerBattler;
        string displayName = isPlayer
            ? _playerBattler.Name
            : entry.Enemy?.EnemyName ?? "?";

        return new InitiativeDisplayEntry
        {
            DisplayName = displayName,
            Portrait = isPlayer
                ? CombatantPortrait.GetPlayerPortrait()
                : CombatantPortrait.GetEnemyPortrait(displayName),
            Initiative = entry.Initiative,
            ActionLabel = DescribeWaveAction(entry),
            IsPlayer = isPlayer,
            IsActive = isActive,
            IsCompleted = isCompleted,
            Buffs = BuildBuffDisplays(entry.Battler),
        };
    }

    List<InitiativeBuffDisplay> BuildBuffDisplays(IBattler battler)
    {
        var displays = new List<InitiativeBuffDisplay>();

        foreach (CombatBuffSnapshot snapshot in _buffTracker.GetSnapshots(battler))
        {
            displays.Add(new InitiativeBuffDisplay
            {
                Icon = UiIcons.GetCombatBuffIcon(snapshot.Kind),
                TurnsLeft = snapshot.TurnsLeft,
                Tooltip = snapshot.Kind switch
                {
                    CombatBuffKind.Force =>
                        $"{snapshot.SourceName} : +{snapshot.Amount} Force · {snapshot.TurnsLeft} tour{(snapshot.TurnsLeft > 1 ? "s" : "")}",
                    _ => $"{snapshot.SourceName} · {snapshot.TurnsLeft} tour{(snapshot.TurnsLeft > 1 ? "s" : "")}",
                },
            });
        }

        return displays;
    }

    static string DescribeWaveAction(WaveActionEntry entry) => entry.Kind switch
    {
        WaveActionEntry.ActionKind.PlayerWaiting => "Ton tour",
        WaveActionEntry.ActionKind.PlayerPhysical => "Attaque",
        WaveActionEntry.ActionKind.PlayerMagic => entry.Skill?.Name ?? "Magie",
        WaveActionEntry.ActionKind.PlayerHeal => entry.Skill?.Name ?? "Soin",
        WaveActionEntry.ActionKind.PlayerBuff => entry.Skill?.Name ?? "Buff",
        WaveActionEntry.ActionKind.PlayerDefend => "Défense",
        WaveActionEntry.ActionKind.PlayerFlee => "Fuite",
        WaveActionEntry.ActionKind.EnemyAttack => "Attaque",
        WaveActionEntry.ActionKind.EnemyMagic => entry.Skill?.Name ?? "Magie",
        WaveActionEntry.ActionKind.EnemyHeal => entry.Skill?.Name ?? "Soin",
        WaveActionEntry.ActionKind.EnemyBuff => entry.Skill?.Name ?? "Buff",
        WaveActionEntry.ActionKind.EnemyDefend => "Défense",
        _ => "",
    };

    string DescribePreviewAction(string actionKey, Skill skill)
    {
        if (actionKey == "Attack")
            return "Attaque";
        if (actionKey == "Defense")
            return "Défense";
        if (actionKey == "Flee")
            return "Fuite";
        if (actionKey == "Magic")
            return "Magie…";
        if (actionKey.StartsWith("Magic:", StringComparison.Ordinal) && skill != null)
            return skill.Name;
        return "…";
    }

    int ComputePreviewInitiative(string actionKey, Skill skill = null)
    {
        if (_playerBattler == null)
            return 0;

        if (actionKey == "Attack")
            return CombatInitiative.ForPhysical(_playerBattler, isPlayer: true, _zoneKarma);
        if (actionKey == "Defense")
            return CombatInitiative.ForDefend(_playerBattler, isPlayer: true, _zoneKarma);
        if (actionKey == "Flee")
            return CombatInitiative.ForFlee(_playerBattler, isPlayer: true, _zoneKarma);

        if (actionKey.StartsWith("Magic:", StringComparison.Ordinal) && skill != null)
            return CombatInitiative.ForSkill(_playerBattler, isPlayer: true, _zoneKarma, skill);

        return CombatInitiative.ForPhysical(_playerBattler, isPlayer: true, _zoneKarma);
    }

    WaveActionEntry PlanEnemyTurn(Enemy enemy) => EnemyTurnPlanner.Plan(enemy);

    void SortRoundByInitiative() =>
        _roundQueue.Sort(CompareRoundEntries);

    int CompareRoundEntries(WaveActionEntry a, WaveActionEntry b)
    {
        int byInit = b.Initiative.CompareTo(a.Initiative);
        if (byInit != 0)
            return byInit;

        if (a.Battler == _playerBattler)
            return -1;
        if (b.Battler == _playerBattler)
            return 1;

        return string.Compare(
            a.Enemy?.EnemyName,
            b.Enemy?.EnemyName,
            StringComparison.Ordinal);
    }

    void LogRoundOrder()
    {
        var parts = _roundQueue
            .Where(IsRoundEntryAlive)
            .Select(e =>
            {
                string name = e.Battler == _playerBattler
                    ? _playerBattler.Name
                    : e.Enemy?.EnemyName ?? "?";
                return $"{name}({e.Initiative})";
            });
        _hud?.ShowLogs($"Ordre du round : {string.Join(" → ", parts)}");
    }

    void AdvanceRoundExecution()
    {
        while (_roundTurnIndex < _roundQueue.Count)
        {
            if (IsRoundEntryAlive(_roundQueue[_roundTurnIndex]))
            {
                SyncInitiativeHud();
                ChangeState(BattleState.Action);
                return;
            }

            _roundTurnIndex++;
        }

        BeginRound();
    }

    void ExecuteCurrentTurn()
    {
        if (_roundTurnIndex >= _roundQueue.Count)
        {
            BeginRound();
            return;
        }

        WaveActionEntry entry = _roundQueue[_roundTurnIndex];
        if (!IsRoundEntryAlive(entry))
        {
            ChangeState(BattleState.Evaluation);
            return;
        }

        if (entry.Battler == _playerBattler)
            _ = RunPlayerTurn(entry);
        else
            _ = RunEnemyTurn(entry);
    }

    async Task RunPlayerTurn(WaveActionEntry entry)
    {
        _hud?.HideMenu();
        if (_isActionRunning)
            return;

        _isActionRunning = true;

        try
        {
            switch (entry.Kind)
            {
                case WaveActionEntry.ActionKind.PlayerPhysical:
                    if (entry.TargetIndex >= 0 && entry.TargetIndex < _enemies.Count
                        && _enemies[entry.TargetIndex].CurrentPv > 0)
                        await RunPlayerPhysicalAttackAsync(_enemies[entry.TargetIndex]);
                    break;

                case WaveActionEntry.ActionKind.PlayerMagic:
                    if (entry.Skill != null && entry.TargetIndex >= 0 && entry.TargetIndex < _enemies.Count
                        && _enemies[entry.TargetIndex].CurrentPv > 0)
                        await RunPlayerMagicAsync(_enemies[entry.TargetIndex], entry.Skill);
                    break;

                case WaveActionEntry.ActionKind.PlayerHeal:
                case WaveActionEntry.ActionKind.PlayerBuff:
                    if (entry.Skill != null)
                        await RunPlayerMagicAsync(_playerBattler, entry.Skill);
                    break;

                case WaveActionEntry.ActionKind.PlayerDefend:
                    await RunPlayerDefenseAsync();
                    break;

                case WaveActionEntry.ActionKind.PlayerFlee:
                    if (await RunPlayerFleeAsync())
                        return;
                    break;
            }
        }
        finally
        {
            _isActionRunning = false;
        }

        ChangeState(BattleState.Evaluation);
    }

    async Task RunEnemyTurn(WaveActionEntry entry)
    {
        _hud?.HideMenu();

        if (entry.Enemy == null)
        {
            ChangeState(BattleState.Evaluation);
            return;
        }

        switch (entry.Kind)
        {
            case WaveActionEntry.ActionKind.EnemyDefend:
                await ExecuteEnemyDefend(entry.Enemy);
                break;

            case WaveActionEntry.ActionKind.EnemyMagic:
            case WaveActionEntry.ActionKind.EnemyHeal:
            case WaveActionEntry.ActionKind.EnemyBuff:
                if (entry.Skill != null)
                    await ExecuteEnemySkillAsync(entry.Enemy, entry.Skill, entry.Kind);
                else
                    ChangeState(BattleState.Evaluation);
                break;

            case WaveActionEntry.ActionKind.EnemyAttack:
            default:
                bool rage = entry.Enemy.Stats?.AiPattern == AiPattern.Aggressive
                    && entry.Enemy.CurrentPv <= entry.Enemy.Stats.Pv * 0.3f;
                await ExecuteEnemyAttack(entry.Enemy, rage);
                break;
        }
    }

    static bool IsRoundEntryAlive(WaveActionEntry entry)
    {
        if (entry.Battler == null)
            return false;

        return entry.Battler.CurrentPv > 0;
    }

    private void CheckBattleStatus()
    {
        if (_playerBattler != null && _playerBattler.CurrentPv <= 0)
        {
            ChangeState(BattleState.Defeat);
            return;
        }

        UpdateActiveEnemies();

        if (_enemies.Count == 0)
        {
            ChangeState(BattleState.Victory);
            return;
        }

        _roundTurnIndex++;
        AdvanceRoundExecution();
    }

    private void UpdateActiveEnemies()
    {
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            if (_enemies[i].CurrentPv <= 0)
            {
                var dead = _enemies[i];
                _hud?.ShowLogs($"{dead.EnemyName} est vaincu !");
                _hud?.RemoveEnemy(dead);
                dead.PlayDefeatAnimation();

                QuestManager.Instance?.NotifyKill(dead.EnemyName);
                ApplyKarmaForMonsterKill(dead.EnemyName);

                _defendingEnemies.Remove(dead);
                _enemies.RemoveAt(i);
            }
        }

        SyncInitiativeHud();
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

    void LogElementCombat(ElementType attackerAffinity, string skillElement, ElementType defenderAffinity)
    {
        foreach (string line in ElementCombat.GetCombatLogLines(attackerAffinity, skillElement, defenderAffinity))
            _hud?.ShowLogs(line);
    }

    private int CalculatePhysicalDamage(IBattler attacker, IBattler target, int attackerAtk, int defenderDef)
    {
        float baseDamage = (attackerAtk / 2.0f) - (defenderDef / 4.0f);
        float variance = (float)GD.RandRange(0.9, 1.1);
        float elementMult = ElementCombat.GetCombinedPowerMultiplier(
            attacker.Affinity, null, target.Affinity);
        return Math.Max(1, Mathf.RoundToInt(baseDamage * variance * elementMult));
    }

    private int CalculateMagicDamage(IBattler attacker, IBattler target, Skill skill)
    {
        int attackerSpirit = attacker == _playerBattler
            ? GetPlayerEffectiveStat(attacker.Spirit, KarmaCombatModifiers.StatKind.Spirit)
            : attacker.Spirit;

        float baseDamage = (skill.Power * (attackerSpirit / 5.0f)) - (target.Spirit / 4.0f);
        float variance = (float)GD.RandRange(0.9, 1.1);
        float elementMult = ElementCombat.GetCombinedPowerMultiplier(
            attacker.Affinity, skill.Element, target.Affinity);
        return Math.Max(1, Mathf.RoundToInt(baseDamage * variance * elementMult));
    }

    int GetPlayerAttackStrength()
    {
        int baseStr = GetPlayerEffectiveStat(_playerBattler.Strength, KarmaCombatModifiers.StatKind.Force);
        return baseStr + _buffTracker.GetForceBonus(_playerBattler);
    }

    int GetEnemyAttackStrength(Enemy enemy, bool aggressiveBonus)
    {
        int strength = enemy.Strength + _buffTracker.GetForceBonus(enemy);
        if (aggressiveBonus)
            strength = Mathf.RoundToInt(strength * 1.2f);

        return strength;
    }

    void TickCombatBuffs()
    {
        foreach (string name in _buffTracker.TickRoundEnd())
            _hud?.ShowLogs($"L'effet de renforcement sur {name} s'est dissipé.");
    }

    private int CalculateHealAmount(Skill skill)
    {
        int playerSpirit = GetPlayerEffectiveStat(_playerBattler.Spirit, KarmaCombatModifiers.StatKind.Spirit);
        float baseHeal = skill.Power + (playerSpirit * 1.5f);
        float variance = (float)GD.RandRange(0.9, 1.1);
        float elementMult = ElementCombat.GetAffinityPowerMultiplier(_playerBattler.Affinity, skill.Element);
        int rawHeal = Mathf.RoundToInt(baseHeal * variance * elementMult);
        return KarmaCombatModifiers.ApplyHealAmount(rawHeal, _zoneKarma);
    }

    void ApplyKarmaForMonsterKill(string enemyName)
    {
        string zone = GameManager.Instance?.ReturnZoneName ?? "Introduction";
        KarmaManager.Instance?.ApplyMonsterKillImpact(zone);

        _zoneKarma = KarmaManager.Instance?.GetZoneKarma(zone) ?? _zoneKarma;
        _karmaBonuses = KarmaCombatModifiers.GetCombatBonuses(_zoneKarma);

        _hud?.ShowLogs($"Karma {KarmaManager.FormatDelta(KarmaManager.KarmaLossPerMonsterKill)} ({enemyName})");
    }

    void LogKarmaCombatStart()
    {
        _hud?.ShowLogs("Un combat commence !");

        if (_playerBattler == null)
            return;

        string state = _karmaBonuses.StateLabel;
        int str = GetPlayerEffectiveStat(_playerBattler.Strength, KarmaCombatModifiers.StatKind.Force);
        int spr = GetPlayerEffectiveStat(_playerBattler.Spirit, KarmaCombatModifiers.StatKind.Spirit);

        if (str != _playerBattler.Strength || spr != _playerBattler.Spirit)
            _hud?.ShowLogs($"Karma ({state}) — Force {str}, Esprit {spr}.");

        if (_karmaBonuses.DamageTakenMultiplier != 1f)
        {
            int pct = Mathf.RoundToInt((_karmaBonuses.DamageTakenMultiplier - 1f) * 100f);
            string sign = pct > 0 ? "+" : "";
            _hud?.ShowLogs($"Karma ({state}) — dégâts subis {sign}{pct}%.");
        }

        if (_karmaBonuses.HealMultiplier <= 0f)
            _hud?.ShowLogs($"Karma ({state}) — les soins sont inefficaces.");
        else if (_karmaBonuses.HealMultiplier != 1f)
        {
            int pct = Mathf.RoundToInt((_karmaBonuses.HealMultiplier - 1f) * 100f);
            _hud?.ShowLogs($"Karma ({state}) — soins +{pct}%.");
        }
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
            GD.PrintErr("[BattleManager] CRITICAL: BattleActorScene.Instantiate<BattleActor>() a retourné null — spawn joueur annulé.");
            return;
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
            enemy.InitializeFromBattleStats(stats);
            
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

            // PV/PM déjà initialisés via InitializeFromBattleStats
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
        if (_playerAnchor != null && IsInstanceValid(_playerAnchor))
            return GetScreenPositionOfNode(_playerAnchor);

        var size = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
        return new Vector2(size.X / 2.0f, 980);
    }

    #endregion

    #region --- Victory / Reward Logic ---

    private async void HandleVictory()
    {
        await ToSignal(GetTree().CreateTimer(_victoryDelay), "timeout");

        int totalXp = _enemyStatsSource.Sum(e => e.XpValue);
        int levelsGained = GameManager.Instance.GrantBattleExperience(totalXp);

        _hud?.ShowLogs($"+{totalXp} XP");
        await ToSignal(GetTree().CreateTimer(_xpDisplayDelay), "timeout");

        if (levelsGained > 0)
        {
            _hud?.ShowLogs($"Niveau {_playerBattler.Level} !");
            _hud?.UpdatePlayerStats(_playerBattler);
            await ToSignal(GetTree().CreateTimer(_levelUpDelay), "timeout");
        }

        var lootAcquired = DistributeBattleLoot();
        foreach (string itemName in lootAcquired)
        {
            _hud?.ShowLogs($"Butin : {itemName}");
            await ToSignal(GetTree().CreateTimer(_lootDisplayDelay), "timeout");
        }

        ExitBattleSequence();
    }

    List<string> DistributeBattleLoot()
    {
        var acquired = new List<string>();
        var inventory = InventoryManager.Instance;
        if (inventory == null || _enemyStatsSource == null)
            return acquired;

        foreach (EnemyStats enemy in _enemyStatsSource)
        {
            foreach (string itemName in EnemyStats.ParseLoot(enemy.Loot))
            {
                if (inventory.TryAddItem(itemName))
                    acquired.Add(itemName);
                else
                    GD.Print($"[BattleManager] Butin non ajouté : '{itemName}' (inventaire plein ou déjà possédé).");
            }
        }

        return acquired;
    }

    private async void ExitBattleSequence()
    {
        await ToSignal(GetTree().CreateTimer(_exitBattleDelay), "timeout");
        GD.Print("[BattleManager] Battle finished. Returning to map...");
        EndBattle(BattleEndReason.Victory);
    }

    #endregion
    
    void EndBattle(BattleEndReason reason)
    {
        GD.Print($"[BattleManager] Battle ended with reason: {reason}");
        // Nettoyage des abonnements pour éviter des callbacks fantômes après la scene change
        if (_hud != null)
        {
            _hud.ActionSelected -= OnPlayerActionSelected;
            PlayerDamage -= _hud.OnPlayerDamageReceived;
        }

        _isSelectingTarget = false; // stoppe la capture d’input locale

        EmitSignal(SignalName.BattleEnded, (int)reason);
        
        // Laisse l’orchestrateur changer de scène; le combat se libère proprement
        CallDeferred(MethodName.QueueFree);
    }
}
