using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Player : MonoBehaviour
{
    [Header("Íàñòðîéêè èãðîêà")]
    public string playerName;
    [SerializeField] public List<Ship> ships = new List<Ship>();

    [Header("Êîìïîíåíòû")]
    [SerializeField] private ShipPlacer shipPlacer;
    [SerializeField] private ShipMovementController movementController;

    [Header("Ïîëå ñòðåëüáû")]
    [SerializeField] private GridManager battleGrid;

    private GridManager playerGrid;
    private Vector2Int selectedTarget = new Vector2Int(-1, -1);
    private bool hasTakenActionThisTurn = false;
    private List<Vector2Int> incomingShots = new List<Vector2Int>();

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
                Debug.LogWarning($"Óäàëÿåì êîðàáëü {ship.shipName} èç ñïèñêà {playerName} - îí ïðèíàäëåæèò {ship.owner.playerName}");
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
            Debug.LogError($"ShipPlacer íå íàéäåí äëÿ èãðîêà {playerName}");
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
        Ship sunkShip = null;

        foreach (Ship ship in ships)
        {
            if (ship.IsHit(target))
            {
                bool wasSunkBefore = ship.isSunk;

                ship.TakeDamage(target);
                hit = true;

                if (!wasSunkBefore && ship.isSunk)
                {
                    sunkShip = ship;
                    Debug.Log($"Êîðàáëü {ship.shipName} ïîòîïëåí ïðè âûñòðåëå â [{target.x},{target.y}]!");
                }
            }
        }

        incomingShots.Add(target);

        if (sunkShip != null)
        {
            sunkShip.RevealToOpponent();
            Debug.Log($"Êîðàáëü {sunkShip.shipName} ïîêàçàí ïðîòèâíèêó íåìåäëåííî!");
        }

        Debug.Log($"{playerName}: âûñòðåë ïî [{target.x},{target.y}] - {(hit ? "ÏÎÏÀÄÀÍÈÅ" : "ÏÐÎÌÀÕ")} {(sunkShip != null ? " è ÊÎÐÀÁËÜ ÏÎÒÎÏËÅÍ" : "")}");
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
        Debug.Log($"{playerName} ñáðîñèë äåéñòâèÿ êîðàáëåé");
    }

    public GridManager GetPlayerGrid()
    {
        return playerGrid;
    }


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
            Debug.Log($"Ïîêàçàíî {sunkCount} ïîòîïëåííûõ êîðàáëåé èãðîêà {playerName}");
        }
    }

    public bool AllShipsPlaced()
    {
        foreach (Ship ship in ships)
        {
            if (ship == null) continue;
            if (!ship.isPlaced)
            {
                Debug.Log($"Êîðàáëü {ship.shipName} íå ðàññòàâëåí!");
                return false;
            }
        }
        return true;
    }

}
