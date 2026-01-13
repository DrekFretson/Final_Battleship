using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

[System.Serializable]
public class Ship : MonoBehaviour
{
    [Header("Íàñòðîéêè êîðàáëÿ")]
    public int length = 1;
    public string shipName;

    [Header("Òåêóùåå ñîñòîÿíèå")]
    public Vector2Int gridPosition;
    public int direction = 0;
    public bool isPlaced = false;
    public bool isSelected = false;
    public bool hasMoved = false;

    [Header("Áîåâàÿ ñèñòåìà")]
    public int health = 1;
    public int maxHealth = 1;
    public bool isSunk = false;
    public bool isVisible = true;

    [Header("Âèçóàë")]
    public GameObject shipModel;
    public Material defaultMaterial;
    public Material placementValidMaterial;
    public Material placementInvalidMaterial;
    public Material selectedMaterial;
    public Material damagedMaterial;
    public Material sunkMaterial;

    [Header("Ýôôåêòû")]
    public GameObject sinkingEffect;

    [Header("Âëàäåëåö")]
    public Player owner;

    [Header("Äåéñòâèÿ çà õîä")]
    public bool hasActedThisTurn = false;

    [Header("Âèçóàëèçàöèÿ ïîòîïëåíèÿ")]
    public bool isRevealedToOpponent = false;

    [Header("Àíèìàöèÿ ïîòîïëåíèÿ")]
    public float sinkAnimationSpeed = 180f;
    public float sinkAnimationDelay = 0.2f;
    private bool isSinking = false;
    private Coroutine sinkCoroutine;


    private List<Vector2Int> occupiedCells = new List<Vector2Int>();

    private List<Vector2Int> hitCells = new List<Vector2Int>();

    public void Init(int shipLength, string name)
    {
        length = shipLength;
        shipName = name;
        maxHealth = shipLength;
        health = maxHealth;

        if (shipModel == null && transform.childCount > 0)
        {
            shipModel = transform.GetChild(0).gameObject;
        }
    }

    public void SetOwner(Player playerOwner)
    {
        owner = playerOwner;
    }

    public bool IsHit(Vector2Int targetCell)
    {
        List<Vector2Int> cells = GetOccupiedCells();
        return cells.Contains(targetCell);
    }

    public bool TakeDamage(Vector2Int hitCell)
    {
        if (isSunk) return false;

        if (hitCells.Contains(hitCell))
        {
            Debug.Log($"Ïîâòîðíîå ïîïàäàíèå â êëåòêó [{hitCell.x}, {hitCell.y}] êîðàáëÿ {shipName}");
            health--;
        }
        else
        {
            hitCells.Add(hitCell);
            health--;
            Debug.Log($"Ïîïàäàíèå â {shipName}! Çäîðîâüå: {health}/{maxHealth}");
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

        transform.position = new Vector3(transform.position.x, 0.3f, transform.position.z);

        Debug.Log($"Êîðàáëü {shipName} ïîòîïëåí!");
    }

    public void RevealToOpponent()
    {
        if (isSunk)
        {
            SetVisible(true);

            if (!isRevealedToOpponent)
            {
                isRevealedToOpponent = true;
                Debug.Log($"Êîðàáëü {shipName} ïîêàçàí ïðîòèâíèêó ÂÏÅÐÂÛÅ");

                PlaySinkAnimation();
            }
            else
            {
                Debug.Log($"Êîðàáëü {shipName} óæå áûë ïîêàçàí ïðîòèâíèêó (ïðîñòî ïîêàçûâàåì)");
            }
        }
    }

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

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (shipModel != null)
        {
            shipModel.SetActive(visible);
        }

        if (visible && isSunk && sunkMaterial != null && shipModel != null)
        {
            Renderer renderer = shipModel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = sunkMaterial;
            }
        }
    }

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

    public void Rotate90()
    {
        direction = (direction + 1) % 4;
        Debug.Log($"Êîðàáëü '{shipName}' ïîâåðíóò. Íàïðàâëåíèå: {GetDirectionName()}");
    }

    public bool RotateLeft(GridManager grid, List<Ship> playerShips)
    {
        return Rotate(grid, playerShips, true);
    }

    public bool RotateRight(GridManager grid, List<Ship> playerShips)
    {
        return Rotate(grid, playerShips, false);
    }

    private bool Rotate(GridManager grid, List<Ship> playerShips, bool isLeftTurn)
    {
        if (isSunk || health <= 0)
        {
            Debug.Log($"{shipName} ïîòîïëåí, äâèæåíèå íåâîçìîæíî");
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
            case 0: return "âëåâî";
            case 1: return "âíèç";
            case 2: return "âïðàâî";
            case 3: return "ââåðõ";
            default: return "íåèçâåñòíî";
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

    public List<Vector2Int> GetAllCells()
    {
        return GetOccupiedCells();
    }

    public bool MoveForward(GridManager grid, List<Ship> playerShips)
    {
        if (isSunk || health <= 0)
        {
            Debug.Log($"{shipName} ïîòîïëåí, äâèæåíèå íåâîçìîæíî");
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

    public bool MoveBackward(GridManager grid, List<Ship> playerShips)
    {
        if (isSunk || health <= 0)
        {
            Debug.Log($"{shipName} ïîòîïëåí, äâèæåíèå íåâîçìîæíî");
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


    public void PlaySinkAnimation()
    {
        if (sinkCoroutine != null || isSinking) return;

        sinkCoroutine = StartCoroutine(SinkAnimation());
    }

    IEnumerator SinkAnimation()
    {
        isSinking = true;

        yield return new WaitForSeconds(sinkAnimationDelay);

        Quaternion startRotation = transform.rotation;
        float currentRotation = 0f;

        while (currentRotation < 180f)
        {
            float rotationStep = sinkAnimationSpeed * Time.deltaTime;
            currentRotation += rotationStep;

            if (currentRotation > 180f)
                currentRotation = 180f;

            transform.rotation = startRotation * Quaternion.Euler(0, 0, currentRotation);

            yield return null;
        }

        sinkCoroutine = null;
        isSinking = false;
    }

}
