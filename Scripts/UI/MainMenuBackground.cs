using Godot;

namespace EchoduKarma.Scripts.UI;

/// <summary>
/// Fond du menu : texture + shader d'ambiance (Ken Burns, brume, scintillements).
/// Le layout plein écran reste défini dans la scène.
/// </summary>
public partial class MainMenuBackground : TextureRect
{
	public const string LandscapePath = "res://Assets/UI/landscape.png";
	public const string LandscapeFallbackPath = "res://Assets/UI/landscape_title.png";

	ShaderMaterial _shaderMaterial;
	TextureRect _glowLayer;
	float _time;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		TextureFilter = TextureFilterEnum.Linear;
		StretchMode = StretchModeEnum.KeepAspectCovered;
		ExpandMode = ExpandModeEnum.IgnoreSize;
		Modulate = Colors.White;
		Visible = true;

		if (Texture == null || Texture.GetWidth() < 1)
			Texture = LoadLandscapeTexture();

		if (Texture == null)
		{
			GD.PrintErr("[MainMenuBackground] Texture paysage introuvable.");
			return;
		}

		SetupShader();
		SetupGlowLayer();
	}

	public override void _Process(double delta)
	{
		_time += (float)delta;
		UpdateShaderAnimation();
		PulseGlowLayer();
	}

	void SetupShader()
	{
		var shader = GD.Load<Shader>("res://Shaders/main_menu_landscape.gdshader");
		if (shader == null)
			return;

		_shaderMaterial = new ShaderMaterial { Shader = shader };
		Material = _shaderMaterial;
	}

	void SetupGlowLayer()
	{
		_glowLayer = GetParent()?.GetNodeOrNull<TextureRect>("LandscapeGlow");
		if (_glowLayer == null)
			return;

		if (_glowLayer.Texture == null)
			_glowLayer.Texture = Texture;

		_glowLayer.TextureFilter = TextureFilterEnum.Linear;
		_glowLayer.StretchMode = StretchModeEnum.KeepAspectCovered;
		_glowLayer.ExpandMode = ExpandModeEnum.IgnoreSize;
		_glowLayer.MouseFilter = MouseFilterEnum.Ignore;
		_glowLayer.Material = new CanvasItemMaterial
		{
			BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
		};
		_glowLayer.Modulate = new Color(0.75f, 0.9f, 1f, 0f);
	}

	void UpdateShaderAnimation()
	{
		if (_shaderMaterial == null)
			return;

		_shaderMaterial.SetShaderParameter("time", _time);

		float breathe = Mathf.Sin(_time * 0.22f) * 0.5f + 0.5f;
		float shimmer = Mathf.Sin(_time * 0.85f) * 0.35f + 0.65f;
		_shaderMaterial.SetShaderParameter("breathe", breathe * 0.018f);
		_shaderMaterial.SetShaderParameter("shimmer", shimmer);

		float panX = Mathf.Sin(_time * 0.07f) * 0.014f;
		float panY = Mathf.Cos(_time * 0.05f) * 0.009f;
		_shaderMaterial.SetShaderParameter("pan_offset", new Vector2(panX, panY));

		float zoom = 1f + Mathf.Sin(_time * 0.09f) * 0.035f;
		_shaderMaterial.SetShaderParameter("zoom", zoom);
	}

	void PulseGlowLayer()
	{
		if (_glowLayer == null)
			return;

		float alpha = 0.05f + Mathf.Abs(Mathf.Sin(_time * 0.5f)) * 0.07f;
		_glowLayer.Modulate = new Color(0.75f, 0.9f, 1f, alpha);
	}

	static Texture2D LoadLandscapeTexture()
	{
		foreach (string resPath in new[] { LandscapePath, LandscapeFallbackPath })
		{
			Texture2D fromDisk = TryLoadPng(resPath);
			if (fromDisk != null)
				return fromDisk;

			if (ResourceLoader.Exists(resPath))
			{
				var tex = ResourceLoader.Load<Texture2D>(resPath);
				if (tex != null && tex.GetWidth() > 0)
					return tex;
			}
		}

		return null;
	}

	static Texture2D TryLoadPng(string resPath)
	{
		string absolute = ProjectSettings.GlobalizePath(resPath);
		if (string.IsNullOrEmpty(absolute) || !FileAccess.FileExists(absolute))
			return null;

		Image image = Image.LoadFromFile(absolute);
		if (image == null || image.IsEmpty())
			return null;

		return ImageTexture.CreateFromImage(image);
	}
}
