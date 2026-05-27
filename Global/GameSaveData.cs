using System.Collections.Generic;

namespace EchoduKarma.Scripts.Data;

public sealed class GameSaveData
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public string SavedAtUtc { get; set; } = "";
    public string ZoneName { get; set; } = "";
    public string ScenePath { get; set; } = "";
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }
    public float PlayerZ { get; set; }
    public int Level { get; set; } = 1;
    public int CurrentExperience { get; set; }
    public int CurrentPv { get; set; }
    public int CurrentMp { get; set; }
    public string PlayerClass { get; set; } = "Magus";
    public int Gold { get; set; }
    public List<string> InventoryItems { get; set; } = new();
    public Dictionary<string, string> EquippedItems { get; set; } = new();
    public Dictionary<string, float> ZoneKarma { get; set; } = new();
    public List<QuestSaveEntry> Quests { get; set; } = new();
}

public sealed class QuestSaveEntry
{
    public string Id { get; set; } = "";
    public int Status { get; set; }
    public int CurrentStep { get; set; }
    public List<int> CompletedStepIndices { get; set; } = new();
    public Dictionary<string, int> KillCounts { get; set; } = new();
}

public readonly struct SaveSlotInfo
{
    public bool Exists { get; init; }
    public int Slot { get; init; }
    public int Level { get; init; }
    public string ZoneName { get; init; }
    public string SavedAtUtc { get; init; }
}
