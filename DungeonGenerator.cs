using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    public int width = 25;
    public int height = 25;
    public int minRoomSize = 3;
    public int maxRoomSize = 10;
    public int roomAttempts = 20;
    public int corridorWidth = 2;
    public float visibilityRadius = 10f;

    public GameObject enemyPrefab;
    private EnemyManager enemyManager;

    public void Initialize(EnemyManager manager)
    {
        enemyManager = manager;
    }

    public void SetEnemyManager(EnemyManager manager)
    {
        enemyManager = manager;
    }
    public Color torchColor = new Color(1f, 0.8f, 0.6f, 1f); // Slight orange tint

    public Material fogMaterial;
    private Texture2D fogTexture;
    private bool[,] revealedTiles;
    public int subPixelResolution = 16; // Subdivide each pixel into 4x4 subpixels

    private float[,] lightLevels;
    public float maxLightIntensity = 4f;
    public float fogPixelSize = .25f;
    public float torchRadius = 3.2f; // Increased default torch radius
    public float revealedDarknessMultiplier = 0.5f; // Very small revealed darkness
    public float lightFalloffExponent = 0.05f; // Default light falloff

    private Color wallColor;
    private Color floorColor;
    public Color entryColor = Color.green;
    public Color exitColor = Color.red;


    private List<EnemyController> enemies = new List<EnemyController>();
    private GameObject fogPlane;

    public GameObject floorTilePrefab;
    public GameObject wallTilePrefab;
    public GameObject entryTilePrefab;
    public GameObject exitTilePrefab;
    public int tileSubdivisions = 4;
    private float[,,] revealedTilePixels;
    private int[,] dungeonMap;
    private List<Rect> rooms;
    public void GenerateNewDungeon()
    {
        ClearExistingDungeon();
        dungeonMap = new int[width, height];
        revealedTiles = new bool[width, height];
        rooms = new List<Rect>();

        GenerateRandomColors();

        GenerateRooms();
        ConnectRooms();
        PlaceWalls();
        PlaceEntryAndExit();
        lightLevels = new float[width, height];
        InitializeLightLevels();
        InstantiateDungeonTiles();
        enemyManager.InitializeEnemies();
        InitializeFogOfWar();
        UpdateVisibility(GetPlayerStartPosition(), visibilityRadius);
    }



    private void GenerateRandomColors()
    {
        wallColor = Random.ColorHSV(0f, 1f, 0.5f, 0.8f, 0.6f, 0.8f);
        floorColor = new Color(
            Mathf.Min(wallColor.r * 1.5f, 1f),
            Mathf.Min(wallColor.g * 1.5f, 1f),
            Mathf.Min(wallColor.b * 1.5f, 1f)
        );
    }
    private void InitializeLightLevels()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                lightLevels[x, y] = 0f;
            }
        }
    }

    private void InitializeFogOfWar()
    {
        fogTexture = new Texture2D(width, height);
        fogTexture.filterMode = FilterMode.Point;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                fogTexture.SetPixel(x, y, Color.black);
                lightLevels[x, y] = 0f;
            }
        }
        fogTexture.Apply();

        fogMaterial.SetTexture("_FogTex", fogTexture);
    }

    public bool IsWalkableTile(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return false;
        }
        return dungeonMap[x, y] == 1 || dungeonMap[x, y] == 3 || dungeonMap[x, y] == 4;
    }
    private void ClearExistingDungeon()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
    private void GenerateRooms()
    {
        for (int i = 0; i < roomAttempts; i++)
        {
            int roomWidth = Random.Range(minRoomSize, maxRoomSize + 1);
            int roomHeight = Random.Range(minRoomSize, maxRoomSize + 1);
            int roomX = Random.Range(1, width - roomWidth - 1);
            int roomY = Random.Range(1, height - roomHeight - 1);

            Rect newRoom = new Rect(roomX, roomY, roomWidth, roomHeight);

            bool overlaps = false;
            foreach (Rect room in rooms)
            {
                if (newRoom.Overlaps(room))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                CreateRoom(newRoom);
                rooms.Add(newRoom);
            }
        }
    }

    private void CreateRoom(Rect room)
    {
        for (int x = (int)room.x; x < (int)room.x + (int)room.width; x++)
        {
            for (int y = (int)room.y; y < (int)room.y + (int)room.height; y++)
            {
                dungeonMap[x, y] = 1;
            }
        }
    }

    private void ConnectRooms()
    {
        for (int i = 0; i < rooms.Count - 1; i++)
        {
            Vector2 roomCenter1 = new Vector2(rooms[i].center.x, rooms[i].center.y);
            Vector2 roomCenter2 = new Vector2(rooms[i + 1].center.x, rooms[i + 1].center.y);

            if (Random.Range(0, 2) == 0)
            {
                CreateHorizontalCorridor((int)roomCenter1.x, (int)roomCenter2.x, (int)roomCenter1.y);
                CreateVerticalCorridor((int)roomCenter1.y, (int)roomCenter2.y, (int)roomCenter2.x);
            }
            else
            {
                CreateVerticalCorridor((int)roomCenter1.y, (int)roomCenter2.y, (int)roomCenter1.x);
                CreateHorizontalCorridor((int)roomCenter1.x, (int)roomCenter2.x, (int)roomCenter2.y);
            }
        }
    }

    private void CreateHorizontalCorridor(int x1, int x2, int y)
    {
        for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
        {
            for (int i = -corridorWidth / 2; i <= corridorWidth / 2; i++)
            {
                if (y + i >= 0 && y + i < height)
                {
                    dungeonMap[x, y + i] = 1;
                }
            }
        }
    }

    private void CreateVerticalCorridor(int y1, int y2, int x)
    {
        for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
        {
            for (int i = -corridorWidth / 2; i <= corridorWidth / 2; i++)
            {
                if (x + i >= 0 && x + i < width)
                {
                    dungeonMap[x + i, y] = 1;
                }
            }
        }
    }

    private void PlaceWalls()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (dungeonMap[x, y] == 0 && HasAdjacentFloor(x, y))
                {
                    dungeonMap[x, y] = 2;
                }
            }
        }
    }

    private bool HasAdjacentFloor(int x, int y)
    {
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;
                int checkX = x + i;
                int checkY = y + j;
                if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
                {
                    if (dungeonMap[checkX, checkY] == 1)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void PlaceEntryAndExit()
    {
        Rect entryRoom = rooms[0];
        Rect exitRoom = rooms[rooms.Count - 1];

        int entryX = (int)entryRoom.center.x;
        int entryY = (int)entryRoom.center.y;
        int exitX = (int)exitRoom.center.x;
        int exitY = (int)exitRoom.center.y;

        dungeonMap[entryX, entryY] = 3;
        dungeonMap[exitX, exitY] = 4;
    }

    public void UpdateVisibility(Vector2 playerPosition, float visibilityRadius)
    {
        int playerX = Mathf.RoundToInt(playerPosition.x);
        int playerY = Mathf.RoundToInt(playerPosition.y);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), playerPosition);
                float normalizedDistance = Mathf.Clamp01(distance / visibilityRadius);

                // Torch-like effect with orange tint
                float torchIntensity = Mathf.Pow(1 - Mathf.Clamp01(distance / torchRadius), lightFalloffExponent);

                // Revealed area darkness
                float revealedDarkness = Mathf.Lerp(1f, revealedDarknessMultiplier, normalizedDistance);

                // Combine torch and revealed darkness, ensuring a minimum visibility for revealed tiles
                float newLightIntensity = Mathf.Max(torchIntensity, revealedDarkness);

                if (distance <= visibilityRadius)
                {
                    revealedTiles[x, y] = true;
                }

                if (dungeonTiles[x, y] != null)
                {
                    if (revealedTiles[x, y])
                    {
                        dungeonTiles[x, y].SetActive(true);
                        Renderer tileRenderer = dungeonTiles[x, y].GetComponent<Renderer>();
                        if (tileRenderer != null)
                        {
                            // Ensure a minimum visibility for revealed tiles
                            float minVisibility = 0.2f; // Adjust this value to control the darkness of revealed areas
                            float finalLightIntensity = Mathf.Max(newLightIntensity, minVisibility);

                            tileRenderer.material.SetFloat("_LightIntensity", finalLightIntensity);
                            tileRenderer.material.SetFloat("_TorchIntensity", torchIntensity);
                            tileRenderer.material.SetColor("_TorchColor", torchColor);
                        }
                    }
                    else
                    {
                        dungeonTiles[x, y].SetActive(false);
                    }
                }
            }
        }
        OnVisibilityUpdated?.Invoke();
    }

    private GameObject[,] dungeonTiles;

    private void InstantiateDungeonTiles()
    {
        dungeonTiles = new GameObject[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(x, y, 0);
                GameObject tileObject = null;
                Color tileColor = Color.white;

                switch (dungeonMap[x, y])
                {
                    case 1: // Floor
                        tileObject = Instantiate(floorTilePrefab, position, Quaternion.identity);
                        tileColor = floorColor;
                        tileObject.GetComponent<SpriteRenderer>().sortingOrder = 0;
                        break;
                    case 2: // Wall
                        tileObject = Instantiate(wallTilePrefab, position, Quaternion.identity);
                        tileColor = wallColor;
                        tileObject.GetComponent<SpriteRenderer>().sortingOrder = 1;
                        break;
                    case 3: // Entry
                        tileObject = Instantiate(entryTilePrefab, position, Quaternion.identity);
                        tileColor = entryColor;
                        tileObject.GetComponent<SpriteRenderer>().sortingOrder = 0;
                        break;
                    case 4: // Exit
                        tileObject = Instantiate(exitTilePrefab, position, Quaternion.identity);
                        tileColor = exitColor;
                        tileObject.GetComponent<SpriteRenderer>().sortingOrder = 0;
                        break;
                }

                if (tileObject != null)
                {
                    tileObject.transform.SetParent(transform);
                    dungeonTiles[x, y] = tileObject;
                    SetTileColor(tileObject, tileColor);
                }
            }
        }
    }

    public bool IsTileVisible(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height && revealedTiles[x, y];
    }

    public event System.Action OnVisibilityUpdated;

    private void SetTileColor(GameObject tileObject, Color color)
    {
        Renderer tileRenderer = tileObject.GetComponent<Renderer>();
        if (tileRenderer != null)
        {
            Material tileMaterial = new Material(fogMaterial);
            tileMaterial.SetColor("_TileColor", color);
            tileMaterial.SetFloat("_LightIntensity", 1f);
            tileRenderer.material = tileMaterial;
        }
    }

    public Vector2 GetPlayerStartPosition()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (dungeonMap[x, y] == 3)
                {
                    return new Vector2(x, y);
                }
            }
        }
        return Vector2.zero;
    }

    public List<Vector2> GetFloorTilePositions()
    {
        List<Vector2> floorTiles = new List<Vector2>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (dungeonMap[x, y] == 1)
                {
                    floorTiles.Add(new Vector2(x, y));
                }
            }
        }
        return floorTiles;
    }
    public float GetRevealedPercentage()
    {
        int revealedCount = 0;
        int totalTiles = width * height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (revealedTiles[x, y])
                {
                    revealedCount++;
                }
            }
        }

        return (float)revealedCount / totalTiles;
    }

}
