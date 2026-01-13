using UnityEngine;
using System.Collections;

public class BattleGridController : MonoBehaviour
{
    [Header("Ññûëêè")]
    [SerializeField] private GridManager battleGrid;
    [SerializeField] private Camera battleCamera;

    [Header("Íàñòðîéêè ñòðåëüáû")]
    [SerializeField] private float shotCooldown = 0.5f;

    private Player currentPlayer;
    private Player opponentPlayer;
    private GameManager gameManager;

    private Vector2Int hoveredCell = new Vector2Int(-1, -1);
    private Color hoverColor = new Color(1, 1, 0, 0.5f);

    private bool canShoot = true;
    private Coroutine cooldownCoroutine;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("BattleGridController: GameManager íå íàéäåí!");
        }

        enabled = false;
    }

    public void SetupForPlayerTurn(Player player, Player opponent, GridManager targetGrid, Camera cam)
    {
        currentPlayer = player;
        opponentPlayer = opponent;
        battleGrid = targetGrid;
        battleCamera = cam;

        canShoot = true;
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }

        enabled = true;
        Debug.Log($"BattleGridController íàñòðîåí äëÿ {player.playerName}");
    }

    public void DisableController()
    {
        enabled = false;
        currentPlayer = null;
        opponentPlayer = null;

        canShoot = true;
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }

        if (hoveredCell.x >= 0 && hoveredCell.y >= 0 && battleGrid != null)
        {
            battleGrid.RestoreCellToBaseState(hoveredCell.x, hoveredCell.y);
        }
        hoveredCell = new Vector2Int(-1, -1);
    }

    void Update()
    {
        if (currentPlayer == null || opponentPlayer == null || battleGrid == null || battleCamera == null)
        {
            return;
        }

        UpdateHoveredCell();

        if (Input.GetMouseButtonDown(0) && canShoot)
        {
            TryShootAtCell();
        }
    }

    void UpdateHoveredCell()
    {
        Ray ray = battleCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        bool foundCell = false;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 localHit = battleGrid.transform.InverseTransformPoint(hit.point);
            float cellSize = battleGrid.GetCellSpacing();

            int gridX = Mathf.FloorToInt((localHit.x + cellSize / 2f) / cellSize);
            int gridY = Mathf.FloorToInt((localHit.z + cellSize / 2f) / cellSize);

            if (gridX >= 0 && gridX < battleGrid.GetGridSize() &&
                gridY >= 0 && gridY < battleGrid.GetGridSize())
            {
                foundCell = true;

                Vector2Int cell = new Vector2Int(gridX, gridY);

                if (hoveredCell.x != gridX || hoveredCell.y != gridY)
                {
                    if (hoveredCell.x >= 0 && hoveredCell.y >= 0)
                    {
                        battleGrid.RestoreCellToBaseState(hoveredCell.x, hoveredCell.y);
                    }

                    if (!battleGrid.WasShotThisTurn(cell))
                    {
                        hoveredCell = cell;
                        if (!battleGrid.WasShot(cell))
                        {
                            battleGrid.HighlightCellColor(gridX, gridY, hoverColor);
                        }
                    }
                    else
                    {
                        hoveredCell = new Vector2Int(-1, -1);
                    }
                }
            }
        }

        if (!foundCell && hoveredCell.x >= 0 && hoveredCell.y >= 0)
        {
            battleGrid.RestoreCellToBaseState(hoveredCell.x, hoveredCell.y);
            hoveredCell = new Vector2Int(-1, -1);
        }
    }

    void TryShootAtCell()
    {
        if (hoveredCell.x >= 0 && hoveredCell.y >= 0 && canShoot)
        {
            if (battleGrid.WasShotThisTurn(hoveredCell))
            {
                Debug.Log($"Óæå ñòðåëÿëè â [{hoveredCell.x}, {hoveredCell.y}] â ýòîì õîäó!");
                return;
            }

            ShootAtCell(hoveredCell);
        }
    }

    void ShootAtCell(Vector2Int targetCell)
    {
        canShoot = false;

        battleGrid.AddShot(targetCell);

        bool hit = opponentPlayer.TakeHit(targetCell);
        Debug.Log($"Âûñòðåë ïî [{targetCell.x}, {targetCell.y}]: {(hit ? "ÏÎÏÀÄÀÍÈÅ" : "ÏÐÎÌÀÕ")}");

        hoveredCell = new Vector2Int(-1, -1);

        if (gameManager != null)
        {
            gameManager.ProcessBattleShot(hit);

            if (hit)
            {
                cooldownCoroutine = StartCoroutine(StartShotCooldown(0.2f));
            }
            else
            {
                Debug.Log("Ïðîìàõ - âûçûâàåì NextTurn()");
                battleGrid.NextTurn();
            }
        }
        else
        {
            Debug.LogError("GameManager íå íàéäåí!");
        }
    }

    IEnumerator StartShotCooldown(float delay)
    {
        yield return new WaitForSeconds(delay);
        canShoot = true;
        cooldownCoroutine = null;
        Debug.Log("Ñòðåëüáà ðàçðåøåíà");
    }

}
