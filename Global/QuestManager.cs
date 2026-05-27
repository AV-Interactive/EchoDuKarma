using System;
using System.Collections.Generic;
using EchoduKarma.Scripts.Data;
using Godot;

public class QuestData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Zone { get; set; }
    public string Description { get; set; }
    /// <summary>Format : "DIALOGUE:dialogueId" — la quête démarre quand ce dialogue est affiché.</summary>
    public string Trigger { get; set; }
    public string[] Steps { get; set; }
    /// <summary>
    /// Formats supportés :
    /// - KILL:NomEnnemi:quantité
    /// - ALL_STEPS (compléter chaque étape — voir STEPS avec syntaxe Label~Trigger)
    /// </summary>
    public string ConditionCompleted { get; set; }
    public int RewardXp { get; set; }
    public int RewardMoney { get; set; }
    public string RewardObject { get; set; }
    public int KarmaImpact { get; set; }
    public string DialLink { get; set; }

    public bool UsesAllStepsCompletion =>
        ConditionCompleted.Equals("ALL_STEPS", StringComparison.OrdinalIgnoreCase);

    /// <summary>Partie affichée du step CSV (avant ~).</summary>
    public static string GetStepLabel(string rawStep)
    {
        if (string.IsNullOrWhiteSpace(rawStep)) return "—";
        int sep = rawStep.IndexOf('~');
        return sep >= 0 ? rawStep[..sep].Trim() : rawStep.Trim();
    }

    /// <summary>Trigger machine de l'étape (après ~). Ex : QUEST_DONE:QUEST_MARCHANDER_01</summary>
    public static string GetStepTrigger(string rawStep)
    {
        if (string.IsNullOrWhiteSpace(rawStep)) return "";
        int sep = rawStep.IndexOf('~');
        return sep >= 0 && sep < rawStep.Length - 1 ? rawStep[(sep + 1)..].Trim() : "";
    }
}

public enum QuestStatus { Inactive, Active, Completed }

public class QuestRuntime
{
    public QuestStatus Status = QuestStatus.Inactive;
    /// <summary>Utilisé par les quêtes à objectif KILL séquentiel (hors ALL_STEPS).</summary>
    public int CurrentStep = 0;
    /// <summary>Indices d'étapes accomplies (quêtes ALL_STEPS — ordre libre).</summary>
    public HashSet<int> CompletedStepIndices = new();
    public Dictionary<string, int> KillCounts = new();

    public bool IsStepCompleted(int index) => CompletedStepIndices.Contains(index);

    public int CompletedStepCount => CompletedStepIndices.Count;
}

/// <summary>
/// Autoload gérant le cycle de vie des quêtes (chargement CSV, déclenchement, progression, complétion).
/// </summary>
public partial class QuestManager : Node
{
    public static QuestManager Instance { get; private set; }

    const string QuestsPath = "res://Datas/Progress/quests.csv";

    readonly Dictionary<string, QuestData> _quests = new();
    readonly Dictionary<string, QuestRuntime> _runtimeStates = new();
    readonly Dictionary<string, string> _dialogueTriggerIndex = new();

    [Signal] public delegate void QuestStartedEventHandler(string questId);
    [Signal] public delegate void QuestCompletedEventHandler(string questId);
    [Signal] public delegate void QuestStepAdvancedEventHandler(string questId, int newStep);

    public override void _Ready()
    {
        GD.Print("[AUTOLOAD] QuestManager Ready - Start");
        Instance = this;
        LoadQuests();
        CallDeferred(nameof(ConnectToSignals));
        GD.Print("[AUTOLOAD] QuestManager Ready - End");
    }

    void ConnectToSignals()
    {
        if (DialogueSystem.Instance is not null)
        {
            DialogueSystem.Instance.DialogueRequested += OnDialogueRequested;
            DialogueSystem.Instance.ActionTriggered += OnActionTriggered;
            GD.Print("[QuestManager] Connecté à DialogueSystem (DialogueRequested + ActionTriggered).");
        }
        else
            GD.PrintErr("[QuestManager] DialogueSystem introuvable — les triggers de quête ne fonctionneront pas !");

        QuestCompleted += OnAnyQuestCompleted;
    }

