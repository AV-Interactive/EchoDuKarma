using EchoduKarma.Scripts.Data;
using Godot;
using EchoduKarma.Scripts.Entities.Common;
using EchoduKarma.Scripts.Entities.Player;

namespace EchoduKarma.Scripts.UI;

public partial class PlayerStatsPage : Control, IGameMenuTabPage
{
    [Export] RichTextLabel _nameLabel;
    [Export] RichTextLabel _classLabel;
    [Export] RichTextLabel _levelLabel;
    [Export] RichTextLabel _hpLabel;
    [Export] RichTextLabel _mpLabel;
    [Export] RichTextLabel _xpLabel;
    [Export] ProgressBar _hpBar;
    [Export] ProgressBar _mpBar;
    [Export] ProgressBar _xpBar;
    [Export] RichTextLabel _forceLabel;
    [Export] RichTextLabel _espritLabel;
    [Export] RichTextLabel _agiLabel;
    [Export] RichTextLabel _defLabel;
    [Export] TextureRect _avatar;
    [Export] string _playerClassName = "Magus";

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;

        if (GameManager.Instance != null)
            GameManager.Instance.PlayerLevelUp += OnPlayerLevelUp;

        if (InventoryManager.Instance is not null)
            InventoryManager.Instance.EquipmentChanged += OnEquipmentChanged;

        SetupAvatar();
    }

    void SetupAvatar()
    {
        if (_avatar == null)
            return;

        _avatar.Texture = CreateIdleAtlasFrame(LpcSpriteLayout.CameraFacingDirection);
    }

    static AtlasTexture CreateIdleAtlasFrame(string direction)
    {
        int frame = LpcSpriteLayout.RowFrame(LpcSpriteLayout.DirectionRow(direction), 0);
        int col = frame % LpcSpriteLayout.HFrames;
        int row = frame / LpcSpriteLayout.HFrames;
        float size = LpcSpriteLayout.FrameSize;

        return new AtlasTexture
        {
            Atlas = GD.Load<Texture2D>(LpcSprites.Idle),
            Region = new Rect2(col * size, row * size, size, size),
        };
    }

    public override void _ExitTree()
    {
        if (InventoryManager.Instance is not null)
            InventoryManager.Instance.EquipmentChanged -= OnEquipmentChanged;

        if (GameManager.Instance != null)
            GameManager.Instance.PlayerLevelUp -= OnPlayerLevelUp;

        base._ExitTree();
    }

    void OnPlayerLevelUp(int _)
    {
        if (Visible)
            Refresh();
    }

    void OnEquipmentChanged()
    {
        if (Visible)
            Refresh();
    }

    public void OnTabShown()
    {
        Visible = true;
        Refresh();
    }

    public void OnTabHidden() => Visible = false;

    public void FocusDefault() { }

    public bool TryHandleCancel() => false;

    void Refresh()
    {
        Player player = GameManager.Instance?.CurrentPlayer;
        if (player == null)
            return;

        StatHandler statHandler = player.GetNodeOrNull<StatHandler>("PlayerStats");

        _nameLabel.Text = $"[b]{player.Name}[/b]";
        _classLabel.Text = $"[color=#58B4C6]{_playerClassName}[/color]";
        _levelLabel.Text = $"Niveau {player.Level}";

        _hpLabel.Text = $"[color=#F54927]{player.CurrentPv} / {player.Pv}[/color]";
        _mpLabel.Text = $"[color=#27B0F5]{player.CurrentMp} / {player.Mp}[/color]";

        _hpBar.MaxValue = player.Pv;
        _hpBar.Value = player.CurrentPv;
        _mpBar.MaxValue = player.Mp;
        _mpBar.Value = player.CurrentMp;

        RefreshExperience(statHandler, player.Level);
        RefreshAttributes(player);
    }

    void RefreshExperience(StatHandler statHandler, int level)
    {
        if (statHandler == null)
        {
            _xpLabel.Text = "—";
            _xpBar.MaxValue = 1;
            _xpBar.Value = 0;
            return;
        }

        Stats currentRow = statHandler.GetStatsForLevel(level);
        Stats nextRow = statHandler.GetStatsForLevel(level + 1);

        if (nextRow == null)
        {
            _xpLabel.Text = "[color=#FFD166]XP MAX[/color]";
            _xpBar.MaxValue = 1;
            _xpBar.Value = 1;
            return;
        }

        int currentXp = statHandler.CurrentExperience;
        int minXp = currentRow?.XPForNextLevel ?? 0;
        int maxXp = nextRow.XPForNextLevel;
        int span = Mathf.Max(maxXp - minXp, 1);

        _xpLabel.Text = $"[color=#FFD166]{currentXp} / {maxXp} XP[/color]";
        _xpBar.MaxValue = span;
        _xpBar.Value = Mathf.Clamp(currentXp - minXp, 0, span);
    }

    void RefreshAttributes(Player player)
    {
        var bonus = InventoryManager.Instance?.GetEquipmentBonuses() ?? default;
        _forceLabel.Text = FormatStat(player.Strength, bonus.Strength);
        _espritLabel.Text = FormatStat(player.Spirit, bonus.Spirit);
        _agiLabel.Text = FormatStat(player.Dexterity, bonus.Dexterity);
        _defLabel.Text = FormatStat(player.Defense, bonus.Defense);
    }

    static string FormatStat(int total, int bonus)
    {
        if (bonus <= 0)
            return total.ToString();

        int baseValue = total - bonus;
        return $"{baseValue} [color=#7AE582](+{bonus})[/color]";
    }
}
