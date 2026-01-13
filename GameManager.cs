using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum GamePhase
{
    Player1Setup,
    Player2Setup,
    Player1Turn,
    Player2Turn,
    TransitionPhase,
    GameOver
}

public class GameManager : MonoBehaviour
{
    [Header("Èãðîêè")]
    [SerializeField] private Player player1;
    [SerializeField] private Player player2;

    [Header("Ñåòêè")]
    [SerializeField] private GridManager player1Grid;
    [SerializeField] private GridManager player2Grid;

    [Header("Êàìåðû")]
    [SerializeField] private Camera player1Camera;
    [SerializeField] private Camera player2Camera;

    [Header("UI")]
    [SerializeField] private GameObject setupInstructions;
    [SerializeField] private GameObject turnInstructions;
    [SerializeField] private GameObject transitionInstructions;
    [SerializeField] private GameObject shotStatusText;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject victoryText;
    [SerializeField] private GameObject player1SetupText;
    [SerializeField] private GameObject player2SetupText;

    [Header("Êîíòðîëëåð áîÿ")]
    [SerializeField] private BattleGridController battleGridController;

    [Header("Íàñòðîéêè èãðû")]
    [SerializeField] private float endGameDelay = 3f;

    private GamePhase currentPhase = GamePhase.Player1Setup;
    private Player currentPlayer;
    private Player opponentPlayer;
    private GridManager currentGrid;
    private Camera currentCamera;
    private bool isTransitioning = false;
    private bool canRestart = false;
    private Coroutine messageCoroutine;

    void Start()
    {
        if (battleGridController == null)
        {
            battleGridController = FindObjectOfType<BattleGridController>();
            if (battleGridController == null)
            {
                Debug.LogError("GameManager: BattleGridController íå íàéäåí â ñöåíå!");
            }
            else
            {
                Debug.Log("GameManager: BattleGridController íàéäåí àâòîìàòè÷åñêè");
            }
        }

        InitializeGame();
        StartSetupPhase();
    }

