using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Godot;

namespace EchoduKarma.Scripts.Data;

public partial class SaveManager : Node
{
    public const int SlotCount = 3;
    public const string NewGameScenePath = "res://Maps/Intro/Map.tscn";
    public const string NewGameZoneName = "Introduction";
    const string SavesDirectory = "user://saves";

    public static SaveManager Instance { get; private set; }

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    GameSaveData _pendingPlayerSave;

    [Signal] public delegate void SaveCompletedEventHandler(int slot);
    [Signal] public delegate void LoadCompletedEventHandler(int slot);
    [Signal] public delegate void SaveFailedEventHandler(int slot, string reason);
    [Signal] public delegate void LoadFailedEventHandler(int slot, string reason);

    public override void _Ready()
    {
        Instance = this;
        EnsureSaveDirectory();
        GD.Print("[AUTOLOAD] SaveManager Ready.");
    }

    /// <summary>Slot le plus récent (1–3), ou -1 si aucune sauvegarde.</summary>
    public int GetMostRecentSaveSlot()
    {
        int bestSlot = -1;
        DateTime bestTime = DateTime.MinValue;

        for (int slot = 1; slot <= SlotCount; slot++)
        {
            SaveSlotInfo info = GetSlotInfo(slot);
            if (!info.Exists || string.IsNullOrWhiteSpace(info.SavedAtUtc))
                continue;

            if (!DateTime.TryParse(info.SavedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime savedAt))
                continue;

            if (savedAt > bestTime)
            {
                bestTime = savedAt;
                bestSlot = slot;
            }
        }

        return bestSlot;
    }

    public void StartNewGame()
    {
        _pendingPlayerSave = null;
        InventoryManager.Instance?.ResetToNewGameLoadout();
        KarmaManager.Instance?.ImportZoneKarma(null, NewGameZoneName);
        QuestManager.Instance?.ImportQuestStates(Array.Empty<QuestSaveEntry>());
        GameManager.Instance?.ClearProgressForNewGame();
        GameManager.Instance?.SetMapContext(NewGameZoneName, NewGameScenePath);
        GameManager.Instance.SetMenuBlockingWorld(false);
        GameManager.Instance.PlayerMoved = true;

        Error err = GetTree().ChangeSceneToFile(NewGameScenePath);
        if (err != Error.Ok)
            GD.PrintErr($"[SaveManager] Nouvelle partie — erreur scène : {err}");
    }

