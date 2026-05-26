using Godot;

public partial class QuestStatRow : Button
{
    [Export] ColorRect _accentBar;
    [Export] Label _nameLabel;
    [Export] Label _typeBadge;
    [Export] Label _zoneLabel;
    [Export] Label _statusLabel;
    [Export] Label _stepLabel;
    [Export] ProgressBar _stepBar;

    static readonly Color ColorActive = new(1f, 0.82f, 0.4f);
    static readonly Color ColorCompleted = new(0.48f, 0.9f, 0.51f);
    static readonly Color ColorInactive = new(0.53f, 0.6f, 0.67f);

    public string QuestId { get; private set; }

    public void Bind(QuestData quest, QuestRuntime runtime)
    {
        QuestId = quest.Id;
        Text = string.Empty;

        _nameLabel.Text = quest.Name;
        _typeBadge.Text = quest.Type;
        _zoneLabel.Text = quest.Zone;

        Color accent = runtime.Status switch
        {
            QuestStatus.Active    => ColorActive,
            QuestStatus.Completed => ColorCompleted,
            _                     => ColorInactive,
        };

        _accentBar.Color = accent;

        switch (runtime.Status)
        {
            case QuestStatus.Active:
                _statusLabel.Text = "En cours";
                _statusLabel.AddThemeColorOverride("font_color", ColorActive);
                break;
            case QuestStatus.Completed:
                _statusLabel.Text = "Terminée";
                _statusLabel.AddThemeColorOverride("font_color", ColorCompleted);
                break;
            default:
                _statusLabel.Text = "—";
                _statusLabel.AddThemeColorOverride("font_color", ColorInactive);
                break;
        }

        bool isPrincipal = quest.Type.Equals("PRINCIPAL", System.StringComparison.OrdinalIgnoreCase);
        _typeBadge.AddThemeColorOverride("font_color", isPrincipal
            ? new Color(1f, 0.85f, 0.55f)
            : new Color(0.55f, 0.75f, 0.92f));

        int stepCount = quest.Steps?.Length ?? 0;
        int currentStep = stepCount > 0
            ? Mathf.Clamp(runtime.CurrentStep + 1, 1, stepCount)
            : 0;

        if (runtime.Status == QuestStatus.Completed)
        {
            _stepLabel.Text = "Accomplie";
            _stepBar.MaxValue = 1;
            _stepBar.Value = 1;
            SetProgressFill(ColorCompleted);
        }
        else if (stepCount > 0)
        {
            _stepLabel.Text = $"{currentStep}/{stepCount}";
            _stepBar.MaxValue = stepCount;
            _stepBar.Value = currentStep;
            SetProgressFill(ColorActive);
        }
        else
        {
            _stepLabel.Text = "—";
            _stepBar.MaxValue = 1;
            _stepBar.Value = 0;
            SetProgressFill(ColorInactive);
        }
    }

    void SetProgressFill(Color color)
    {
        if (_stepBar == null)
            return;

        var fill = new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusBottomLeft = 2,
        };
        _stepBar.AddThemeStyleboxOverride("fill", fill);
    }
}
