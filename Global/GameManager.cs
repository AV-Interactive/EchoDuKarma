using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace EchoduKarma.Scripts.Data;

public partial class GameManager: Node
{
    public static GameManager Instance { get; private set; }
    
    public bool PlayerMoved { get; set; } = true;

    /// <summary>True quand inventaire, stats ou journal bloquent les interactions monde.</summary>
    public bool IsMenuBlockingWorld { get; private set; }

    public void SetMenuBlockingWorld(bool blocking) => IsMenuBlockingWorld = blocking;

    /// <summary>False pendant un menu, un dialogue ou toute UI bloquante.</summary>
    public bool CanInteractWithWorld => PlayerMoved && !IsMenuBlockingWorld;
    
    [Signal] public delegate void PlayerLevelUpEventHandler(int levelUpAmount);
    
    readonly Dictionary<string, Action<string[]>> _eventLibrary = new Dictionary<string, Action<string[]>>();
    
    public List<EnemyStats> ListEnemiesBattle { get; set; } = new List<EnemyStats>();
    public Player CurrentPlayer { get; set; }

    int _battleSignalRetryCount;
    BattleManager _subscribedBattleManager;

    PlayerBattleSnapshot _battleSnapshot;
    Dictionary<int, Stats> _progressionTable;
    string _pendingBattleEnemies;
    string _pendingBattleQuantity;
    string _pendingShopId;
    int _pendingLevelUpPopupCount;

    /// <summary>Scène monde à charger après le combat (définie par MapLoader ou la map courante).</summary>
    public string ReturnScenePath { get; private set; } = "res://Maps/Intro/Map.tscn";

    /// <summary>Nom de zone pour Datas/Progress/{zone}/dialogues.csv.</summary>
    public string ReturnZoneName { get; private set; } = "Introduction";

    public void SetMapContext(string zoneName, string scenePath)
    {
        if (!string.IsNullOrWhiteSpace(zoneName))
            ReturnZoneName = zoneName.Trim();

        if (!string.IsNullOrWhiteSpace(scenePath))
            ReturnScenePath = scenePath;

        KarmaManager.Instance?.SetCurrentZone(ReturnZoneName);
        ZoneEnemyCatalog.LoadZone(ReturnZoneName);
    }

    /// <summary>Efface la progression en mémoire avant une nouvelle partie.</summary>
    public void ClearProgressForNewGame()
    {
        _battleSnapshot = null;
        _progressionTable = null;
        _pendingLevelUpPopupCount = 0;
        ListEnemiesBattle.Clear();
        SetMenuBlockingWorld(false);
        PlayerMoved = true;
    }

    public PlayerBattleSnapshot GetBattleSnapshot() => _battleSnapshot;

    /// <summary>Montées de niveau gagnées en combat, en attente d'affichage sur la map.</summary>
    public int ConsumePendingLevelUpPopups()
    {
        int pending = _pendingLevelUpPopupCount;
        _pendingLevelUpPopupCount = 0;
        return pending;
    }

    public void PersistPlayerForBattle()
    {
        if (CurrentPlayer != null && GodotObject.IsInstanceValid(CurrentPlayer))
        {
            var statHandler = CurrentPlayer.GetNodeOrNull<StatHandler>("PlayerStats");
            _battleSnapshot = PlayerBattleSnapshot.FromPlayer(CurrentPlayer);
            _progressionTable = statHandler != null
                ? new Dictionary<int, Stats>(statHandler.GetProgressionTable())
                : null;
        }
        else if (_battleSnapshot == null)
        {
            GD.PrintErr("[GameManager] Impossible de sauvegarder le joueur avant combat.");
        }
    }

    /// <summary>
    /// Applique l'XP de victoire sur le snapshot de combat. Retourne le nombre de niveaux gagnés.
    /// </summary>
    public int GrantBattleExperience(int amount)
    {
        if (_battleSnapshot == null || _progressionTable == null)
        {
            GD.PrintErr("[GameManager] GrantBattleExperience : snapshot ou progression manquants.");
            return 0;
        }

        int levelsGained = _battleSnapshot.AddExperience(amount, _progressionTable);

        if (levelsGained > 0)
        {
            if (CurrentPlayer != null && GodotObject.IsInstanceValid(CurrentPlayer))
            {
                CurrentPlayer.RefreshLearnedSkills(logNewSkills: true);
                EmitSignal(SignalName.PlayerLevelUp, levelsGained);
            }
            else
                _pendingLevelUpPopupCount += levelsGained;
        }

        return levelsGained;
    }

    public void ApplyBattleSnapshotToPlayer(Player player)
    {
        _battleSnapshot?.ApplyToPlayer(player);
    }
    
