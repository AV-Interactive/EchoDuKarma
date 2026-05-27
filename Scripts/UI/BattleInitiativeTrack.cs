using System.Collections.Generic;
using Godot;

namespace EchoduKarma.Scripts.UI;

/// <summary>Panneau gauche : ordre d'initiative du round (portraits + tri temps réel).</summary>
public partial class BattleInitiativeTrack : Panel
{
    const int PortraitSize = 22;
    const int BuffIconSize = 12;
    const float RowHeight = 24f;
    const float TitleHeight = 12f;
    const float PanelChrome = 10f;
    const float PanelWidth = 148f;
    const float MaxPanelHeight = 118f;

    static readonly StyleBoxFlat PanelStyle = new()
    {
        BgColor = new Color(0f, 0f, 0f, 0.72f),
        BorderColor = new Color(0.35f, 0.55f, 0.75f, 0.9f),
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusTopLeft = 3,
        CornerRadiusTopRight = 3,
        CornerRadiusBottomLeft = 3,
        CornerRadiusBottomRight = 3,
        ContentMarginLeft = 4,
        ContentMarginRight = 4,
        ContentMarginTop = 3,
        ContentMarginBottom = 3,
    };

    static readonly StyleBoxFlat RowActiveStyle = new()
    {
        BgColor = new Color(0.45f, 0.38f, 0.12f, 0.85f),
        CornerRadiusTopLeft = 3,
        CornerRadiusTopRight = 3,
        CornerRadiusBottomLeft = 3,
        CornerRadiusBottomRight = 3,
    };

    static readonly StyleBoxFlat RowPlayerStyle = new()
    {
        BgColor = new Color(0.12f, 0.22f, 0.38f, 0.55f),
        CornerRadiusTopLeft = 3,
        CornerRadiusTopRight = 3,
        CornerRadiusBottomLeft = 3,
        CornerRadiusBottomRight = 3,
    };

    VBoxContainer _list;

