using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BotPlayer : Player
{
    [Header("Настройки бота")]
    private List<Vector2Int> availableShots = new List<Vector2Int>();
    private List<Vector2Int> hitCells = new List<Vector2Int>();
    private Vector2Int lastHit = new Vector2Int(-1, -1);
    private bool isHuntingMode = false;

    // для движения кораблей бота
    private List<Ship> shipsThatCanMove = new List<Ship>();

    // для хранения запрещённых клеток
    private List<Vector2Int> forbiddenCells = new List<Vector2Int>();

    public new void Initialize(string name, GridManager grid)
    {
        base.Initialize(name, grid);
        InitializeBot();
    }

    void InitializeBot()
    {
        if (battleGrid == null)
        {
            Debug.LogError("Battle Grid не назначен для бота!");
            return;
        }

        // Генерация всех возможных выстрелов
        int gridSize = battleGrid.GetGridSize();
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                availableShots.Add(new Vector2Int(x, y));
            }
        }

        ShuffleShots();
    }

    // метод для случайной расстановки кораблей
    public void RandomPlaceShips()
    {
        foreach (Ship ship in ships)
        {
            PlaceShipRandomly(ship);
        }
    }

    void PlaceShipRandomly(Ship ship)
    {
        int maxAttempts = 100;
        GridManager myGrid = GetPlayerGrid();

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int x = Random.Range(0, myGrid.GetGridSize());
            int y = Random.Range(0, myGrid.GetGridSize());
            int direction = Random.Range(0, 4);

            if (ship.CanPlaceAt(new Vector2Int(x, y), direction, myGrid,
                ships.Where(s => s != ship && s.isPlaced).ToList()))
            {
                ship.gridPosition = new Vector2Int(x, y);
                ship.direction = direction;
                ship.PlaceShip(myGrid, ships.Where(s => s != ship && s.isPlaced).ToList());
                return;
            }
        }

        Debug.LogError($"Не удалось разместить {ship.shipName} после {maxAttempts} попыток");
    }

    // логика выстрела бота
    public Vector2Int MakeShot(Player opponentPlayer)
    {
        // обновляем запрещённые клетки перед выстрелом
        UpdateForbiddenCells(opponentPlayer);

        Vector2Int target;

        if (isHuntingMode && lastHit.x != -1)
        {
            target = GetHuntingShot();
        }
        else
        {
            target = GetRandomShot();
        }

        // если выбранная клетка запрещена - пробуем найти другую
        if (forbiddenCells.Contains(target))
        {
            Debug.LogWarning($"Бот: выбранная клетка [{target.x},{target.y}] запрещена, ищу другую");
            target = FindValidShot();
        }

        // удаляем выстрел из доступных
        if (availableShots.Contains(target))
        {
            availableShots.Remove(target);
        }

        return target;
    }

    // найти допустимый выстрел
    Vector2Int FindValidShot()
    {
        List<Vector2Int> validShots = new List<Vector2Int>();

        foreach (Vector2Int shot in availableShots)
        {
            if (!forbiddenCells.Contains(shot))
            {
                validShots.Add(shot);
            }
        }

        if (validShots.Count > 0)
        {
            return validShots[Random.Range(0, validShots.Count)];
        }

        if (availableShots.Count > 0)
        {
            Debug.LogWarning("Бот: все доступные выстрелы запрещены, выбираю случайный");
            return availableShots[Random.Range(0, availableShots.Count)];
        }

        return new Vector2Int(-1, -1);
    }

    Vector2Int GetRandomShot()
    {
        if (availableShots.Count == 0) return new Vector2Int(-1, -1);

        Vector2Int target;
        int attempts = 0;
        int maxAttempts = 100;

        do
        {
            target = availableShots[Random.Range(0, availableShots.Count)];
            attempts++;

            if (attempts >= maxAttempts)
            {
                Debug.LogWarning($"Бот: не нашёл допустимую клетку за {maxAttempts} попыток");
                return target;
            }
        }
        while (forbiddenCells.Contains(target) && attempts < maxAttempts);

        return target;
    }

    Vector2Int GetHuntingShot()
    {
        List<Vector2Int> possibleShots = new List<Vector2Int>();
        Vector2Int[] directions = {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int potentialTarget = lastHit + dir;
            if (availableShots.Contains(potentialTarget) && !forbiddenCells.Contains(potentialTarget))
            {
                possibleShots.Add(potentialTarget);
            }
        }

        if (possibleShots.Count > 0)
        {
            return possibleShots[Random.Range(0, possibleShots.Count)];
        }

        return GetRandomShot();
    }

    public void ProcessShotResult(Vector2Int target, bool hit)
    {
        if (hit)
        {
            hitCells.Add(target);
            lastHit = target;
            isHuntingMode = true;
        }
        else
        {
            isHuntingMode = false;
        }
    }

    // обновить запрещённые клетки
    void UpdateForbiddenCells(Player opponentPlayer)
    {
        forbiddenCells.Clear();

        if (opponentPlayer == null || opponentPlayer.ships == null)
        {
            Debug.LogWarning("Бот: opponentPlayer или его корабли равны null");
            return;
        }

        foreach (Ship ship in opponentPlayer.ships)
        {
            if (ship != null && ship.isSunk)
            {
                MarkForbiddenCellsAroundShip(ship);
            }
        }

        Debug.Log($"Бот: обновлены запрещённые клетки. Всего: {forbiddenCells.Count}");
    }

    // пометить клетки вокруг потопленного корабля как запрещённые
    void MarkForbiddenCellsAroundShip(Ship ship)
    {
        if (ship == null) return;

        List<Vector2Int> shipCells = ship.GetOccupiedCells();
        int gridSize = battleGrid != null ? battleGrid.GetGridSize() : 10;

        foreach (Vector2Int cell in shipCells)
        {
            if (!forbiddenCells.Contains(cell))
            {
                forbiddenCells.Add(cell);
            }

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int neighbor = new Vector2Int(cell.x + dx, cell.y + dy);

                    if (neighbor.x >= 0 && neighbor.x < gridSize &&
                        neighbor.y >= 0 && neighbor.y < gridSize)
                    {
                        if (!forbiddenCells.Contains(neighbor))
                        {
                            forbiddenCells.Add(neighbor);
                        }
                    }
                }
            }
        }
    }

    void ShuffleShots()
    {
        for (int i = 0; i < availableShots.Count; i++)
        {
            Vector2Int temp = availableShots[i];
            int randomIndex = Random.Range(i, availableShots.Count);
            availableShots[i] = availableShots[randomIndex];
            availableShots[randomIndex] = temp;
        }
    }

    // переопределяем методы
    public override void EnableShipPlacement()
    {
        RandomPlaceShips();
    }

    public override void EnableShipMovement()
    {
        Debug.Log("Бот двигает корабли");
        MoveBotShips();
    }

    public override void ShowAllShips()
    {
        foreach (Ship ship in ships)
        {
            ship.SetVisible(ship.isSunk);
        }
    }

    // бот двигает свои корабли
    public void MoveBotShips()
    {
        shipsThatCanMove.Clear();

        foreach (Ship ship in ships)
        {
            if (ship != null && !ship.isSunk && ship.isPlaced)
            {
                shipsThatCanMove.Add(ship);
                ship.hasActedThisTurn = false;
            }
        }

        Debug.Log($"Бот может двигать {shipsThatCanMove.Count} кораблей");

        foreach (Ship ship in shipsThatCanMove)
        {
            // 80% шанс что корабль попробует двигаться
            if (Random.Range(0, 100) < 80 && !ship.hasActedThisTurn)
            {
                TryMoveShip(ship);
            }
        }
    }

    void TryMoveShip(Ship ship)
    {
        if (ship == null || ship.isSunk) return;

        GridManager myGrid = GetPlayerGrid();
        if (myGrid == null) return;

        List<Ship> otherShips = ships.Where(s => s != ship && s.isPlaced && s.owner == this).ToList();

        // Выделяем корабль перед движением
        ship.SetSelected(true);
        ship.isSelected = true;

        // сбрасываем флаг действия
        ship.hasActedThisTurn = false;

        // 0 движение, 1 поворот
        bool shouldMove = Random.Range(0, 2) == 0;

        bool moved = false;

        if (shouldMove)
        {
            // 0 вперёд, 1 назад
            bool moveForward = Random.Range(0, 2) == 0;

            if (moveForward)
            {
                moved = ship.MoveForward(myGrid, otherShips);
            }
            else
            {
                moved = ship.MoveBackward(myGrid, otherShips);
            }
        }
        else
        {
            // 0 влево, 1 вправо
            bool rotateLeft = Random.Range(0, 2) == 0;

            if (rotateLeft)
            {
                moved = ship.RotateLeft(myGrid, otherShips);
            }
            else
            {
                moved = ship.RotateRight(myGrid, otherShips);
            }
        }

        ship.SetSelected(false);
        ship.isSelected = false;

        if (moved)
        {
            ship.hasActedThisTurn = true;
            ship.hasMoved = true;
            Debug.Log($"Бот: {ship.shipName} перемещён");
        }
        else
        {
            Debug.Log($"Бот: не удалось переместить {ship.shipName}");
        }
    }
}