    public SaveSlotInfo GetSlotInfo(int slot)
    {
        if (!IsValidSlot(slot))
            return new SaveSlotInfo { Slot = slot };

        string path = GetSlotPath(slot);
        if (!FileAccess.FileExists(path))
            return new SaveSlotInfo { Slot = slot };

        try
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
                return new SaveSlotInfo { Slot = slot };

            var data = JsonSerializer.Deserialize<GameSaveData>(file.GetAsText(), JsonOptions);
            if (data == null)
                return new SaveSlotInfo { Slot = slot };

            return new SaveSlotInfo
            {
                Exists = true,
                Slot = slot,
                Level = data.Level,
                ZoneName = data.ZoneName,
                SavedAtUtc = data.SavedAtUtc,
            };
        }
        catch (Exception e)
        {
            GD.PrintErr($"[SaveManager] Lecture slot {slot} : {e.Message}");
            return new SaveSlotInfo { Slot = slot };
        }
    }

    public bool SaveToSlot(int slot, out string error, out SaveSlotInfo slotInfo)
    {
        error = "";
        slotInfo = new SaveSlotInfo { Slot = slot };

        if (!IsValidSlot(slot))
        {
            error = "Emplacement invalide.";
            EmitSignal(SignalName.SaveFailed, slot, error);
            return false;
        }

        if (!TryCaptureCurrentState(out GameSaveData data, out error))
        {
            EmitSignal(SignalName.SaveFailed, slot, error);
            return false;
        }

        EnsureSaveDirectory();

        try
        {
            string json = JsonSerializer.Serialize(data, JsonOptions);
            using var file = FileAccess.Open(GetSlotPath(slot), FileAccess.ModeFlags.Write);
            if (file == null)
            {
                error = "Impossible d'écrire le fichier de sauvegarde.";
                EmitSignal(SignalName.SaveFailed, slot, error);
                return false;
            }

            file.StoreString(json);

            slotInfo = new SaveSlotInfo
            {
                Exists = true,
                Slot = slot,
                Level = data.Level,
                ZoneName = data.ZoneName,
                SavedAtUtc = data.SavedAtUtc,
            };

            GD.Print($"[SaveManager] Sauvegarde slot {slot} ({data.ZoneName}, niv. {data.Level}).");
            EmitSignal(SignalName.SaveCompleted, slot);
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            GD.PrintErr($"[SaveManager] Échec sauvegarde slot {slot} : {e.Message}");
            EmitSignal(SignalName.SaveFailed, slot, error);
            return false;
        }
    }

    public bool LoadFromSlot(int slot, out string error)
    {
        error = "";

        if (!IsValidSlot(slot))
        {
            error = "Emplacement invalide.";
            EmitSignal(SignalName.LoadFailed, slot, error);
            return false;
        }

        string path = GetSlotPath(slot);
        if (!FileAccess.FileExists(path))
        {
            error = "Aucune sauvegarde sur cet emplacement.";
            EmitSignal(SignalName.LoadFailed, slot, error);
            return false;
        }

        try
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                error = "Impossible de lire la sauvegarde.";
                EmitSignal(SignalName.LoadFailed, slot, error);
                return false;
            }

            var data = JsonSerializer.Deserialize<GameSaveData>(file.GetAsText(), JsonOptions);
            if (data == null || data.Version > GameSaveData.CurrentVersion)
            {
                error = "Fichier de sauvegarde invalide ou trop récent.";
                EmitSignal(SignalName.LoadFailed, slot, error);
                return false;
            }

            ApplySaveData(data);
            GD.Print($"[SaveManager] Chargement slot {slot} → {data.ScenePath}.");
            EmitSignal(SignalName.LoadCompleted, slot);
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            GD.PrintErr($"[SaveManager] Échec chargement slot {slot} : {e.Message}");
            EmitSignal(SignalName.LoadFailed, slot, error);
            return false;
        }
    }

    public bool DeleteSlot(int slot, out string error)
    {
        error = "";

        if (!IsValidSlot(slot))
        {
            error = "Emplacement invalide.";
            return false;
        }

        string path = GetSlotPath(slot);
        if (!FileAccess.FileExists(path))
        {
            error = "Emplacement déjà vide.";
            return false;
        }

        Error err = DirAccess.RemoveAbsolute(path);
        if (err != Error.Ok)
        {
            error = "Impossible de supprimer la sauvegarde.";
            return false;
        }

        GD.Print($"[SaveManager] Slot {slot} supprimé.");
        return true;
    }

    public bool TryApplyPendingPlayerState(Player player)
    {
        if (_pendingPlayerSave == null || player == null || !GodotObject.IsInstanceValid(player))
            return false;

        var data = _pendingPlayerSave;
        _pendingPlayerSave = null;

        player.GlobalPosition = new Vector3(data.PlayerX, data.PlayerY, data.PlayerZ);

        var stats = player.GetNodeOrNull<StatHandler>("PlayerStats");
        if (stats != null)
        {
            stats.CurrentLevel = data.Level;
            stats.CurrentExperience = data.CurrentExperience;
            stats.CurrentPv = data.CurrentPv;
            stats.CurrentMp = data.CurrentMp;

            Stats row = stats.GetStatsForLevel(data.Level);
            if (row != null)
            {
                stats.PvMax = row.Pv;
                stats.MpMax = row.Mp;
                stats.Strength = row.Strength;
                stats.Dexterity = row.Dexterity;
                stats.Spirit = row.Spirit;
                stats.Defense = row.Defense;
            }
        }

        player.RefreshLearnedSkills(logNewSkills: false);
        GameManager.Instance.CurrentPlayer = player;
        return true;
    }

    bool TryCaptureCurrentState(out GameSaveData data, out string error)
    {
        data = null;
        error = "";

        var player = GameManager.Instance?.CurrentPlayer;
        if (player == null || !GodotObject.IsInstanceValid(player))
        {
            error = "Joueur introuvable.";
            return false;
        }

        var stats = player.GetNodeOrNull<StatHandler>("PlayerStats");
        if (stats == null)
        {
            error = "Statistiques joueur introuvables.";
            return false;
        }

        if (InventoryManager.Instance == null)
        {
            error = "Inventaire indisponible.";
            return false;
        }

        Vector3 pos = player.GlobalPosition;
        var inventory = InventoryManager.Instance.ExportSaveData();

        data = new GameSaveData
        {
            SavedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            ZoneName = GameManager.Instance.ReturnZoneName,
            ScenePath = GameManager.Instance.ReturnScenePath,
            PlayerX = pos.X,
            PlayerY = pos.Y,
            PlayerZ = pos.Z,
            Level = stats.CurrentLevel,
            CurrentExperience = stats.CurrentExperience,
            CurrentPv = stats.CurrentPv,
            CurrentMp = stats.CurrentMp,
            PlayerClass = inventory.PlayerClass,
            Gold = inventory.Gold,
            InventoryItems = inventory.Items,
            EquippedItems = inventory.Equipped,
            ZoneKarma = KarmaManager.Instance?.ExportZoneKarma() ?? new Dictionary<string, float>(),
            Quests = QuestManager.Instance?.ExportQuestStates() ?? new List<QuestSaveEntry>(),
        };

        if (string.IsNullOrWhiteSpace(data.ScenePath))
        {
            error = "Scène courante inconnue.";
            return false;
        }

        return true;
    }

    void ApplySaveData(GameSaveData data)
    {
        InventoryManager.Instance?.ImportSaveData(data.Gold, data.PlayerClass, data.InventoryItems, data.EquippedItems);
        KarmaManager.Instance?.ImportZoneKarma(data.ZoneKarma, data.ZoneName);
        QuestManager.Instance?.ImportQuestStates(data.Quests);

        GameManager.Instance?.SetMapContext(data.ZoneName, data.ScenePath);
        _pendingPlayerSave = data;

        GameManager.Instance.SetMenuBlockingWorld(false);
        GameManager.Instance.PlayerMoved = true;

        Error err = GetTree().ChangeSceneToFile(data.ScenePath);
        if (err != Error.Ok)
            GD.PrintErr($"[SaveManager] Erreur changement scène ({data.ScenePath}) : {err}");
    }

    static bool IsValidSlot(int slot) => slot >= 1 && slot <= SlotCount;

    static string GetSlotPath(int slot) => $"{SavesDirectory}/slot_{slot}.json";

    static void EnsureSaveDirectory()
    {
        if (DirAccess.DirExistsAbsolute(SavesDirectory))
            return;

        Error err = DirAccess.MakeDirRecursiveAbsolute(SavesDirectory);
        if (err != Error.Ok)
            GD.PrintErr($"[SaveManager] Impossible de créer {SavesDirectory} : {err}");
    }
}
