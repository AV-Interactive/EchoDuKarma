using System.Collections.Generic;
using System.Linq;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

namespace EchoduKarma.Scripts.UI;

/// <summary>Toast de montée de niveau — même style et animation que <see cref="PickupToast"/>.</summary>
public partial class LevelUpPopup : Control
{
    const string ScenePath = "res://UI/level_up_popup.tscn";
    const float FadeInDuration = 0.22f;
    const float HoldDuration = 4.0f;
    const float FadeOutDuration = 0.35f;

    readonly Queue<int> _pendingLevelsGained = new();

    [Export] PanelContainer _panel;
    [Export] TextureRect _icon;
    [Export] RichTextLabel _headerLabel;
    [Export] RichTextLabel _detailLabel;

    Tween _activeTween;
    bool _isShowing;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        Modulate = new Color(1f, 1f, 1f, 0f);

        if (GameManager.Instance != null)
            GameManager.Instance.PlayerLevelUp += OnPlayerLevelUp;

        CallDeferred(MethodName.CheckPendingFromBattle);
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.PlayerLevelUp -= OnPlayerLevelUp;

        _activeTween?.Kill();
        base._ExitTree();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible || !@event.IsActionPressed("ui_accept"))
            return;

        SkipToFadeOut();
        GetViewport().SetInputAsHandled();
    }

    void OnPlayerLevelUp(int levelsGained)
    {
        if (levelsGained <= 0)
            return;

        Enqueue(levelsGained);
    }

    void CheckPendingFromBattle()
    {
        int pending = GameManager.Instance?.ConsumePendingLevelUpPopups() ?? 0;
        if (pending > 0)
            Enqueue(pending);
    }

    void Enqueue(int levelsGained)
    {
        _pendingLevelsGained.Enqueue(levelsGained);
        if (!_isShowing)
            ShowNext();
    }

    void ShowNext()
    {
        if (_pendingLevelsGained.Count == 0)
        {
            _isShowing = false;
            Visible = false;
            return;
        }

        _isShowing = true;
        int levelsGained = _pendingLevelsGained.Dequeue();
        int newLevel = ResolvePlayerLevel();
        if (newLevel <= 0)
        {
            ShowNext();
            return;
        }

        int oldLevel = Mathf.Max(1, newLevel - levelsGained);
        PopulateContent(newLevel, oldLevel, levelsGained);
        PlayShowTween();
    }

    void PopulateContent(int newLevel, int oldLevel, int levelsGained)
    {
        _headerLabel.Text = levelsGained > 1
            ? $"[color=#7AE582]Niveaux supérieurs[/color]  [color=#8899AA](+{levelsGained})[/color]"
            : "[color=#7AE582]Niveau supérieur[/color]";

        UiIcons.Apply(_icon, UiIcons.Load(UiIcons.Karma));

        var lines = new List<string>
        {
            levelsGained > 1
                ? $"[b]Niveau {newLevel}[/b]  [color=#8899AA](+{levelsGained})[/color]"
                : $"[b]Niveau {newLevel}[/b]",
        };

        AppendStatDeltas(lines, oldLevel, newLevel);
        AppendNewSkills(lines, oldLevel, newLevel);

        _detailLabel.Text = string.Join("\n", lines);
    }

    static void AppendStatDeltas(List<string> lines, int oldLevel, int newLevel)
    {
        StatHandler statHandler = ResolveStatHandler();
        if (statHandler == null)
            return;

        Stats before = statHandler.GetStatsForLevel(oldLevel);
        Stats after = statHandler.GetStatsForLevel(newLevel);
        if (before == null || after == null)
            return;

        var deltas = new List<string>();
        AppendDelta(deltas, "PV", after.Pv - before.Pv);
        AppendDelta(deltas, "PM", after.Mp - before.Mp);
        AppendDelta(deltas, "Force", after.Strength - before.Strength);
        AppendDelta(deltas, "Esprit", after.Spirit - before.Spirit);
        AppendDelta(deltas, "Agi", after.Dexterity - before.Dexterity);
        AppendDelta(deltas, "Déf", after.Defense - before.Defense);

        if (deltas.Count == 0)
            return;

        lines.Add($"[color=#FFD166]{string.Join("  ", deltas)}[/color]");
    }

    static void AppendDelta(List<string> parts, string label, int delta)
    {
        if (delta == 0)
            return;

        string sign = delta > 0 ? "+" : "";
        parts.Add($"{label} {sign}{delta}");
    }

    static void AppendNewSkills(List<string> lines, int oldLevel, int newLevel)
    {
        var hero = HeroManager.GetDefaultHero();
        if (hero == null)
            return;

        List<Skill> newSkills = SkillManager.LoadSkills()
            .Where(s => SkillManager.MatchesClass(s, hero.ClassName)
                        && s.LevelRequired > oldLevel
                        && s.LevelRequired <= newLevel)
            .OrderBy(s => s.LevelRequired)
            .ToList();

        if (newSkills.Count == 0)
            return;

        lines.Add("[color=#7AE582]Compétences[/color]");
        foreach (Skill skill in newSkills)
            lines.Add($"[b]{skill.Name}[/b]  [color=#8899AA](niv. {skill.LevelRequired})[/color]");
    }

    static StatHandler ResolveStatHandler()
    {
        Player player = GameManager.Instance?.CurrentPlayer;
        if (player == null || !GodotObject.IsInstanceValid(player))
            return null;

        return player.GetNodeOrNull<StatHandler>("PlayerStats");
    }

    void PlayShowTween()
    {
        Visible = true;
        _panel.Scale = new Vector2(0.94f, 0.94f);
        Modulate = new Color(1f, 1f, 1f, 0f);

        _activeTween?.Kill();
        _activeTween = CreateTween();
        _activeTween.SetParallel(true);
        _activeTween.TweenProperty(this, "modulate:a", 1f, FadeInDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        _activeTween.TweenProperty(_panel, "scale", Vector2.One, FadeInDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        _activeTween.SetParallel(false);
        _activeTween.TweenInterval(HoldDuration);
        _activeTween.TweenProperty(this, "modulate:a", 0f, FadeOutDuration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);
        _activeTween.TweenCallback(Callable.From(ShowNext));
    }

    void SkipToFadeOut()
    {
        if (_activeTween == null || !Visible)
            return;

        _activeTween.Kill();
        _activeTween = CreateTween();
        _activeTween.TweenProperty(this, "modulate:a", 0f, FadeOutDuration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);
        _activeTween.TweenCallback(Callable.From(ShowNext));
    }

    static int ResolvePlayerLevel()
    {
        Player player = GameManager.Instance?.CurrentPlayer;
        if (player != null && GodotObject.IsInstanceValid(player))
            return player.Level;

        return GameManager.Instance?.GetBattleSnapshot()?.Level ?? 0;
    }

    public static LevelUpPopup EnsureOn(Control uiRoot)
    {
        if (uiRoot == null)
            return null;

        var existing = uiRoot.GetNodeOrNull<LevelUpPopup>("LevelUpPopup");
        if (existing != null)
            return existing;

        var packed = GD.Load<PackedScene>(ScenePath);
        if (packed == null)
        {
            GD.PrintErr("[LevelUpPopup] Scène introuvable.");
            return null;
        }

        var popup = packed.Instantiate<LevelUpPopup>();
        popup.Name = "LevelUpPopup";
        uiRoot.AddChild(popup);
        return popup;
    }
}
