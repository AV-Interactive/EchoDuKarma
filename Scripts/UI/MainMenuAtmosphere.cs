using Godot;

namespace EchoduKarma.Scripts.UI;

/// <summary>
/// Vignette légère et halo autour du titre — complète le paysage sans le masquer.
/// </summary>
public partial class MainMenuAtmosphere : Control
{
    float _time;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 size = Size;
        if (size.X < 2f || size.Y < 2f)
            return;

        DrawSideVignette(size);
        DrawTopHaze(size);
        DrawBottomFade(size);
        DrawTitleGlow(size);
    }

    void DrawSideVignette(Vector2 size)
    {
        float width = size.X * 0.18f;
        DrawHorizontalVignette(new Rect2(0, 0, width, size.Y), new Color(0.02f, 0.05f, 0.12f, 0.38f), fadeRight: true);
        DrawHorizontalVignette(
            new Rect2(size.X - width, 0, width, size.Y),
            new Color(0.02f, 0.05f, 0.12f, 0.24f),
            fadeRight: false);
    }

    void DrawTopHaze(Vector2 size)
    {
        float height = size.Y * 0.38f;
        DrawVerticalGradient(
            new Rect2(0, 0, size.X, height),
            new Color(0.05f, 0.12f, 0.22f, 0.22f),
            new Color(0.05f, 0.12f, 0.22f, 0f));
    }

    void DrawBottomFade(Vector2 size)
    {
        float height = size.Y * 0.42f;
        DrawVerticalGradient(
            new Rect2(0, size.Y - height, size.X, height),
            new Color(0.02f, 0.04f, 0.08f, 0f),
            new Color(0.02f, 0.04f, 0.08f, 0.55f));
    }

    void DrawVerticalGradient(Rect2 rect, Color top, Color bottom)
    {
        Vector2 p0 = rect.Position;
        Vector2 p1 = new(rect.End.X, rect.Position.Y);
        Vector2 p2 = rect.End;
        Vector2 p3 = new(rect.Position.X, rect.End.Y);
        DrawPolygon(new[] { p0, p1, p2, p3 }, new[] { top, top, bottom, bottom });
    }

    void DrawHorizontalVignette(Rect2 rect, Color edge, bool fadeRight)
    {
        Color inner = edge with { A = 0f };
        Vector2 p0 = rect.Position;
        Vector2 p1 = new(rect.End.X, rect.Position.Y);
        Vector2 p2 = rect.End;
        Vector2 p3 = new(rect.Position.X, rect.End.Y);

        if (fadeRight)
            DrawPolygon(new[] { p0, p1, p2, p3 }, new[] { edge, inner, inner, edge });
        else
            DrawPolygon(new[] { p0, p1, p2, p3 }, new[] { inner, edge, edge, inner });
    }

    void DrawTitleGlow(Vector2 size)
    {
        Vector2 center = new(size.X * 0.5f, size.Y * 0.14f);
        float pulse = 0.85f + Mathf.Sin(_time * 0.6f) * 0.15f;
        DrawCircle(center, 180f * pulse, new Color(0.35f, 0.62f, 0.85f, 0.06f));
        DrawCircle(center, 110f * pulse, new Color(0.55f, 0.82f, 0.95f, 0.04f));
    }
}
