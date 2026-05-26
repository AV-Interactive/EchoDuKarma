using Godot;

namespace EchoduKarma.Scripts.World;

/// <summary>
/// Redimensionne le SubViewportContainer sur toute la fenêtre.
/// Nécessaire car le parent de la map est un Node3D (les ancres Control ne s'appliquent pas).
/// </summary>
public partial class MapViewportLayout : SubViewportContainer
{
    public override void _Ready()
    {
        GetViewport().SizeChanged += OnWindowResized;
        CallDeferred(MethodName.ApplyLayout);
    }

    public override void _ExitTree()
    {
        if (GetViewport() != null)
            GetViewport().SizeChanged -= OnWindowResized;
        base._ExitTree();
    }

    void OnWindowResized() => ApplyLayout();

    void ApplyLayout()
    {
        if (!TryGetWindowRect(out Rect2 rect))
            return;

        Position = rect.Position;
        Size = rect.Size;
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
