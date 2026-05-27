using System;
using System.Globalization;
using EchoduKarma.Scripts.Data;
using Godot;

namespace EchoduKarma.Scripts.UI;

public partial class MainMenu : Control
{
    static readonly Color TextPrimary = new(0.88f, 0.92f, 0.97f);
    static readonly Color TextMuted = new(0.58f, 0.68f, 0.78f);
    static readonly Color TextAccent = new(0.55f, 0.82f, 0.95f);
    [Export] RichTextLabel _titleLabel;
    [Export] Label _subtitleLabel;
    [Export] Label _taglineLabel;
    [Export] Button _newGameButton;
    [Export] Button _continueButton;
    [Export] Button _loadButton;
    [Export] Button _quitButton;
    [Export] ColorRect _fadeOverlay;
    [Export] Control _loadOverlayHost;

    Control _loadOverlay;
    readonly LoadSlotRow[] _loadRows = new LoadSlotRow[SaveManager.SlotCount];
    Label _loadStatusLabel;

    sealed class LoadSlotRow
    {
        public Label SummaryLabel;
        public Button LoadButton;
    }
    int _menuFocusIndex;
    bool _isTransitioning;
    bool _loadPanelOpen;

    static readonly StyleBoxFlat MenuButtonNormal = CreateMenuButtonStyle(false);
    static readonly StyleBoxFlat MenuButtonHover = CreateMenuButtonStyle(true);
    static readonly StyleBoxFlat SlotPanelStyle = new()
    {
        BgColor = new Color(0.06f, 0.08f, 0.13f, 0.9f),
        BorderColor = new Color(0.22f, 0.34f, 0.48f, 0.85f),
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        CornerRadiusTopLeft = 4,
        CornerRadiusTopRight = 4,
        CornerRadiusBottomLeft = 4,
        CornerRadiusBottomRight = 4,
        ContentMarginLeft = 8,
        ContentMarginRight = 8,
        ContentMarginTop = 6,
        ContentMarginBottom = 6,
    };

