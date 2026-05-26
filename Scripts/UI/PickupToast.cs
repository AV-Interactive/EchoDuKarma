using System.Collections.Generic;
using EchoduKarma.Scripts.Data;
using EchoduKarma.Scripts.Entities.Player;
using Godot;

public partial class PickupToast : Control
{
    const float FadeInDuration = 0.22f;
    const float HoldDuration = 2.6f;
    const float FadeOutDuration = 0.35f;

    [Export] PanelContainer _panel;
    [Export] TextureRect _icon;
    [Export] RichTextLabel _headerLabel;
    [Export] RichTextLabel _itemLabel;

    readonly Queue<string> _pendingItems = new();
    Tween _activeTween;
    bool _isShowing;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        Modulate = new Color(1f, 1f, 1f, 0f);

        if (InventoryManager.Instance is not null)
            InventoryManager.Instance.ItemAcquired += OnItemAcquired;
    }

    public override void _ExitTree()
    {
        if (InventoryManager.Instance is not null)
            InventoryManager.Instance.ItemAcquired -= OnItemAcquired;

        _activeTween?.Kill();
        base._ExitTree();
    }

    void OnItemAcquired(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return;

        _pendingItems.Enqueue(itemName.Trim());

        if (!_isShowing)
            ShowNext();
    }

    void ShowNext()
    {
        if (_pendingItems.Count == 0)
        {
            _isShowing = false;
            Visible = false;
            return;
        }

        _isShowing = true;
        string itemName = _pendingItems.Dequeue();
        Equipment equipment = EquipmentManager.GetEquipment(itemName);
        ResourceItem resource = ResourceItemManager.GetResource(itemName);

        _headerLabel.Text = "[color=#7AE582]Objet obtenu[/color]";

        if (equipment is not null)
        {
            UiIcons.Apply(_icon, UiIcons.GetEquipmentIcon(equipment));
            string stats = equipment.FormatStatSummary();
            string statsLine = stats == "Aucun bonus"
                ? "[color=#8899AA]Aucun bonus[/color]"
                : $"[color=#FFD166]{stats}[/color]";

            _itemLabel.Text = $"[b]{equipment.Name}[/b]\n{statsLine}";
        }
        else if (resource is not null)
        {
            UiIcons.Apply(_icon, UiIcons.GetItemIcon(resource.Name));
            _itemLabel.Text = $"[b]{resource.Name}[/b]\n[color=#8899AA]{resource.Type}[/color]";
        }
        else
        {
            UiIcons.Apply(_icon, UiIcons.Load(UiIcons.Pocket));
            _itemLabel.Text = $"[b]{itemName}[/b]";
        }

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
}
