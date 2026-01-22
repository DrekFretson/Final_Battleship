using UnityEngine;
using System.Collections;

public class BattleGridControllerBot : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private GridManager battleGrid;
    [SerializeField] private Camera battleCamera;

    [Header("Настройки стрельбы")]
    [SerializeField] private float shotCooldown = 0.5f;

    private Player currentPlayer;
    private BotPlayer opponentBot;
    private GameManagerBot gameManagerBot;

    private Vector2Int hoveredCell = new Vector2Int(-1, -1);
    private Color hoverColor = new Color(1, 1, 0, 0.5f);

    private bool canShoot = true;
    private Coroutine cooldownCoroutine;

    void Start()
    {
        gameManagerBot = FindObjectOfType<GameManagerBot>();
        if (gameManagerBot == null)
        {
            Debug.LogError("BattleGridControllerBot: GameManagerBot не найден!");
        }

        enabled = false;
    }

    public void SetupForPlayerTurn(Player player, BotPlayer bot, GridManager targetGrid, Camera cam)
    {
        currentPlayer = player;
        opponentBot = bot;
        battleGrid = targetGrid;
        battleCamera = cam;

        canShoot = true;
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }

        //сбрасываем подсветку при начале хода
        if (battleGrid != null)
        {
            battleGrid.ResetAllHighlights();
        }

        enabled = true;
        Debug.Log($"BattleGridControllerBot настроен для {player.playerName} против бота");
    }

    public void DisableController()
    {
        enabled = false;
        currentPlayer = null;
        opponentBot = null;

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
        if (currentPlayer == null || opponentBot == null || battleGrid == null || battleCamera == null)
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

                //если перешли на новую клетку
                if (hoveredCell.x != gridX || hoveredCell.y != gridY)
                {
                    //восстанавливаем предыдущую клетку
                    if (hoveredCell.x >= 0 && hoveredCell.y >= 0)
                    {
                        battleGrid.RestoreCellToBaseState(hoveredCell.x, hoveredCell.y);
                    }

                    //проверяем, можно ли стрелять в эту клетку
                    if (!battleGrid.WasShotThisTurn(cell))
                    {
                        hoveredCell = cell;
                        //подсвечиваем только если не была выстрелена вообще
                        if (!battleGrid.WasShot(cell))
                        {
                            battleGrid.HighlightCellColor(gridX, gridY, hoverColor);
                        }
                    }
                    else
                    {
                        //клетка уже была выстрелена в этом ходу
                        hoveredCell = new Vector2Int(-1, -1);
                    }
                }
            }
        }

        //если не нашли клетку, восстанавливаем подсветку
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
                Debug.Log($"Уже стреляли в [{hoveredCell.x}, {hoveredCell.y}] в этом ходу!");
                return;
            }

            ShootAtCell(hoveredCell);
        }
    }

    void ShootAtCell(Vector2Int targetCell)
    {
        canShoot = false;

        //отмечаем выстрел
        battleGrid.AddShot(targetCell);

        bool hit = opponentBot.TakeHit(targetCell);
        Debug.Log($"Выстрел по [{targetCell.x}, {targetCell.y}]: {(hit ? "ПОПАДАНИЕ" : "ПРОМАХ")}");

        hoveredCell = new Vector2Int(-1, -1);

        //сообщаем GameManagerBot
        if (gameManagerBot != null)
        {
            gameManagerBot.ProcessPlayerShot(hit);

            if (hit)
            {
                cooldownCoroutine = StartCoroutine(StartShotCooldown(0.2f));
            }
            else
            {
                Debug.Log("Промах - вызываем NextTurn()");
                battleGrid.NextTurn();

                //после NextTurn() нужно обновить подсветку ВСЕЙ сетки
                StartCoroutine(RefreshGridAfterTurn());
            }
        }
        else
        {
            Debug.LogError("GameManagerBot не найден!");
        }
    }

    //обновить подсветку сетки после смены хода
    IEnumerator RefreshGridAfterTurn()
    {
        //ждём один кадр чтобы GridManager обновил цвета
        yield return null;

        if (battleGrid != null)
        {
            //обновляем все клетки на сетке
            for (int x = 0; x < battleGrid.GetGridSize(); x++)
            {
                for (int y = 0; y < battleGrid.GetGridSize(); y++)
                {
                    battleGrid.RestoreCellToBaseState(x, y);
                }
            }
            Debug.Log("Обновлена подсветка всей сетки после смены хода");
        }
    }

    IEnumerator StartShotCooldown(float delay)
    {
        yield return new WaitForSeconds(delay);
        canShoot = true;
        cooldownCoroutine = null;
        Debug.Log("Стрельба разрешена");
    }
}
