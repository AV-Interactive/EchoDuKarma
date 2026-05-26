using EchoduKarma.Scripts.Data;
using Godot;

public partial class KarmaBanner : PanelContainer
{
    [Export] Label _titleLabel;
    [Export] Label _minLabel;
    [Export] Label _maxLabel;
    [Export] Control _barTrack;
    [Export] ColorRect _negativeFill;
    [Export] ColorRect _positiveFill;
    [Export] ColorRect _centerMarker;
    [Export] ColorRect _valueMarker;
    [Export] Label _deltaLabel;
    [Export] Label _stateLabel;

    /// <summary>Si vide, suit KarmaManager.CurrentZone / GameManager.ReturnZoneName.</summary>
    [Export] string _zoneOverride = "";

    Tween _deltaTween;
    string _displayZone = "Introduction";
    float _lastKarma;

    const float FillHeight = 4f;

    public override void _Ready()
    {
        _displayZone = ResolveDisplayZone();

        if (KarmaManager.Instance is not null)
        {
            KarmaManager.Instance.KarmaChanged += OnKarmaChanged;
            KarmaManager.Instance.CurrentZoneChanged += OnCurrentZoneChanged;
            RefreshFromManager(0);
        }

        if (_barTrack != null)
            _barTrack.Resized += OnBarTrackResized;
    }

    void OnBarTrackResized() => CallDeferred(nameof(UpdateBarLayout));

    public override void _ExitTree()
    {
        if (KarmaManager.Instance is not null)
        {
            KarmaManager.Instance.KarmaChanged -= OnKarmaChanged;
            KarmaManager.Instance.CurrentZoneChanged -= OnCurrentZoneChanged;
        }

        base._ExitTree();
    }

    void OnKarmaChanged(string zone, float newValue, float delta)
    {
        if (zone != _displayZone)
            return;

        Refresh(newValue, delta);
    }

    void OnCurrentZoneChanged(string zone)
    {
        _displayZone = ResolveDisplayZone();
        RefreshFromManager(0);
    }

    string ResolveDisplayZone()
    {
        if (!string.IsNullOrWhiteSpace(_zoneOverride))
            return _zoneOverride.Trim();

        if (KarmaManager.Instance is not null && !string.IsNullOrWhiteSpace(KarmaManager.Instance.CurrentZone))
            return KarmaManager.Instance.CurrentZone;

        return GameManager.Instance?.ReturnZoneName ?? "Introduction";
    }

    void RefreshFromManager(float delta)
    {
        _displayZone = ResolveDisplayZone();
        float karma = KarmaManager.Instance?.GetZoneKarma(_displayZone) ?? 0f;
        Refresh(karma, delta);
    }

    public void Refresh(float karma, float delta)
    {
        _lastKarma = karma;

        if (_titleLabel != null)
            _titleLabel.Text = $"⚖ {_displayZone}";

        if (_minLabel != null)
            _minLabel.Text = "-100";

        if (_maxLabel != null)
            _maxLabel.Text = "+100";

        if (_stateLabel != null)
            _stateLabel.Text = $"{KarmaManager.FormatKarma(karma)} · {GetShortStateLabel(karma)}";

        CallDeferred(nameof(UpdateBarLayout));
        ShowDelta(delta);
    }

    static string GetShortStateLabel(float karma)
    {
        if (karma >= 70) return "Utopie";
        if (karma >= 30) return "Stable";
        if (karma >= -20) return "Équilibre";
        if (karma >= -69) return "Instable";
        return "Chaos";
    }

    void ShowDelta(float delta)
    {
        if (_deltaLabel == null)
            return;

        if (Mathf.IsZeroApprox(delta))
        {
            _deltaLabel.Visible = false;
            return;
        }

        _deltaLabel.Text = KarmaManager.FormatDelta(delta);
        _deltaLabel.Modulate = delta > 0
            ? new Color(0.48f, 0.9f, 0.51f)
            : new Color(0.96f, 0.35f, 0.27f);
        _deltaLabel.Visible = true;

        if (_deltaTween != null && _deltaTween.IsValid())
            _deltaTween.Kill();

        _deltaTween = CreateTween();
        _deltaTween.TweenInterval(2.0);
        _deltaTween.TweenProperty(_deltaLabel, "modulate:a", 0f, 0.6);
        _deltaTween.TweenCallback(Callable.From(() => _deltaLabel.Visible = false));
    }

    void UpdateBarLayout() => UpdateBarLayout(_lastKarma);

    void UpdateBarLayout(float karma)
    {
        if (_barTrack == null)
            return;

        float trackWidth = _barTrack.Size.X;
        if (trackWidth <= 0f || !Mathf.IsFinite(trackWidth))
            return;

        float normalized = KarmaManager.KarmaToNormalized(karma);
        float centerX = trackWidth * 0.5f;
        float markerX = trackWidth * normalized;

        if (!Mathf.IsFinite(normalized) || !Mathf.IsFinite(centerX) || !Mathf.IsFinite(markerX))
            return;

        if (_centerMarker != null)
        {
            float markerWidth = Mathf.Max(_centerMarker.Size.X, 1f);
            _centerMarker.Position = new Vector2(centerX - markerWidth * 0.5f, _centerMarker.Position.Y);
        }

        if (_valueMarker != null)
        {
            float markerWidth = Mathf.Max(_valueMarker.Size.X, 2f);
            _valueMarker.Position = new Vector2(markerX - markerWidth * 0.5f, _valueMarker.Position.Y);
            _valueMarker.Color = karma >= 0
                ? new Color(0.48f, 0.88f, 0.51f)
                : new Color(0.96f, 0.35f, 0.27f);
        }

        if (_negativeFill != null)
        {
            float negWidth = Mathf.Max(centerX - markerX, 0f);
            if (negWidth > 0f)
            {
                _negativeFill.Position = new Vector2(markerX, 1f);
                _negativeFill.Size = new Vector2(negWidth, FillHeight);
            }
            _negativeFill.Visible = karma < 0 && negWidth > 0f;
        }

        if (_positiveFill != null)
        {
            float posWidth = Mathf.Max(markerX - centerX, 0f);
            if (posWidth > 0f)
            {
                _positiveFill.Position = new Vector2(centerX, 1f);
                _positiveFill.Size = new Vector2(posWidth, FillHeight);
            }
            _positiveFill.Visible = karma > 0 && posWidth > 0f;
        }
    }
}
