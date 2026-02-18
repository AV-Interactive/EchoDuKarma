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

    BattleHud _hud;

    // Scène d'ennemi à instancier (assigner enemy.tscn dans l'éditeur)
    [Export] public PackedScene EnemyScene { get; set; }
    [Export] public NodePath PlayerPath { get; set; }
    
    [Signal] public delegate void PlayerDamageEventHandler(int damage);
    
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
        }
    }
    
    void OnPlayerActionSelected(string actionName)
    {
        if (actionName == "Attack")
        {
            if (_enemies.Count > 0)
            {
                ExecutePlayerAttack(_enemies[0]);
            }
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
    
        GD.Print($"Toine attaque {target.EnemyName} pour {damage} dégâts !");
    
        await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
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

        GD.Print($"{enemy.EnemyName} prépare son attaque...");
        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
    
        // Calcul des dégâts sécurisé
        int damage = CalculateFinalDamage(enemy.Stats.Strength, _player.Defense);
        _player.CurrentPv -= damage;
    
        GD.Print($"{enemy.EnemyName} inflige {damage} dégâts !");
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
                _enemies.RemoveAt(i);
                _turnOrder.Remove(dead);
                if (IsInstanceValid(dead)) dead.QueueFree();
                if (_currentTurnIndex >= _turnOrder.Count) _currentTurnIndex = 0;
            }
        }

        if (_enemies.Count == 0)
        {
            // Joueur gagne
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
        foreach (var stats in _enemyStatsSource)
        {
            var enemy = EnemyScene.Instantiate<Enemy>();
            enemy.EnemyName = stats.EnemyName; // injecte les stats via le nom
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
}
