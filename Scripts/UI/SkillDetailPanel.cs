using EchoduKarma.Scripts.Entities.Player;
using Godot;

namespace EchoduKarma.Scripts.UI;

public partial class SkillDetailPanel : PanelContainer
{
    [Export] TextureRect _icon;
    [Export] RichTextLabel _nameLabel;
    [Export] RichTextLabel _metaLabel;
    [Export] RichTextLabel _statsLabel;
    [Export] RichTextLabel _effectLabel;
    [Export] RichTextLabel _descriptionLabel;

    public void SetSkill(Skill skill)
    {
        UiIcons.Apply(_icon, UiIcons.GetSkillIcon(skill));

        _nameLabel.Text = $"[b]{skill.Name}[/b]";

        string typeName = skill.Type == SkillType.Support ? "Soutien" : "Attaque";
        string element = string.IsNullOrWhiteSpace(skill.Element) ? "—" : skill.Element;
        string target = string.IsNullOrWhiteSpace(skill.TargetType) ? "—" : skill.TargetType;
        string accentHex = UiIcons.GetSkillAccentColor(skill).ToHtml(false);

        _metaLabel.Text =
            $"[color={accentHex}]{typeName}[/color]  ·  [color={accentHex}]{element}[/color]  ·  Cible : {target}";

        _statsLabel.Text = BuildStatsText(skill);

        _effectLabel.Text = string.IsNullOrWhiteSpace(skill.Effect)
            ? "[color=#8899AA]Effet spécial : aucun[/color]"
            : $"[color=#FFD166]Effet :[/color] {skill.Effect}";

        _descriptionLabel.Text = string.IsNullOrWhiteSpace(skill.Description)
            ? "[color=#8899AA]Aucune description[/color]"
            : skill.Description;
    }

    static string BuildStatsText(Skill skill)
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"[color=#27B0F5]{skill.Cost} PM[/color]",
            $"[color=#FFD166]{skill.Power} puissance[/color]",
        };

        if (skill.Speed > 0)
            parts.Add($"Vitesse {skill.Speed}");

        if (skill.LevelRequired > 1)
            parts.Add($"Niveau {skill.LevelRequired}+");

        if (skill.Classes is { Count: > 0 })
            parts.Add(string.Join(", ", skill.Classes));

        return string.Join("  ·  ", parts);
    }
}
