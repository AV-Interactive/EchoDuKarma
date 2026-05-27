using System;
using System.Globalization;
using EchoduKarma.Scripts.Data;
using Godot;

namespace EchoduKarma.Scripts.UI;

public partial class SavePage : Control, IGameMenuTabPage
{
    const int InfoFontSize = 8;
    const int MetaFontSize = 7;
    const int ButtonFontSize = 8;

    [Export] VBoxContainer _slotsContainer;
    [Export] Label _statusLabel;

    readonly SlotRow[] _rows = new SlotRow[SaveManager.SlotCount];
    bool _signalsConnected;
    Control _confirmOverlay;
    Label _confirmTitle;
    Label _confirmMessage;
    Button _confirmButton;
    Button _cancelButton;
    int _pendingSaveSlot = -1;
    Control _focusAfterConfirm;

    static readonly Color TextPrimary = new(0.88f, 0.92f, 0.97f);
    static readonly Color TextMuted = new(0.52f, 0.58f, 0.68f);
    static readonly Color TextEmpty = new(0.42f, 0.48f, 0.56f);

    static readonly StyleBoxFlat SlotPanelStyle = new()
    {
        BgColor = new Color(0.06f, 0.08f, 0.13f, 0.88f),
        BorderColor = new Color(0.22f, 0.34f, 0.48f, 0.85f),
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusTopLeft = 4,
        CornerRadiusTopRight = 4,
        CornerRadiusBottomLeft = 4,
        CornerRadiusBottomRight = 4,
        ContentMarginLeft = 6,
        ContentMarginRight = 6,
        ContentMarginTop = 4,
        ContentMarginBottom = 4,
    };

    static readonly StyleBoxFlat ConfirmPanelStyle = new()
    {
        BgColor = new Color(0.08f, 0.11f, 0.18f, 0.96f),
        BorderColor = new Color(0.35f, 0.62f, 0.78f, 1f),
        BorderWidthTop = 2,
        BorderWidthBottom = 2,
        BorderWidthLeft = 2,
        BorderWidthRight = 2,
        CornerRadiusTopLeft = 6,
        CornerRadiusTopRight = 6,
        CornerRadiusBottomLeft = 6,
        CornerRadiusBottomRight = 6,
        ContentMarginLeft = 12,
        ContentMarginRight = 12,
        ContentMarginTop = 10,
        ContentMarginBottom = 10,
    };

    static readonly StyleBoxFlat ConfirmAcceptStyle = new()
    {
        BgColor = new Color(0.1f, 0.24f, 0.14f, 0.95f),
        BorderColor = new Color(0.48f, 0.9f, 0.51f, 1f),
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusTopLeft = 4,
        CornerRadiusTopRight = 4,
        CornerRadiusBottomLeft = 4,
        CornerRadiusBottomRight = 4,
        ContentMarginLeft = 10,
        ContentMarginRight = 10,
        ContentMarginTop = 4,
        ContentMarginBottom = 4,
    };

    static readonly StyleBoxFlat ConfirmCancelStyle = new()
    {
        BgColor = new Color(0.22f, 0.08f, 0.08f, 0.9f),
        BorderColor = new Color(0.95f, 0.45f, 0.42f, 1f),
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusTopLeft = 4,
        CornerRadiusTopRight = 4,
        CornerRadiusBottomLeft = 4,
        CornerRadiusBottomRight = 4,
        ContentMarginLeft = 10,
        ContentMarginRight = 10,
        ContentMarginTop = 4,
        ContentMarginBottom = 4,
    };

    static readonly Color ConfirmAcceptText = new(0.75f, 0.98f, 0.78f);
    static readonly Color ConfirmCancelText = new(1f, 0.72f, 0.68f);
    static readonly StyleBoxFlat ActionButtonStyle = new()
    {
        BgColor = new Color(0.1f, 0.16f, 0.26f, 0.95f),
        BorderColor = new Color(0.32f, 0.52f, 0.68f, 0.9f),
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
        ContentMarginTop = 1,
        ContentMarginBottom = 1,
    };

