using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

// Relay 서비스만 사용하도록 명시적 using
using RelayAllocation = Unity.Services.Relay.Models.Allocation;
using RelayJoinAllocation = Unity.Services.Relay.Models.JoinAllocation;
using RelayServiceInstance = Unity.Services.Relay.RelayService;

public class RelayNetworkManager : MonoBehaviour
{
    [SerializeField] private int maxPlayers = 2;
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private bool enableDebugLogs = true;

    public static event Action<string> OnJoinCodeGenerated;
    public static event Action<bool> OnConnectionStatusChanged;
    public static event Action<string> OnNetworkError;
    public static event Action OnGameStarted;

    public bool IsHost { get; private set; }
    public bool IsConnected => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
    public string CurrentJoinCode { get; private set; }
    public bool IsCreatingRoom { get; private set; }
    public bool IsJoiningRoom { get; private set; }

    private NetworkManager networkManager;
    private UnityTransport transport;

    public static RelayNetworkManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Root GameObject로 만들기
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);

        InitializeComponents();
    }

    private void Start()
    {
        InitializeUnityServices();
    }

    private void InitializeComponents()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();
        if (networkManager == null)
        {
            var nmObject = new GameObject("NetworkManager");
            DontDestroyOnLoad(nmObject);
            networkManager = nmObject.AddComponent<NetworkManager>();
        }

        transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            transport = networkManager.gameObject.AddComponent<UnityTransport>();
        }

        networkManager.NetworkConfig.NetworkTransport = transport;

        // 네트워크 씬 관리 설정 - 중요!
        if (networkManager.NetworkConfig != null)
        {
            networkManager.NetworkConfig.EnableSceneManagement = true;
        }

        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        networkManager.OnServerStarted += OnServerStarted;

        // 씬 이벤트 리스너 추가
        if (networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnSceneEvent += OnSceneEvent;
        }
    }

    private async void InitializeUnityServices()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
                DebugLog("Unity Services initialized successfully");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                DebugLog("Unity Authentication signed in: " + AuthenticationService.Instance.PlayerId);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Unity Services initialization failed: " + e.Message);
            OnNetworkError?.Invoke("Service initialization failed.");
        }
    }

    public async Task<string> StartHostAsync()
    {
        // 중복 실행 방지 - 단, 이미 성공한 경우 기존 코드 반환
        if (IsCreatingRoom)
        {
            DebugLog("Room creation already in progress");
            return null;
        }

        if (IsHost || IsConnected)
        {
            DebugLog("Already hosting or connected, returning existing join code");
            return CurrentJoinCode;
        }

        // 다른 네트워크 작업이 진행 중이면 대기
        if (IsJoiningRoom)
        {
            DebugLog("Room join in progress, cannot create room now");
            return null;
        }

        try
        {
            IsCreatingRoom = true;
            DebugLog("Starting host...");

            // 기존 연결이 있다면 정리
            await EnsureNetworkShutdown();


            DebugLog("Creating relay allocation...");
            RelayAllocation allocation = await RelayServiceInstance.Instance.CreateAllocationAsync(maxPlayers - 1);

            DebugLog("Getting join code...");
            CurrentJoinCode = await RelayServiceInstance.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var relayServerData = new RelayServerData(allocation, "dtls");
            transport.SetRelayServerData(relayServerData);

            bool success = networkManager.StartHost();

            if (success)
            {
                IsHost = true;
                DebugLog("Host started successfully! Join Code: " + CurrentJoinCode);
                OnJoinCodeGenerated?.Invoke(CurrentJoinCode);
                OnConnectionStatusChanged?.Invoke(true);
                return CurrentJoinCode;
            }
            else
            {
                throw new Exception("Failed to start host.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start host: " + e.Message);
            OnNetworkError?.Invoke("Failed to create room.");
            return null;
        }
        finally
        {
            IsCreatingRoom = false;
        }
    }

    public async Task<bool> JoinGameAsync(string joinCode)
    {
        // 중복 실행 방지
        if (IsJoiningRoom)
        {
            DebugLog("Room join already in progress");
            return false;
        }

        if (IsHost || IsConnected)
        {
            DebugLog("Already hosting or connected, cannot join new room");
            return false;
        }

        // 다른 네트워크 작업이 진행 중이면 대기
        if (IsCreatingRoom)
        {
            DebugLog("Room creation in progress, cannot join room now");
            return false;
        }

        try
        {
            IsJoiningRoom = true;
            DebugLog("Joining game... Join Code: " + joinCode);

            // 기존 연결이 있다면 정리
            await EnsureNetworkShutdown();

            RelayJoinAllocation allocation = await RelayServiceInstance.Instance.JoinAllocationAsync(joinCode);

            var relayServerData = new RelayServerData(allocation, "dtls");
            transport.SetRelayServerData(relayServerData);

            bool success = networkManager.StartClient();

            if (success)
            {
                IsHost = false;
                CurrentJoinCode = joinCode;
                DebugLog("Client started successfully!");
                return true;
            }
            else
            {
                throw new Exception("Failed to start client.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to join game: " + e.Message);
            OnNetworkError?.Invoke("Failed to join game. Please check the room code.");
            return false;
        }
        finally
        {
            IsJoiningRoom = false;
        }
    }

    /// <summary>
    /// 기존 네트워크 연결을 안전하게 종료하고 정리
    /// </summary>
    private async Task EnsureNetworkShutdown()
    {
        if (networkManager != null && (networkManager.IsHost || networkManager.IsClient || networkManager.IsServer))
        {
            DebugLog("Shutting down existing network connection...");
            networkManager.Shutdown();

            // 네트워크 정리를 위한 잠시 대기
            await Task.Delay(200);

            // 상태 초기화
            ResetNetworkState();

            DebugLog("Network shutdown completed");
        }
    }

    private void OnServerStarted()
    {
        DebugLog("Server started");
    }

    private void OnClientConnected(ulong clientId)
    {
        DebugLog("Client connected: " + clientId);

        if (IsHost)
        {
            int connectedPlayers = networkManager.ConnectedClients.Count;
            DebugLog("Connected players: " + connectedPlayers + "/" + maxPlayers);

            if (connectedPlayers >= maxPlayers)
            {
                StartGameForAllClients();
            }
        }
        else
        {
            OnConnectionStatusChanged?.Invoke(true);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        DebugLog("Client disconnected: " + clientId);

        if (clientId == networkManager.LocalClientId)
        {
            OnConnectionStatusChanged?.Invoke(false);
            ResetNetworkState();
        }
    }

    private void StartGameForAllClients()
    {
        if (!IsHost) return;

        DebugLog("All players connected. Starting game!");

        // 먼저 클라이언트들에게 알림
        StartGameClientRpc();

        // 네트워크 씬 매니저를 사용하여 씬 로드 - 핵심 수정사항!
        LoadGameSceneNetwork();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        DebugLog("Received game start signal");
        OnGameStarted?.Invoke();
    }

    // 네트워크 씬 로드 - 새로운 메서드
    private void LoadGameSceneNetwork()
    {
        if (!IsHost)
        {
            DebugLog("Only host can initiate scene changes");
            return;
        }

        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("Game scene name is empty!");
            return;
        }

        DebugLog($"Loading game scene via NetworkSceneManager: {gameSceneName}");

        try
        {
            // NetworkSceneManager를 사용하여 모든 클라이언트에게 씬 변경 요청
            var sceneEventProgressStatus = networkManager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);

            if (sceneEventProgressStatus != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"Failed to start scene loading. Status: {sceneEventProgressStatus}");

                // 백업: 빌드 인덱스로 시도
                TryLoadSceneByIndex();
            }
            else
            {
                DebugLog($"Scene loading started successfully: {gameSceneName}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception during scene loading: {e.Message}");
            TryLoadSceneByIndex();
        }
    }

    private void TryLoadSceneByIndex()
    {
        try
        {
            DebugLog("Trying alternative scene loading approaches...");

            // 방법 1: 일반적인 씬 이름들 시도
            string[] commonSceneNames = { "GameScene", "Game" };

            foreach (string sceneName in commonSceneNames)
            {
                try
                {
                    var sceneEventProgressStatus = networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

                    if (sceneEventProgressStatus == SceneEventProgressStatus.Started)
                    {
                        DebugLog($"Scene loading started successfully with name: {sceneName}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    DebugLog($"Failed to load scene '{sceneName}': {ex.Message}");
                }
            }

            Debug.LogError("All scene loading attempts failed. Please check Build Settings.");
            Debug.LogError("Make sure your GameScene is added to Build Settings (File -> Build Settings)");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load scene by alternative methods: {e.Message}");
        }
    }

    // 씬 이벤트 핸들러 - 새로운 메서드
    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        DebugLog($"Scene event: {sceneEvent.SceneEventType} - Scene: {sceneEvent.SceneName}");

        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.Load:
                DebugLog($"Scene load started: {sceneEvent.SceneName}");
                break;
            case SceneEventType.LoadComplete:
                DebugLog($"Scene load completed: {sceneEvent.SceneName}");
                break;
            case SceneEventType.LoadEventCompleted:
                DebugLog($"All clients loaded scene: {sceneEvent.SceneName}");
                break;
            case SceneEventType.Unload:
                DebugLog($"Scene unload started: {sceneEvent.SceneName}");
                break;
            case SceneEventType.UnloadComplete:
                DebugLog($"Scene unload completed: {sceneEvent.SceneName}");
                break;
        }
    }

    public async void DisconnectClient()
    {
        if (networkManager != null && networkManager.IsClient)
        {
            DebugLog("Disconnecting client...");
            networkManager.Shutdown();
            await Task.Delay(100); // 정리 시간
        }
        ResetNetworkState();
    }

    public async void StopHost()
    {
        if (networkManager != null && networkManager.IsHost)
        {
            DebugLog("Stopping host...");
            networkManager.Shutdown();
            await Task.Delay(100); // 정리 시간
        }
        ResetNetworkState();
    }

    private void ResetNetworkState()
    {
        IsHost = false;
        IsCreatingRoom = false;
        IsJoiningRoom = false;
        CurrentJoinCode = null;
        DebugLog("Network state reset");
    }

    public string GenerateRoomCode()
    {
        return UnityEngine.Random.Range(100000, 999999).ToString();
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log("[RelayNetworkManager] " + message);
        }
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            networkManager.OnServerStarted -= OnServerStarted;

            if (networkManager.SceneManager != null)
            {
                networkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && IsConnected)
        {
            DebugLog("App paused");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && IsConnected)
        {
            DebugLog("App lost focus");
        }
    }


    public void LogCurrentState()
    {
        if (enableDebugLogs)
        {
            DebugLog($"Current State - IsHost: {IsHost}, IsConnected: {IsConnected}, " +
                    $"IsCreatingRoom: {IsCreatingRoom}, IsJoiningRoom: {IsJoiningRoom}, " +
                    $"JoinCode: {CurrentJoinCode ?? "null"}");

            if (networkManager != null)
            {
                DebugLog($"NetworkManager State - IsHost: {networkManager.IsHost}, " +
                        $"IsClient: {networkManager.IsClient}, IsServer: {networkManager.IsServer}");
            }
        }
    }
}