    void Update()
    {
        HandleInput();

        if (canRestart && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    void InitializeGame()
    {
        player1.Initialize("Èãðîê 1", player1Grid);
        player2.Initialize("Èãðîê 2", player2Grid);
        player1.HideAllShips();
        player2.HideAllShips();

        if (victoryScreen != null)
        {
            victoryScreen.SetActive(false);
        }

        canRestart = false;
    }

    void StartSetupPhase()
    {
        Debug.Log($"=== StartSetupPhase: {currentPhase} ===");
        Debug.Log($"player1SetupText íàçíà÷åí: {player1SetupText != null}");
        Debug.Log($"player2SetupText íàçíà÷åí: {player2SetupText != null}");

        switch (currentPhase)
        {
            case GamePhase.Player1Setup:
                Debug.Log("Ïîêàçûâàåì player1SetupText");
                if (player1SetupText != null)
                {
                    player1SetupText.SetActive(true);
                    Debug.Log($"player1SetupText àêòèâåí: {player1SetupText.activeSelf}");
                }
                if (player2SetupText != null)
                {
                    player2SetupText.SetActive(false);
                    Debug.Log($"player2SetupText àêòèâåí: {player2SetupText.activeSelf}");
                }
                StartPlayerSetup(player1, player1Grid, player1Camera);
                break;

            case GamePhase.Player2Setup:
                Debug.Log("Ïîêàçûâàåì player2SetupText");
                if (player2SetupText != null)
                {
                    player2SetupText.SetActive(true);
                    Debug.Log($"player2SetupText àêòèâåí: {player2SetupText.activeSelf}");
                }
                if (player1SetupText != null)
                {
                    player1SetupText.SetActive(false);
                    Debug.Log($"player1SetupText àêòèâåí: {player1SetupText.activeSelf}");
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
        if (player1SetupText != null) player1SetupText.SetActive(false);
        if (player2SetupText != null) player2SetupText.SetActive(false);

        switch (currentPhase)
        {
            case GamePhase.Player1Turn:
                StartPlayerTurn(player1, player2, player1Grid, player1Camera);
                break;
            case GamePhase.Player2Turn:
                StartPlayerTurn(player2, player1, player2Grid, player2Camera);
                break;
        }

        setupInstructions.SetActive(false);
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

        opponent.RevealAllSunkShips();
        Debug.Log($"Â íà÷àëå õîäà {player.playerName} ïîêàçàíû ïîòîïëåííûå êîðàáëè {opponent.playerName}");

        player.ShowAllShips();
        player.ResetShipsActions();
        player.EnableShipMovement();

        if (battleGridController != null)
        {
            GridManager targetGrid = (player == player1) ? player2Grid : player1Grid;
            Camera targetCamera = (player == player1) ? player1Camera : player2Camera;

            battleGridController.SetupForPlayerTurn(player, opponent, targetGrid, targetCamera);
            Debug.Log($"BattleGridController íàñòðîåí äëÿ ñòðåëüáû {player.playerName} -> {opponent.playerName}");
        }

        setupInstructions.SetActive(false);
        turnInstructions.SetActive(true);
        if (transitionInstructions != null) transitionInstructions.SetActive(false);
        if (shotStatusText != null) shotStatusText.SetActive(false);

        isTransitioning = false;

        Debug.Log($"Õîä {player.playerName}. Âèäíû ïîòîïëåííûå êîðàáëè ïðîòèâíèêà.");
    }

    public void ProcessBattleShot(bool hit)
    {
        ShowShotMessage(hit);

        if (hit)
        {
            Debug.Log($"{currentPlayer.playerName} ïîïàë! Ìîæåò ñòðåëÿòü åùå ðàç");
            CheckGameOver();
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} ïðîìàõíóëñÿ! Ïåðåõîä õîäà");
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
                    if (EndPlayerSetup())
                    {
                        currentPhase = GamePhase.TransitionPhase;
                        StartCoroutine(TransitionToPhase(GamePhase.Player2Setup, 0.5f));
                    }
                    break;

                case GamePhase.Player2Setup:
                    if (EndPlayerSetup())
                    {
                        currentPhase = GamePhase.TransitionPhase;
                        StartCoroutine(TransitionToPhase(GamePhase.Player1Turn, 0.5f));
                    }
                    break;

                case GamePhase.Player1Turn:
                case GamePhase.Player2Turn:
                    Debug.Log($"Ïðèíóäèòåëüíûé ïåðåõîä õîäà îò {currentPlayer.playerName}");
                    CheckGameOver();
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

        if (battleGridController != null)
        {
            battleGridController.DisableController();
        }

        if (currentPlayer != null)
        {
            currentPlayer.DisableShipMovement();
        }

        setupInstructions.SetActive(false);
        turnInstructions.SetActive(false);
        if (shotStatusText != null) shotStatusText.SetActive(false);

        if (transitionInstructions != null)
        {
            transitionInstructions.SetActive(true);
        }

        Debug.Log($"Ïåðåõîä ê ôàçå {nextPhase}... Íàæìèòå SPACE ÷òîáû ïðîäîëæèòü");

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
        Debug.Log("Îæèäàíèå íàæàòèÿ SPACE äëÿ ïðîäîëæåíèÿ...");

        while (!Input.GetKeyDown(KeyCode.Space))
        {
            yield return null;
        }

        Debug.Log("SPACE íàæàò, ïðîäîëæàåì...");
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
            if (currentPlayer.AllShipsPlaced())
            {
                currentPlayer.DisableShipPlacement();
                Debug.Log($"Âñå êîðàáëè {currentPlayer.playerName} ðàññòàâëåíû!");
                return true;
            }
            else
            {
                Debug.Log($"Íå âñå êîðàáëè {currentPlayer.playerName} ðàññòàâëåíû!");

                if (shotStatusText != null)
                {
                    Text textComponent = shotStatusText.GetComponent<Text>();
                    if (textComponent != null)
                    {
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

                        textComponent.text = $"ÐÀÑÑÒÀÂÜÒÅ ÂÑÅ ÊÎÐÀÁËÈ!\nÎñòàëîñü: {total - placed}";
                        textComponent.color = Color.yellow;
                        shotStatusText.SetActive(true);
                        StartCoroutine(HideMessageAfterDelay(2f));
                    }
                }

                return false;
            }
        }

        Debug.LogError("currentPlayer ðàâåí null â EndPlayerSetup!");
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
            textComponent.text = "ÏÎÏÀÄÀÍÈÅ! Ñòðåëÿéòå åùå ðàç";
            textComponent.color = Color.green;
        }
        else
        {
            textComponent.text = "ÏÐÎÌÀÕ! Õîä ïåðåõîäèò ïðîòèâíèêó";
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
                Debug.Log($"Êîðàáëü {ship.shipName} èãðîêà 1 ïîòîïëåí");
            }
        }

        foreach (Ship ship in player2.GetComponentsInChildren<Ship>())
        {
            if (ship.isSunk)
            {
                Debug.Log($"Êîðàáëü {ship.shipName} èãðîêà 2 ïîòîïëåí");
            }
        }
    }

    void SwitchTurn()
    {
        Debug.Log("=== SwitchTurn() íà÷àëñÿ ===");

        CheckSunkShips();

        if (currentPlayer != null)
        {
            currentPlayer.DisableShipMovement();
            currentPlayer.HideAllShips();
        }

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

            if (player2AllSunk)
            {
                winnerName = player1.playerName;
                winner = player1;
                loser = player2;
                Debug.Log($"{player1.playerName} ïîáåäèë! Âñå êîðàáëè {player2.playerName} ïîòîïëåíû!");
            }
            else
            {
                winnerName = player2.playerName;
                winner = player2;
                loser = player1;
                Debug.Log($"{player2.playerName} ïîáåäèë! Âñå êîðàáëè {player1.playerName} ïîòîïëåíû!");
            }

            EndGame(winner, loser);
        }
    }

