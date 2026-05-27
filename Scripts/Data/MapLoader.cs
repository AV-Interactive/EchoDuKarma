using Godot;
using System;
using EchoduKarma.Scripts.Data;

public partial class MapLoader : Node3D
{
	[Export] public string ZoneName;
	
	public override void _Ready()
	{
		string scenePath = GetTree().CurrentScene?.SceneFilePath ?? "";
		GameManager.Instance?.SetMapContext(ZoneName, scenePath);
		DialogueSystem.Instance.LoadZoneDialogues(ZoneName);
		MusicManager.Instance?.PlayZone(ZoneName);
	}
}
