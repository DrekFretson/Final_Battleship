using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public enum BotGamePhase
{
    PlayerSetup,
    PlayerTurn,
    BotTurn,
    GameOver
}

public class GameManagerBot : MonoBehaviour
{
    [Header("Игрок")]
    [SerializeField] private Player player;
    [SerializeField] private GridManager playerGrid;

    [Header("Бот")]
    [SerializeField] private BotPlayer botPlayer;
    [SerializeField] private GridManager botGrid;

    [Header("Камера")]
    [SerializeField] private Camera gameCamera;

    [Header("UI - Тексты как в PvP")]
    [SerializeField] private GameObject playerSetupText;
    [SerializeField] private GameObject victoryText;
    [SerializeField] private GameObject turnInstructions;
    [SerializeField] private GameObject transitionInstructions;
    [SerializeField] private GameObject shotStatusText;

    [Header("Контроллеры")]
    [SerializeField] private ShipPlacer playerShipPlacer;
    [SerializeField] private BattleGridControllerBot battleGridControllerBot;

    private BotGamePhase currentPhase = BotGamePhase.PlayerSetup;
    private bool isTransitioning = false;
    private Coroutine messageCoroutine;

    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        //проверки обязательных полей
        if (player == null)
        {
            Debug.LogError("GameManagerBot: player не назначен!");
            return;
        }

        if (botPlayer == null)
        {
            Debug.LogError("GameManagerBot: botPlayer не назначен!");
            return;
        }

        //скрываем все UI при старте
        HideAllUI();

        //инициализация игрока
        player.Initialize("Игрок", playerGrid);
        player.HideAllShips();

        //инициализация бота
        botPlayer.Initialize("Бот", botGrid);
        botPlayer.HideAllShips();
        botPlayer.RandomPlaceShips();

        Debug.Log("Бот расставил корабли!");
        Debug.Log($"Корабли бота: {botPlayer.ships.Count} штук");

