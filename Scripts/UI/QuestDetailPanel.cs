using System;
using Godot;

public partial class QuestDetailPanel : PanelContainer
{
    [Export] RichTextLabel _nameLabel;
    [Export] RichTextLabel _metaLabel;
    [Export] RichTextLabel _descriptionLabel;
    [Export] VBoxContainer _stepsContainer;
    [Export] RichTextLabel _objectiveLabel;
    [Export] RichTextLabel _rewardsLabel;

    public void SetQuest(QuestData quest, QuestRuntime runtime)
    {
        _nameLabel.Text = $"[b]{quest.Name}[/b]";

        string statusText = runtime.Status switch
        {
            QuestStatus.Active    => "[color=#FFD166]En cours[/color]",
            QuestStatus.Completed => "[color=#7AE582]Terminée[/color]",
            _                     => "[color=#8899AA]Inactive[/color]",
        };
        _metaLabel.Text =
            $"[color=#58B4C6]{quest.Type}[/color]  ·  {quest.Zone}  ·  {statusText}";

        _descriptionLabel.Text = string.IsNullOrWhiteSpace(quest.Description)
            ? "—"
            : quest.Description;

        PopulateSteps(quest, runtime);
        _objectiveLabel.Text = FormatObjective(quest, runtime);
        _rewardsLabel.Text = FormatRewards(quest);
    }

    void PopulateSteps(QuestData quest, QuestRuntime runtime)
    {
        foreach (Node child in _stepsContainer.GetChildren())
            child.QueueFree();

        if (quest.Steps == null || quest.Steps.Length == 0)
        {
            AddStepLine("—", "neutral");
            return;
        }

        for (int i = 0; i < quest.Steps.Length; i++)
        {
            string prefix = GetStepPrefix(i, quest, runtime);
            AddStepLine($"{prefix} {QuestData.GetStepLabel(quest.Steps[i])}", GetStepTone(i, quest, runtime));
        }
    }

    static string GetStepPrefix(int index, QuestData quest, QuestRuntime runtime)
    {
        if (runtime.Status == QuestStatus.Completed)
            return "✓";

        if (index < runtime.CurrentStep)
            return "✓";

        if (index == runtime.CurrentStep)
            return "▶";

        return "·";
    }

    static string GetStepTone(int index, QuestData quest, QuestRuntime runtime)
    {
        if (runtime.Status == QuestStatus.Completed || index < runtime.CurrentStep)
            return "done";

        if (index == runtime.CurrentStep)
            return "current";

        return "pending";
    }

    void AddStepLine(string text, string tone)
    {
        string color = tone switch
        {
            "done"    => "#7AE582",
            "current" => "#FFD166",
            _         => "#8899AA",
        };

        var label = new RichTextLabel
        {
            BbcodeEnabled = true,
            Text = $"[color={color}]{text}[/color]",
            FitContent = true,
            ScrollActive = false,
        };
        label.AddThemeFontSizeOverride("normal_font_size", 11);
        _stepsContainer.AddChild(label);
    }

    static string FormatObjective(QuestData quest, QuestRuntime runtime)
    {
        if (quest.UsesAllStepsCompletion)
        {
            int total = quest.Steps?.Length ?? 0;
            int done = runtime.Status == QuestStatus.Completed
                ? total
                : Mathf.Clamp(runtime.CurrentStep, 0, total);
            string color = done >= total && total > 0 ? "#7AE582" : "#FFD166";
            return total > 0
                ? $"[color={color}]Étapes accomplies : {done}/{total}[/color]"
                : "[color=#8899AA]Aucune étape définie[/color]";
        }

        if (string.IsNullOrWhiteSpace(quest.ConditionCompleted))
            return "[color=#8899AA]Aucun objectif défini[/color]";

        string[] parts = quest.ConditionCompleted.Split(':');
        if (parts.Length >= 3 && parts[0].Equals("KILL", StringComparison.OrdinalIgnoreCase))
        {
            string enemy = parts[1].Trim();
            if (!int.TryParse(parts[2].Trim(), out int required))
                required = 1;

            runtime.KillCounts.TryGetValue(enemy, out int current);
            if (runtime.Status == QuestStatus.Completed)
                current = required;

            string color = current >= required ? "#7AE582" : "#FFD166";
            return $"[color={color}]Éliminer {enemy} : {current}/{required}[/color]";
        }

        return quest.ConditionCompleted;
    }

    static string FormatRewards(QuestData quest)
    {
        var parts = new System.Collections.Generic.List<string>();

        if (quest.RewardXp > 0)
            parts.Add($"[color=#FFD166]+{quest.RewardXp} XP[/color]");

        if (quest.RewardMoney > 0)
            parts.Add($"[color=#F5D76E]+{quest.RewardMoney} or[/color]");

        if (!string.IsNullOrWhiteSpace(quest.RewardObject))
            parts.Add(quest.RewardObject);

        if (quest.KarmaImpact != 0)
        {
            string sign = quest.KarmaImpact > 0 ? "+" : "";
            parts.Add($"[color=#58B4C6]Karma {sign}{quest.KarmaImpact}[/color]");
        }

        return parts.Count > 0 ? string.Join("  ·  ", parts) : "[color=#8899AA]—[/color]";
    }
}
