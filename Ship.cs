//Ship.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

[System.Serializable]
public class Ship : MonoBehaviour
{
    [Header("Настройки корабля")]
    public int length = 1; // 2, 3, 4, 5
    public string shipName;

    [Header("Текущее состояние")]
    public Vector2Int gridPosition; // Позиция носа корабля
    public int direction = 0; // 0 влево, 1 вниз, 2 вправо, 3 вверх
    public bool isPlaced = false;
    public bool isSelected = false;
    public bool hasMoved = false; // Новое: двигался ли корабль хотя бы раз

    [Header("Боевая система")]
    public int health = 1; // Здоровье корабля
    public int maxHealth = 1; // Максимальное здоровье
    public bool isSunk = false; // Потоплен ли корабль
    public bool isVisible = true; // Видим ли корабль (для противника)

    [Header("Визуал")]
    public GameObject shipModel; // Ссылка на 3D модель
    public Material defaultMaterial;
    public Material placementValidMaterial;
    public Material placementInvalidMaterial;
    public Material selectedMaterial;
    public Material damagedMaterial; // Материал для поврежденного корабля
    public Material sunkMaterial; // Материал для потопленного корабля

    [Header("Эффекты")]
    public GameObject sinkingEffect; // Эффект потопления

    [Header("Владелец")]
    public Player owner;

    [Header("Действия за ход")]
    public bool hasActedThisTurn = false; // Совершил ли действие в этом ходу

    [Header("Визуализация потопления")]
    public bool isRevealedToOpponent = false; // Показан ли противнику

    [Header("Анимация потопления")]
    public float sinkAnimationSpeed = 180f; // градусов в секунду
    public float sinkAnimationDelay = 0.2f; // задержка перед анимацией
    private bool isSinking = false;
    private Coroutine sinkCoroutine;


    // Клетки, которые занимает корабль
    private List<Vector2Int> occupiedCells = new List<Vector2Int>();

    // Список клеток, по которым уже стреляли
    private List<Vector2Int> hitCells = new List<Vector2Int>();

    // Инициализация
    public void Init(int shipLength, string name)
    {
        length = shipLength;
        shipName = name;
        maxHealth = shipLength; // Здоровье = длине корабля
        health = maxHealth;

        // Получаем модель если она есть
        if (shipModel == null && transform.childCount > 0)
        {
            shipModel = transform.GetChild(0).gameObject;
        }
    }

    // Установить владельца корабля
    public void SetOwner(Player playerOwner)
    {
        owner = playerOwner;
    }

    // Проверить, попадает ли выстрел в корабль
    public bool IsHit(Vector2Int targetCell)
    {
        List<Vector2Int> cells = GetOccupiedCells();
        return cells.Contains(targetCell);
    }

    // Получить урон
    public bool TakeDamage(Vector2Int hitCell)
    {
        if (isSunk) return false;

        if (hitCells.Contains(hitCell))
        {
            Debug.Log($"Повторное попадание в клетку [{hitCell.x}, {hitCell.y}] корабля {shipName}");
            health--;
        }
        else
        {
            hitCells.Add(hitCell);
            health--;
            Debug.Log($"Попадание в {shipName}! Здоровье: {health}/{maxHealth}");
        }

        if (health <= 0)
        {
            Sink();
            return true;
        }
        else if (health <= maxHealth / 2)
        {
            ApplyDamagedVisual();
        }

        return false;
    }

    private void Sink()
    {
        isSunk = true;

        if (sinkingEffect != null)
        {
            Instantiate(sinkingEffect, transform.position, transform.rotation);
        }

        if (shipModel != null && sunkMaterial != null)
        {
            Renderer renderer = shipModel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = sunkMaterial;
            }
        }

        //transform.rotation *= Quaternion.Euler(0, 0, 180);
        transform.position = new Vector3(transform.position.x, 0.3f, transform.position.z);

