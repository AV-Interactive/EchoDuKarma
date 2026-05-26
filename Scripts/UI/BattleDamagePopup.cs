using Godot;

namespace EchoduKarma.Scripts.UI;

public static class BattleDamagePopup
{
    static readonly Color DamageFill = new(1f, 0.38f, 0.22f);
    static readonly Color HealFill = new(0.42f, 1f, 0.55f);
    static readonly Color Outline = new(0.03f, 0.03f, 0.05f, 1f);

    public static void Spawn(Control parent, Vector2 localPosition, int amount, Color kindHint, Node tweenOwner)
    {
        if (parent == null || tweenOwner == null || amount <= 0)
            return;

        bool isHeal = kindHint.G > kindHint.R;
        string text = isHeal ? $"+{amount}" : amount.ToString();

        var anchor = new Control
        {
            Position = new Vector2(Mathf.Round(localPosition.X), Mathf.Round(localPosition.Y)),
            ZIndex = 512,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        parent.AddChild(anchor);

        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            LabelSettings = new LabelSettings
            {
                FontSize = 34,
                FontColor = isHeal ? HealFill : DamageFill,
                OutlineSize = 9,
                OutlineColor = Outline,
                ShadowSize = 3,
                ShadowColor = new Color(0f, 0f, 0f, 0.7f),
                ShadowOffset = new Vector2(0, 2),
            },
        };
        anchor.AddChild(label);

        Vector2 labelSize = label.GetMinimumSize();
        label.Size = labelSize;
        label.Position = -labelSize / 2f;

        anchor.Scale = new Vector2(1.45f, 1.45f);

        var tween = tweenOwner.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(anchor, "scale", Vector2.One, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(anchor, "position:y", anchor.Position.Y - 44f, 0.9f)
            .SetDelay(0.05f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);

        tween.SetParallel(false);
        tween.TweenInterval(0.3f);
        tween.TweenProperty(anchor, "modulate:a", 0f, 0.4f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.Finished += () => anchor.QueueFree();
    }
}
