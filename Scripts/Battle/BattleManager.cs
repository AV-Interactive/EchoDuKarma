using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using EchoduKarma.Scripts.Data;

public partial class BattleManager : Node
{
    public enum BattleState
    {
        Setup,
        Selection,
        Action,
        Evaluation,
        Victory,
        Defeat,
    }
    
    BattleState _currentState;
    
    Player _player;
    // Instances réelles d'ennemis utilisées pendant le combat
    List<Enemy> _enemies = new List<Enemy>();
    // Source de données provenant du GameManager pour instantiation
    List<EnemyStats> _enemyStatsSource = new List<EnemyStats>();
    
    List<IBattler> _turnOrder = new List<IBattler>();
    int _currentTurnIndex = 0;

    [Export] BattleHud _hud;

    // Scène d'ennemi à instancier (assigner enemy.tscn dans l'éditeur)
    [Export] public PackedScene EnemyScene { get; set; }
    [Export] public NodePath PlayerPath { get; set; }
    
    [Signal] public delegate void PlayerDamageEventHandler(int damage);
    
    bool _isPlayerDefending = false;
    
    public override void _Ready()
    {
        _player = GameManager.Instance.CurrentPlayer;
        
        if (_player == null)
        {
            GD.PrintErr("ERREUR CRITIQUE : Le joueur est introuvable dans le GameManager.");
        }

        // On récupère la source des stats d'ennemis depuis le GameManager pour instantiation
        _enemyStatsSource = GameManager.Instance.ListEnemiesBattle;
        
        _hud = GetTree().Root.FindChild("BattleHUD", true, false) as BattleHud;
        if (_hud != null)
        {
            _hud.ActionSelected += OnPlayerActionSelected;
        }
        
        ChangeState(BattleState.Setup);
    }

    public void ChangeState(BattleState newState)
    {
        _currentState = newState;
        switch (newState)
        {
            case BattleState.Setup:
                SpawnEnemies();
                DetermineTurnOrder();
                break;
            case BattleState.Selection:
                if (_turnOrder.Count == 0)
                {
                    GD.PushError("[Battle] Aucun combattant dans l'ordre de tour. Retour au Setup.");
                    ChangeState(BattleState.Setup);
                    return;
                }

                if (_currentTurnIndex >= _turnOrder.Count)
                {
                    _currentTurnIndex = 0;
                }

                // Attente de choix du joueur
                var activeUnit = _turnOrder[_currentTurnIndex];

                if (activeUnit is Player)
                {
                    _hud?.ShowMenu();
                }
                else
                {
                    ChangeState(BattleState.Action);
                }
                
                break;
            case BattleState.Action:
                ExecuteCurrentTurn();
                break;
            case BattleState.Evaluation:
                CheckBattleStatus();
                break;
            case BattleState.Victory:
                HandleVictory();
                break;
        }
    }
    
    void OnPlayerActionSelected(string actionName)
    {
        _isPlayerDefending = false;

        switch (actionName)
        {
            case "Attack":
                if (_enemies.Count > 0)
                {
                    ExecutePlayerAttack(_enemies[0]);
                }
                break;
            case "Defense":
                ExecuteDefense();
                break;
            case "Flee":
                ExecuteFlee();
                break;
        }
    }

    async void ExecuteDefense()
    {
        _isPlayerDefending = true;
        _hud.ShowLogs($"{_player.Name} est en mode DEFENSE !");
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
        ChangeState(BattleState.Evaluation);
    }

    async void ExecuteFlee()
    {
        _hud.ShowLogs($"{_player.Name} tente de fuir...");
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");

        if (GD.Randf() > .5f)
        {
            _hud.ShowLogs("Fuite réussie !");
            await ToSignal(GetTree().CreateTimer(1), "timeout");
            GetTree().ChangeSceneToFile("res://Maps/Intro/Road.tscn");
        }
        else
        {
            _hud.ShowLogs("L'ennemie empêche la fuite !");
            await ToSignal(GetTree().CreateTimer(1), "timeout");
            ChangeState(BattleState.Evaluation);
        }
    }
    
    async void ExecutePlayerAttack(Enemy target)
    {
        if (_player == null || target == null || target.Stats == null)
        {
            GD.PushError("[Battle] Player ou Target invalide dans ExecutePlayerAttack.");
            ChangeState(BattleState.Evaluation);
            return;
        }

        ChangeState(BattleState.Action);
        
        int damage = CalculateFinalDamage(_player.Strength, target.Defense);
        target.CurrentPv -= damage;
        if (target.CurrentPv < 0) target.CurrentPv = 0;
        
        _hud.ShowLogs($"Toine attaque {target.EnemyName} pour {damage} dégâts !");
        _hud.ShowDamage(new Vector2(target.Position.X, target.Position.Y - 50), damage, Colors.Red);
        target.PlayHitEffect();
        
        await ToSignal(GetTree().CreateTimer(1), "timeout");
        ChangeState(BattleState.Evaluation);
    }

    public void StartBattle()
    {
        _currentState = BattleState.Setup;
    }

    void DetermineTurnOrder()
    {
        _turnOrder.Clear();
        if (_player != null) _turnOrder.Add(_player);
        _turnOrder.AddRange(_enemies);
        
        _turnOrder = _turnOrder.OrderByDescending(x => x.Dexterity).ToList();
        ChangeState(BattleState.Selection);
    }
    
    void ExecuteCurrentTurn()
    {
        var activeUnit = _turnOrder[_currentTurnIndex];

        if (activeUnit is Enemy enemy)
        {
            EnemyAttack(enemy);
        }
    }

