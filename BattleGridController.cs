using UnityEngine;
using System.Collections;

public class BattleGridController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private GridManager battleGrid;
    [SerializeField] private Camera battleCamera;

    [Header("Настройки стрельбы")]
    [SerializeField] private float shotCooldown = 0.5f;

    private Player currentPlayer; //владелец контроллера 
    private Player opponentPlayer; //игрок, по полю которого стреляем
    private GameManager gameManager;

    private Vector2Int hoveredCell = new Vector2Int(-1, -1); //клетка, над которой курсор
    private Color hoverColor = new Color(1, 1, 0, 0.5f); //эту клетку подсвечиввваем 

    private bool canShoot = true;
    private Coroutine cooldownCoroutine;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("BattleGridController: GameManager не найден!");
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
        Debug.Log($"BattleGridController настроен для {player.playerName}");
    }

    public void DisableController() //диактивация контроллера до конца хода
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
                        //подсвечиваем только если не была выстрелена
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

        //сообщаем клетке, что по ней выстрелили
        battleGrid.AddShot(targetCell);

        bool hit = opponentPlayer.TakeHit(targetCell); //Передаем оппоненту координаты клетки и он проверяет, есть ли корабль
        //регистрирует поподание и т.д.
        Debug.Log($"Выстрел по [{targetCell.x}, {targetCell.y}]: {(hit ? "ПОПАДАНИЕ" : "ПРОМАХ")}");

        hoveredCell = new Vector2Int(-1, -1);

        //сообщаем GameManager
        if (gameManager != null)
        {
            gameManager.ProcessBattleShot(hit);

            if (hit)
            {
                cooldownCoroutine = StartCoroutine(StartShotCooldown(0.2f));
            }
            else
            {
                Debug.Log("Промах - вызываем NextTurn()");
                battleGrid.NextTurn();
            }
        }
        else
        {
            Debug.LogError("GameManager не найден!");
        }
    }
    //корутина(функция с возможностью отсоновки) для кулдауна
    IEnumerator StartShotCooldown(float delay)
    {
        yield return new WaitForSeconds(delay);
        canShoot = true;
        cooldownCoroutine = null;
        Debug.Log("Стрельба разрешена");
    }
}