    void LoadQuests()
    {
        using var file = FileAccess.Open(QuestsPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[QuestManager] Fichier introuvable : {QuestsPath}");
            return;
        }

        file.GetLine();
        int count = 0;

        while (!file.EofReached())
        {
            string[] cols = file.GetCsvLine(";");
            if (cols == null || cols.Length < 13) continue;
            if (string.IsNullOrWhiteSpace(cols[0])) continue;

            try
            {
                var quest = new QuestData
                {
                    Id                 = cols[0].Trim(),
                    Name               = cols[1].Trim(),
                    Type               = cols[2].Trim(),
                    Zone               = cols[3].Trim(),
                    Description        = cols[4].Trim(),
                    Trigger            = cols[5].Trim(),
                    Steps              = cols[6].Split('|'),
                    ConditionCompleted = cols[7].Trim(),
                    RewardXp           = int.TryParse(cols[8].Trim(), out int xp) ? xp : 0,
                    RewardMoney        = int.TryParse(cols[9].Trim(), out int gold) ? gold : 0,
                    RewardObject       = cols[10].Trim(),
                    KarmaImpact        = int.TryParse(cols[11].Trim(), out int karma) ? karma : 0,
                    DialLink           = cols[12].Trim(),
                };

                _quests[quest.Id] = quest;

                if (!string.IsNullOrWhiteSpace(quest.Trigger))
                    _dialogueTriggerIndex[quest.Trigger] = quest.Id;

                count++;
            }
            catch (Exception e)
            {
                GD.PrintErr($"[QuestManager] Erreur parsing : {string.Join("|", cols)} → {e.Message}");
            }
        }

        GD.Print($"[QuestManager] {count} quête(s) chargée(s).");
    }

    void OnDialogueRequested(DialogueLine line)
    {
        if (line is null) return;

        string triggerKey = $"DIALOGUE:{line.Id}";
        if (_dialogueTriggerIndex.TryGetValue(triggerKey, out string questId))
        {
            GD.Print($"[QuestManager] Trigger détecté : dialogue '{line.Id}' → quête '{questId}'.");
            StartQuest(questId);
        }

        ProcessStepTrigger(triggerKey);
    }

    void OnActionTriggered(string action)
    {
        if (string.IsNullOrWhiteSpace(action)) return;

        string[] parts = action.Split(':');
        if (parts[0] == "QUEST_START" && parts.Length > 1)
            StartQuest(parts[1].Trim());
        else if (parts[0] == "QUEST_STEP" && parts.Length > 1)
            AdvanceQuestStepManual(parts[1].Trim());
    }

    void OnAnyQuestCompleted(string completedQuestId)
        => ProcessStepTrigger($"QUEST_DONE:{completedQuestId}");

    public void StartQuest(string questId)
    {
        if (!_quests.ContainsKey(questId))
        {
            GD.PrintErr($"[QuestManager] Quête inconnue : {questId}");
            return;
        }

        var rt = GetOrCreateRuntime(questId);
        if (rt.Status != QuestStatus.Inactive)
        {
            GD.Print($"[QuestManager] StartQuest ignoré : '{questId}' est déjà {rt.Status}.");
            return;
        }

        rt.Status = QuestStatus.Active;
        rt.CurrentStep = 0;
        rt.CompletedStepIndices.Clear();

        var q = _quests[questId];
        GD.Print($"[QuestManager] ══ QUÊTE DÉMARRÉE ══ {q.Name} [{q.Type} / {q.Zone}]");
        GD.Print($"[QuestManager]   Objectif  : {q.ConditionCompleted}");
        if (q.Steps.Length > 0)
        {
            string mode = q.UsesAllStepsCompletion ? "ordre libre" : "séquentiel";
            GD.Print($"[QuestManager]   Étapes ({q.Steps.Length}, {mode}) :");
            foreach (string step in q.Steps)
                GD.Print($"[QuestManager]     · {QuestData.GetStepLabel(step)}");
        }
        GD.Print($"[QuestManager]   Récompenses : {q.RewardXp} XP | {q.RewardMoney} or | {(string.IsNullOrWhiteSpace(q.RewardObject) ? "—" : q.RewardObject)} | karma {(q.KarmaImpact >= 0 ? "+" : "")}{q.KarmaImpact}");

        EmitSignal(SignalName.QuestStarted, questId);
    }

