using Unity.Netcode;
using UnityEngine;

public class SnowGridManager : NetworkBehaviour
{
    public static SnowGridManager Instance { get; private set; }

    [Header("Arena References")]
    [Tooltip("Drag your 50x50 stretched Arena floor Cube/Plane here")]
    [SerializeField] private Renderer arenaRenderer;

    [Header("Grid Settings")]
    [Tooltip("Size of each snow square in meters (2m = 25x25 grid on a 50x50 floor)")]
    [SerializeField] private float cellSize = 2f;
    [SerializeField] private float maxSnowPerCell = 100f;

    [Header("Visual Colors")]
    [SerializeField] private Color fullSnowColor = Color.white;
    [SerializeField] private Color depletedGroundColor = new Color(0.35f, 0.32f, 0.30f);

    private int gridWidth;
    private int gridDepth;
    private float[,] snowGrid;
    private Vector3 arenaMinCorner;
    private Texture2D snowMaskTexture;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        Bounds bounds = arenaRenderer.bounds;
        arenaMinCorner = new Vector3(bounds.min.x, bounds.center.y, bounds.min.z);

        gridWidth = Mathf.Max(1, Mathf.RoundToInt(bounds.size.x / cellSize));
        gridDepth = Mathf.Max(1, Mathf.RoundToInt(bounds.size.z / cellSize));

        snowGrid = new float[gridWidth, gridDepth];

        // Create a lightweight runtime texture matching the grid resolution
        snowMaskTexture = new Texture2D(gridWidth, gridDepth, TextureFormat.RGBA32, false);
        snowMaskTexture.filterMode = FilterMode.Bilinear;
        snowMaskTexture.wrapMode = TextureWrapMode.Clamp;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                snowGrid[x, z] = maxSnowPerCell;
                snowMaskTexture.SetPixel(x, z, fullSnowColor);
            }
        }

        snowMaskTexture.Apply();
        arenaRenderer.material.mainTexture = snowMaskTexture;
    }

    /// <summary>
    /// Converts a world position into (gridX, gridZ) coordinates.
    /// Returns false if the position is outside the arena floor.
    /// </summary>
    public bool WorldToGrid(Vector3 worldPos, out int gridX, out int gridZ)
    {
        gridX = Mathf.FloorToInt((worldPos.x - arenaMinCorner.x) / cellSize);
        gridZ = Mathf.FloorToInt((worldPos.z - arenaMinCorner.z) / cellSize);

        return gridX >= 0 && gridX < gridWidth && gridZ >= 0 && gridZ < gridDepth;
    }

    /// <summary>
    /// Called by a rolling snowball to collect snow at its current world position.
    /// Returns the actual amount of snow collected.
    /// </summary>
    public float ConsumeSnowAt(Vector3 worldPos, float requestedAmount)
    {
        if (!WorldToGrid(worldPos, out int x, out int z))
            return 0f;

        float available = snowGrid[x, z];
        if (available <= 0f)
            return 0f;

        float taken = Mathf.Min(available, requestedAmount);

        if (IsServer)
        {
            ApplySnowChange(x, z, available - taken);
            UpdateCellClientRpc(x, z, snowGrid[x, z]);
        }
        else if (IsClient)
        {
            RequestConsumeSnowServerRpc(x, z, requestedAmount);
        }

        return taken;
    }

    /// <summary>
    /// Refills snow across the entire arena (call this during a Snowstorm event).
    /// </summary>
    public void RegenerateAllSnow(float amountToAdd)
    {
        if (!IsServer) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                if (snowGrid[x, z] < maxSnowPerCell)
                {
                    float newValue = Mathf.Min(maxSnowPerCell, snowGrid[x, z] + amountToAdd);
                    ApplySnowChange(x, z, newValue);
                }
            }
        }

        snowMaskTexture.Apply();
        RegenerateAllClientRpc(amountToAdd);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestConsumeSnowServerRpc(int x, int z, float requestedAmount)
    {
        if (x < 0 || x >= gridWidth || z < 0 || z >= gridDepth) return;

        float available = snowGrid[x, z];
        if (available <= 0f) return;

        float newValue = Mathf.Max(0f, available - requestedAmount);
        ApplySnowChange(x, z, newValue);
        UpdateCellClientRpc(x, z, newValue);
    }

    [ClientRpc]
    private void UpdateCellClientRpc(int x, int z, float newValue)
    {
        if (IsServer) return; // Server already updated locally
        ApplySnowChange(x, z, newValue);
    }

    [ClientRpc]
    private void RegenerateAllClientRpc(float amountToAdd)
    {
        if (IsServer) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                if (snowGrid[x, z] < maxSnowPerCell)
                {
                    float newValue = Mathf.Min(maxSnowPerCell, snowGrid[x, z] + amountToAdd);
                    ApplySnowChange(x, z, newValue);
                }
            }
        }
        snowMaskTexture.Apply();
    }

    private void ApplySnowChange(int x, int z, float newValue)
    {
        snowGrid[x, z] = newValue;
        float ratio = newValue / maxSnowPerCell;
        snowMaskTexture.SetPixel(x, z, Color.Lerp(depletedGroundColor, fullSnowColor, ratio));
        snowMaskTexture.Apply();
    }
}