    void EndGame(Player winner, Player loser)
    {
        Debug.Log($"=== ÊÎÍÅÖ ÈÃÐÛ ===");
        Debug.Log($"Ïîáåäèòåëü: {winner.playerName}");
        Debug.Log($"Ïðîèãðàâøèé: {loser.playerName}");

        player1.ShowAllShips();
        player2.ShowAllShips();

        loser.RevealAllSunkShips();

        foreach (Ship ship in winner.GetComponentsInChildren<Ship>())
        {
            ship.SetVisible(true);
        }

        if (battleGridController != null)
        {
            battleGridController.DisableController();
        }

        player1.DisableShipMovement();
        player2.DisableShipMovement();

        currentPhase = GamePhase.GameOver;

        if (setupInstructions != null) setupInstructions.SetActive(false);
        if (turnInstructions != null) turnInstructions.SetActive(false);
        if (transitionInstructions != null) transitionInstructions.SetActive(false);
        if (shotStatusText != null) shotStatusText.SetActive(false);
        if (player1SetupText != null) player1SetupText.SetActive(false);
        if (player2SetupText != null) player2SetupText.SetActive(false);

        if (victoryText != null)
        {
            victoryText.SetActive(true);

            Text textComponent = victoryText.GetComponent<Text>();
            if (textComponent != null)
            {
                textComponent.text = $"{winner.playerName} ïîáåäèë!\nÍàæìèòå R äëÿ íîâîé èãðû";
            }
        }

        Debug.Log($"=== ÔÈÍÀËÜÍÀß ÑÒÀÒÈÑÒÈÊÀ ===");
        Debug.Log($"Ïîáåäèòåëü {winner.playerName}:");
        int winnerAlive = 0;
        foreach (Ship ship in winner.GetComponentsInChildren<Ship>())
        {
            if (!ship.isSunk)
            {
                winnerAlive++;
                Debug.Log($"  {ship.shipName} - öåë");
            }
            else
            {
                Debug.Log($"  {ship.shipName} - ïîòîïëåí");
            }
        }

        Debug.Log($"Ïðîèãðàâøèé {loser.playerName}:");
        foreach (Ship ship in loser.GetComponentsInChildren<Ship>())
        {
            if (ship.isSunk)
            {
                Debug.Log($"  {ship.shipName} - ïîòîïëåí");
            }
            else
            {
                Debug.Log($"  {ship.shipName} - öåë");
            }
        }

        Debug.Log($"Îñòàëîñü öåëûõ êîðàáëåé ó ïîáåäèòåëÿ: {winnerAlive}");
        Debug.Log($"=== Íàæìèòå R äëÿ íîâîé èãðû ===");

        canRestart = true;
    }

    void RestartGame()
    {
        if (victoryText != null)
        {
            victoryText.SetActive(false);
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void SetActiveCamera(Camera cam)
    {
        if (player1Camera != null) player1Camera.gameObject.SetActive(false);
        if (player2Camera != null) player2Camera.gameObject.SetActive(false);
        if (cam != null) cam.gameObject.SetActive(true);
    }

}
