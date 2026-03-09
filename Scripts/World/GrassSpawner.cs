using Godot;
using System.Collections.Generic;

public partial class GrassSpawner : Node3D
{
    [Export] public float ChunkSize { get; set; } = 4f;        // Taille d'un chunk en unités
    [Export] public int ChunkRadius { get; set; } = 3;         // Nb de chunks autour du joueur
    [Export] public int GrassPerChunk { get; set; } = 300;     // Brins par chunk
    [Export] public float NoiseThreshold { get; set; } = 0.15f;
    [Export] public NodePath TerrainPath { get; set; }
    [Export] public Texture2D TerrainAlbedo { get; set; }
    [Export] public NodePath MultiMeshPath { get; set; }
    [Export] public NodePath PlayerPath { get; set; }

    FastNoiseLite _noise = new FastNoiseLite();
    Node3D _player;
    Vector2I _lastChunkPos = new Vector2I(int.MaxValue, int.MaxValue);

    float _minX, _maxX, _minZ, _maxZ, _sizeX, _sizeZ;
    Image _image;

    // Cache des chunks générés : clé = coordonnée chunk, valeur = liste de transforms
    Dictionary<Vector2I, List<(Transform3D, Color)>> _chunkCache = new();
    
    // Dictionnaire heightmap précalculé
    Dictionary<Vector2I, float> _heightmap = new();
    float _heightmapResolution = 0.5f;

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

        // Cache image
        _image = TerrainAlbedo.GetImage();
        _image.Decompress();
        _image.Convert(Image.Format.Rgba8);

        // Cache player
        if (PlayerPath != null)
            _player = GetNode<Node3D>(PlayerPath);

        _noise.Seed = 42;
        _noise.Frequency = 0.05f;
        
        // On lance le bake de la hauteur
        BakeHeightmap();
        
        UpdateChunks();
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;

        // Coordonnée du chunk actuel du joueur
        Vector2I currentChunk = WorldToChunk(_player.GlobalPosition);