    public override void _Ready()
    {
        ZIndex = 25;
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeStyleboxOverride("panel", PanelStyle);
        SetAnchorsPreset(LayoutPreset.TopLeft);
        OffsetLeft = 6;
        OffsetTop = 44;
        ApplyPanelSize(1);

        var root = new VBoxContainer
        {
            Name = "Root",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(root);

        var title = new Label
        {
            Text = "Initiative",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 9);
        title.AddThemeColorOverride("font_color", new Color(0.75f, 0.88f, 1f));
        root.AddChild(title);

        _list = new VBoxContainer
        {
            Name = "List",
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _list.AddThemeConstantOverride("separation", 1);
        root.AddChild(_list);
    }

    void ApplyPanelSize(int rowCount)
    {
        float contentHeight = TitleHeight + PanelChrome + rowCount * RowHeight;
        OffsetRight = OffsetLeft + PanelWidth;
        OffsetBottom = OffsetTop + Mathf.Min(contentHeight, MaxPanelHeight);
    }

    public void SetEntries(IReadOnlyList<InitiativeDisplayEntry> entries)
    {
        if (_list == null)
            return;

        foreach (Node child in _list.GetChildren())
            child.QueueFree();

        int rowCount = 1;
        if (entries == null || entries.Count == 0)
            _list.AddChild(MakeHintLabel("—"));
        else
        {
            rowCount = entries.Count;
            for (int i = 0; i < entries.Count; i++)
                _list.AddChild(BuildRow(entries[i], i + 1));
        }

        ApplyPanelSize(rowCount);
    }

    static Label MakeHintLabel(string text) => new()
    {
        Text = text,
        HorizontalAlignment = HorizontalAlignment.Center,
    };

    Control BuildRow(InitiativeDisplayEntry entry, int rank)
    {
        var row = new PanelContainer();
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        if (entry.IsActive)
            row.AddThemeStyleboxOverride("panel", RowActiveStyle);
        else if (entry.IsPlayer && !entry.IsCompleted)
            row.AddThemeStyleboxOverride("panel", RowPlayerStyle);

        float modulate = entry.IsCompleted ? 0.45f : 1f;
        row.Modulate = new Color(modulate, modulate, modulate, 1f);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 2);
        row.AddChild(hbox);

        var rankLabel = new Label
        {
            Text = entry.IsActive ? "▶" : $"{rank}",
            CustomMinimumSize = new Vector2(10, PortraitSize),
            VerticalAlignment = VerticalAlignment.Center,
        };
        rankLabel.AddThemeFontSizeOverride("font_size", 8);
        hbox.AddChild(rankLabel);

        hbox.AddChild(BuildPortrait(entry));

        if (entry.Buffs is { Count: > 0 })
            hbox.AddChild(BuildBuffStrip(entry.Buffs));

        string lineText = string.IsNullOrEmpty(entry.ActionLabel)
            ? entry.DisplayName
            : $"{entry.DisplayName} · {entry.ActionLabel}";

        var name = new Label
        {
            Text = lineText,
            ClipText = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(0, PortraitSize),
        };
        name.AddThemeFontSizeOverride("font_size", 8);
        if (entry.IsPlayer)
            name.AddThemeColorOverride("font_color", new Color(0.55f, 0.82f, 1f));
        else
            name.AddThemeColorOverride("font_color", new Color(0.82f, 0.84f, 0.88f));
        hbox.AddChild(name);

        string initText = entry.IsPending && entry.Initiative < 0
            ? "?"
            : entry.Initiative.ToString();
        var initLabel = new Label
        {
            Text = initText,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(18, PortraitSize),
        };
        initLabel.AddThemeFontSizeOverride("font_size", 9);
        if (entry.IsPending)
            initLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.5f));
        else
            initLabel.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.55f));
        hbox.AddChild(initLabel);

        return row;
    }

    static Control BuildPortrait(InitiativeDisplayEntry entry)
    {
        var frame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(PortraitSize, PortraitSize),
        };

        var portraitStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.1f, 0.14f, 0.9f),
            BorderColor = entry.IsPlayer
                ? new Color(0.35f, 0.55f, 0.85f, 0.8f)
                : new Color(0.4f, 0.35f, 0.35f, 0.7f),
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
        };
        frame.AddThemeStyleboxOverride("panel", portraitStyle);

        if (entry.Portrait != null)
        {
            var tex = new TextureRect
            {
                Texture = entry.Portrait,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(PortraitSize - 2, PortraitSize - 2),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            tex.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            frame.AddChild(tex);
        }
        else
        {
            var placeholder = new Label
            {
                Text = "?",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            placeholder.AddThemeFontSizeOverride("font_size", 10);
            frame.AddChild(placeholder);
        }

        return frame;
    }

    static Control BuildBuffStrip(IReadOnlyList<InitiativeBuffDisplay> buffs)
    {
        var strip = new HBoxContainer();
        strip.AddThemeConstantOverride("separation", 1);

        foreach (InitiativeBuffDisplay buff in buffs)
            strip.AddChild(BuildBuffBadge(buff));

        return strip;
    }

    static Control BuildBuffBadge(InitiativeBuffDisplay buff)
    {
        var badge = new PanelContainer
        {
            CustomMinimumSize = new Vector2(BuffIconSize, BuffIconSize),
            TooltipText = buff.Tooltip,
        };

        var badgeStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.12f, 0.08f, 0.92f),
            BorderColor = new Color(0.72f, 0.62f, 0.28f, 0.95f),
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0,
        };
        badge.AddThemeStyleboxOverride("panel", badgeStyle);

        var root = new Control
        {
            CustomMinimumSize = new Vector2(BuffIconSize, BuffIconSize),
        };
        badge.AddChild(root);

        if (buff.Icon != null)
        {
            var icon = new TextureRect
            {
                Texture = buff.Icon,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(BuffIconSize - 2, BuffIconSize - 2),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            icon.SetAnchorsPreset(LayoutPreset.Center);
            icon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            root.AddChild(icon);
        }

        var turns = new Label
        {
            Text = buff.TurnsLeft.ToString(),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        turns.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        turns.AddThemeFontSizeOverride("font_size", 7);
        turns.AddThemeColorOverride("font_color", Colors.White);
        turns.AddThemeConstantOverride("outline_size", 2);
        turns.AddThemeColorOverride("font_outline_color", Colors.Black);
        root.AddChild(turns);

        return badge;
    }
}
