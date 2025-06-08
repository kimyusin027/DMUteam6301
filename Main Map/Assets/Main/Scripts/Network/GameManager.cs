using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private string lobbySceneName = "LobbyScene";

    [Header("UI Components")]
    [SerializeField] private GameObject gameUI;
    [SerializeField] private TextMeshProUGUI connectedPlayersText;
    [SerializeField] private TextMeshProUGUI gameStatusText;
    [SerializeField] private Button leaveGameButton;
    [SerializeField] private GameObject pauseMenu;

    [Header("Game Settings")]
    [SerializeField] private float gameTimer = 300f;

    private NetworkVariable<float> networkGameTimer = new NetworkVariable<float>();
    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>();

    private bool gameInitialized = false;
    private bool gamePaused = false;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeGame();
        SetupUI();
    }

    public override void OnNetworkSpawn()
    {
        networkGameTimer.OnValueChanged += OnGameTimerChanged;
        gameStarted.OnValueChanged += OnGameStartedChanged;

        if (IsServer)
        {
            StartCoroutine(InitializeGameCoroutine());
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        Debug.Log($"GameManager spawned - IsServer: {IsServer}, IsHost: {IsHost}, IsClient: {IsClient}");
    }

    public override void OnNetworkDespawn()
    {
        networkGameTimer.OnValueChanged -= OnGameTimerChanged;
        gameStarted.OnValueChanged -= OnGameStartedChanged;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void InitializeGame()
    {
        if (gameUI != null) gameUI.SetActive(true);
        if (pauseMenu != null) pauseMenu.SetActive(false);

        if (IsServer)
        {
            networkGameTimer.Value = gameTimer;
        }

        Debug.Log("Game Manager initialized");
    }

    private void SetupUI()
    {
        if (leaveGameButton != null)
        {
            leaveGameButton.onClick.AddListener(OnLeaveGameClicked);
        }

        UpdateUI();
    }

    private IEnumerator InitializeGameCoroutine()
    {
        Debug.Log("Initializing game coroutine started");

        // 잠시 대기
        yield return new WaitForSeconds(2f);

        Debug.Log($"Connected clients count: {NetworkManager.Singleton.ConnectedClients.Count}");

        // 게임 시작
        StartGame();
    }

    private void StartGame()
    {
        if (!IsServer) return;

        gameStarted.Value = true;
        Debug.Log("Game started!");

        StartCoroutine(GameTimerCoroutine());
        NotifyGameStartClientRpc();
    }

    [ClientRpc]
    private void NotifyGameStartClientRpc()
    {
        Debug.Log("Received game start notification");
        UpdateGameStatus("Game Started!");
    }

    private IEnumerator GameTimerCoroutine()
    {
        while (networkGameTimer.Value > 0 && gameStarted.Value)
        {
            yield return new WaitForSeconds(1f);

            if (!gamePaused)
            {
                networkGameTimer.Value -= 1f;
            }
        }

        EndGame();
    }

    private void EndGame()
    {
        if (!IsServer) return;

        gameStarted.Value = false;
        Debug.Log("Game ended!");

        NotifyGameEndClientRpc();
        StartCoroutine(ReturnToLobbyCoroutine());
    }

    [ClientRpc]
    private void NotifyGameEndClientRpc()
    {
        Debug.Log("Received game end notification");
        UpdateGameStatus("Game Ended!");
    }

    private IEnumerator ReturnToLobbyCoroutine()
    {
        yield return new WaitForSeconds(5f);

        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
        UpdateConnectedPlayersCount();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client disconnected: {clientId}");
        UpdateConnectedPlayersCount();

        // 호스트가 연결을 끊었을 때 처리
        if (clientId == 0 && IsClient && !IsServer)
        {
            HandleHostDisconnection();
        }
    }

    private void HandleHostDisconnection()
    {
        Debug.Log("Host disconnected. Returning to lobby.");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(lobbySceneName);
    }

    private void OnGameTimerChanged(float oldValue, float newValue)
    {
        UpdateGameTimerDisplay();
    }

    private void OnGameStartedChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            UpdateGameStatus("Game In Progress");
        }
        else
        {
            UpdateGameStatus("Game Waiting");
        }
    }

    private void UpdateUI()
    {
        UpdateConnectedPlayersCount();
        UpdateGameTimerDisplay();

        if (gameStarted.Value)
        {
            UpdateGameStatus("Game In Progress");
        }
        else
        {
            UpdateGameStatus("Game Waiting");
        }
    }

    private void UpdateConnectedPlayersCount()
    {
        if (connectedPlayersText != null && NetworkManager.Singleton != null)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
            connectedPlayersText.text = $"Players: {playerCount}/2";
        }
    }

    private void UpdateGameTimerDisplay()
    {
        if (gameStatusText != null)
        {
            int minutes = Mathf.FloorToInt(networkGameTimer.Value / 60f);
            int seconds = Mathf.FloorToInt(networkGameTimer.Value % 60f);
            string timerText = minutes.ToString("00") + ":" + seconds.ToString("00");

            if (gameStarted.Value)
            {
                gameStatusText.text = "Time Remaining: " + timerText;
            }
        }
    }

    private void UpdateGameStatus(string status)
    {
        if (gameStatusText != null && !gameStarted.Value)
        {
            gameStatusText.text = status;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    private void TogglePauseMenu()
    {
        if (pauseMenu != null)
        {
            bool isActive = !pauseMenu.activeInHierarchy;
            pauseMenu.SetActive(isActive);
            gamePaused = isActive;

            Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isActive;
        }
    }

    private void OnLeaveGameClicked()
    {
        LeaveGame();
    }

    public void LeaveGame()
    {
        Debug.Log("Leaving game");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(lobbySceneName);
    }

    public bool IsGameActive()
    {
        return gameStarted.Value;
    }

    public float GetRemainingTime()
    {
        return networkGameTimer.Value;
    }

    public int GetConnectedPlayersCount()
    {
        return NetworkManager.Singleton?.ConnectedClients?.Count ?? 0;
    }
}