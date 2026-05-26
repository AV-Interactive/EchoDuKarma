using Godot;

namespace EchoduKarma.Scripts.Data;

public partial class ResourceItem : RefCounted
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string IconFile { get; set; } = "";
    public string Description { get; set; } = "";
}
