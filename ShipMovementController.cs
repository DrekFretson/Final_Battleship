using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ShipMovementController : MonoBehaviour
{
    [Header("Ńńűëęč")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Camera gameCamera;

    [Header("Íŕńňđîéęč öâĺňîâ")]
    [SerializeField] private Color activeShipColor = Color.blue;
    [SerializeField] private Color movedShipColor = new Color(0f, 1f, 1f);

    private List<Ship> allShips = new List<Ship>();
    private Ship selectedShip = null;
    private bool movementModeActive = false;
    private List<Vector2Int> lastHighlightedCells = new List<Vector2Int>();
    private Player currentPlayer;

    void Start()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (gameCamera == null) gameCamera = Camera.main;
        movementModeActive = false;
    }

    void Update()
    {
        if (!movementModeActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            TrySelectShip();
        }

        if (Input.GetKeyDown(KeyCode.S) && selectedShip != null)
        {
            MoveSelectedShipForward();
        }

        if (Input.GetKeyDown(KeyCode.W) && selectedShip != null)
        {
            MoveSelectedShipBackward();
        }

        if (Input.GetKeyDown(KeyCode.A) && selectedShip != null)
        {
            RotateSelectedShipLeft();
        }

        if (Input.GetKeyDown(KeyCode.D) && selectedShip != null)
        {
            RotateSelectedShipRight();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && selectedShip != null)
        {
            DeselectShip();
        }
    }

    void TrySelectShip()
    {
        Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Ship ship = hit.collider.GetComponentInParent<Ship>();
            if (ship != null && ship.isPlaced && ship.owner == currentPlayer)
            {
                if (ship.isSunk || ship.health <= 0)
                {
                    Debug.Log($"{ship.shipName} ďîňîďëĺí, âűáîđ íĺâîçěîćĺí");
                    return;
                }

                SelectShip(ship);
                return;
            }

            Vector3 localHit = gridManager.transform.InverseTransformPoint(hit.point);
            float cellSize = gridManager.GetCellSpacing();

            int gridX = Mathf.FloorToInt((localHit.x + cellSize / 2f) / cellSize);
            int gridY = Mathf.FloorToInt((localHit.z + cellSize / 2f) / cellSize);

            foreach (Ship s in allShips)
            {
                if (s.isPlaced && s.owner == currentPlayer)
                {
                    if (s.isSunk || s.health <= 0) continue;

                    List<Vector2Int> cells = s.GetAllCells();
                    foreach (Vector2Int cell in cells)
                    {
                        if (cell.x == gridX && cell.y == gridY)
                        {
                            SelectShip(s);
                            return;
                        }
                    }
                }
            }
        }
    }

    void SelectShip(Ship ship)
    {
        if (ship.isSunk || ship.health <= 0)
        {
            Debug.Log($"Íĺëüç˙ âűáđŕňü ďîňîďëĺííűé ęîđŕáëü {ship.shipName}");
            return;
        }

        if (selectedShip != null && selectedShip != ship)
        {
            DeselectShip();
        }

        selectedShip = ship;
        selectedShip.SetSelected(true);

        lastHighlightedCells = new List<Vector2Int>(ship.GetAllCells());
        HighlightShipCells(ship, activeShipColor);

        Debug.Log($"Âűáđŕí ęîđŕáëü: {ship.shipName}, íŕďđŕâëĺíčĺ: {ship.GetDirectionName()}");
    }

    void DeselectShip()
    {
        if (selectedShip != null)
        {
            selectedShip.SetSelected(false);
            ClearLastHighlightedCells();

            if (selectedShip.hasMoved)
            {
                HighlightShipCells(selectedShip, movedShipColor);
            }

            Debug.Log($"Ńí˙ňî âűäĺëĺíčĺ ń ęîđŕáë˙: {selectedShip.shipName}");
            selectedShip = null;
            lastHighlightedCells.Clear();
        }
    }

    void HighlightShipCells(Ship ship, Color color)
    {
        List<Vector2Int> cells = ship.GetAllCells();
        foreach (Vector2Int cell in cells)
        {
            gridManager.HighlightCellColor(cell.x, cell.y, color);
        }
    }

    void ClearLastHighlightedCells()
    {
        foreach (Vector2Int cell in lastHighlightedCells)
        {
            gridManager.HighlightCell(cell.x, cell.y, false);
        }
    }

    void UpdateAllShipsHighlight()
    {
        gridManager.ResetAllHighlights();

        foreach (Ship ship in allShips)
        {
            if (ship.isPlaced && ship.owner == currentPlayer)
            {
                if (ship == selectedShip)
                {
                    HighlightShipCells(ship, activeShipColor);
                }
                else if (ship.hasMoved)
                {
                    HighlightShipCells(ship, movedShipColor);
                }
            }
        }
    }

    void MoveSelectedShipForward()
    {
        if (selectedShip == null) return;

        List<Ship> playerShips = GetCurrentPlayerShips();
        bool moved = selectedShip.MoveForward(gridManager, playerShips);

        if (moved)
        {
            UpdateAllShipsHighlight();
            Debug.Log($"{selectedShip.shipName} ńäâčíóň íŕçŕä");
        }
        else
        {
            Debug.Log($"Íĺëüç˙ ńäâčíóňü {selectedShip.shipName} íŕçŕä");
        }
    }

    void MoveSelectedShipBackward()
    {
        if (selectedShip == null) return;

        List<Ship> playerShips = GetCurrentPlayerShips();
        bool moved = selectedShip.MoveBackward(gridManager, playerShips);

        if (moved)
        {
            UpdateAllShipsHighlight();
            Debug.Log($"{selectedShip.shipName} ńäâčíóň âďĺđĺä");
        }
        else
        {
            Debug.Log($"Íĺëüç˙ ńäâčíóňü {selectedShip.shipName} âďĺđĺä");
        }
    }

    void RotateSelectedShipLeft()
    {
        if (selectedShip == null) return;

        List<Ship> playerShips = GetCurrentPlayerShips();
        bool rotated = selectedShip.RotateLeft(gridManager, playerShips);

        if (rotated)
        {
            UpdateAllShipsHighlight();
            Debug.Log($"{selectedShip.shipName} ďîâĺđíóň íŕëĺâî");
        }
        else
        {
            Debug.Log($"Íĺëüç˙ ďîâĺđíóňü {selectedShip.shipName} íŕëĺâî");
        }
    }

    void RotateSelectedShipRight()
    {
        if (selectedShip == null) return;

        List<Ship> playerShips = GetCurrentPlayerShips();
        bool rotated = selectedShip.RotateRight(gridManager, playerShips);

        if (rotated)
        {
            UpdateAllShipsHighlight();
            Debug.Log($"{selectedShip.shipName} ďîâĺđíóň íŕďđŕâî");
        }
        else
        {
            Debug.Log($"Íĺëüç˙ ďîâĺđíóňü {selectedShip.shipName} íŕďđŕâî");
        }
    }

    private List<Ship> GetCurrentPlayerShips()
    {
        return allShips.Where(s => s.owner == currentPlayer).ToList();
    }

    public void EnableMovementMode(List<Ship> ships, Player player)
    {
        allShips = ships;
        currentPlayer = player;
        movementModeActive = true;

        foreach (Ship ship in allShips)
        {
            ship.hasMoved = false;
        }

        gridManager.ResetAllHighlights();
        Debug.Log($"Đĺćčě ďĺđĺěĺůĺíč˙ ŕęňčâčđîâŕí äë˙ {player.playerName}");
    }

    public void EnableMovementMode(List<Ship> ships)
    {
        EnableMovementMode(ships, null);
    }

    public void DisableMovementMode()
    {
        movementModeActive = false;

        if (selectedShip != null)
        {
            DeselectShip();
        }

        gridManager.ResetAllHighlights();
    }

}
