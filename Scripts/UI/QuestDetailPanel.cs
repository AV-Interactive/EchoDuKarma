using System;
using System.Collections.Generic;
using Godot;

public partial class QuestDetailPanel : PanelContainer
{
    [Export] RichTextLabel _nameLabel;
    [Export] RichTextLabel _metaLabel;
    [Export] RichTextLabel _descriptionLabel;
    [Export] HBoxContainer _stepsContainer;
    [Export] RichTextLabel _objectiveLabel;
    [Export] RichTextLabel _rewardsLabel;

    public void SetQuest(QuestData quest, QuestRuntime runtime)
    {
        if (quest == null || runtime == null)
            return;

        if (_nameLabel == null || _metaLabel == null || _descriptionLabel == null
            || _stepsContainer == null || _objectiveLabel == null || _rewardsLabel == null)
        {
            GD.PrintErr("[QuestDetailPanel] Exports manquants — détail quête non affiché.");
            return;
        }

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

        var entries = new List<(int Index, string Text, string Tone)>();

        if (quest.Steps != null)
        {
            for (int i = 0; i < quest.Steps.Length; i++)
            {
                string rawStep = quest.Steps[i];
                if (string.IsNullOrWhiteSpace(rawStep))
                    continue;

                string prefix = GetStepPrefix(i, quest, runtime);
                entries.Add((i, $"{prefix} {QuestData.GetStepLabel(rawStep)}", GetStepTone(i, quest, runtime)));
            }
        }

        if (entries.Count == 0)
        {
            BuildSingleColumnSteps(new[] { ("—", "neutral") }, fontSize: 10);
            return;
        }

        (int columnCount, int fontSize) = GetStepLayout(entries.Count);
        int rowsPerColumn = (int)Math.Ceiling(entries.Count / (double)columnCount);

        _stepsContainer.AddThemeConstantOverride("separation", columnCount > 1 ? 10 : 0);

        var columns = new List<VBoxContainer>(columnCount);
        for (int c = 0; c < columnCount; c++)
        {
            var column = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            column.AddThemeConstantOverride("separation", 1);
            columns.Add(column);
            _stepsContainer.AddChild(column);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            int col = Math.Min(i / rowsPerColumn, columnCount - 1);
            var (_, text, tone) = entries[i];
            columns[col].AddChild(CreateStepLabel(text, tone, fontSize));
        }
    }

    void BuildSingleColumnSteps(IEnumerable<(string Text, string Tone)> lines, int fontSize)
    {
        var column = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        column.AddThemeConstantOverride("separation", 1);
        _stepsContainer.AddChild(column);

        foreach (var (text, tone) in lines)
            column.AddChild(CreateStepLabel(text, tone, fontSize));
    }

    static (int ColumnCount, int FontSize) GetStepLayout(int stepCount) => stepCount switch
    {
        <= 3 => (1, 10),
        <= 6 => (2, 9),
        <= 9 => (3, 9),
        <= 12 => (3, 8),
        _    => (3, 7),
    };

    static Label CreateStepLabel(string text, string tone, int fontSize)
    {
        Color color = tone switch
        {
            "done"    => new Color("#7AE582"),
            "current" => new Color("#FFD166"),
            _         => new Color("#8899AA"),
        };

        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.Off,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    static string GetStepPrefix(int index, QuestData quest, QuestRuntime runtime)
    {
        if (runtime.Status == QuestStatus.Completed)
            return "✓";

        if (quest.UsesAllStepsCompletion)
        {
            if (runtime.IsStepCompleted(index))
                return "✓";
            return "·";
        }

        if (index < runtime.CurrentStep)
            return "✓";

        if (index == runtime.CurrentStep)
            return "▶";

        return "·";
    }

    static string GetStepTone(int index, QuestData quest, QuestRuntime runtime)
    {
        if (runtime.Status == QuestStatus.Completed)
            return "done";

        if (quest.UsesAllStepsCompletion)
            return runtime.IsStepCompleted(index) ? "done" : "pending";

        if (index < runtime.CurrentStep)
            return "done";

        if (index == runtime.CurrentStep)
            return "current";

        return "pending";
    }

    static string FormatObjective(QuestData quest, QuestRuntime runtime)
    {
        if (quest.UsesAllStepsCompletion)
        {
            int total = quest.Steps?.Length ?? 0;
            int done = runtime.Status == QuestStatus.Completed
                ? total
                : Mathf.Clamp(runtime.CompletedStepCount, 0, total);
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
        var parts = new List<string>();

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
