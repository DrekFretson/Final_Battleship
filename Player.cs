//Player.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Player : MonoBehaviour
{
    [Header("Настройки игрока")]
    public string playerName;
    [SerializeField] public List<Ship> ships = new List<Ship>();

    [Header("Компоненты")]
    [SerializeField] private ShipPlacer shipPlacer;
    [SerializeField] private ShipMovementController movementController;

    [Header("Поле стрельбы")]
    [SerializeField] private GridManager battleGrid; // Сетка для отображения выстрелов

    private GridManager playerGrid;
    private Vector2Int selectedTarget = new Vector2Int(-1, -1);
    private bool hasTakenActionThisTurn = false;
    private List<Vector2Int> incomingShots = new List<Vector2Int>(); // Выстрелы по этому игроку

    public void Initialize(string name, GridManager grid)
    {
        playerName = name;
        playerGrid = grid;

        ClearOtherPlayersShips();

        foreach (Ship ship in ships)
        {
            if (ship != null)
            {
                ship.Init(ship.length, ship.shipName);
                ship.SetOwner(this);
            }
        }
    }

    private void ClearOtherPlayersShips()
    {
        List<Ship> shipsToRemove = new List<Ship>();

        foreach (Ship ship in ships)
        {
            if (ship == null) continue;

            if (ship.owner != null && ship.owner != this)
            {
                Debug.LogWarning($"Удаляем корабль {ship.shipName} из списка {playerName} - он принадлежит {ship.owner.playerName}");
                shipsToRemove.Add(ship);
            }
        }

        foreach (Ship ship in shipsToRemove)
        {
            ships.Remove(ship);
        }
    }

    public void EnableShipPlacement()
    {
        if (shipPlacer != null)
        {
            shipPlacer.BeginPlacement();
        }
        else
        {
            Debug.LogError($"ShipPlacer не найден для игрока {playerName}");
        }
    }

    public void DisableShipPlacement()
    {
        if (shipPlacer != null)
        {
            shipPlacer.enabled = false;
        }
    }

    public void EnableShipMovement()
    {
        if (movementController != null)
        {
            movementController.EnableMovementMode(ships, this);
            hasTakenActionThisTurn = false;
        }
    }

    public void DisableShipMovement()
    {
        if (movementController != null)
        {
            movementController.DisableMovementMode();
        }
    }

    public bool TakeHit(Vector2Int target)
    {
        bool hit = false;
        Ship sunkShip = null; // Запоминаем, какой корабль потопили

        foreach (Ship ship in ships)
        {
            if (ship.IsHit(target))
            {
                // Запоминаем состояние до выстрела
                bool wasSunkBefore = ship.isSunk;

                // Наносим урон
                ship.TakeDamage(target);
                hit = true;

                // Если корабль только что потопили (был не потоплен, стал потоплен)
                if (!wasSunkBefore && ship.isSunk)
                {
                    sunkShip = ship; // Запоминаем потопленный корабль
                    Debug.Log($"Корабль {ship.shipName} потоплен при выстреле в [{target.x},{target.y}]!");
                }
            }
        }

        // Сохраняем информацию о выстреле
        incomingShots.Add(target);

        // Если потопили корабль - НЕМЕДЛЕННО показываем его противнику
        if (sunkShip != null)
        {
            sunkShip.RevealToOpponent();
            Debug.Log($"Корабль {sunkShip.shipName} показан противнику немедленно!");
        }

        Debug.Log($"{playerName}: выстрел по [{target.x},{target.y}] - {(hit ? "ПОПАДАНИЕ" : "ПРОМАХ")} {(sunkShip != null ? " и КОРАБЛЬ ПОТОПЛЕН" : "")}");
        return hit;
    }

    public bool AllShipsSunk()
    {
        foreach (Ship ship in ships)
        {
            if (!ship.isSunk)
            {
                return false;
            }
        }
        return true;
    }

    public void ShowAllShips()
    {
        foreach (Ship ship in ships)
        {
            ship.gameObject.SetActive(true);
            // Или ship.SetVisible(true); в зависимости от логики
        }
    }

    public void HideAllShips()
    {
        foreach (Ship ship in ships)
        {
            ship.SetVisible(false);
        }
    }

    public bool TrySelectTarget(Vector2Int target)
    {
        if (!hasTakenActionThisTurn)
        {
            selectedTarget = target;
            if (battleGrid != null)
            {
                battleGrid.HighlightCellColor(target.x, target.y, Color.yellow);
            }
            return true;
        }
        return false;
    }

    public bool HasSelectedTarget()
    {
        return selectedTarget.x >= 0 && selectedTarget.y >= 0;
    }

    public Vector2Int GetSelectedTarget()
    {
        return selectedTarget;
    }

    public void ClearTarget()
    {
        selectedTarget = new Vector2Int(-1, -1);
    }

    public void ResetTurnActions()
    {
        hasTakenActionThisTurn = false;
    }

    public void OnShipActionTaken()
    {
        hasTakenActionThisTurn = true;
    }

    public void ResetShipsActions()
    {
        foreach (Ship ship in ships)
        {
            ship.hasActedThisTurn = false;
        }
        Debug.Log($"{playerName} сбросил действия кораблей");
    }

    // Получить сетку игрока (для отображения выстрелов)
    public GridManager GetPlayerGrid()
    {
        return playerGrid;
    }


    // Показать ВСЕ потопленные корабли этого игрока
    public void RevealAllSunkShips()
    {
        int sunkCount = 0;
        foreach (Ship ship in ships)
        {
            if (ship.isSunk)
            {
                ship.RevealToOpponent();
                sunkCount++;
            }
        }

        if (sunkCount > 0)
        {
            Debug.Log($"Показано {sunkCount} потопленных кораблей игрока {playerName}");
        }
    }

    public bool AllShipsPlaced()
    {
        foreach (Ship ship in ships)
        {
            if (ship == null) continue;
            if (!ship.isPlaced)
            {
                Debug.Log($"Корабль {ship.shipName} не расставлен!");
                return false;
            }
        }
        return true;
    }
}