    public void NotifyKill(string enemyName)
    {
        foreach (var (questId, rt) in _runtimeStates)
        {
            if (rt.Status != QuestStatus.Active) continue;

            var quest = _quests[questId];

            if (quest.UsesAllStepsCompletion)
            {
                for (int i = 0; i < quest.Steps.Length; i++)
                {
                    if (rt.IsStepCompleted(i)) continue;

                    string expected = QuestData.GetStepTrigger(quest.Steps[i]);
                    if (!expected.StartsWith("KILL:", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string[] exp = expected.Split(':');
                    if (exp.Length >= 2 && exp[1].Equals(enemyName, StringComparison.OrdinalIgnoreCase))
                    {
                        rt.KillCounts.TryGetValue(enemyName, out int count);
                        rt.KillCounts[enemyName] = count + 1;
                    }
                }

                TryCompleteMatchingSteps(quest, rt, questId, $"KILL:{enemyName}");
                continue;
            }

            if (!quest.ConditionCompleted.StartsWith("KILL:", StringComparison.OrdinalIgnoreCase))
                continue;

            string[] parts = quest.ConditionCompleted.Split(':');
            if (parts.Length < 3) continue;

            string targetEnemy = parts[1].Trim();
            if (!enemyName.Equals(targetEnemy, StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(parts[2].Trim(), out int required)) continue;

            rt.KillCounts.TryGetValue(targetEnemy, out int current);
            int newCount = current + 1;
            rt.KillCounts[targetEnemy] = newCount;

            GD.Print($"[QuestManager] [{questId}] Kill {targetEnemy} : {newCount}/{required}");

            if (newCount >= required)
            {
                GD.Print($"[QuestManager] [{questId}] Condition remplie ({quest.ConditionCompleted}) → complétion.");
                CompleteQuest(questId);
            }
            else
            {
                int step = Math.Min(rt.CurrentStep + 1, quest.Steps.Length - 1);
                rt.CurrentStep = step;
                GD.Print($"[QuestManager] [{questId}] Étape {step + 1}/{quest.Steps.Length} : {QuestData.GetStepLabel(quest.Steps[step])}");
            }
        }
    }

    /// <summary>Avance manuellement l'étape courante (action dialogue QUEST_STEP:id).</summary>
    public void AdvanceQuestStepManual(string questId)
    {
        if (!_quests.TryGetValue(questId, out QuestData quest))
            return;

        var rt = GetOrCreateRuntime(questId);
        if (rt.Status != QuestStatus.Active || !quest.UsesAllStepsCompletion)
            return;

        for (int i = 0; i < quest.Steps.Length; i++)
        {
            if (rt.IsStepCompleted(i)) continue;
            rt.CompletedStepIndices.Add(i);
            rt.CurrentStep = rt.CompletedStepCount;
            EmitSignal(SignalName.QuestStepAdvanced, questId, rt.CompletedStepCount);
            if (rt.CompletedStepCount >= quest.Steps.Length)
                CompleteQuest(questId);
            return;
        }
    }

    void ProcessStepTrigger(string eventKey)
    {
        foreach (var (questId, rt) in _runtimeStates)
        {
            if (rt.Status != QuestStatus.Active) continue;
            if (!_quests.TryGetValue(questId, out QuestData quest)) continue;
            if (!quest.UsesAllStepsCompletion) continue;

            TryCompleteMatchingSteps(quest, rt, questId, eventKey);
        }
    }

    /// <summary>Valide toute étape ALL_STEPS dont le trigger correspond (sans ordre imposé).</summary>
    void TryCompleteMatchingSteps(QuestData quest, QuestRuntime rt, string questId, string eventKey)
    {
        if (rt.CompletedStepCount >= quest.Steps.Length)
            return;

        bool anyNew = false;

        for (int i = 0; i < quest.Steps.Length; i++)
        {
            if (rt.IsStepCompleted(i))
                continue;

            string expected = QuestData.GetStepTrigger(quest.Steps[i]);
            if (string.IsNullOrWhiteSpace(expected))
                continue;

            if (!MatchesStepTrigger(expected, eventKey, rt))
                continue;

            rt.CompletedStepIndices.Add(i);
            anyNew = true;
            GD.Print($"[QuestManager] [{questId}] Étape validée ({eventKey}) : « {QuestData.GetStepLabel(quest.Steps[i])} » ({rt.CompletedStepCount}/{quest.Steps.Length})");
        }

        if (!anyNew)
            return;

        rt.CurrentStep = rt.CompletedStepCount;
        EmitSignal(SignalName.QuestStepAdvanced, questId, rt.CompletedStepCount);

        if (rt.CompletedStepCount >= quest.Steps.Length)
            CompleteQuest(questId);
    }

    bool MatchesStepTrigger(string expected, string eventKey, QuestRuntime rt)
    {
        if (expected.Equals(eventKey, StringComparison.OrdinalIgnoreCase))
            return true;

        // KILL:Rat:2 — vérifie le compteur si l'événement est KILL:Rat
        if (expected.StartsWith("KILL:", StringComparison.OrdinalIgnoreCase)
            && eventKey.StartsWith("KILL:", StringComparison.OrdinalIgnoreCase))
        {
            string[] exp = expected.Split(':');
            string[] evt = eventKey.Split(':');
            if (exp.Length < 3 || evt.Length < 2) return false;
            if (!exp[1].Equals(evt[1], StringComparison.OrdinalIgnoreCase)) return false;
            if (!int.TryParse(exp[2].Trim(), out int required)) return false;

            rt.KillCounts.TryGetValue(exp[1], out int current);
            return current >= required;
        }

        return false;
    }

    public bool CheckCondition(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;

        string[] parts = condition.Split(':');
        if (parts.Length < 2) return true;

        string check = parts[0].Trim();
        string id    = parts[1].Trim();

        bool result = check switch
        {
            "QUEST_ACTIVE"   => GetStatus(id) == QuestStatus.Active,
            "QUEST_DONE"     => GetStatus(id) == QuestStatus.Completed,
            "QUEST_INACTIVE" => GetStatus(id) == QuestStatus.Inactive,
            _                => true,
        };

        if (!result)
            GD.Print($"[QuestManager] Condition dialogue refusée : {condition} (statut actuel : {GetStatus(id)})");

        return result;
    }

    public QuestStatus GetStatus(string questId)
    {
        if (_runtimeStates.TryGetValue(questId, out var rt)) return rt.Status;
        return QuestStatus.Inactive;
    }

    public QuestData GetQuest(string questId) => _quests.GetValueOrDefault(questId);

    public QuestRuntime GetRuntime(string questId) => GetOrCreateRuntime(questId);

    public IEnumerable<(QuestData Data, QuestRuntime Runtime)> GetTrackedQuests()
    {
        foreach (var (questId, data) in _quests)
        {
            var rt = GetOrCreateRuntime(questId);
            if (rt.Status == QuestStatus.Inactive)
                continue;

            yield return (data, rt);
        }
    }

    void CompleteQuest(string questId)
    {
        if (!_runtimeStates.TryGetValue(questId, out var rt)) return;
        if (rt.Status == QuestStatus.Completed) return;

        rt.Status = QuestStatus.Completed;
        var quest = _quests[questId];

        GD.Print($"[QuestManager] ══ QUÊTE TERMINÉE ══ {quest.Name}");
        GD.Print($"[QuestManager]   XP      : +{quest.RewardXp}");
        GD.Print($"[QuestManager]   Or      : +{quest.RewardMoney}");
        GD.Print($"[QuestManager]   Objet   : {(string.IsNullOrWhiteSpace(quest.RewardObject) ? "—" : quest.RewardObject)}");
        GD.Print($"[QuestManager]   Karma   : {(quest.KarmaImpact >= 0 ? "+" : "")}{quest.KarmaImpact}");

        if (quest.KarmaImpact != 0)
            KarmaManager.Instance?.ApplyKarmaImpact(quest.Zone, (float)quest.KarmaImpact);

        EmitSignal(SignalName.QuestCompleted, questId);
    }

    QuestRuntime GetOrCreateRuntime(string questId)
    {
        if (!_runtimeStates.ContainsKey(questId))
            _runtimeStates[questId] = new QuestRuntime();
        return _runtimeStates[questId];
    }

    public List<QuestSaveEntry> ExportQuestStates()
    {
        var entries = new List<QuestSaveEntry>();

        foreach (var questId in _quests.Keys)
        {
            if (!_runtimeStates.TryGetValue(questId, out QuestRuntime rt))
                continue;

            if (rt.Status == QuestStatus.Inactive)
                continue;

            entries.Add(new QuestSaveEntry
            {
                Id = questId,
                Status = (int)rt.Status,
                CurrentStep = rt.CurrentStep,
                CompletedStepIndices = new List<int>(rt.CompletedStepIndices),
                KillCounts = new Dictionary<string, int>(rt.KillCounts),
            });
        }

        return entries;
    }

    public void ImportQuestStates(IReadOnlyList<QuestSaveEntry> entries)
    {
        _runtimeStates.Clear();

        if (entries == null)
            return;

        foreach (QuestSaveEntry entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id) || !_quests.ContainsKey(entry.Id))
                continue;

            var rt = new QuestRuntime
            {
                Status = Enum.IsDefined(typeof(QuestStatus), entry.Status)
                    ? (QuestStatus)entry.Status
                    : QuestStatus.Inactive,
                CurrentStep = entry.CurrentStep,
            };

            if (entry.CompletedStepIndices != null)
            {
                foreach (int index in entry.CompletedStepIndices)
                    rt.CompletedStepIndices.Add(index);
            }

            if (entry.KillCounts != null)
            {
                foreach (var pair in entry.KillCounts)
                    rt.KillCounts[pair.Key] = pair.Value;
            }

            _runtimeStates[entry.Id] = rt;
        }
    }
}
