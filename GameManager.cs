using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum GamePhase
{
    Player1Setup,    //игрок 1 расставляет свои корабли
    Player2Setup,    //игрок 2 расставляет свои корабли  
    Player1Turn,     //активный ход игрока 1
    Player2Turn,     //активный ход игрока 2
    TransitionPhase, //пауза между фазами
    GameOver         //игра завершена, есть победитель
}

public class GameManager : MonoBehaviour
{
    [Header("Игроки")]
    [SerializeField] private Player player1;
    [SerializeField] private Player player2;

    [Header("Сетки")]
    [SerializeField] private GridManager player1Grid;
    [SerializeField] private GridManager player2Grid;

    [Header("Камеры")]
    [SerializeField] private Camera player1Camera;
    [SerializeField] private Camera player2Camera;

    [Header("UI")]
    [SerializeField] private GameObject setupInstructions;
    [SerializeField] private GameObject turnInstructions;
    [SerializeField] private GameObject transitionInstructions;
    [SerializeField] private GameObject shotStatusText;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject victoryText; //текст победы
    [SerializeField] private GameObject player1SetupText; //текст для игрока 1
    [SerializeField] private GameObject player2SetupText; //текст для игрока 2

    [Header("Контроллер боя")]
    [SerializeField] private BattleGridController battleGridController; //контроллер боя, выключен во время расстоновки 

    [Header("Настройки игры")]
    [SerializeField] private float endGameDelay = 3f;

    private GamePhase currentPhase = GamePhase.Player1Setup; //фаза игры, начинаем всегда с 1-ого игорька
    private Player currentPlayer;
    private Player opponentPlayer;
    private GridManager currentGrid;
    private Camera currentCamera;
    private bool isTransitioning = false; //флаг перехода, когда true - игра ждет пробела
    private bool canRestart = false;
    private Coroutine messageCoroutine;

    void Start()
    {

        if (battleGridController == null)
        {
            battleGridController = FindObjectOfType<BattleGridController>();
            if (battleGridController == null)
            {
                Debug.LogError("GameManager: BattleGridController не найден в сцене!");
            }
            else
            {
                Debug.Log("GameManager: BattleGridController найден автоматически");
            }
        }

        InitializeGame();
        StartSetupPhase();
    }

