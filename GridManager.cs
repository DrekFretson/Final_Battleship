using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Íàñòðîéêè ñåòêè")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private int gridSize = 10;
    [SerializeField] private float cellSpacing = 1.1f;

    [Header("Öâåòà êëåòîê (îðèãèíàëüíûå)")]
    [SerializeField] private Color lightCellColor = Color.white;
    [SerializeField] private Color darkCellColor = Color.gray;

    [Header("Öâåòà âûñòðåëîâ")]
    [SerializeField] private Color currentTurnShotColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    [SerializeField] private Color previousTurnShotColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);

    [Header("Íàñòðîéêè ãðàíèöû")]
    [SerializeField] private bool createBorder = true;
    public Material borderMaterial;
    [SerializeField] private float borderThickness = 0.3f;
    [SerializeField] private float borderHeight = 0.5f;

    [Header("Îòëàäêà")]
    [SerializeField] private bool showGridInEditor = true;

    private Cell[,] gridCells;
    private GameObject gridParent;

    private List<Vector2Int> currentTurnShots = new List<Vector2Int>();
    private List<Vector2Int> previousTurnShots = new List<Vector2Int>();

    void Start()
    {
        InitializeGrid();

        if (createBorder)
        {
            CreateBorder();
        }

        Debug.Log($"Ñåòêà {gridSize}x{gridSize} ñîçäàíà óñïåøíî!");
    }

    void InitializeGrid()
    {
        gridParent = new GameObject("GridCells");
        gridParent.transform.SetParent(transform);
        gridParent.transform.localPosition = Vector3.zero;

        gridCells = new Cell[gridSize, gridSize];
        CreateGrid();
    }

    void CreateGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector3 position = new Vector3(
                    x * cellSpacing,
                    0,
                    y * cellSpacing
                );

                GameObject cellGO = Instantiate(cellPrefab, position, Quaternion.identity);
                cellGO.transform.SetParent(gridParent.transform);
                cellGO.transform.localPosition = position;

                Cell cellComponent = cellGO.GetComponent<Cell>();
                if (cellComponent != null)
                {
                    cellComponent.Init(x, y);
                    gridCells[x, y] = cellComponent;
                }

                cellGO.name = $"Cell_{x}_{y}";

                SetupCellColor(cellGO, x, y);
            }
        }
    }

    void SetupCellColor(GameObject cellGO, int x, int y)
    {
        Renderer renderer = cellGO.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(renderer.material);

            bool isDark = (x + y) % 2 == 0;
            Color cellColor = isDark ? darkCellColor : lightCellColor;
            renderer.material.color = cellColor;
        }
    }

    void CreateBorder()
    {
        if (!createBorder) return;

        GameObject borderParent = new GameObject("Border");
        borderParent.transform.SetParent(transform);
        borderParent.transform.localPosition = Vector3.zero;

        float totalSize = (gridSize - 1) * cellSpacing;
        float borderOffset = 0.6f;

        CreateBorderPiece(borderParent, "TopBorder",
            new Vector3(totalSize / 2, 0, totalSize + borderThickness / 2 + borderOffset),
            new Vector3(totalSize + borderThickness * 2 + borderOffset * 2, borderHeight, borderThickness),
            borderMaterial);

        CreateBorderPiece(borderParent, "BottomBorder",
            new Vector3(totalSize / 2, 0, -borderThickness / 2 - borderOffset),
            new Vector3(totalSize + borderThickness * 2 + borderOffset * 2, borderHeight, borderThickness),
            borderMaterial);

        CreateBorderPiece(borderParent, "LeftBorder",
            new Vector3(-borderThickness / 2 - borderOffset, 0, totalSize / 2),
            new Vector3(borderThickness, borderHeight, totalSize + borderOffset * 2),
            borderMaterial);

        CreateBorderPiece(borderParent, "RightBorder",
            new Vector3(totalSize + borderThickness / 2 + borderOffset, 0, totalSize / 2),
            new Vector3(borderThickness, borderHeight, totalSize + borderOffset * 2),
            borderMaterial);
    }

    void CreateBorderPiece(GameObject parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject borderPiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        borderPiece.name = name;
        borderPiece.transform.SetParent(parent.transform);
        borderPiece.transform.localPosition = position;
        borderPiece.transform.localScale = scale;

        Destroy(borderPiece.GetComponent<Collider>());

        Renderer renderer = borderPiece.GetComponent<Renderer>();

        if (material != null)
        {
            renderer.material = material;
        }
        else
        {
            Debug.LogWarning($"Material äëÿ ãðàíèöû íå íàçíà÷åí! Ñîçäàåì âðåìåííûé.");

            Material fallbackMat = new Material(Shader.Find("Mobile/Diffuse"));
            fallbackMat.color = new Color(0.65f, 0.5f, 0.35f);
            renderer.material = fallbackMat;
        }
    }

    public Cell GetCell(int x, int y)
    {
        if (x >= 0 && x < gridSize && y >= 0 && y < gridSize)
        {
            return gridCells[x, y];
        }
        return null;
    }

    public int GetGridSize()
    {
        return gridSize;
    }

    public float GetCellSpacing()
    {
        return cellSpacing;
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * cellSpacing, 0, y * cellSpacing);
    }


    public void HighlightCell(int x, int y, bool highlight, Color color)
    {
        Cell cell = GetCell(x, y);
        if (cell == null) return;

        Renderer renderer = cell.GetComponent<Renderer>();
        if (renderer == null) return;

        if (highlight)
        {
            Color highlightColor = color;
            highlightColor.a = 0.7f;
            renderer.material.color = highlightColor;
        }
        else
        {
            RestoreCellToBaseState(x, y);
        }
    }

    public void HighlightCell(int x, int y, bool highlight)
    {
        HighlightCell(x, y, highlight, Color.yellow);
    }

    public void HighlightCellColor(int x, int y, Color color)
    {
        Cell cell = GetCell(x, y);
        if (cell == null) return;

        Renderer renderer = cell.GetComponent<Renderer>();
        if (renderer == null) return;

        color.a = 0.7f;
        renderer.material.color = color;
    }

    public void HighlightCellRed(int x, int y)
    {
        HighlightCellColor(x, y, new Color(1f, 0f, 0f, 0.9f));
    }

    public void HighlightCellGreen(int x, int y)
    {
        HighlightCellColor(x, y, new Color(0.3f, 1f, 0.3f, 0.8f));
    }

    public void RestoreCellToBaseState(int x, int y)
    {
        Cell cell = GetCell(x, y);
        if (cell == null)
        {
            Debug.LogError($"RestoreCellToBaseState: Íå íàéäåí Cell [{x},{y}]");
            return;
        }

        Renderer renderer = cell.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError($"RestoreCellToBaseState: Íåò Renderer ó Cell [{x},{y}]");
            return;
        }

        Vector2Int key = new Vector2Int(x, y);

        bool isCurrent = currentTurnShots.Contains(key);
        bool isPrevious = previousTurnShots.Contains(key);

        if (isCurrent)
        {
            renderer.material.color = currentTurnShotColor;
            Debug.Log($"RestoreCellToBaseState: [{x},{y}] -> currentTurnShotColor (â ñïèñêå currentTurnShots)");
        }
        else if (isPrevious)
        {
            renderer.material.color = previousTurnShotColor;
            Debug.Log($"RestoreCellToBaseState: [{x},{y}] -> previousTurnShotColor (â ñïèñêå previousTurnShots)");
        }
        else
        {
            bool isDark = (x + y) % 2 == 0;
            Color baseColor = isDark ? darkCellColor : lightCellColor;
            renderer.material.color = baseColor;
            Debug.Log($"RestoreCellToBaseState: [{x},{y}] -> áàçîâûé öâåò");
        }
    }

    public void ResetAllHighlights()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                RestoreCellToBaseState(x, y);
            }
        }
    }


    public void AddShot(Vector2Int cell)
    {
        Debug.Log($"=== AddShot [{cell.x},{cell.y}] ===");

        if (!currentTurnShots.Contains(cell))
        {
            currentTurnShots.Add(cell);
            Debug.Log($"  Äîáàâëåí â currentTurnShots");
        }

        Cell cellObj = GetCell(cell.x, cell.y);
        if (cellObj != null)
        {
            Renderer renderer = cellObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = currentTurnShotColor;
                Debug.Log($"  Óñòàíîâëåí currentTurnShotColor");
            }
        }

        if (previousTurnShots.Contains(cell))
        {
            previousTurnShots.Remove(cell);
            Debug.Log($"  Óäàëåí èç previousTurnShots");
        }

        Debug.Log($"Èòîã: current ñîäåðæèò? {currentTurnShots.Contains(cell)}");
        Debug.Log($"Èòîã: previous ñîäåðæèò? {previousTurnShots.Contains(cell)}");
        Debug.Log($"=== Êîíåö AddShot ===");
    }

    public bool WasShotThisTurn(Vector2Int cell)
    {
        return currentTurnShots.Contains(cell);
    }

    public bool WasShotPreviousTurn(Vector2Int cell)
    {
        return previousTurnShots.Contains(cell);
    }

    public bool WasShot(Vector2Int cell)
    {
        return currentTurnShots.Contains(cell) || previousTurnShots.Contains(cell);
    }

    public void NextTurn()
    {
        Debug.Log("=== ÍÀ×ÀËÎ NextTurn() ===");
        Debug.Log($"Äî ïåðåõîäà: currentTurnShots = {currentTurnShots.Count}, previousTurnShots = {previousTurnShots.Count}");

        List<Vector2Int> shotsToMove = new List<Vector2Int>(currentTurnShots);

        currentTurnShots.Clear();

        previousTurnShots = shotsToMove;

        Debug.Log($"Ïîñëå ïåðåìåùåíèÿ: currentTurnShots = {currentTurnShots.Count}, previousTurnShots = {previousTurnShots.Count}");

        foreach (Vector2Int cell in previousTurnShots)
        {
            Cell cellObj = GetCell(cell.x, cell.y);
            if (cellObj != null)
            {
                Renderer renderer = cellObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = previousTurnShotColor;
                    Debug.Log($"NextTurn: Êëåòêà [{cell.x},{cell.y}] òåïåðü previousTurnShotColor");
                }
            }
            else
            {
                Debug.LogError($"NextTurn: Íå íàéäåí Cell äëÿ [{cell.x},{cell.y}]");
            }
        }

        Debug.Log("=== ÊÎÍÅÖ NextTurn() ===");
    }

    public void ClearAllShots()
    {
        currentTurnShots.Clear();
        previousTurnShots.Clear();
        ResetAllHighlights();

        Debug.Log("Âñå âûñòðåëû î÷èùåíû");
    }

    void OnDrawGizmos()
    {
        if (!showGridInEditor || !Application.isPlaying) return;

        Gizmos.color = Color.green;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Cell cell = GetCell(x, y);
                if (cell != null)
                {
                    Vector3 center = cell.transform.position;
                    Gizmos.DrawWireCube(center, Vector3.one * (cellSpacing * 0.8f));
                }
            }
        }
    }

}