    public override void _Ready()
    {
        GD.Print("[AUTOLOAD] GameManager Ready - Start");
        Instance = this;
        
        RegisterEvents();
        ZoneEnemyCatalog.LoadZone(ReturnZoneName);
        
        CallDeferred(nameof(ConnectToSignals));
        GD.Print("[AUTOLOAD] GameManager Ready - End");
    }

    void ConnectToSignals()
    {
        if (DialogueSystem.Instance is not null)
            DialogueSystem.Instance.ActionTriggered += OnActionTriggered;

        if (QuestManager.Instance is not null)
            QuestManager.Instance.QuestCompleted += OnQuestCompleted;
    }
    
    public void RegisterEvents()
    {
        _eventLibrary.Add("BATTLE", (args) => StartBattle(args[0], args[1]));
        _eventLibrary.Add("TELEPORT", (args) => Teleport(args[0]));
        _eventLibrary.Add("CHANGE_SCENE", (args) => ChangeScene(args[0]));
        _eventLibrary.Add("GOLD", (args) => GainGold(int.Parse(args[0])));
        _eventLibrary.Add("ITEM", (args) => GainItem(args[0]));
        _eventLibrary.Add("LEVEL_UP", (args) => LevelUp(args[0]));
        _eventLibrary.Add("KARMA", ApplyKarmaImpact);
    }

    /// <summary>
    /// ACTION_POST_DIALOGUE : KARMA:delta (zone courante) ou KARMA:ZoneName:delta.
    /// Ex. KARMA:+10, KARMA:-10, KARMA:Introduction:-5
    /// </summary>
    void ApplyKarmaImpact(string[] args)
    {
        if (args.Length < 1)
        {
            GD.PrintErr("[GameManager] Action KARMA invalide (format: KARMA:delta ou KARMA:zone:delta).");
            return;
        }

        string zone = ReturnZoneName;
        string deltaRaw = args[0];

        if (args.Length >= 2)
        {
            zone = args[0].Trim();
            deltaRaw = args[1];
        }

        if (!float.TryParse(deltaRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out float delta))
        {
            GD.PrintErr($"[GameManager] Action KARMA : delta invalide '{deltaRaw}'.");
            return;
        }

        KarmaManager.Instance?.ApplyKarmaImpact(zone, delta);
    }
    
    void LevelUp(string level)
    {
        GD.Print($"Niveau augmenté de {level}");
        EmitSignal(SignalName.PlayerLevelUp, int.Parse(level));
    }

    void GainItem(string item)
    {
        if (InventoryManager.Instance?.TryAddItem(item) != true)
            GD.PrintErr($"[GameManager] Impossible d'ajouter l'objet : {item}");
    }

    void GainGold(int amount)
    {
        if (InventoryManager.Instance?.AddGold(amount) != true)
            GD.PrintErr($"[GameManager] Impossible d'ajouter {amount} or.");
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
        CaptureReturnContextFromCurrentScene();
        PersistPlayerForBattle();

        ListEnemiesBattle.Clear();
        string[] enemiesArray = enemies.Split('|');
        string[] quantityArray = quantity.Split('|');
        
        for (int i = 0; i < enemiesArray.Length; i++)
        {
            string enemyName = enemiesArray[i].Trim();
            int nbEnemies = int.Parse(quantityArray[i]);

            for (int j = 0; j < nbEnemies; j++)
            {
                int level = ZoneEnemyCatalog.RollEnemyLevel(ReturnZoneName, enemyName);
                EnemyStats stats = Bestiary.Instance.GetEnemyAtLevel(enemyName, level);

                if (stats != null)
                    ListEnemiesBattle.Add(stats.Clone());
                else if (j == 0)
                    GD.PrintErr($"Erreur : L'ennemi '{enemyName}' n'existe pas dans le Bestiaire !");
            }
        }
        
        GD.Print("[GameManager] Transition vers la scène de combat...");
        MusicManager.Instance?.PlayBattle();
        GetTree().ChangeSceneToFile("res://Maps/Battles/Basic.tscn");

        _battleSignalRetryCount = 0;
        GetTree().CreateTimer(0.1f).Timeout += ConnectBattleSignals;
    }

    void OnBattleEnded(BattleManager.BattleEndReason reason)
    {
        if (_subscribedBattleManager != null && GodotObject.IsInstanceValid(_subscribedBattleManager))
            _subscribedBattleManager.BattleEnded -= OnBattleEnded;
        _subscribedBattleManager = null;

        GD.Print($"[GameManager] Signal de fin de combat reçu : {reason}");
        PlayerMoved = true;
        GetTree().Paused = false;

        if (reason == BattleManager.BattleEndReason.Defeat && _battleSnapshot != null)
        {
            // Pas de sauvegarde pour l'instant : le joueur revient sur la map avec 1 PV et 0 PM.
            _battleSnapshot.CurrentPv = 1;
            _battleSnapshot.CurrentMp = 0;
            GD.Print("[GameManager] Défaite — joueur restauré à 1 PV.");
        }

        DialogueSystem.Instance?.RequestDialogue(null);

        Error err = GetTree().ChangeSceneToFile(ReturnScenePath);
        if (err != Error.Ok)
        {
            GD.PrintErr($"[GameManager] Erreur lors du retour sur map ({ReturnScenePath}) : {err}");
        }
        else
        {
            GD.Print($"[GameManager] Retour sur {ReturnScenePath} (zone {ReturnZoneName}).");
        }
    }

