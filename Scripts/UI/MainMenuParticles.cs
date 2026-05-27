using Godot;

namespace EchoduKarma.Scripts.UI;

/// <summary>
/// Particules GPU pour le title screen paysage : ciel, eau, karma et lueurs du château.
/// </summary>
public partial class MainMenuParticles : Node2D
{
    static Texture2D _cachedGlowTexture;

    GpuParticles2D _starfield;
    GpuParticles2D _auroraMist;
    GpuParticles2D _karmaRise;
    GpuParticles2D _castleSparkles;
    GpuParticles2D _fireflies;
    GpuParticles2D _waterGlint;

    Control _bounds;
    Vector2 _lastSize;

    public override void _Ready()
    {
        _bounds = GetParent() as Control;
        Texture2D glow = EnsureGlowTexture();

        _starfield = CreateStarfield(glow);
        _auroraMist = CreateAuroraMist(glow);
        _karmaRise = CreateKarmaRise(glow);
        _castleSparkles = CreateCastleSparkles(glow);
        _fireflies = CreateFireflies(glow);
        _waterGlint = CreateWaterGlint(glow);

        AddChild(_starfield);
        AddChild(_auroraMist);
        AddChild(_karmaRise);
        AddChild(_castleSparkles);
        AddChild(_fireflies);
        AddChild(_waterGlint);

        CallDeferred(MethodName.RefreshLayout);
    }

    public override void _Process(double delta)
    {
        if (_bounds == null)
            return;

        Vector2 size = _bounds.Size;
        if (size != _lastSize)
            RefreshLayout();
    }

    void RefreshLayout()
    {
        if (_bounds == null)
            return;

        Vector2 size = _bounds.Size;
        _lastSize = size;
        if (size.X < 2f || size.Y < 2f)
            return;

        Position = Vector2.Zero;

        LayoutStarfield(size);
        LayoutAuroraMist(size);
        LayoutKarmaRise(size);
        LayoutCastleSparkles(size);
        LayoutFireflies(size);
        LayoutWaterGlint(size);
    }

    static Texture2D EnsureGlowTexture()
    {
        if (_cachedGlowTexture != null && GodotObject.IsInstanceValid(_cachedGlowTexture))
            return _cachedGlowTexture;

        const int texSize = 32;
        var image = Image.CreateEmpty(texSize, texSize, false, Image.Format.Rgba8);
        Vector2 center = new(texSize * 0.5f, texSize * 0.5f);
        float radius = texSize * 0.5f;

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dist = new Vector2(x, y).DistanceTo(center) / radius;
                float alpha = Mathf.Clamp(1f - dist, 0f, 1f);
                alpha *= alpha;
                image.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        _cachedGlowTexture = ImageTexture.CreateFromImage(image);
        return _cachedGlowTexture;
    }

    static GpuParticles2D CreateBase(string name, Texture2D texture, int amount, float lifetime, bool additive)
    {
        var node = new GpuParticles2D
        {
            Name = name,
            Amount = amount,
            Lifetime = lifetime,
            Preprocess = lifetime * 0.55f,
            Explosiveness = 0f,
            Randomness = 0.4f,
            Texture = texture,
            VisibilityRect = new Rect2(-1920, -1080, 3840, 2160),
        };

        node.Material = new CanvasItemMaterial
        {
            BlendMode = additive ? CanvasItemMaterial.BlendModeEnum.Add : CanvasItemMaterial.BlendModeEnum.Mix,
        };

        return node;
    }

    static ParticleProcessMaterial CreateMaterial(
        ParticleProcessMaterial.EmissionShapeEnum shape,
        float spread,
        Vector3 direction,
        float velocityMin,
        float velocityMax,
        float gravityY,
        float scaleMin,
        float scaleMax,
        Gradient colorRamp,
        Vector3? emissionBoxExtents = null)
    {
        var mat = new ParticleProcessMaterial
        {
            EmissionShape = shape,
            Spread = spread,
            Direction = direction,
            InitialVelocityMin = velocityMin,
            InitialVelocityMax = velocityMax,
            Gravity = new Vector3(0, gravityY, 0),
            ScaleMin = scaleMin,
            ScaleMax = scaleMax,
            Color = Colors.White,
            ColorRamp = new GradientTexture1D { Gradient = colorRamp },
        };

        if (shape == ParticleProcessMaterial.EmissionShapeEnum.Box && emissionBoxExtents.HasValue)
            mat.EmissionBoxExtents = emissionBoxExtents.Value;

        return mat;
    }

