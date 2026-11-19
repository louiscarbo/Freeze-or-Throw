using Unity.Netcode;
using UnityEngine;

public class SnowGridManager : NetworkBehaviour
{
    public static SnowGridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 55;
    [SerializeField] private int gridHeight = 55;
    [SerializeField] private float arenaWorldSize = 55f;
    [SerializeField] private float maxSnowPerCell = 10f;

    [Header("Radial Snow Distribution")]
    [Range(0f, 1f)]
    [SerializeField] private float edgeSnowMultiplier = 0.15f; // 15% snow at outer edges (0 = bare ice at edges)
    [SerializeField] private float falloffSharpness = 1.5f;    // Higher = deeper snow concentrated closer to center

    [Header("Visual Quad Reference")]
    [SerializeField] private MeshRenderer snowQuadRenderer;
    [SerializeField] private Color fullSnowColor = Color.white;
    [SerializeField] private Color emptyGroundColor = new Color(0.25f, 0.35f, 0.45f, 0f);

    private float[,] _snowGrid;
    private Texture2D _snowTexture;
    private Color[] _pixels;
    private bool _textureDirty = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitGridAndTexture();
    }

    private void InitGridAndTexture()
    {
        if (gridWidth <= 0) gridWidth = 55;
        if (gridHeight <= 0) gridHeight = 55;
        if (arenaWorldSize <= 0f) arenaWorldSize = 55f;
        if (maxSnowPerCell <= 0f) maxSnowPerCell = 10f;

        _snowGrid = new float[gridWidth, gridHeight];
        _snowTexture = new Texture2D(gridWidth, gridHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _pixels = new Color[gridWidth * gridHeight];

        Vector2 center = new Vector2((gridWidth - 1) * 0.5f, (gridHeight - 1) * 0.5f);
        float maxRadius = Mathf.Max(center.x, center.y);

        for (int z = 0; z < gridHeight; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                // 0 at exact center, 1 at outer border (clamped for corners)
                float normalizedDist = Mathf.Clamp01(Vector2.Distance(new Vector2(x, z), center) / maxRadius);

                // 1 at center -> edgeSnowMultiplier at the outer edge
                float radialFactor = Mathf.Lerp(1f, edgeSnowMultiplier, Mathf.Pow(normalizedDist, falloffSharpness));
                float initialSnow = maxSnowPerCell * radialFactor;

                _snowGrid[x, z] = initialSnow;

                float colorT = Mathf.Clamp01(initialSnow / maxSnowPerCell);
                _pixels[z * gridWidth + x] = Color.Lerp(emptyGroundColor, fullSnowColor, colorT);
            }
        }

        _snowTexture.SetPixels(_pixels);
        _snowTexture.Apply();

        if (snowQuadRenderer != null)
        {
            snowQuadRenderer.material.mainTexture = _snowTexture;
            snowQuadRenderer.material.SetTexture("_BaseMap", _snowTexture);
        }
    }

    private void LateUpdate()
    {
        if (_textureDirty && _snowTexture != null)
        {
            _snowTexture.SetPixels(_pixels);
            _snowTexture.Apply();
            _textureDirty = false;
        }
    }

    public float ConsumeSnowAt(Vector3 worldPos, float requestedAmount)
    {
        if (!IsServer) return 0f;

        if (!WorldToGrid(worldPos, out int cx, out int cz))
            return 0f;

        float totalCollected = 0f;

        for (int dz = -1; dz <= 1; dz++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int gx = cx + dx;
                int gz = cz + dz;

                if (gx < 0 || gx >= gridWidth || gz < 0 || gz >= gridHeight)
                    continue;

                float available = _snowGrid[gx, gz];
                if (available <= 0f) continue;

                float taken = Mathf.Min(available, requestedAmount);
                _snowGrid[gx, gz] -= taken;
                totalCollected += taken;

                UpdateCellPixel(gx, gz);
                UpdateCellClientRpc(gx, gz, _snowGrid[gx, gz]);
            }
        }

        return totalCollected;
    }

    [ClientRpc]
    private void UpdateCellClientRpc(int x, int z, float newSnowAmount)
    {
        if (IsServer) return;

        _snowGrid[x, z] = newSnowAmount;
        UpdateCellPixel(x, z);
    }

    private void UpdateCellPixel(int x, int z)
    {
        float t = Mathf.Clamp01(_snowGrid[x, z] / maxSnowPerCell);
        _pixels[z * gridWidth + x] = Color.Lerp(emptyGroundColor, fullSnowColor, t);
        _textureDirty = true;
    }

    private bool WorldToGrid(Vector3 worldPos, out int gx, out int gz)
    {
        float halfSize = arenaWorldSize * 0.5f;
        float normX = (worldPos.x + halfSize) / arenaWorldSize;
        float normZ = (worldPos.z + halfSize) / arenaWorldSize;

        gx = Mathf.FloorToInt(normX * gridWidth);
        gz = Mathf.FloorToInt(normZ * gridHeight);

        return gx >= 0 && gx < gridWidth && gz >= 0 && gz < gridHeight;
    }
}