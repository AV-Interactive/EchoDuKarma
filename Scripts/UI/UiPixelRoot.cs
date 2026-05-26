using Godot;

namespace EchoduKarma.Scripts.UI;

/// <summary>
/// Racine UI en résolution de design 640×360, mise à l'échelle en entiers pour un rendu pixel-crisp.
/// </summary>
public partial class UiPixelRoot : Control
{
    public const int DesignWidth = 640;
    public const int DesignHeight = 360;

    public override void _Ready()
    {
        GetViewport().SizeChanged += OnViewportResized;
        CallDeferred(MethodName.ApplyPixelScale);
    }

    public override void _ExitTree()
    {
        if (GetViewport() != null)
            GetViewport().SizeChanged -= OnViewportResized;
        base._ExitTree();
    }

    void OnViewportResized() => ApplyPixelScale();

    void ApplyPixelScale()
    {
        if (!TryGetWindowRect(out Rect2 rect))
            return;

        float scaleX = rect.Size.X / DesignWidth;
        float scaleY = rect.Size.Y / DesignHeight;
        int scale = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(scaleX, scaleY)));

        Size = new Vector2(DesignWidth, DesignHeight);
        Scale = new Vector2(scale, scale);
        Position = rect.Position + (rect.Size - Size * scale) / 2f;
    }

    bool TryGetWindowRect(out Rect2 rect)
    {
        rect = default;

        Viewport viewport = GetViewport();
        if (viewport == null)
            return false;

        rect = viewport.GetVisibleRect();
        if (IsRectValid(rect))
            return true;

        Vector2I windowSize = DisplayServer.WindowGetSize();
        if (windowSize.X <= 0 || windowSize.Y <= 0)
            return false;

        rect = new Rect2(Vector2.Zero, windowSize);
        return IsRectValid(rect);
    }

    static bool IsRectValid(Rect2 rect)
    {
        return rect.Size.X > 0f
            && rect.Size.Y > 0f
            && Mathf.IsFinite(rect.Size.X)
            && Mathf.IsFinite(rect.Size.Y)
            && Mathf.IsFinite(rect.Position.X)
            && Mathf.IsFinite(rect.Position.Y);
    }
}
