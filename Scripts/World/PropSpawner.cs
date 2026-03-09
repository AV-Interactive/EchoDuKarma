using Godot;
using System.Collections.Generic;

public partial class PropSpawner : Node3D
{
    [Export] public PackedScene PropScene { get; set; }        // Tree.tscn
    [Export] public NodePath TerrainPath { get; set; }
    [Export] public Texture2D TerrainAlbedo { get; set; }
    [Export] public int PropCount { get; set; } = 100;         // Nb d'arbres max
    [Export] public float MinDistanceBetweenProps { get; set; } = 3f; // Distance min entre arbres
    [Export] public int Seed { get; set; } = 12345;

    float _minX, _maxX, _minZ, _maxZ, _sizeX, _sizeZ;
    Image _image;

    public override void _Ready()
    {
        // Cache terrain bounds
        var terrainMesh = GetNode<MeshInstance3D>(TerrainPath);
        var localAABB = terrainMesh.GetAabb();
        var gt = terrainMesh.GlobalTransform;
        Vector3 worldMin = gt * localAABB.Position;
        Vector3 worldMax = gt * (localAABB.Position + localAABB.Size);
        _minX = Mathf.Min(worldMin.X, worldMax.X);
        _maxX = Mathf.Max(worldMin.X, worldMax.X);
        _minZ = Mathf.Min(worldMin.Z, worldMax.Z);
        _maxZ = Mathf.Max(worldMin.Z, worldMax.Z);
        _sizeX = _maxX - _minX;
        _sizeZ = _maxZ - _minZ;

        // Cache GrassMask
        _image = TerrainAlbedo.GetImage();
        _image.Decompress();
        _image.Convert(Image.Format.Rgba8);

        SpawnProps();
    }

    void SpawnProps()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = (ulong)Seed;

        var spaceState = GetWorld3D().DirectSpaceState;
        var placedPositions = new List<Vector3>();

        int attempts = 0;
        int spawned = 0;

        while (spawned < PropCount && attempts < PropCount * 20)
        {
            attempts++;

            float x = rng.RandfRange(_minX, _maxX);
            float z = rng.RandfRange(_minZ, _maxZ);

            // Vérifie que c'est sur l'herbe (pas le chemin)
            if (!IsGrass(x, z)) continue;

            // Vérifie distance minimum entre arbres
            if (!IsFarEnough(new Vector3(x, 0, z), placedPositions)) continue;

            // Raycast pour trouver la hauteur du terrain
            float y = GetHeight(spaceState, x, z);

            // Instancie l'arbre
            var instance = PropScene.Instantiate<Node3D>();
            AddChild(instance);

            // Position + rotation Y aléatoire pour la variété
            float randomRotY = rng.RandfRange(0f, Mathf.Tau);
            instance.GlobalPosition = new Vector3(x, y, z);
            instance.GlobalRotation = new Vector3(0, randomRotY, 0);

            placedPositions.Add(new Vector3(x, 0, z));
            spawned++;
        }

        GD.Print($"[PropSpawner] {spawned} arbres placés.");
    }

    bool IsGrass(float x, float z)
    {
        float u = (x - _minX) / _sizeX;
        float v = (z - _minZ) / _sizeZ;
        int px = Mathf.Clamp((int)(u * _image.GetWidth()), 0, _image.GetWidth() - 1);
        int pz = Mathf.Clamp((int)(v * _image.GetHeight()), 0, _image.GetHeight() - 1);
        Color c = _image.GetPixel(px, pz);
        return c.G > c.R * 1.3f;
    }

    bool IsFarEnough(Vector3 pos, List<Vector3> placed)
    {
        foreach (var p in placed)
        {
            if (pos.DistanceTo(p) < MinDistanceBetweenProps)
                return false;
        }
        return true;
    }

    float GetHeight(PhysicsDirectSpaceState3D spaceState, float x, float z)
    {
        var query = PhysicsRayQueryParameters3D.Create(
            new Vector3(x, 100f, z),
            new Vector3(x, -100f, z)
        );
        query.CollisionMask = 1; // Terrain uniquement
        var result = spaceState.IntersectRay(query);
        return result.Count > 0 ? ((Vector3)result["position"]).Y : 0f;
    }
}