        // Regénère seulement si le joueur change de chunk
        if (currentChunk != _lastChunkPos)
        {
            _lastChunkPos = currentChunk;
            UpdateChunks();
        }
    }

    Vector2I WorldToChunk(Vector3 worldPos)
    {
        return new Vector2I(
            Mathf.FloorToInt(worldPos.X / ChunkSize),
            Mathf.FloorToInt(worldPos.Z / ChunkSize)
        );
    }

    void UpdateChunks()
    {
        var multiMesh = GetNode<MultiMeshInstance3D>(MultiMeshPath);
        var mm = new MultiMesh();
        mm.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        mm.UseColors = true;
        mm.Mesh = CreateBladeMesh();

        var allTransforms = new List<(Transform3D, Color)>();

        // Collecte tous les chunks visibles
        for (int cx = -ChunkRadius; cx <= ChunkRadius; cx++)
        {
            for (int cz = -ChunkRadius; cz <= ChunkRadius; cz++)
            {
                var chunkCoord = new Vector2I(_lastChunkPos.X + cx, _lastChunkPos.Y + cz);
                var chunkData = GetOrGenerateChunk(chunkCoord);
                allTransforms.AddRange(chunkData);
            }
        }

        mm.InstanceCount = allTransforms.Count;
        for (int i = 0; i < allTransforms.Count; i++)
        {
            mm.SetInstanceTransform(i, allTransforms[i].Item1);
            mm.SetInstanceColor(i, allTransforms[i].Item2);
        }

        multiMesh.Multimesh = mm;
    }

    List<(Transform3D, Color)> GetOrGenerateChunk(Vector2I chunkCoord)
    {
        // Si déjà en cache → on retourne directement (stable, pas de saccade !)
        if (_chunkCache.TryGetValue(chunkCoord, out var cached))
            return cached;

        // Sinon on génère avec une seed fixe basée sur les coordonnées du chunk
        var result = GenerateChunk(chunkCoord);
        _chunkCache[chunkCoord] = result;
        return result;
    }

    List<(Transform3D, Color)> GenerateChunk(Vector2I chunkCoord)
    {
        var transforms = new List<(Transform3D, Color)>();

        // Seed déterministe basée sur les coordonnées du chunk
        var rng = new RandomNumberGenerator();
        rng.Seed = (ulong)(chunkCoord.X * 73856093 ^ chunkCoord.Y * 19349663);

        float chunkWorldX = chunkCoord.X * ChunkSize;
        float chunkWorldZ = chunkCoord.Y * ChunkSize;

        int attempts = 0;
        while (transforms.Count < GrassPerChunk && attempts < GrassPerChunk * 5)
        {
            attempts++;
            float x = rng.RandfRange(chunkWorldX, chunkWorldX + ChunkSize);
            float z = rng.RandfRange(chunkWorldZ, chunkWorldZ + ChunkSize);

            // Hors terrain → skip
            if (x < _minX || x > _maxX || z < _minZ || z > _maxZ) continue;

            // Sample texture masque
            float u = (x - _minX) / _sizeX;
            float v = (z - _minZ) / _sizeZ;
            int px = Mathf.Clamp((int)(u * _image.GetWidth()), 0, _image.GetWidth() - 1);
            int pz = Mathf.Clamp((int)(v * _image.GetHeight()), 0, _image.GetHeight() - 1);
            Color terrainColor = _image.GetPixel(px, pz);

            bool isGreen = terrainColor.G > terrainColor.R * 1.3f;
            float noiseVal = (_noise.GetNoise2D(x, z) + 1f) / 2f;
            bool inCluster = noiseVal > NoiseThreshold;

            if (!isGreen || !inCluster) continue;

            float y = GetHeight(x, z);

            float hueVariation = rng.RandfRange(-0.05f, 0.05f);
            float brightness = rng.RandfRange(0.7f, 1.0f);
            Color grassColor = new Color(
                0.2f + hueVariation,
                0.5f + hueVariation + rng.RandfRange(0f, 0.2f),
                0.1f
            ) * brightness;

            transforms.Add((new Transform3D(Basis.Identity, new Vector3(x, y, z)), grassColor));
        }

        return transforms;
    }

    private Mesh CreateBladeMesh()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        float w = 0.03f;
        float h = 0.08f;
        AddBlade(st, 0f, w, h, 0f);
        AddBlade(st, Mathf.Pi / 3f, w, h, 0.05f);
        AddBlade(st, Mathf.Pi * 2f / 3f, w, h, -0.05f);
        st.GenerateNormals();
        return st.Commit();
    }

    private void AddBlade(SurfaceTool st, float angle, float w, float h, float offset)
    {
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        st.SetUV(new Vector2(0, 1)); st.AddVertex(new Vector3(-w * cos + offset, 0, -w * sin));
        st.SetUV(new Vector2(1, 1)); st.AddVertex(new Vector3(w * cos + offset, 0, w * sin));
        st.SetUV(new Vector2(1, 0)); st.AddVertex(new Vector3(w * cos + offset, h, w * sin));
        st.SetUV(new Vector2(0, 1)); st.AddVertex(new Vector3(-w * cos + offset, 0, -w * sin));
        st.SetUV(new Vector2(1, 0)); st.AddVertex(new Vector3(w * cos + offset, h, w * sin));
        st.SetUV(new Vector2(0, 0)); st.AddVertex(new Vector3(-w * cos + offset, h, -w * sin));
    }
    
    void BakeHeightmap()
    {
        var spaceState = GetWorld3D().DirectSpaceState;
    
        for (float x = _minX; x <= _maxX; x += _heightmapResolution)
        {
            for (float z = _minZ; z <= _maxZ; z += _heightmapResolution)
            {
                var query = PhysicsRayQueryParameters3D.Create(
                    new Vector3(x, 100f, z),
                    new Vector3(x, -100f, z)
                );

                query.CollisionMask = 1;
                query.HitFromInside = false;
                
                var result = spaceState.IntersectRay(query);
                float y = result.Count > 0 ? ((Vector3)result["position"]).Y : 0f;
            
                var key = new Vector2I(
                    Mathf.RoundToInt(x / _heightmapResolution),
                    Mathf.RoundToInt(z / _heightmapResolution)
                );
                _heightmap[key] = y;
            }
        }
    }

    float GetHeight(float x, float z)
    {
        var key = new Vector2I(
            Mathf.RoundToInt(x / _heightmapResolution),
            Mathf.RoundToInt(z / _heightmapResolution)
        );
        return _heightmap.TryGetValue(key, out float h) ? h : 0f;
    }
}