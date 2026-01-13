using UnityEngine;
using System.Collections.Generic;

public class ShipPlacer : MonoBehaviour
{
    [Header("Ññûëêè")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Camera placementCamera;
    [SerializeField] private ShipMovementController shipMovementController;
    [SerializeField] private Player player;

    [Header("Êîðàáëè äëÿ ðàññòàíîâêè")]
    [SerializeField] private List<Ship> shipsToPlace = new List<Ship>();

    private Ship currentlyPlacingShip;
    private int currentShipIndex = 0;
    private bool isPlacing = false;
    private List<Vector2Int> lastHighlightedCells = new List<Vector2Int>();

    void Start()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (placementCamera == null) placementCamera = Camera.main;
        if (shipMovementController == null) shipMovementController = FindObjectOfType<ShipMovementController>();
        enabled = false;

        Debug.Log($"ShipPlacer {player.playerName} ãîòîâ, îæèäàåò êîìàíäû");
    }

    public void BeginPlacement()
    {
        if (shipsToPlace.Count == 0) return;

        enabled = true;
        currentShipIndex = 0;
        BeginPlaceShip(shipsToPlace[0]);

        Debug.Log($"ShipPlacer {player.playerName} íà÷àë ðàññòàíîâêó");
    }


    public void StartShipPlacement()
    {
        if (player == null)
        {
            Debug.LogError("Player íå íàçíà÷åí!");
            return;
        }

        enabled = true;
        InitializeShips();
        StartPlacing();

        Debug.Log($"Íà÷àòà ðàññòàíîâêà êîðàáëåé äëÿ {player.playerName}");
    }

    public void StopShipPlacement()
    {
        enabled = false;
        isPlacing = false;
        currentlyPlacingShip = null;
        ClearAllHighlights();

        Debug.Log($"Îñòàíîâëåíà ðàññòàíîâêà êîðàáëåé äëÿ {player.playerName}");
    }

    void InitializeShips()
    {
        foreach (Ship ship in shipsToPlace)
        {
            ship.gameObject.SetActive(false);
        }
    }

    void StartPlacing()
    {
        if (shipsToPlace.Count == 0) return;

        currentShipIndex = 0;
        BeginPlaceShip(shipsToPlace[0]);
    }

    void BeginPlaceShip(Ship ship)
    {
        ClearAllHighlights();

        currentlyPlacingShip = ship;
        isPlacing = true;
        ship.gameObject.SetActive(true);

        ship.gridPosition = new Vector2Int(
            gridManager.GetGridSize() / 2,
            gridManager.GetGridSize() / 2
        );

        Debug.Log($"Ðàçìåùàåì {ship.shipName}");
    }

