using System;
using System.Collections.Generic;
using Godot;

namespace EchoduKarma.Scripts.Data;

public partial class GameManager: Node
{
    public static GameManager Instance { get; private set; }
    
    public bool PlayerMoved { get; set; } = true;
    
    [Signal] public delegate void PlayerLevelUpEventHandler(int levelUpAmount);
    
    readonly Dictionary<string, Action<string[]>> _eventLibrary = new Dictionary<string, Action<string[]>>();
    
    public List<EnemyStats> ListEnemiesBattle { get; set; } = new List<EnemyStats>();
    public Player CurrentPlayer { get; set; }
    
    public override void _Ready()
    {
        GD.Print("[AUTOLOAD] GameManager Ready - Start");
        Instance = this;
        
        RegisterEvents();
        
        CallDeferred(nameof(ConnectToSignals));
        GD.Print("[AUTOLOAD] GameManager Ready - End");
    }

    void ConnectToSignals()
    {
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.ActionTriggered += OnActionTriggered;
        }
    }
    
    public void RegisterEvents()
    {
        _eventLibrary.Add("BATTLE", (args) => StartBattle(args[0], args[1]));
        _eventLibrary.Add("TELEPORT", (args) => Teleport(args[0]));
        _eventLibrary.Add("CHANGE_SCENE", (args) => ChangeScene(args[0]));
        _eventLibrary.Add("GOLD", (args) => GainGold(int.Parse(args[0])));
        _eventLibrary.Add("ITEM", (args) => GainItem(args[0]));
        _eventLibrary.Add("LEVEL_UP", (args) => LevelUp(args[0]));
    }
    
    void LevelUp(string level)
    {
        GD.Print($"Niveau augmenté de {level}");
        EmitSignal(SignalName.PlayerLevelUp, int.Parse(level));
    }

    void GainItem(string item)
    {
        GD.Print($"Obtention d'un nouvel objet : {item}");
    }

    void GainGold(int amount)
    {
        GD.Print($"Obtention d'argent : {amount}");
    }

    void ChangeScene(string sceneName)
    {
        GD.Print($"Changement de scène vers : {sceneName}");
    }

    void Teleport(string destination)
    {
        GD.Print($"Téléportation vers : {destination}");
    }

    void StartBattle(string enemies, string quantity)
    {
        ListEnemiesBattle.Clear();
        string[] enemiesArray = enemies.Split(',');
        string[] quantityArray = quantity.Split(',');
        
        // Enemies = ["Rats", "Gobi"]
        // Quantity = ["2", "1"]
        for (int i = 0; i < enemiesArray.Length; i++)
        {
            string enemyName = enemiesArray[i].Trim();
            int nbEnemies = int.Parse(quantityArray[i]);

            // On récupère les stats depuis le Bestiaire
            EnemyStats stats = Bestiary.Instance.GetEnemy(enemyName);

            if (stats != null)
            {
                for (int j = 0; j < nbEnemies; j++)
                {
                    // On ajoute les stats à la liste pour le BattleManager
                    ListEnemiesBattle.Add(stats);
                }
            }
            else
            {
                GD.PrintErr($"Erreur : L'ennemi '{enemyName}' n'existe pas dans le Bestiaire !");
            }
        }
        GD.Print("On se téléporte dans le combat !");
        GetTree().ChangeSceneToFile("res://Scripts/Battle/battle_map.tscn");
    }

    void OnActionTriggered(string fullActionRaw)
    {
        if(string.IsNullOrEmpty(fullActionRaw)) return;
        
        string[] parts = fullActionRaw.Split(':');
        string actionKey = parts[0];
        string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        if (_eventLibrary.TryGetValue(actionKey, out var action))
        {
            action.Invoke(args);
        }
    }
}