    static readonly StyleBoxFlat OverlayPanelStyle = new()
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
        ContentMarginLeft = 14,
        ContentMarginRight = 14,
        ContentMarginTop = 12,
        ContentMarginBottom = 12,
    };

    Button[] _menuButtons;

    public override void _Ready()
    {
        _menuButtons = new[] { _newGameButton, _continueButton, _loadButton, _quitButton };
        MusicManager.Instance?.PlayMenu();

        _titleLabel.Text =
            "[center][font_size=58][color=#9AD4F0]Echo[/color] [color=#F0DCA0]du[/color] [color=#6EC4E8]Karma[/color][/font_size][/center]";
        _subtitleLabel.Text = "Les échos du passé résonnent encore…";
        _taglineLabel.Text = "HD-2D · Tour par tour · Karma";

        StyleMenuButtons();
        BuildLoadOverlay();
        RefreshContinueButton();

        _newGameButton.Pressed += OnNewGamePressed;
        _continueButton.Pressed += OnContinuePressed;
        _loadButton.Pressed += OpenLoadPanel;
        _quitButton.Pressed += OnQuitPressed;

        if (_fadeOverlay != null)
        {
            _fadeOverlay.Color = new Color(0, 0, 0, 1);
            _fadeOverlay.MouseFilter = MouseFilterEnum.Ignore;
            var tween = CreateTween();
            tween.TweenProperty(_fadeOverlay, "color:a", 0f, 0.9f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }

        CallDeferred(MethodName.FocusFirstButton);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isTransitioning)
            return;

        if (@event.IsActionPressed("ui_cancel"))
        {
            if (_loadPanelOpen)
            {
                CloseLoadPanel();
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (!_loadPanelOpen && @event.IsActionPressed("ui_down"))
        {
            MoveMenuFocus(1);
            GetViewport().SetInputAsHandled();
        }
        else if (!_loadPanelOpen && @event.IsActionPressed("ui_up"))
        {
            MoveMenuFocus(-1);
            GetViewport().SetInputAsHandled();
        }
    }

    void StyleMenuButtons()
    {
        foreach (Button btn in _menuButtons)
        {
            if (btn == null)
                continue;

            btn.AddThemeStyleboxOverride("normal", MenuButtonNormal);
            btn.AddThemeStyleboxOverride("hover", MenuButtonHover);
            btn.AddThemeStyleboxOverride("focus", MenuButtonHover);
            btn.AddThemeStyleboxOverride("pressed", MenuButtonHover);
            btn.AddThemeColorOverride("font_color", TextPrimary);
            btn.AddThemeColorOverride("font_hover_color", TextAccent);
            btn.AddThemeColorOverride("font_focus_color", TextAccent);
            btn.AddThemeFontSizeOverride("font_size", 14);
            btn.CustomMinimumSize = new Vector2(360, 40);
            btn.Alignment = HorizontalAlignment.Left;
            btn.FocusMode = FocusModeEnum.All;
        }
    }

    static StyleBoxFlat CreateMenuButtonStyle(bool highlighted)
    {
        return new StyleBoxFlat
        {
            BgColor = highlighted
                ? new Color(0.12f, 0.2f, 0.32f, 0.95f)
                : new Color(0.08f, 0.11f, 0.18f, 0.82f),
            BorderColor = highlighted
                ? new Color(0.48f, 0.78f, 0.95f, 1f)
                : new Color(0.28f, 0.48f, 0.62f, 0.75f),
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 2,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 16,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
    }

    void BuildLoadOverlay()
    {
        _loadOverlay = new Control
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = 30,
        };
        _loadOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
        _loadOverlayHost.AddChild(_loadOverlay);

        var dimmer = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.6f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        dimmer.SetAnchorsPreset(LayoutPreset.FullRect);
        _loadOverlay.AddChild(dimmer);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        _loadOverlay.AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(420, 0) };
        panel.AddThemeStyleboxOverride("panel", OverlayPanelStyle);
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        var title = new Label
        {
            Text = "Charger une partie",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 16);
        title.AddThemeColorOverride("font_color", TextPrimary);
        vbox.AddChild(title);

        _loadStatusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _loadStatusLabel.AddThemeFontSizeOverride("font_size", 10);
        _loadStatusLabel.AddThemeColorOverride("font_color", TextMuted);
        vbox.AddChild(_loadStatusLabel);

        for (int i = 0; i < SaveManager.SlotCount; i++)
        {
            int slot = i + 1;
            var row = new PanelContainer();
            row.AddThemeStyleboxOverride("panel", SlotPanelStyle);
            vbox.AddChild(row);

            var rowHBox = new HBoxContainer();
            rowHBox.AddThemeConstantOverride("separation", 8);
            row.AddChild(rowHBox);

            var infoVBox = new VBoxContainer();
            infoVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            rowHBox.AddChild(infoVBox);

            var slotLabel = new Label { Text = $"Emplacement {slot}" };
            slotLabel.AddThemeFontSizeOverride("font_size", 11);
            slotLabel.AddThemeColorOverride("font_color", TextPrimary);
            infoVBox.AddChild(slotLabel);

            var summary = new Label();
            summary.AddThemeFontSizeOverride("font_size", 9);
            summary.AddThemeColorOverride("font_color", TextMuted);
            infoVBox.AddChild(summary);

            var loadBtn = new Button { Text = "Charger", CustomMinimumSize = new Vector2(72, 28) };
            loadBtn.AddThemeStyleboxOverride("normal", MenuButtonNormal);
            loadBtn.AddThemeStyleboxOverride("hover", MenuButtonHover);
            loadBtn.AddThemeStyleboxOverride("focus", MenuButtonHover);
            loadBtn.AddThemeFontSizeOverride("font_size", 10);
            loadBtn.AddThemeColorOverride("font_color", TextPrimary);
            int capturedSlot = slot;
            loadBtn.Pressed += () => TryLoadSlot(capturedSlot);
            rowHBox.AddChild(loadBtn);

            _loadRows[i] = new LoadSlotRow { SummaryLabel = summary, LoadButton = loadBtn };
        }

        var closeBtn = new Button { Text = "Retour", CustomMinimumSize = new Vector2(100, 30) };
        closeBtn.AddThemeStyleboxOverride("normal", MenuButtonNormal);
        closeBtn.AddThemeStyleboxOverride("hover", MenuButtonHover);
        closeBtn.AddThemeFontSizeOverride("font_size", 11);
        closeBtn.Pressed += CloseLoadPanel;
        vbox.AddChild(closeBtn);
    }

    void RefreshContinueButton()
    {
        int slot = SaveManager.Instance?.GetMostRecentSaveSlot() ?? -1;
        bool hasSave = slot > 0;
        _continueButton.Disabled = !hasSave;
        _continueButton.Modulate = hasSave ? Colors.White : new Color(1, 1, 1, 0.45f);

        if (!hasSave)
            _continueButton.Text = "  Continuer";
        else
        {
            SaveSlotInfo info = SaveManager.Instance.GetSlotInfo(slot);
            _continueButton.Text = $"  Continuer — niv. {info.Level} ({info.ZoneName})";
        }
    }

    void RefreshLoadPanel()
    {
        _loadStatusLabel.Text = "";

        for (int i = 0; i < SaveManager.SlotCount; i++)
        {
            int slot = i + 1;
            LoadSlotRow row = _loadRows[i];
            if (row == null)
                continue;

            SaveSlotInfo info = SaveManager.Instance?.GetSlotInfo(slot) ?? new SaveSlotInfo { Slot = slot };

            if (info.Exists)
            {
                string when = FormatSaveDate(info.SavedAtUtc);
                row.SummaryLabel.Text = $"Niveau {info.Level} · {info.ZoneName}\n{when}";
                row.SummaryLabel.AddThemeColorOverride("font_color", TextMuted);
                row.LoadButton.Disabled = false;
            }
            else
            {
                row.SummaryLabel.Text = "Vide";
                row.SummaryLabel.AddThemeColorOverride("font_color", new Color(0.42f, 0.48f, 0.56f));
                row.LoadButton.Disabled = true;
            }
        }
    }

    static string FormatSaveDate(string savedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(savedAtUtc))
            return "";

        if (!DateTime.TryParse(savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime utc))
            return savedAtUtc;

        DateTime local = utc.ToLocalTime();
        return local.ToString("g", CultureInfo.CurrentCulture);
    }

    void OnNewGamePressed() => RunTransition(SaveManager.Instance.StartNewGame);

    void OnContinuePressed()
    {
        int slot = SaveManager.Instance?.GetMostRecentSaveSlot() ?? -1;
        if (slot < 1)
            return;

        RunTransition(() =>
        {
            if (!SaveManager.Instance.LoadFromSlot(slot, out string error))
                GD.PrintErr($"[MainMenu] Continuer : {error}");
        });
    }

    void TryLoadSlot(int slot)
    {
        RunTransition(() =>
        {
            if (SaveManager.Instance.LoadFromSlot(slot, out string error))
                return;

            _loadStatusLabel.Text = error;
            FadeBackFromFailedTransition();
        });
    }

    void FadeBackFromFailedTransition()
    {
        _isTransitioning = false;
        _fadeOverlay.MouseFilter = MouseFilterEnum.Ignore;
        CreateTween()
            .TweenProperty(_fadeOverlay, "color:a", 0f, 0.45f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    void OpenLoadPanel()
    {
        RefreshLoadPanel();
        _loadOverlay.Visible = true;
        _loadPanelOpen = true;
        _loadRows[0]?.LoadButton?.GrabFocus();
    }

    void CloseLoadPanel()
    {
        _loadOverlay.Visible = false;
        _loadPanelOpen = false;
        _loadButton.GrabFocus();
    }

    void OnQuitPressed() => GetTree().Quit();

    void RunTransition(Action action)
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;
        _fadeOverlay.MouseFilter = MouseFilterEnum.Stop;

        MusicManager.Instance?.FadeOutAndStop(0.55f);

        var tween = CreateTween();
        tween.TweenProperty(_fadeOverlay, "color:a", 1f, 0.55f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenCallback(Callable.From(action));
    }

    void MoveMenuFocus(int delta)
    {
        if (_menuButtons == null || _menuButtons.Length == 0)
            return;

        for (int attempt = 0; attempt < _menuButtons.Length; attempt++)
        {
            _menuFocusIndex = (_menuFocusIndex + delta + _menuButtons.Length) % _menuButtons.Length;
            Button btn = _menuButtons[_menuFocusIndex];
            if (btn != null && !btn.Disabled)
            {
                btn.GrabFocus();
                break;
            }
        }
    }

    void FocusFirstButton()
    {
        _menuFocusIndex = 0;
        _newGameButton?.GrabFocus();
    }
}
