using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    public int width = 50;
    public int height = 50;
    public int minRoomSize = 5;
    public int maxRoomSize = 15;
    public int roomAttempts = 30;
    public int corridorWidth = 3;
    public GameObject floorTilePrefab;
    public GameObject wallTilePrefab;
    public GameObject entryTilePrefab;
    public GameObject exitTilePrefab;
    public Color floorColor = Color.white;
    public Color wallColor = Color.gray;
    public Color entryColor = Color.green;
    public Color exitColor = Color.red;

    private int[,] dungeonMap;
    private bool[,] revealedTiles;
    private float[,] lightLevels;
    private GameObject[,] dungeonTiles;
    private List<Rect> rooms = new List<Rect>();
    private EnemyManager enemyManager;

    public void Initialize(EnemyManager manager)
    {
        enemyManager = manager;
    }

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

    private void ClearExistingDungeon()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    private void GenerateRandomColors()
    {
        floorColor = Random.ColorHSV();
        wallColor = Random.ColorHSV();
        entryColor = Random.ColorHSV();
        exitColor = Random.ColorHSV();
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
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && dungeonMap[nx, ny] == 1)
                {
                    return true;
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

    private void SetTileColor(GameObject tileObject, Color color)
    {
        SpriteRenderer renderer = tileObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = color;
        }
    }

    private void InitializeFogOfWar()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (dungeonMap[x, y] == 1)
                {
                    SetTileVisibility(x, y, false);
                }
            }
        }
    }

    private void SetTileVisibility(int x, int y, bool visible)
    {
        if (dungeonTiles[x, y] != null)
        {
            SpriteRenderer renderer = dungeonTiles[x, y].GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = visible ? 1f : 0.2f;
                renderer.color = color;
            }
        }
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
                bool visible = distance <= visibilityRadius;
                SetTileVisibility(x, y, visible);
                revealedTiles[x, y] = revealedTiles[x, y] || visible;
            }
        }
    }

    private Vector2 GetPlayerStartPosition()
    {
        Rect entryRoom = rooms[0];
        return new Vector2(entryRoom.center.x, entryRoom.center.y);
    }
}