    sealed class SlotRow
    {
        public Label SlotLabel;
        public Label SummaryLabel;
        public Label DateLabel;
        public Button SaveButton;
        public Button LoadButton;
    }

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        BuildSlotRows();
        SetupConfirmOverlay();
        CallDeferred(nameof(ConnectSaveSignals));
    }

    void SetupConfirmOverlay()
    {
        _confirmOverlay = new Control
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = 20,
        };
        _confirmOverlay.SetAnchorsPreset(LayoutPreset.FullRect);

        var dimmer = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.55f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        dimmer.SetAnchorsPreset(LayoutPreset.FullRect);
        _confirmOverlay.AddChild(dimmer);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        _confirmOverlay.AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(320, 0),
        };
        panel.AddThemeStyleboxOverride("panel", ConfirmPanelStyle);
        center.AddChild(panel);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 8);
        panel.AddChild(content);

        _confirmTitle = CreateLabel("Confirmer la sauvegarde", 11, TextPrimary);
        _confirmTitle.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_confirmTitle);

        _confirmMessage = CreateLabel("", InfoFontSize, TextPrimary);
        _confirmMessage.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _confirmMessage.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_confirmMessage);

        var buttons = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        buttons.AddThemeConstantOverride("separation", 10);
        content.AddChild(buttons);

        _cancelButton = CreateActionButton("Annuler", ConfirmCancelStyle, ButtonFontSize, ConfirmCancelText);
        _cancelButton.CustomMinimumSize = new Vector2(72, 22);
        _cancelButton.Pressed += OnSaveCanceled;
        buttons.AddChild(_cancelButton);

        _confirmButton = CreateActionButton("Sauver", ConfirmAcceptStyle, ButtonFontSize, ConfirmAcceptText);
        _confirmButton.CustomMinimumSize = new Vector2(72, 22);
        _confirmButton.Pressed += OnSaveConfirmed;
        buttons.AddChild(_confirmButton);

        AddChild(_confirmOverlay);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_confirmOverlay == null || !_confirmOverlay.Visible)
            return;

        if (@event.IsActionPressed("ui_cancel"))
        {
            CloseSaveConfirmDialog();
            GetViewport().SetInputAsHandled();
        }
    }

    void ConnectSaveSignals()
    {
        if (_signalsConnected || SaveManager.Instance == null)
            return;

        SaveManager.Instance.SaveCompleted += OnSaveCompleted;
        SaveManager.Instance.LoadCompleted += OnLoadCompleted;
        SaveManager.Instance.SaveFailed += OnSaveFailed;
        SaveManager.Instance.LoadFailed += OnLoadFailed;
        _signalsConnected = true;
    }

    public override void _ExitTree()
    {
        if (_signalsConnected && SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveCompleted -= OnSaveCompleted;
            SaveManager.Instance.LoadCompleted -= OnLoadCompleted;
            SaveManager.Instance.SaveFailed -= OnSaveFailed;
            SaveManager.Instance.LoadFailed -= OnLoadFailed;
        }

        base._ExitTree();
    }

    void BuildSlotRows()
    {
        if (_slotsContainer == null)
            return;

        foreach (Node child in _slotsContainer.GetChildren())
            child.QueueFree();

        for (int slot = 1; slot <= SaveManager.SlotCount; slot++)
        {
            int capturedSlot = slot;
            var row = new SlotRow();

            var panel = new PanelContainer();
            panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            panel.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            panel.AddThemeStyleboxOverride("panel", SlotPanelStyle);

            var rootHBox = new HBoxContainer();
            rootHBox.AddThemeConstantOverride("separation", 6);
            panel.AddChild(rootHBox);

            var infoVBox = new VBoxContainer();
            infoVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            infoVBox.AddThemeConstantOverride("separation", 0);
            rootHBox.AddChild(infoVBox);

            row.SlotLabel = CreateLabel($"Emplacement {slot}", MetaFontSize, TextMuted);
            infoVBox.AddChild(row.SlotLabel);

            row.SummaryLabel = CreateLabel("", InfoFontSize, TextPrimary);
            infoVBox.AddChild(row.SummaryLabel);

            row.DateLabel = CreateLabel("", MetaFontSize, TextMuted);
            infoVBox.AddChild(row.DateLabel);

            var actions = new HBoxContainer();
            actions.Alignment = BoxContainer.AlignmentMode.Center;
            actions.AddThemeConstantOverride("separation", 4);
            rootHBox.AddChild(actions);

            row.SaveButton = CreateActionButton("Sauver", ActionButtonStyle, ButtonFontSize);
            row.SaveButton.Pressed += () => OnSavePressed(capturedSlot);
            actions.AddChild(row.SaveButton);

            row.LoadButton = CreateActionButton("Charger", ActionButtonStyle, ButtonFontSize);
            row.LoadButton.Pressed += () => OnLoadPressed(capturedSlot);
            actions.AddChild(row.LoadButton);

            _rows[slot - 1] = row;
            _slotsContainer.AddChild(panel);
        }
    }

    static Label CreateLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.Off,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    static Button CreateActionButton(string text, StyleBoxFlat style, int fontSize, Color? textColor = null)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(46, 18),
            FocusMode = FocusModeEnum.All,
        };
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", BrightenStyle(style, 1.15f));
        button.AddThemeStyleboxOverride("pressed", BrightenStyle(style, 0.85f));
        button.AddThemeStyleboxOverride("disabled", DimStyle(style));
        button.AddThemeStyleboxOverride("focus", BrightenStyle(style, 1.25f));
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeColorOverride("font_color", textColor ?? TextPrimary);
        button.AddThemeColorOverride("font_disabled_color", TextMuted);
        return button;
    }

    static StyleBoxFlat BrightenStyle(StyleBoxFlat source, float factor)
    {
        var copy = (StyleBoxFlat)source.Duplicate();
        copy.BgColor = new Color(
            Mathf.Clamp(source.BgColor.R * factor, 0f, 1f),
            Mathf.Clamp(source.BgColor.G * factor, 0f, 1f),
            Mathf.Clamp(source.BgColor.B * factor, 0f, 1f),
            source.BgColor.A);
        return copy;
    }

    static StyleBoxFlat DimStyle(StyleBoxFlat source)
    {
        var copy = (StyleBoxFlat)source.Duplicate();
        copy.BgColor = new Color(source.BgColor.R, source.BgColor.G, source.BgColor.B, source.BgColor.A * 0.45f);
        copy.BorderColor = new Color(source.BorderColor.R, source.BorderColor.G, source.BorderColor.B, source.BorderColor.A * 0.4f);
        return copy;
    }

    public void OnTabShown()
    {
        Visible = true;
        ConnectSaveSignals();
        SetStatus("");
        RefreshSlots();
        _rows[0]?.SaveButton?.GrabFocus();
    }

    public void OnTabHidden()
    {
        Visible = false;
        SetStatus("");
        CloseSaveConfirmDialog();
    }

    public void FocusDefault() => _rows[0]?.SaveButton?.GrabFocus();

    public bool TryHandleCancel()
    {
        if (_confirmOverlay != null && _confirmOverlay.Visible)
        {
            CloseSaveConfirmDialog();
            return true;
        }

        return false;
    }

    void RefreshSlots()
    {
        if (SaveManager.Instance == null)
            return;

        for (int slot = 1; slot <= SaveManager.SlotCount; slot++)
            RefreshSlot(slot, SaveManager.Instance.GetSlotInfo(slot));
    }

    void RefreshSlot(int slot, SaveSlotInfo info)
    {
        var row = _rows[slot - 1];
        if (row == null)
            return;

        if (!info.Exists)
        {
            row.SummaryLabel.Text = "— vide —";
            row.SummaryLabel.AddThemeColorOverride("font_color", TextEmpty);
            row.DateLabel.Text = "";
            row.DateLabel.Visible = false;
            row.LoadButton.Disabled = true;
            return;
        }

        string zone = string.IsNullOrWhiteSpace(info.ZoneName) ? "?" : info.ZoneName;
        row.SummaryLabel.Text = $"Niveau {info.Level}  ·  {zone}";
        row.SummaryLabel.AddThemeColorOverride("font_color", TextPrimary);
        row.DateLabel.Text = FormatSavedAt(info.SavedAtUtc);
        row.DateLabel.Visible = true;
        row.LoadButton.Disabled = false;
    }

    static string FormatSavedAt(string savedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(savedAtUtc))
            return "inconnue";

        if (DateTime.TryParse(savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime utc))
            return utc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

        return savedAtUtc;
    }

    void OnSavePressed(int slot)
    {
        if (SaveManager.Instance == null)
        {
            SetStatus("Système de sauvegarde indisponible.", success: false);
            return;
        }

        SaveSlotInfo info = SaveManager.Instance.GetSlotInfo(slot);
        _pendingSaveSlot = slot;
        _focusAfterConfirm = _rows[slot - 1]?.SaveButton;

        _confirmMessage.Text = info.Exists
            ? $"L'emplacement {slot} contient déjà une sauvegarde (niveau {info.Level} · {FormatZone(info.ZoneName)}).\n\nRemplacer cette sauvegarde ?"
            : $"Enregistrer la partie sur l'emplacement {slot} ?";

        _confirmOverlay.Visible = true;
        _confirmOverlay.MoveToFront();
        _confirmButton.GrabFocus();
    }

    void OnSaveConfirmed()
    {
        if (_pendingSaveSlot < 0)
            return;

        _confirmOverlay.Visible = false;

        int slot = _pendingSaveSlot;
        _pendingSaveSlot = -1;
        ExecuteSave(slot);
        RestoreFocusAfterConfirm();
    }

    void OnSaveCanceled()
    {
        _pendingSaveSlot = -1;
        if (_confirmOverlay != null)
            _confirmOverlay.Visible = false;
        RestoreFocusAfterConfirm();
    }

    void CloseSaveConfirmDialog()
    {
        if (_confirmOverlay == null || !_confirmOverlay.Visible)
            return;

        OnSaveCanceled();
    }

    void RestoreFocusAfterConfirm()
    {
        if (_focusAfterConfirm != null && GodotObject.IsInstanceValid(_focusAfterConfirm))
            _focusAfterConfirm.GrabFocus();
    }

    void ExecuteSave(int slot)
    {
        if (SaveManager.Instance == null)
        {
            SetStatus("Système de sauvegarde indisponible.", success: false);
            return;
        }

        if (SaveManager.Instance.SaveToSlot(slot, out string error, out SaveSlotInfo info))
        {
            RefreshSlot(slot, info);
            SetStatus($"Sauvegarde {slot} enregistrée.", success: true);
        }
        else
        {
            SetStatus(error, success: false);
        }
    }

    static string FormatZone(string zoneName) =>
        string.IsNullOrWhiteSpace(zoneName) ? "?" : zoneName;

    void OnLoadPressed(int slot)
    {
        if (SaveManager.Instance == null)
        {
            SetStatus("Système de sauvegarde indisponible.", success: false);
            return;
        }

        if (SaveManager.Instance.LoadFromSlot(slot, out string error))
            SetStatus($"Chargement emplacement {slot}…", success: true);
        else
            SetStatus(error, success: false);
    }

    void OnSaveCompleted(int slot)
    {
        if (SaveManager.Instance == null)
            return;

        RefreshSlot(slot, SaveManager.Instance.GetSlotInfo(slot));
        SetStatus($"Sauvegarde {slot} enregistrée.", success: true);
    }

    void OnLoadCompleted(int slot) => SetStatus($"Partie chargée (emplacement {slot}).", success: true);

    void OnSaveFailed(int slot, string reason) => SetStatus(reason, success: false);

    void OnLoadFailed(int slot, string reason) => SetStatus(reason, success: false);

    void SetStatus(string message, bool success = true)
    {
        if (_statusLabel == null)
            return;

        _statusLabel.Text = message;
        _statusLabel.Modulate = success
            ? new Color(0.55f, 0.88f, 1f)
            : new Color(1f, 0.55f, 0.55f);
    }
}