    void CaptureReturnContextFromCurrentScene()
    {
        var scene = GetTree().CurrentScene;
        if (scene == null)
            return;

        string scenePath = scene.SceneFilePath;
        if (string.IsNullOrEmpty(scenePath))
            return;

        if (scene is MapLoader mapLoader)
        {
            SetMapContext(mapLoader.ZoneName, scenePath);
            return;
        }

        SetMapContext(ReturnZoneName, scenePath);
    }

    void OnQuestCompleted(string questId)
    {
        var quest = QuestManager.Instance?.GetQuest(questId);
        if (quest == null) return;

        GD.Print($"[GameManager] Récompenses quête {questId} : +{quest.RewardXp} XP, +{quest.RewardMoney} or");

        if (quest.RewardXp > 0)
            GrantBattleExperience(quest.RewardXp);

        if (quest.RewardMoney > 0)
            GainGold(quest.RewardMoney);

        if (!string.IsNullOrWhiteSpace(quest.RewardObject))
            GainItem(quest.RewardObject);
    }

    void OnActionTriggered(string fullActionRaw)
    {
        if(string.IsNullOrEmpty(fullActionRaw)) return;
        
        string[] parts = fullActionRaw.Split(':');
        string actionKey = parts[0];
        string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        if (actionKey == "BATTLE")
        {
            if (args.Length < 2)
            {
                GD.PrintErr("[GameManager] Action BATTLE invalide (ennemis:quantités attendus).");
                return;
            }

            _pendingBattleEnemies = args[0];
            _pendingBattleQuantity = args[1];
            PersistPlayerForBattle();
            PlayerMoved = true;
            DialogueSystem.Instance?.RequestDialogue(null);
            CallDeferred(nameof(RunPendingBattle));
            return;
        }

        if (actionKey == "SHOP")
        {
            if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
            {
                GD.PrintErr("[GameManager] Action SHOP invalide (format: SHOP:ShopId).");
                return;
            }

            _pendingShopId = args[0].Trim();
            PlayerMoved = true;
            DialogueSystem.Instance?.RequestDialogue(null);
            CallDeferred(nameof(RunPendingShop));
            return;
        }

        if (_eventLibrary.TryGetValue(actionKey, out var action))
        {
            action.Invoke(args);
        }
    }

    void RunPendingBattle()
    {
        if (string.IsNullOrEmpty(_pendingBattleEnemies))
            return;

        StartBattle(_pendingBattleEnemies, _pendingBattleQuantity);
        _pendingBattleEnemies = null;
        _pendingBattleQuantity = null;
    }

    void RunPendingShop()
    {
        if (string.IsNullOrEmpty(_pendingShopId))
            return;

        var shopUi = GetTree().GetFirstNodeInGroup(EchoduKarma.Scripts.UI.ShopUI.GroupName)
            as EchoduKarma.Scripts.UI.ShopUI;

        if (shopUi == null)
        {
            GD.PrintErr("[GameManager] ShopUI introuvable dans la scène courante.");
            _pendingShopId = null;
            return;
        }

        shopUi.Open(_pendingShopId);
        _pendingShopId = null;
    }
    
    void ConnectBattleSignals()
    {
        var bm = GetTree().GetFirstNodeInGroup(BattleManager.GroupName) as BattleManager;
        if (bm == null)
        {
            if (++_battleSignalRetryCount > 10)
            {
                GD.PrintErr("[GameManager] BattleManager introuvable après 10 tentatives.");
                _battleSignalRetryCount = 0;
                return;
            }

            CallDeferred(nameof(ConnectBattleSignals));
            return;
        }

        _battleSignalRetryCount = 0;

        if (_subscribedBattleManager != null && GodotObject.IsInstanceValid(_subscribedBattleManager))
            _subscribedBattleManager.BattleEnded -= OnBattleEnded;

        _subscribedBattleManager = bm;
        bm.BattleEnded += OnBattleEnded;
        GD.Print("[GameManager] Signal BattleEnded connecté au BattleManager.");
    }
    
    void PrintNodeTree(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            GD.Print($"Node: {child.Name} (Type: {child.GetType()})");
            PrintNodeTree(child);
        }
    }
}
