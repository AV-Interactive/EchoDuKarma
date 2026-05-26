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
    public int CurrentStep = 0;
    public Dictionary<string, int> KillCounts = new();
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

        var q = _quests[questId];
        GD.Print($"[QuestManager] ══ QUÊTE DÉMARRÉE ══ {q.Name} [{q.Type} / {q.Zone}]");
        GD.Print($"[QuestManager]   Objectif  : {q.ConditionCompleted}");
        if (q.Steps.Length > 0)
            GD.Print($"[QuestManager]   Étape 1/{q.Steps.Length} : {QuestData.GetStepLabel(q.Steps[0])}");
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
                string expected = QuestData.GetStepTrigger(quest.Steps[rt.CurrentStep]);
                if (expected.StartsWith("KILL:", StringComparison.OrdinalIgnoreCase))
                {
                    string[] exp = expected.Split(':');
                    if (exp.Length >= 2 && exp[1].Equals(enemyName, StringComparison.OrdinalIgnoreCase))
                    {
                        rt.KillCounts.TryGetValue(enemyName, out int count);
                        rt.KillCounts[enemyName] = count + 1;
                    }
                }

                TryAdvanceAllStepsQuest(quest, rt, questId, $"KILL:{enemyName}");
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

        AdvanceAllStepsQuest(quest, rt, questId, "QUEST_STEP (manuel)");
    }

    void ProcessStepTrigger(string eventKey)
    {
        foreach (var (questId, rt) in _runtimeStates)
        {
            if (rt.Status != QuestStatus.Active) continue;
            if (!_quests.TryGetValue(questId, out QuestData quest)) continue;
            if (!quest.UsesAllStepsCompletion) continue;

            TryAdvanceAllStepsQuest(quest, rt, questId, eventKey);
        }
    }

    void TryAdvanceAllStepsQuest(QuestData quest, QuestRuntime rt, string questId, string eventKey)
    {
        if (rt.CurrentStep >= quest.Steps.Length)
            return;

        string expected = QuestData.GetStepTrigger(quest.Steps[rt.CurrentStep]);
        if (string.IsNullOrWhiteSpace(expected))
            return;

        if (!MatchesStepTrigger(expected, eventKey, rt))
            return;

        AdvanceAllStepsQuest(quest, rt, questId, eventKey);
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

    void AdvanceAllStepsQuest(QuestData quest, QuestRuntime rt, string questId, string cause)
    {
        string completedLabel = QuestData.GetStepLabel(quest.Steps[rt.CurrentStep]);
        rt.CurrentStep++;

        GD.Print($"[QuestManager] [{questId}] Étape validée ({cause}) : « {completedLabel} » → {rt.CurrentStep}/{quest.Steps.Length}");

        EmitSignal(SignalName.QuestStepAdvanced, questId, rt.CurrentStep);

        if (rt.CurrentStep >= quest.Steps.Length)
            CompleteQuest(questId);
        else
            GD.Print($"[QuestManager] [{questId}] Prochaine étape : {QuestData.GetStepLabel(quest.Steps[rt.CurrentStep])}");
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
            KarmaManager.Instance?.ApplyKarmaImpact(quest.Zone, quest.KarmaImpact);

        EmitSignal(SignalName.QuestCompleted, questId);
    }

    QuestRuntime GetOrCreateRuntime(string questId)
    {
        if (!_runtimeStates.ContainsKey(questId))
            _runtimeStates[questId] = new QuestRuntime();
        return _runtimeStates[questId];
    }
}