        StartPlayerSetup();
    }

    void StartPlayerSetup()
    {
        currentPhase = BotGamePhase.PlayerSetup;

        //показываем текст расстановки
        if (playerSetupText != null)
        {
            playerSetupText.SetActive(true);
        }

        //начинаем расстановку кораблей игрока
        if (playerShipPlacer != null)
        {
            playerShipPlacer.BeginPlacement();
        }
        else if (player != null)
        {
            //альтернатива через игрока
            player.EnableShipPlacement();
        }
        else
        {
            Debug.LogError("Не могу начать расстановку кораблей!");
        }
    }

    public void StartGame()
    {
        if (!player.AllShipsPlaced())
        {
            Debug.Log("Не все корабли игрока размещены!");
            ShowMessage("РАССТАВЬТЕ ВСЕ КОРАБЛИ!", Color.yellow, 2f);
            return;
        }

        Debug.Log("=== НАЧАЛО ИГРЫ ===");
        currentPhase = BotGamePhase.PlayerTurn;

        //скрываем текст расстановки
        if (playerSetupText != null)
        {
            playerSetupText.SetActive(false);
        }

        player.DisableShipPlacement();
        EnablePlayerTurn();
    }

    void EnablePlayerTurn()
    {
        Debug.Log("=== ХОД ИГРОКА ===");
        Debug.Log("Двигайте корабли (WASD) и стреляйте (ЛКМ) по сетке бота");

        //сбрасываем флаги действий у кораблей игрока
        if (player != null)
        {
            player.ResetShipsActions();
            Debug.Log("Сброшены флаги действий у кораблей игрока");
        }

        //показываем инструкцию для хода игрока
        if (turnInstructions != null)
        {
            turnInstructions.SetActive(true);
        }

        //скрываем остальные UI
        if (transitionInstructions != null)
        {
            transitionInstructions.SetActive(false);
        }

        //включаем режим движения кораблей
        if (player != null)
        {
            player.EnableShipMovement();
            Debug.Log("Режим движения кораблей включен");
        }

        //включаем режим стрельбы ПО БОТУ
        if (battleGridControllerBot != null)
        {
            battleGridControllerBot.SetupForPlayerTurn(player, botPlayer, botGrid, gameCamera);
            Debug.Log("Режим стрельбы включен");
        }
        else
        {
            Debug.LogError("BattleGridControllerBot не назначен!");
        }
    }

    void EnableBotTurn()
    {
        if (isTransitioning) return;

        Debug.Log("=== ХОД БОТА ===");

        //бот двигает свои корабли
        if (botPlayer != null)
        {
            botPlayer.EnableShipMovement(); // вызовет MoveBotShips()
        }

        //показываем "Ход противника"
        if (transitionInstructions != null)
        {
            transitionInstructions.SetActive(true);
        }

        //скрываем инструкцию для игрока
        if (turnInstructions != null)
        {
            turnInstructions.SetActive(false);
        }

        isTransitioning = true;

        //задержка перед ходом бота
        Invoke("ExecuteBotTurn", 1f);
    }

    void ExecuteBotTurn()
    {
        Vector2Int botShot = botPlayer.MakeShot(player);

        bool hit = player.TakeHit(botShot);

        playerGrid.AddShot(botShot);

        //обработка результата
        botPlayer.ProcessShotResult(botShot, hit);
        Debug.Log(hit ? "Бот попал!" : "Бот промахнулся!");

        //проверить конец игры
        if (player.AllShipsSunk())
        {
            EndGame(false);
        }
        else
        {
            if (hit)
            {
                Debug.Log("Бот попал - стреляет ещё раз");
                Invoke("ExecuteBotTurn", 1.5f);
            }
            else
            {
                playerGrid.NextTurn(); //сбрасываем выстрелы этого хода
                Invoke("SwitchToPlayerTurn", 1.5f);
            }
        }
    }

    public void ProcessPlayerShot(bool hit)
    {
        Debug.Log(hit ? "ПОПАДАНИЕ!" : "ПРОМАХ!");

        ShowShotMessage(hit);

        if (botPlayer.AllShipsSunk())
        {
            EndGame(true); //игрок выиграл
            return;
        }

        //если игрок попал - продолжает ходить (двигать корабли и стрелять)
        if (hit)
        {
            Debug.Log("Игрок попал - продолжает ход");
            //оставляем включенными оба режима
        }
        else
        {
            //если промахнулся - отключаем управление игроку, ход бота
            Debug.Log("Промах - ход переходит боту");

            //отключаем управление игроку
            player.DisableShipMovement();
            if (battleGridControllerBot != null)
            {
                battleGridControllerBot.DisableController();
            }

            //включаем ход бота
            currentPhase = BotGamePhase.BotTurn;
            EnableBotTurn();
        }
    }

    void SwitchToPlayerTurn()
    {
        isTransitioning = false;
        currentPhase = BotGamePhase.PlayerTurn;

        if (transitionInstructions != null)
        {
            transitionInstructions.SetActive(false);
        }

        //сбрасываем флаги действий у кораблей игрока
        if (player != null)
        {
            player.ResetShipsActions();
            Debug.Log("Сброшены флаги действий у кораблей игрока (при переключении хода)");
        }

        EnablePlayerTurn(); //включаем движение и стрельбу
    }

    void EndGame(bool playerWon)
    {
        currentPhase = BotGamePhase.GameOver;

        //скрываем все UI кроме victoryText
        HideAllUI();

        //показываем текст победы
        if (victoryText != null)
        {
            victoryText.SetActive(true);

            //меняем текст в зависимости от победителя
            Text textComponent = victoryText.GetComponent<Text>();
            if (textComponent != null)
            {
                if (playerWon)
                {
                    textComponent.text = "ИГРОК ПОБЕДИЛ!\nНажмите R для новой игры";
                }
                else
                {
                    textComponent.text = "БОТ ПОБЕДИЛ!\nНажмите R для новой игры";
                }
            }
        }

        if (playerWon)
        {
            Debug.Log("Игрок победил!");
        }
        else
        {
            Debug.Log("Бот победил!");
        }

        //показать все корабли
        player.ShowAllShips();
        botPlayer.ShowAllShips();

        //отключить стрельбу
        if (battleGridControllerBot != null)
        {
            battleGridControllerBot.DisableController();
        }
    }

    void Update()
    {
        //обработка клавиши Escape для возврата в меню
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackToMainMenu();
        }

        //пробел для завершения расстановки
        if (Input.GetKeyDown(KeyCode.Space) && currentPhase == BotGamePhase.PlayerSetup)
        {
            if (player.AllShipsPlaced())
            {
                StartGame();
            }
            else
            {
                Debug.Log("Не все корабли расставлены!");
                ShowMessage("Не все корабли расставлены!", Color.yellow, 2f);
            }
        }

        //перезапуск сцены в фазе GameOver
        if (currentPhase == BotGamePhase.GameOver && Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }


    //методы для работы с UI
    void ShowShotMessage(bool hit)
    {
        if (shotStatusText == null) return;

        Text textComponent = shotStatusText.GetComponent<Text>();
        if (textComponent == null) return;

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        if (hit)
        {
            textComponent.text = "ПОПАДАНИЕ! Стреляйте еще раз";
            textComponent.color = Color.green;
        }
        else
        {
            textComponent.text = "ПРОМАХ! Ход переходит противнику";
            textComponent.color = Color.red;
        }

        shotStatusText.SetActive(true);
        messageCoroutine = StartCoroutine(HideMessageAfterDelay(2f));
    }

    void ShowMessage(string message, Color color, float delay)
    {
        if (shotStatusText == null) return;

        Text textComponent = shotStatusText.GetComponent<Text>();
        if (textComponent == null) return;

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        textComponent.text = message;
        textComponent.color = color;

        shotStatusText.SetActive(true);
        messageCoroutine = StartCoroutine(HideMessageAfterDelay(delay));
    }

    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (shotStatusText != null)
        {
            shotStatusText.SetActive(false);
        }

        messageCoroutine = null;
    }

    void HideAllUI()
    {
        if (playerSetupText != null) playerSetupText.SetActive(false);
        if (victoryText != null) victoryText.SetActive(false);
        if (turnInstructions != null) turnInstructions.SetActive(false);
        if (transitionInstructions != null) transitionInstructions.SetActive(false);
        if (shotStatusText != null) shotStatusText.SetActive(false);
    }
}