    void Update()
    {
        if (!isPlacing || currentlyPlacingShip == null) return;

        UpdateShipPosition();
        UpdateShipPreview();

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.R))
        {
            currentlyPlacingShip.Rotate90();
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceCurrentShip();
        }
    }

    void UpdateShipPosition()
    {
        Ray ray = placementCamera.ScreenPointToRay(Input.mousePosition);
        Plane gridPlane = new Plane(Vector3.up, Vector3.zero);

        float enter;
        if (gridPlane.Raycast(ray, out enter))
        {
            Vector3 worldPoint = ray.GetPoint(enter);
            Vector3 localPoint = gridManager.transform.InverseTransformPoint(worldPoint);

            float cellSize = gridManager.GetCellSpacing();

            float offsetX = localPoint.x + cellSize / 2f;
            float offsetZ = localPoint.z + cellSize / 2f;

            int gridX = Mathf.FloorToInt(offsetX / cellSize);
            int gridY = Mathf.FloorToInt(offsetZ / cellSize);

            Vector2Int gridPos = new Vector2Int(gridX, gridY);

            int maxX = gridManager.GetGridSize() - 1;
            int maxY = gridManager.GetGridSize() - 1;
            int minX = 0;
            int minY = 0;

            switch (currentlyPlacingShip.direction)
            {
                case 0:
                    minX = currentlyPlacingShip.length - 1;
                    break;
                case 1:
                    minY = currentlyPlacingShip.length - 1;
                    break;
                case 2:
                    maxX = gridManager.GetGridSize() - currentlyPlacingShip.length;
                    break;
                case 3:
                    maxY = gridManager.GetGridSize() - currentlyPlacingShip.length;
                    break;
            }

            gridPos.x = Mathf.Clamp(gridPos.x, minX, maxX);
            gridPos.y = Mathf.Clamp(gridPos.y, minY, maxY);

            currentlyPlacingShip.gridPosition = gridPos;
        }
    }

    void UpdateShipPreview()
    {
        List<Vector2Int> newCells = currentlyPlacingShip.GetOccupiedCells();

        List<Ship> placedShips = GetPlacedShips();
        List<Ship> sameOwnerPlacedShips = placedShips.FindAll(s => s.owner == this.player);

        List<bool> cellValidStatus = currentlyPlacingShip.GetCellValidStatus(
            gridManager,
            GetPlacedShips()
        );

        foreach (Vector2Int oldCell in lastHighlightedCells)
        {
            if (!newCells.Contains(oldCell))
            {
                gridManager.HighlightCell(oldCell.x, oldCell.y, false);
            }
        }

        bool canPlaceWholeShip = true;
        foreach (bool isValid in cellValidStatus)
        {
            if (!isValid)
            {
                canPlaceWholeShip = false;
                break;
            }
        }

        currentlyPlacingShip.UpdateVisualPosition(gridManager);
        currentlyPlacingShip.SetPlacementVisual(canPlaceWholeShip);

        for (int i = 0; i < newCells.Count; i++)
        {
            Vector2Int cell = newCells[i];
            bool isValid = cellValidStatus[i];

            if (isValid)
            {
                gridManager.HighlightCellGreen(cell.x, cell.y);
            }
        }

        lastHighlightedCells = new List<Vector2Int>(newCells);
    }

    void TryPlaceCurrentShip()
    {
        List<Ship> placedShips = GetPlacedShips();
        List<Ship> sameOwnerPlacedShips = placedShips.FindAll(s => s.owner == this.player);

        bool canPlace = currentlyPlacingShip.CanPlaceAt(
            currentlyPlacingShip.gridPosition,
            currentlyPlacingShip.direction,
            gridManager,
            GetPlacedShips()
        );

        if (canPlace)
        {
            PlaceCurrentShip();
        }
    }

    void PlaceCurrentShip()
    {
        if (player != null && currentlyPlacingShip.owner == null)
        {
            currentlyPlacingShip.SetOwner(player);
        }

        List<Ship> placedShips = GetPlacedShips();
        List<Ship> sameOwnerPlacedShips = placedShips.FindAll(s => s.owner == this.player);

        currentlyPlacingShip.PlaceShip(gridManager, GetPlacedShips());
        ClearAllHighlights();

        currentShipIndex++;

        if (currentShipIndex < shipsToPlace.Count)
        {
            BeginPlaceShip(shipsToPlace[currentShipIndex]);
        }
        else
        {
            FinishPlacing();
        }
    }

    List<Ship> GetPlacedShips()
    {
        List<Ship> placedShips = new List<Ship>();
        for (int i = 0; i < currentShipIndex; i++)
        {
            placedShips.Add(shipsToPlace[i]);
        }
        return placedShips;
    }

    void FinishPlacing()
    {
        isPlacing = false;
        currentlyPlacingShip = null;

        enabled = false;

        Debug.Log($"ShipPlacer {player.playerName} çàâåðøèë ðàññòàíîâêó");

        if (shipMovementController != null && player != null)
        {
            shipMovementController.EnableMovementMode(shipsToPlace, player);
        }
        else
        {
            Debug.LogError("ShipMovementController èëè Player íå íàçíà÷åíû!");
        }
    }

    void ClearAllHighlights()
    {
        foreach (Vector2Int cell in lastHighlightedCells)
        {
            gridManager.HighlightCell(cell.x, cell.y, false);
        }
        lastHighlightedCells.Clear();
    }

}