        Debug.Log($"Корабль {shipName} потоплен!");
    }

    // Показать корабль противнику (просто показывает модель)
    public void RevealToOpponent()
    {
        if (isSunk)
        {
            // ВСЕГДА показываем корабль
            SetVisible(true);

            // Анимацию запускаем ТОЛЬКО при первом показе
            if (!isRevealedToOpponent)
            {
                isRevealedToOpponent = true;
                Debug.Log($"Корабль {shipName} показан противнику ВПЕРВЫЕ");

                // ЗАПУСКАЕМ АНИМАЦИЮ ТОЛЬКО ЗДЕСЬ
                PlaySinkAnimation();
            }
            else
            {
                // Уже был показан - просто показываем
                Debug.Log($"Корабль {shipName} уже был показан противнику (просто показываем)");
            }
        }
    }

    // Применить визуальный эффект повреждения
    private void ApplyDamagedVisual()
    {
        if (shipModel != null && damagedMaterial != null)
        {
            Renderer renderer = shipModel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = damagedMaterial;
            }
        }
    }

    // Показать/скрыть корабль
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (shipModel != null)
        {
            shipModel.SetActive(visible);
        }

        // Если корабль потоплен и его показывают
        if (visible && isSunk && sunkMaterial != null && shipModel != null)
        {
            Renderer renderer = shipModel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = sunkMaterial;
            }
        }
    }

    // Сбросить состояние для новой игры
    public void ResetBattleState()
    {
        health = maxHealth;
        isSunk = false;
        hitCells.Clear();

        if (shipModel != null && defaultMaterial != null)
        {
            Renderer renderer = shipModel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = defaultMaterial;
            }
        }

        transform.rotation = Quaternion.identity;
    }

    // Повернуть корабль на 90 градусов
    public void Rotate90()
    {
        direction = (direction + 1) % 4;
        Debug.Log($"Корабль '{shipName}' повернут. Направление: {GetDirectionName()}");
    }

    // Повернуть корабль налево (A)
    public bool RotateLeft(GridManager grid, List<Ship> playerShips)
    {
        return Rotate(grid, playerShips, true);
    }

    // Повернуть корабль направо (D)
    public bool RotateRight(GridManager grid, List<Ship> playerShips)
    {
        return Rotate(grid, playerShips, false);
    }

    // Общий метод поворота
    private bool Rotate(GridManager grid, List<Ship> playerShips, bool isLeftTurn)
    {
        // ДОБАВЬ ЭТУ ПРОВЕРКУ в начало метода
        if (isSunk || health <= 0)
        {
            Debug.Log($"{shipName} потоплен, движение невозможно");
            return false;
        }

        if (!isSelected || owner == null || hasActedThisTurn) return false;

        Vector2Int oldPos = gridPosition;
        int oldDirection = direction;

        int newDirection = isLeftTurn ? (direction + 1) % 4 : (direction - 1 + 4) % 4;

        int dx = 0, dy = 0;

        if (isLeftTurn)
        {
            switch (oldDirection)
            {
                case 0: dy = +(length - 1); break;
                case 1: dx = -(length - 1); break;
                case 2: dy = -(length - 1); break;
                case 3: dx = +(length - 1); break;
            }
        }
        else
        {
            switch (oldDirection)
            {
                case 0: dy = -(length - 1); break;
                case 1: dx = +(length - 1); break;
                case 2: dy = +(length - 1); break;
                case 3: dx = -(length - 1); break;
            }
        }

        Vector2Int newNosePosition = new Vector2Int(oldPos.x + dx, oldPos.y + dy);

        gridPosition = newNosePosition;
        direction = newDirection;

        List<Ship> otherShips = playerShips.Where(s => s != this && s.owner == this.owner).ToList();
        bool canRotate = CanPlaceAt(gridPosition, direction, grid, otherShips);

        if (canRotate)
        {
            hasMoved = true;
            hasActedThisTurn = true;
            UpdateVisualPosition(grid);
            return true;
        }
        else
        {
            gridPosition = oldPos;
            direction = oldDirection;
            return false;
        }
    }

    public string GetDirectionName()
    {
        switch (direction)
        {
            case 0: return "влево";
            case 1: return "вниз";
            case 2: return "вправо";
            case 3: return "вверх";
            default: return "неизвестно";
        }
    }

    public List<Vector2Int> GetOccupiedCells()
    {
        occupiedCells.Clear();

        for (int i = 0; i < length; i++)
        {
            switch (direction)
            {
                case 0:
                    occupiedCells.Add(new Vector2Int(gridPosition.x - i, gridPosition.y));
                    break;
                case 1:
                    occupiedCells.Add(new Vector2Int(gridPosition.x, gridPosition.y - i));
                    break;
                case 2:
                    occupiedCells.Add(new Vector2Int(gridPosition.x + i, gridPosition.y));
                    break;
                case 3:
                    occupiedCells.Add(new Vector2Int(gridPosition.x, gridPosition.y + i));
                    break;
            }
        }

        return occupiedCells;
    }

    // Проверить, можно ли разместить корабль на позиции
    public bool CanPlaceAt(Vector2Int position, int dir, GridManager grid, List<Ship> otherShips)
    {
        Vector2Int oldPos = gridPosition;
        int oldDirection = direction;

        gridPosition = position;
        direction = dir;

        bool canPlace = IsPlacementValid(grid, otherShips);

        gridPosition = oldPos;
        direction = oldDirection;

        return canPlace;
    }

    // Проверка валидности размещения
    private bool IsPlacementValid(GridManager grid, List<Ship> otherShips)
    {
        List<Vector2Int> cells = GetOccupiedCells();

        foreach (Vector2Int cell in cells)
        {
            if (cell.x < 0 || cell.x >= grid.GetGridSize() ||
                cell.y < 0 || cell.y >= grid.GetGridSize())
            {
                return false;
            }
        }

        foreach (Ship otherShip in otherShips)
        {
            if (otherShip == this || !otherShip.isPlaced) continue;

            List<Vector2Int> otherCells = otherShip.GetOccupiedCells();

            foreach (Vector2Int ourCell in cells)
            {
                foreach (Vector2Int otherCell in otherCells)
                {
                    if (ourCell == otherCell)
                    {
                        return false;
                    }

                    int dx = Mathf.Abs(ourCell.x - otherCell.x);
                    int dy = Mathf.Abs(ourCell.y - otherCell.y);
                    if (dx <= 1 && dy <= 1)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    // Разместить корабль
    public void PlaceShip(GridManager grid, List<Ship> otherShips)
    {
        if (CanPlaceAt(gridPosition, direction, grid, otherShips))
        {
            isPlaced = true;
            UpdateVisualPosition(grid);

            if (shipModel != null)
            {
                Renderer renderer = shipModel.GetComponent<Renderer>();
                if (renderer != null && defaultMaterial != null)
                {
                    renderer.material = defaultMaterial;
                }
            }
        }
    }

    public void UpdateVisualPosition(GridManager grid)
    {
        if (grid == null) return;

        Vector3 noseWorldPos = grid.GetWorldPosition(gridPosition.x, gridPosition.y);
        Vector3 worldPos = grid.transform.TransformPoint(noseWorldPos);

        float cellSize = grid.GetCellSpacing();

        Vector3 offset = Vector3.zero;

        switch (direction)
        {
            case 0:
                offset = new Vector3(-(length - 1) * cellSize / 2f, 0, 0);
                break;
            case 1:
                offset = new Vector3(0, 0, -(length - 1) * cellSize / 2f);
                break;
            case 2:
                offset = new Vector3((length - 1) * cellSize / 2f, 0, 0);
                break;
            case 3:
                offset = new Vector3(0, 0, (length - 1) * cellSize / 2f);
                break;
        }

        offset = grid.transform.TransformDirection(offset);
        worldPos += offset;

        worldPos.y = grid.transform.position.y;
        transform.position = worldPos;

        float rotationY = 0f;

        switch (direction)
        {
            case 0:
                rotationY = 180f;
                break;
            case 1:
                rotationY = 90f;
                break;
            case 2:
                rotationY = 0f;
                break;
            case 3:
                rotationY = 270f;
                break;
        }

        rotationY -= 90f;
        transform.rotation = grid.transform.rotation * Quaternion.Euler(0, rotationY, 0);
    }

    // Подсветка для режима размещения
    public void SetPlacementVisual(bool isValid)
    {
        if (shipModel == null) return;

        Renderer renderer = shipModel.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (isValid && placementValidMaterial != null)
            {
                renderer.material = placementValidMaterial;
            }
            else if (!isValid && placementInvalidMaterial != null)
            {
                renderer.material = placementInvalidMaterial;
            }
        }
    }

    // Проверить валидность каждой клетки отдельно
    public List<bool> GetCellValidStatus(GridManager grid, List<Ship> otherShips)
    {
        List<bool> validStatus = new List<bool>();
        List<Vector2Int> cells = GetOccupiedCells();

        foreach (Vector2Int cell in cells)
        {
            bool isValid = true;

            if (cell.x < 0 || cell.x >= grid.GetGridSize() ||
                cell.y < 0 || cell.y >= grid.GetGridSize())
            {
                isValid = false;
                validStatus.Add(isValid);
                continue;
            }

            foreach (Ship otherShip in otherShips)
            {
                if (otherShip == this || !otherShip.isPlaced) continue;

                List<Vector2Int> otherCells = otherShip.GetOccupiedCells();

                foreach (Vector2Int otherCell in otherCells)
                {
                    if (cell == otherCell)
                    {
                        isValid = false;
                        break;
                    }

                    int dx = Mathf.Abs(cell.x - otherCell.x);
                    int dy = Mathf.Abs(cell.y - otherCell.y);
                    if (dx <= 1 && dy <= 1)
                    {
                        isValid = false;
                        break;
                    }
                }

                if (!isValid) break;
            }

            validStatus.Add(isValid);
        }

        return validStatus;
    }

    // Получить список проблемных клеток
    public List<Vector2Int> GetInvalidCells(GridManager grid, List<Ship> otherShips)
    {
        List<Vector2Int> invalidCells = new List<Vector2Int>();
        List<Vector2Int> cells = GetOccupiedCells();
        List<bool> validStatus = GetCellValidStatus(grid, otherShips);

        for (int i = 0; i < cells.Count; i++)
        {
            if (!validStatus[i])
            {
                invalidCells.Add(cells[i]);
            }
        }

        return invalidCells;
    }

    // Выделить корабль
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (shipModel != null)
        {
            Renderer renderer = shipModel.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (selected && selectedMaterial != null)
                {
                    renderer.material = selectedMaterial;
                }
                else if (!selected && defaultMaterial != null)
                {
                    renderer.material = defaultMaterial;
                }
            }
        }
    }

    // Получить все клетки корабля
    public List<Vector2Int> GetAllCells()
    {
        return GetOccupiedCells();
    }

    // Движение вперед (по направлению носа)
    public bool MoveForward(GridManager grid, List<Ship> playerShips)
    {
        // ДОБАВЬ ЭТУ ПРОВЕРКУ в начало метода
        if (isSunk || health <= 0)
        {
            Debug.Log($"{shipName} потоплен, движение невозможно");
            return false;
        }

        if (!isSelected || owner == null || hasActedThisTurn) return false;

        Vector2Int oldPos = gridPosition;

        switch (direction)
        {
            case 0: gridPosition.x -= 1; break;
            case 1: gridPosition.y -= 1; break;
            case 2: gridPosition.x += 1; break;
            case 3: gridPosition.y += 1; break;
        }

        List<Ship> otherShips = playerShips.Where(s => s != this && s.owner == this.owner).ToList();
        bool canMove = CanPlaceAt(gridPosition, direction, grid, otherShips);

        if (canMove)
        {
            hasMoved = true;
            hasActedThisTurn = true;
            UpdateVisualPosition(grid);
            return true;
        }
        else
        {
            gridPosition = oldPos;
            return false;
        }
    }

    // Движение назад (против направления носа)
    public bool MoveBackward(GridManager grid, List<Ship> playerShips)
    {
        // ДОБАВЬ ЭТУ ПРОВЕРКУ в начало метода
        if (isSunk || health <= 0)
        {
            Debug.Log($"{shipName} потоплен, движение невозможно");
            return false;
        }

        if (!isSelected || owner == null || hasActedThisTurn) return false;

        Vector2Int oldPos = gridPosition;

        switch (direction)
        {
            case 0: gridPosition.x += 1; break;
            case 1: gridPosition.y += 1; break;
            case 2: gridPosition.x -= 1; break;
            case 3: gridPosition.y -= 1; break;
        }

        List<Ship> otherShips = playerShips.Where(s => s != this && s.owner == this.owner).ToList();
        bool canMove = CanPlaceAt(gridPosition, direction, grid, otherShips);

        if (canMove)
        {
            hasMoved = true;
            hasActedThisTurn = true;
            UpdateVisualPosition(grid);
            return true;
        }
        else
        {
            gridPosition = oldPos;
            return false;
        }
    }


    // Метод для анимации потопления
    public void PlaySinkAnimation()
    {
        // Проверяем не запущена ли уже анимация
        if (sinkCoroutine != null || isSinking) return;

        sinkCoroutine = StartCoroutine(SinkAnimation());
    }

    IEnumerator SinkAnimation()
    {
        isSinking = true; // Устанавливаем флаг

        // Ждем перед началом анимации
        yield return new WaitForSeconds(sinkAnimationDelay);

        Quaternion startRotation = transform.rotation;
        float currentRotation = 0f;

        // Поворачиваем на 180 градусов
        while (currentRotation < 180f)
        {
            float rotationStep = sinkAnimationSpeed * Time.deltaTime;
            currentRotation += rotationStep;

            if (currentRotation > 180f)
                currentRotation = 180f;

            // Поворачиваем вокруг оси Z
            transform.rotation = startRotation * Quaternion.Euler(0, 0, currentRotation);

            yield return null;
        }

        sinkCoroutine = null;
        isSinking = false; // Сбрасываем флаг
    }
}