    GpuParticles2D CreateStarfield(Texture2D glow)
    {
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(0.9f, 0.95f, 1f, 0f));
        gradient.AddPoint(0.3f, new Color(1f, 1f, 1f, 0.6f));
        gradient.SetColor(1, new Color(0.7f, 0.88f, 1f, 0f));

        var node = CreateBase("Starfield", glow, 220, 10f, additive: true);
        node.ProcessMaterial = CreateMaterial(
            ParticleProcessMaterial.EmissionShapeEnum.Box,
            160f,
            new Vector3(0, -0.2f, 0),
            1f,
            8f,
            0f,
            0.12f,
            0.45f,
            gradient,
            new Vector3(960, 280, 1));
        return node;
    }

    GpuParticles2D CreateAuroraMist(Texture2D glow)
    {
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(0.5f, 0.8f, 1f, 0f));
        gradient.AddPoint(0.45f, new Color(0.65f, 0.9f, 1f, 0.18f));
        gradient.SetColor(1, new Color(0.4f, 0.65f, 0.9f, 0f));

        var node = CreateBase("AuroraMist", glow, 90, 14f, additive: true);
        node.ProcessMaterial = CreateMaterial(
            ParticleProcessMaterial.EmissionShapeEnum.Box,
            25f,
            new Vector3(0.08f, 0.02f, 0),
            0.5f,
            4f,
            0f,
            2f,
            5.5f,
            gradient,
            new Vector3(700, 120, 1));
        var mat = (ParticleProcessMaterial)node.ProcessMaterial;
        mat.TurbulenceEnabled = true;
        mat.TurbulenceNoiseStrength = 0.5f;
        mat.TurbulenceNoiseSpeed = new Vector3(0.12f, 0.08f, 0);
        return node;
    }

    GpuParticles2D CreateKarmaRise(Texture2D glow)
    {
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(0.6f, 0.9f, 1f, 0f));
        gradient.AddPoint(0.25f, new Color(0.75f, 0.95f, 1f, 0.75f));
        gradient.SetColor(1, new Color(0.45f, 0.75f, 0.95f, 0f));

        var node = CreateBase("KarmaRise", glow, 160, 9f, additive: true);
        node.ProcessMaterial = CreateMaterial(
            ParticleProcessMaterial.EmissionShapeEnum.Box,
            18f,
            new Vector3(0, -1, 0),
            14f,
            36f,
            -10f,
            0.2f,
            0.75f,
            gradient,
            new Vector3(900, 10, 1));
        var mat = (ParticleProcessMaterial)node.ProcessMaterial;
        mat.TurbulenceEnabled = true;
        mat.TurbulenceNoiseStrength = 0.55f;
        mat.TurbulenceNoiseSpeed = new Vector3(0.18f, 0.3f, 0);
        return node;
    }

    GpuParticles2D CreateCastleSparkles(Texture2D glow)
    {
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(1f, 0.92f, 0.65f, 0f));
        gradient.AddPoint(0.35f, new Color(1f, 0.98f, 0.85f, 0.9f));
        gradient.SetColor(1, new Color(0.85f, 0.95f, 1f, 0f));

        var node = CreateBase("CastleSparkles", glow, 56, 2.8f, additive: true);
        node.ProcessMaterial = CreateMaterial(
            ParticleProcessMaterial.EmissionShapeEnum.Sphere,
            160f,
            new Vector3(0, -0.2f, 0),
            8f,
            22f,
            4f,
            0.15f,
            0.55f,
            gradient);
        var mat = (ParticleProcessMaterial)node.ProcessMaterial;
        mat.EmissionSphereRadius = 55f;
        mat.RadialVelocityMin = -6f;
        mat.RadialVelocityMax = 14f;
        return node;
    }

    GpuParticles2D CreateFireflies(Texture2D glow)
    {
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(0.95f, 0.85f, 0.45f, 0f));
        gradient.AddPoint(0.4f, new Color(1f, 0.95f, 0.6f, 0.7f));
        gradient.SetColor(1, new Color(0.9f, 0.75f, 0.35f, 0f));

        var node = CreateBase("Fireflies", glow, 48, 6f, additive: true);
        node.ProcessMaterial = CreateMaterial(
            ParticleProcessMaterial.EmissionShapeEnum.Box,
            180f,
            new Vector3(0, 0, 0),
            2f,
            8f,
            0f,
            0.25f,
            0.7f,
            gradient,
            new Vector3(500, 320, 1));
        var mat = (ParticleProcessMaterial)node.ProcessMaterial;
        mat.TurbulenceEnabled = true;
        mat.TurbulenceNoiseStrength = 0.8f;
        mat.TurbulenceNoiseSpeed = new Vector3(0.25f, 0.15f, 0);
        return node;
    }

    GpuParticles2D CreateWaterGlint(Texture2D glow)
    {
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(0.7f, 0.95f, 1f, 0f));
        gradient.AddPoint(0.5f, new Color(0.9f, 1f, 1f, 0.55f));
        gradient.SetColor(1, new Color(0.5f, 0.85f, 1f, 0f));

        var node = CreateBase("WaterGlint", glow, 70, 3.5f, additive: true);
        node.ProcessMaterial = CreateMaterial(
            ParticleProcessMaterial.EmissionShapeEnum.Box,
            40f,
            new Vector3(0, 0.1f, 0),
            1f,
            5f,
            0f,
            0.15f,
            0.5f,
            gradient,
            new Vector3(420, 80, 1));
        return node;
    }

    void LayoutStarfield(Vector2 size)
    {
        _starfield.Position = new Vector2(size.X * 0.5f, size.Y * 0.18f);
        ((ParticleProcessMaterial)_starfield.ProcessMaterial).EmissionBoxExtents =
            new Vector3(size.X * 0.55f, size.Y * 0.22f, 1);
    }

    void LayoutAuroraMist(Vector2 size)
    {
        _auroraMist.Position = new Vector2(size.X * 0.5f, size.Y * 0.12f);
        ((ParticleProcessMaterial)_auroraMist.ProcessMaterial).EmissionBoxExtents =
            new Vector3(size.X * 0.45f, size.Y * 0.12f, 1);
    }

    void LayoutKarmaRise(Vector2 size)
    {
        _karmaRise.Position = new Vector2(size.X * 0.5f, size.Y * 0.94f);
        ((ParticleProcessMaterial)_karmaRise.ProcessMaterial).EmissionBoxExtents =
            new Vector3(size.X * 0.5f, 14f, 1);
    }

    void LayoutCastleSparkles(Vector2 size)
    {
        _castleSparkles.Position = new Vector2(size.X * 0.84f, size.Y * 0.36f);
        ((ParticleProcessMaterial)_castleSparkles.ProcessMaterial).EmissionSphereRadius = size.X * 0.04f;
    }

    void LayoutFireflies(Vector2 size)
    {
        _fireflies.Position = new Vector2(size.X * 0.38f, size.Y * 0.52f);
        ((ParticleProcessMaterial)_fireflies.ProcessMaterial).EmissionBoxExtents =
            new Vector3(size.X * 0.32f, size.Y * 0.28f, 1);
    }

    void LayoutWaterGlint(Vector2 size)
    {
        _waterGlint.Position = new Vector2(size.X * 0.36f, size.Y * 0.58f);
        ((ParticleProcessMaterial)_waterGlint.ProcessMaterial).EmissionBoxExtents =
            new Vector3(size.X * 0.28f, size.Y * 0.08f, 1);
    }
}
