using EchoduKarma.Scripts.Entities.Player;
using Godot;

namespace EchoduKarma.Scripts.UI;

public partial class SkillStatRow : Button
{
    [Export] ColorRect _accentBar;
    [Export] TextureRect _icon;
    [Export] Label _nameLabel;
    [Export] Label _typeBadge;
    [Export] Label _costLabel;

    public Skill BoundSkill { get; private set; }

    public void Bind(Skill skill)
    {
        BoundSkill = skill;
        Text = string.Empty;

        _nameLabel.Text = skill.Name;

        bool isSupport = skill.Type == SkillType.Support;
        _typeBadge.Text = isSupport ? "Soutien" : "Attaque";

        Color accent = UiIcons.GetSkillAccentColor(skill);
        _accentBar.Color = accent;
        _typeBadge.AddThemeColorOverride("font_color", accent);

        UiIcons.Apply(_icon, UiIcons.GetSkillIcon(skill));

        _costLabel.Text = $"{skill.Cost} PM";
    }
}
