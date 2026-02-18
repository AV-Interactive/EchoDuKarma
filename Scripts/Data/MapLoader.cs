using Godot;
using System;
using EchoduKarma.Scripts.Data;

public partial class MapLoader : Node2D
{
    [Export] public string ZoneName;
    
    public override void _Ready()
    {
        DialogueSystem.Instance.LoadZoneDialogues(ZoneName);
    }
}