    void Update()
    {
        HandleInput();

        //перезапуск игры после победы
        if (canRestart && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    void InitializeGame()
    {
        player1.Initialize("Игрок 1", player1Grid);
        player2.Initialize("Игрок 2", player2Grid);
        player1.HideAllShips();
        player2.HideAllShips();

        //скрываем экран победы
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(false);
        }

        canRestart = false;
    }
    //фаза расстановки кораблей 
    void StartSetupPhase()
    {
        Debug.Log($"=== StartSetupPhase: {currentPhase} ===");

        //отключаем все тексты расстановки
        if (player1SetupText != null) player1SetupText.SetActive(false);
        if (player2SetupText != null) player2SetupText.SetActive(false);
        if (setupInstructions != null) setupInstructions.SetActive(false);
        //меняем игроьков, чтобы он расставили корабли
        switch (currentPhase)
        {
            case GamePhase.Player1Setup:
                Debug.Log("Показываем player1SetupText");
                if (player1SetupText != null)
                {
                    player1SetupText.SetActive(true);
                    Debug.Log($"player1SetupText активен: {player1SetupText.activeSelf}");
                }
                StartPlayerSetup(player1, player1Grid, player1Camera);
                break;

            case GamePhase.Player2Setup:
                Debug.Log("Показываем player2SetupText");
                if (player2SetupText != null)
                {
                    player2SetupText.SetActive(true);
                    Debug.Log($"player2SetupText активен: {player2SetupText.activeSelf}");
                }
                StartPlayerSetup(player2, player2Grid, player2Camera);
                break;
        }

        turnInstructions.SetActive(false);
        if (transitionInstructions != null) transitionInstructions.SetActive(false);
        if (shotStatusText != null) shotStatusText.SetActive(false);
    }

    void StartPlayerSetup(Player player, GridManager grid, Camera cam)
    {
        currentPlayer = player;
        currentGrid = grid;
        currentCamera = cam;

        SetActiveCamera(cam);
        player1.HideAllShips();
        player2.HideAllShips();
        player.ShowAllShips();
        player.EnableShipPlacement();

        //отключаем BattleGridController во время расстановки
        if (battleGridController != null)
        {
            battleGridController.DisableController();
        }


        turnInstructions.SetActive(false);
        if (transitionInstructions != null) transitionInstructions.SetActive(false);
        if (shotStatusText != null) shotStatusText.SetActive(false);
    }

    void StartBattlePhase()
    {
        //скрываем тексты расстановки при начале боя
        if (player1SetupText != null) player1SetupText.SetActive(false);
        if (player2SetupText != null) player2SetupText.SetActive(false);
        if (setupInstructions != null) setupInstructions.SetActive(false);

        switch (currentPhase)
        {
            case GamePhase.Player1Turn:
                StartPlayerTurn(player1, player2, player1Grid, player1Camera);
                break;
            case GamePhase.Player2Turn:
                StartPlayerTurn(player2, player1, player2Grid, player2Camera);
                break;
        }

        turnInstructions.SetActive(true);
        if (shotStatusText != null) shotStatusText.SetActive(false);
    }

    void StartPlayerTurn(Player player, Player opponent, GridManager grid, Camera cam)
    {
        currentPlayer = player;
        opponentPlayer = opponent;
        currentGrid = grid;
        currentCamera = cam;

        SetActiveCamera(cam);
        player1.HideAllShips();
        player2.HideAllShips();

        //показываем потопленные корабли противника игроку
        opponent.RevealAllSunkShips();
        Debug.Log($"В начале хода {player.playerName} показаны потопленные корабли {opponent.playerName}");

        player.ShowAllShips();
        player.ResetShipsActions();
        player.EnableShipMovement();

        //настраиваем BattleGridController
        if (battleGridController != null)
        {
            GridManager targetGrid = (player == player1) ? player2Grid : player1Grid;
            Camera targetCamera = (player == player1) ? player1Camera : player2Camera;

            battleGridController.SetupForPlayerTurn(player, opponent, targetGrid, targetCamera);
            Debug.Log($"BattleGridController настроен для стрельбы {player.playerName} -> {opponent.playerName}");
        }

        //отключаем все тексты расстановки
        if (player1SetupText != null) player1SetupText.SetActive(false);
        if (player2SetupText != null) player2SetupText.SetActive(false);
        if (setupInstructions != null) setupInstructions.SetActive(false);

        turnInstructions.SetActive(true);
        if (transitionInstructions != null) transitionInstructions.SetActive(false);
        if (shotStatusText != null) shotStatusText.SetActive(false);

        isTransitioning = false;

        Debug.Log($"Ход {player.playerName}. Видны потопленные корабли противника.");
    }

    //обработка выстрела из BattleGridController
    public void ProcessBattleShot(bool hit)
    {
        ShowShotMessage(hit);

        if (hit)
        {
            Debug.Log($"{currentPlayer.playerName} попал! Может стрелять еще раз");
            //проверяем победу после попадания
            CheckGameOver();
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} промахнулся! Переход хода");
            //проверяем победу перед сменой хода
            CheckGameOver();

            if (currentPhase != GamePhase.GameOver)
            {
                StartCoroutine(DelayedTurnSwitch());
            }
        }
    }

    IEnumerator DelayedTurnSwitch()
    {
        yield return new WaitForSeconds(2f);
        SwitchTurn();
    }

