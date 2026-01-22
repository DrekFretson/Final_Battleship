using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ShipMovementController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Camera gameCamera;

    [Header("Настройки цветов")]
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

    // попробовать выбрать корабль
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
                    Debug.Log($"{ship.shipName} потоплен, выбор невозможен");
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

    // выбрать корабль
    void SelectShip(Ship ship)
    {
        if (ship.isSunk || ship.health <= 0)
        {
            Debug.Log($"Нельзя выбрать потопленный корабль {ship.shipName}");
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

        Debug.Log($"Выбран корабль: {ship.shipName}, направление: {ship.GetDirectionName()}");
    }

    // снять выделение с корабля
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

            Debug.Log($"Снято выделение с корабля: {selectedShip.shipName}");
            selectedShip = null;
            lastHighlightedCells.Clear();
        }
    }

    // подсветить клетки корабля определенным цветом
    void HighlightShipCells(Ship ship, Color color)
    {
        List<Vector2Int> cells = ship.GetAllCells();
        foreach (Vector2Int cell in cells)
        {
            gridManager.HighlightCellColor(cell.x, cell.y, color);
        }
    }

    // убрать подсветку с последних подсвеченных клеток
    void ClearLastHighlightedCells()
    {
        foreach (Vector2Int cell in lastHighlightedCells)
        {
            gridManager.HighlightCell(cell.x, cell.y, false);
        }
    }

    // обновить подсветку всех кораблей
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

    // двинуть выбранный корабль вперед
    void MoveSelectedShipForward()
    {
        if (selectedShip == null) return;

        List<Ship> playerShips = GetCurrentPlayerShips();
        bool moved = selectedShip.MoveForward(gridManager, playerShips);

        if (moved)
        {
            UpdateAllShipsHighlight();
            Debug.Log($"{selectedShip.shipName} сдвинут назад");
        }
        else
        {
            Debug.Log($"Нельзя сдвинуть {selectedShip.shipName} назад");
        }
    }

    // двинуть выбранный корабль назад
    void MoveSelectedShipBackward()
    {
        if (selectedShip == null) return;

        List<Ship> playerShips = GetCurrentPlayerShips();
        bool moved = selectedShip.MoveBackward(gridManager, playerShips);

        if (moved)
        {
            UpdateAllShipsHighlight();
            Debug.Log($"{selectedShip.shipName} сдвинут вперед");
        }
        else
        {
            Debug.Log($"Нельзя сдвинуть {selectedShip.shipName} вперед");
        }
    }

    // повернуть выбранный корабль налево
    void RotateSelectedShipLeft()
    {
        if (selectedShip == null) return;

        List<Ship> playerShips = GetCurrentPlayerShips();
        bool rotated = selectedShip.RotateLeft(gridManager, playerShips);

        if (rotated)
        {
            UpdateAllShipsHighlight();
            Debug.Log($"{selectedShip.shipName} повернут налево");
        }
        else
        {
            Debug.Log($"Нельзя повернуть {selectedShip.shipName} налево");
        }
    }

    // повернуть выбранный корабль направо
    void RotateSelectedShipRight()
    {
        if (selectedShip == null) return;

        List<Ship> playerShips = GetCurrentPlayerShips();
        bool rotated = selectedShip.RotateRight(gridManager, playerShips);

        if (rotated)
        {
            UpdateAllShipsHighlight();
            Debug.Log($"{selectedShip.shipName} повернут направо");
        }
        else
        {
            Debug.Log($"Нельзя повернуть {selectedShip.shipName} направо");
        }
    }

    // получить корабли текущего игрока
    private List<Ship> GetCurrentPlayerShips()
    {
        return allShips.Where(s => s.owner == currentPlayer).ToList();
    }

    // включить режим перемещения
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
        Debug.Log($"Режим перемещения активирован для {player.playerName}");
    }


    // отключить режим перемещения
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