    async void EnemyAttack(Enemy enemy)
    {
        await ToSignal(GetTree().CreateTimer(1f), "timeout");

        if (enemy == null || enemy.Stats == null) {
            GD.PrintErr("Erreur : L'ennemi ou ses stats sont nuls !");
            ChangeState(BattleState.Evaluation);
            return;
        }

        // Sécurité 2 : On vérifie si le joueur est bien là
        if (_player == null) {
            GD.PrintErr("Erreur : Le joueur est introuvable pour l'attaque ennemie !");
            return;
        }

        _hud.ShowLogs($"{enemy.EnemyName} prépare son attaque...");
        
        
        // Calcul des dégâts sécurisé
        int damage = CalculateFinalDamage(enemy.Stats.Strength, _player.Defense);

        if (_isPlayerDefending)
        {
            damage /= 2;
            _hud.ShowLogs($"La défense de {_player.Name} réduit les dégâts !");
        }
        
        ShakeScreen();
        _player.CurrentPv -= damage;
        
        _hud.ShowDamage(new Vector2(1920/2, 980), damage, Colors.Red);
    
        _hud.ShowLogs($"{enemy.EnemyName} inflige {damage} dégâts !");
        
        EmitSignal(SignalName.PlayerDamage, damage);
        ChangeState(BattleState.Evaluation);
    }

    void CheckBattleStatus()
    {
        if (_player != null && _player.CurrentPv <= 0)
        {
            // Joueur perdu
            ChangeState(BattleState.Defeat);
            return;
        }

        // Nettoyage des ennemis vaincus
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            if (_enemies[i].CurrentPv <= 0)
            {
                var dead = _enemies[i];

                var tween = CreateTween();
                tween.TweenProperty(dead, "modulate:a", 0, 0.5f);
                tween.Parallel().TweenProperty(dead, "scale", Vector2.Zero, .5f);
                
                _enemies.RemoveAt(i);
                _turnOrder.Remove(dead);

                tween.Finished += () =>
                {
                    dead.QueueFree();
                };
                
                if (_currentTurnIndex >= _turnOrder.Count) _currentTurnIndex = 0;
                
                _hud.ShowLogs($"{dead.EnemyName} est mort !");
            }
        }

        if (_enemies.Count == 0)
        {
            // Joueur gagne
            _hud.ShowLogs("Tous les ennemis on été vaincus !");
            ChangeState(BattleState.Victory);
            return;
        }
        
        if (_turnOrder.Count == 0)
        {
            // Sécurité: si tout le monde est retiré, recalculer
            DetermineTurnOrder();
            return;
        }

        _currentTurnIndex = (_currentTurnIndex + 1) % _turnOrder.Count;
        ChangeState(BattleState.Selection);
    }
    void SpawnEnemies()
    {
        // Instancie les ennemis à partir des stats fournies par le GameManager
        if (_enemyStatsSource == null || _enemyStatsSource.Count == 0)
        {
            GD.PrintErr("Aucun ennemi fourni par le GameManager pour ce combat.");
            return;
        }
        if (EnemyScene == null)
        {
            GD.PrintErr("EnemyScene n'est pas assigné dans le BattleManager. Assigne Scripts/Entities/Enemy/enemy.tscn dans l'éditeur.");
            return;
        }

        _enemies.Clear();
        int spacing = 200;
        Vector2 startPos = new Vector2((1920 / 2) - spacing, 1080/2);
        
        for (int i = 0; i < _enemyStatsSource.Count; i++)
        {
            var stats = _enemyStatsSource[i];
            var enemy = EnemyScene.Instantiate<Enemy>();
        
            enemy.EnemyName = stats.EnemyName;
        
            // On décale chaque ennemi pour éviter qu'ils soient l'un sur l'autre
            enemy.Position = startPos + new Vector2(i * spacing, 0);
        
            AddChild(enemy);
            _enemies.Add(enemy);
        }
    }
    
    int CalculateFinalDamage(int attackerAtk, int defenderDef, float karmaMod = 1.0f)
    {
        // 1. Dégâts de base (Ta formule GDD)
        float baseDamage = (attackerAtk / 2f) - (defenderDef / 4f);
    
        // 2. Ajout de la variance (+/- 10%) pour le côté "Rétro"
        float variance = (float)GD.RandRange(0.9, 1.1);
    
        // 3. Application du Karma (issu de tes fichiers persos-et-stats.csv)
        // On multiplie par le modificateur (ex: 1.25 si Chaos)
        float finalDamage = baseDamage * variance * karmaMod;

        // On s'assure d'infliger au moins 1 dégât et on arrondit proprement
        return Math.Max(1, Mathf.RoundToInt(finalDamage));
    }

    void ShakeScreen(float intensity = 10)
    {
        var tween = CreateTween();
        var map = GetParent<Node2D>();
        
        tween.TweenProperty(map, "position", new Vector2(intensity, 0), .05f);
        tween.TweenProperty(map, "position", new Vector2(-intensity, 0), .05f);
        tween.TweenProperty(map, "position", Vector2.Zero, .05f);
    }

    async void HandleVictory()
    {
        float delay = 2.0f;
        await ToSignal(GetTree().CreateTimer(delay), "timeout");
        
        int totalXp = 0;
        int totalGold = 0;

        foreach (var enemy in _enemyStatsSource)
        {
            totalXp += enemy.XpValue;
        }
        _hud.ShowLogs($"{totalXp.ToString()} XP Gagnés !");
        ExitBattleWithDelay();
    }

    async void ExitBattleWithDelay()
    {
        float delay = 2.0f;
        await ToSignal(GetTree().CreateTimer(delay), "timeout");
        GD.Print("Retour au menu...");
    }
}