    void HandleInput()
    {
        if (isTransitioning || currentPhase == GamePhase.TransitionPhase || currentPhase == GamePhase.GameOver) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            switch (currentPhase)
            {
                case GamePhase.Player1Setup:
                    //проверяем можно ли закончить расстановку
                    if (EndPlayerSetup()) //возвращает true если все корабли расставлены
                    {
                        currentPhase = GamePhase.TransitionPhase;
                        StartCoroutine(TransitionToPhase(GamePhase.Player2Setup, 0.5f));
                    }
                    break;

                case GamePhase.Player2Setup://так же для 2 игрока
                    if (EndPlayerSetup())
                    {
                        currentPhase = GamePhase.TransitionPhase;
                        StartCoroutine(TransitionToPhase(GamePhase.Player1Turn, 0.5f));
                    }
                    break;

                case GamePhase.Player1Turn:
                case GamePhase.Player2Turn:
                    //Space в фазе боя - принудительный переход хода
                    Debug.Log($"Принудительный переход хода от {currentPlayer.playerName}");
                    CheckGameOver(); //проверяем перед переходом
                    if (currentPhase != GamePhase.GameOver)
                    {
                        SwitchTurn();
                    }
                    break;
            }
        }
    }

    IEnumerator TransitionToPhase(GamePhase nextPhase, float delay)
    {
        isTransitioning = true;

        player1.HideAllShips();
        player2.HideAllShips();

        //отключаем ВСЕ тексты
        if (player1SetupText != null) player1SetupText.SetActive(false);
        if (player2SetupText != null) player2SetupText.SetActive(false);
        if (setupInstructions != null) setupInstructions.SetActive(false);

        //отключаем BattleGridController
        if (battleGridController != null)
        {
            battleGridController.DisableController();
        }

        //отключаем движение кораблей
        if (currentPlayer != null)
        {
            currentPlayer.DisableShipMovement();
        }

        turnInstructions.SetActive(false);
        if (shotStatusText != null) shotStatusText.SetActive(false);

        if (transitionInstructions != null)
        {
            transitionInstructions.SetActive(true);
        }

        Debug.Log($"Переход к фазе {nextPhase}... Нажмите SPACE чтобы продолжить");

        yield return new WaitForSeconds(delay);
        yield return WaitForSpacePress();

        currentPhase = nextPhase;

        switch (nextPhase)
        {
            case GamePhase.Player1Setup:
            case GamePhase.Player2Setup:
                StartSetupPhase();
                break;
            case GamePhase.Player1Turn:
            case GamePhase.Player2Turn:
                StartBattlePhase();
                break;
        }
    }

    IEnumerator WaitForSpacePress()
    {
        Debug.Log("Ожидание нажатия SPACE для продолжения...");

        while (!Input.GetKeyDown(KeyCode.Space))
        {
            yield return null;
        }

        Debug.Log("SPACE нажат, продолжаем...");
        isTransitioning = false;

        if (transitionInstructions != null)
        {
            transitionInstructions.SetActive(false);
        }
    }

    bool EndPlayerSetup()
    {
        if (currentPlayer != null)
        {
            //проверяем все ли корабли расставлены
            if (currentPlayer.AllShipsPlaced())
            {
                currentPlayer.DisableShipPlacement();
                Debug.Log($"Все корабли {currentPlayer.playerName} расставлены!");
                return true;
            }
            else
            {
                //показываем сообщение игроку
                Debug.Log($"Не все корабли {currentPlayer.playerName} расставлены!");

                //можно показать UI сообщение
                if (shotStatusText != null)
                {
                    Text textComponent = shotStatusText.GetComponent<Text>();
                    if (textComponent != null)
                    {
                        //считаем сколько осталось
                        int total = 0;
                        int placed = 0;
                        foreach (Ship ship in currentPlayer.ships)
                        {
                            if (ship != null)
                            {
                                total++;
                                if (ship.isPlaced) placed++;
                            }
                        }

                        textComponent.text = $"РАССТАВЬТЕ ВСЕ КОРАБЛИ!\nОсталось: {total - placed}";
                        textComponent.color = Color.yellow;
                        shotStatusText.SetActive(true);
                        StartCoroutine(HideMessageAfterDelay(2f));
                    }
                }

                return false;
            }
        }

        Debug.LogError("currentPlayer равен null в EndPlayerSetup!");
        return false;
    }

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

    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (shotStatusText != null)
        {
            shotStatusText.SetActive(false);
        }

        messageCoroutine = null;
    }

    void CheckSunkShips()
    {

        foreach (Ship ship in player1.GetComponentsInChildren<Ship>())
        {
            if (ship.isSunk)
            {
                Debug.Log($"Корабль {ship.shipName} игрока 1 потоплен");
            }
        }

        foreach (Ship ship in player2.GetComponentsInChildren<Ship>())
        {
            if (ship.isSunk)
            {
                Debug.Log($"Корабль {ship.shipName} игрока 2 потоплен");
            }
        }
    }

    void SwitchTurn()
    {
        Debug.Log("=== SwitchTurn() начался ===");

        CheckSunkShips();

        if (currentPlayer != null)
        {
            currentPlayer.DisableShipMovement();
            currentPlayer.HideAllShips();
        }

        //отключаем BattleGridController
        if (battleGridController != null)
        {
            battleGridController.DisableController();
        }

        if (currentPhase == GamePhase.Player1Turn)
        {
            currentPhase = GamePhase.TransitionPhase;
            StartCoroutine(TransitionToPhase(GamePhase.Player2Turn, 0.5f));
        }
        else if (currentPhase == GamePhase.Player2Turn)
        {
            currentPhase = GamePhase.TransitionPhase;
            StartCoroutine(TransitionToPhase(GamePhase.Player1Turn, 0.5f));
        }
    }

    void CheckGameOver()
    {
        bool player1AllSunk = player1.AllShipsSunk();
        bool player2AllSunk = player2.AllShipsSunk();

        if (player1AllSunk || player2AllSunk)
        {
            currentPhase = GamePhase.GameOver;

            string winnerName;
            Player winner;
            Player loser;

            if (player2AllSunk) //игрок 2 потоплен -> победил игрок 1
            {
                winnerName = player1.playerName;
                winner = player1;
                loser = player2;
                Debug.Log($"{player1.playerName} победил! Все корабли {player2.playerName} потоплены!");
            }
            else //игрок 1 потоплен -> победил игрок 2
            {
                winnerName = player2.playerName;
                winner = player2;
                loser = player1;
                Debug.Log($"{player2.playerName} победил! Все корабли {player1.playerName} потоплены!");
            }

            EndGame(winner, loser);
        }
    }

    void EndGame(Player winner, Player loser)
    {
        Debug.Log($"=== КОНЕЦ ИГРЫ ===");

        if (player1SetupText != null) player1SetupText.SetActive(false);
        if (player2SetupText != null) player2SetupText.SetActive(false);
        if (setupInstructions != null) setupInstructions.SetActive(false);

        Debug.Log($"Победитель: {winner.playerName}");
        Debug.Log($"Проигравший: {loser.playerName}");

        //показываем ВСЕ корабли на обеих досках
        player1.ShowAllShips();
        player2.ShowAllShips();

        //показываем ВСЕ потопленные корабли проигравшего
        loser.RevealAllSunkShips();

        //также показываем все корабли победителя
        foreach (Ship ship in winner.GetComponentsInChildren<Ship>())
        {
            ship.SetVisible(true);
        }

        //отключаем BattleGridController
        if (battleGridController != null)
        {
            battleGridController.DisableController();
        }

        //отключаем движение кораблей
        player1.DisableShipMovement();
        player2.DisableShipMovement();

        //показываем экран победы
        currentPhase = GamePhase.GameOver;

        //скрываем все UI кроме victoryText
        if (setupInstructions != null) setupInstructions.SetActive(false);
        if (turnInstructions != null) turnInstructions.SetActive(false);
        if (transitionInstructions != null) transitionInstructions.SetActive(false);
        if (shotStatusText != null) shotStatusText.SetActive(false);
        if (player1SetupText != null) player1SetupText.SetActive(false);
        if (player2SetupText != null) player2SetupText.SetActive(false);

        //показываем текст победы (GameObject)
        if (victoryText != null)
        {
            victoryText.SetActive(true);

            //меняем текст в компоненте Text внутри GameObject
            Text textComponent = victoryText.GetComponent<Text>();
            if (textComponent != null)
            {
                textComponent.text = $"{winner.playerName} победил!\nНажмите R для новой игры";
            }
        }

        //выводим финальную статистику
        Debug.Log($"=== ФИНАЛЬНАЯ СТАТИСТИКА ===");
        Debug.Log($"Победитель {winner.playerName}:");
        int winnerAlive = 0;
        foreach (Ship ship in winner.GetComponentsInChildren<Ship>())
        {
            if (!ship.isSunk)
            {
                winnerAlive++;
                Debug.Log($"  {ship.shipName} - цел");
            }
            else
            {
                Debug.Log($"  {ship.shipName} - потоплен");
            }
        }

        Debug.Log($"Проигравший {loser.playerName}:");
        foreach (Ship ship in loser.GetComponentsInChildren<Ship>())
        {
            if (ship.isSunk)
            {
                Debug.Log($"  {ship.shipName} - потоплен");
            }
            else
            {
                Debug.Log($"  {ship.shipName} - цел");
            }
        }

        Debug.Log($"Осталось целых кораблей у победителя: {winnerAlive}");
        Debug.Log($"=== Нажмите R для новой игры ===");

        //включаем возможность перезапуска
        canRestart = true;
    }

    //перезапуск игры (перезагрузка сцены)
    void RestartGame()
    {
        //скрываем текст победы
        if (victoryText != null)
        {
            victoryText.SetActive(false);
        }

        //перезагружаем сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void SetActiveCamera(Camera cam)
    {
        if (player1Camera != null) player1Camera.gameObject.SetActive(false);
        if (player2Camera != null) player2Camera.gameObject.SetActive(false);
        if (cam != null) cam.gameObject.SetActive(true);
    }
}
