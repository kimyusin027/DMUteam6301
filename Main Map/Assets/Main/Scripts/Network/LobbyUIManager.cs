using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private GameObject joinRoomPanel;
    [SerializeField] private GameObject waitingRoomPanel;

    [SerializeField] private TextMeshProUGUI welcomeText;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private Button logoutButton;

    [SerializeField] private TextMeshProUGUI roomCodeDisplay;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private Button cancelCreateButton;
    [SerializeField] private Button enterWaitingRoomButton;
    [SerializeField] private TextMeshProUGUI waitingText;

    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button confirmJoinButton;
    [SerializeField] private Button cancelJoinButton;

    [SerializeField] private TextMeshProUGUI connectedPlayersText;
    [SerializeField] private TextMeshProUGUI currentRoomCodeText;
    [SerializeField] private Button leaveRoomButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private AudioSource buttonClickSound;

    [SerializeField] private bool enableSounds = true;

    private RelayNetworkManager networkManager;
    private AuthenticationManager authManager;
    private bool isProcessingRequest = false;
    private string currentJoinCode = "";

    private void Start()
    {
        networkManager = RelayNetworkManager.Instance;
        authManager = AuthenticationManager.Instance;

        InitializeUI();
        SetupEventListeners();

        UpdateWelcomeText();

        ShowMainMenu();
    }

    private void InitializeUI()
    {
        SetLoadingState(false);
        ClearErrorText();

        // 패널들 초기화 - 모든 패널을 비활성화
        HideAllPanels();

        // 룸 코드 입력 필드 설정
        if (roomCodeInput != null)
        {
            roomCodeInput.characterLimit = 6;
            roomCodeInput.contentType = TMP_InputField.ContentType.Alphanumeric;
        }
    }

    private void SetupEventListeners()
    {
        // 버튼 이벤트 - Null 체크 추가
        if (createRoomButton != null) createRoomButton.onClick.AddListener(() => OnButtonClick(OnCreateRoomButtonClicked));
        if (joinRoomButton != null) joinRoomButton.onClick.AddListener(() => OnButtonClick(OnJoinRoomButtonClicked));
        if (logoutButton != null) logoutButton.onClick.AddListener(() => OnButtonClick(OnLogoutButtonClicked));

        if (copyCodeButton != null) copyCodeButton.onClick.AddListener(() => OnButtonClick(OnCopyCodeButtonClicked));
        if (cancelCreateButton != null) cancelCreateButton.onClick.AddListener(() => OnButtonClick(OnCancelCreateButtonClicked));
        if (enterWaitingRoomButton != null) enterWaitingRoomButton.onClick.AddListener(() => OnButtonClick(OnEnterWaitingRoomClicked));

        if (confirmJoinButton != null) confirmJoinButton.onClick.AddListener(() => OnButtonClick(OnConfirmJoinButtonClicked));
        if (cancelJoinButton != null) cancelJoinButton.onClick.AddListener(() => OnButtonClick(OnCancelJoinButtonClicked));
        if (roomCodeInput != null) roomCodeInput.onValueChanged.AddListener(OnRoomCodeInputChanged);

        if (leaveRoomButton != null) leaveRoomButton.onClick.AddListener(() => OnButtonClick(OnLeaveRoomButtonClicked));

        // 네트워크 이벤트
        RelayNetworkManager.OnJoinCodeGenerated += OnJoinCodeGenerated;
        RelayNetworkManager.OnConnectionStatusChanged += OnConnectionStatusChanged;
        RelayNetworkManager.OnNetworkError += OnNetworkError;
        RelayNetworkManager.OnGameStarted += OnGameStarted;
    }

    private void OnDestroy()
    {
        RelayNetworkManager.OnJoinCodeGenerated -= OnJoinCodeGenerated;
        RelayNetworkManager.OnConnectionStatusChanged -= OnConnectionStatusChanged;
        RelayNetworkManager.OnNetworkError -= OnNetworkError;
        RelayNetworkManager.OnGameStarted -= OnGameStarted;
    }

    private void ShowMainMenu()
    {
        Debug.Log("ShowMainMenu called");
        HideAllPanels();
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            Debug.Log("Main menu panel activated");
        }
        else
        {
            Debug.LogError("Main menu panel is null!");
        }
        UpdateWelcomeText();
    }

    private void ShowCreateRoomPanel()
    {
        Debug.Log("ShowCreateRoomPanel called");
        HideAllPanels();

        if (createRoomPanel != null)
        {
            createRoomPanel.SetActive(true);
            Debug.Log("Create room panel activated - Active state: " + createRoomPanel.activeInHierarchy);

            // Canvas Group이 있는지 확인
            CanvasGroup canvasGroup = createRoomPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Debug.Log("CanvasGroup found - Alpha: " + canvasGroup.alpha + ", Interactable: " + canvasGroup.interactable);
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
            }
        }
        else
        {
            Debug.LogError("Create room panel is null!");
        }

        // UI 요소들 초기화
        if (waitingText != null)
        {
            waitingText.text = "Creating room...";
            Debug.Log("Waiting text set to: Creating room...");
        }

        if (roomCodeDisplay != null)
        {
            roomCodeDisplay.text = "Creating...";
            Debug.Log("Room code display set to: Creating...");
        }

        // 버튼 상태 초기화
        if (enterWaitingRoomButton != null)
        {
            enterWaitingRoomButton.interactable = false;
            Debug.Log("Enter Waiting Room button disabled initially");
        }
    }

    private void ShowJoinRoomPanel()
    {
        Debug.Log("ShowJoinRoomPanel called");
        HideAllPanels();

        if (joinRoomPanel != null)
        {
            joinRoomPanel.SetActive(true);
            Debug.Log("Join room panel activated - Active state: " + joinRoomPanel.activeInHierarchy);

            // Canvas Group이 있는지 확인
            CanvasGroup canvasGroup = joinRoomPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
            }
        }
        else
        {
            Debug.LogError("Join room panel is null!");
        }

        if (roomCodeInput != null) roomCodeInput.text = "";
        if (confirmJoinButton != null) confirmJoinButton.interactable = false;
    }

    private void ShowWaitingRoom()
    {
        Debug.Log("ShowWaitingRoom called");
        HideAllPanels();

        if (waitingRoomPanel != null)
        {
            waitingRoomPanel.SetActive(true);
            Debug.Log("Waiting room panel activated - Active state: " + waitingRoomPanel.activeInHierarchy);

            // Canvas Group이 있는지 확인
            CanvasGroup canvasGroup = waitingRoomPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
            }
        }
        else
        {
            Debug.LogError("Waiting room panel is null!");
        }

        UpdateWaitingRoomInfo();
    }

    private void HideAllPanels()
    {
        Debug.Log("HideAllPanels called");

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
            Debug.Log("Main menu panel hidden");
        }

        if (createRoomPanel != null)
        {
            createRoomPanel.SetActive(false);
            Debug.Log("Create room panel hidden");
        }

        if (joinRoomPanel != null)
        {
            joinRoomPanel.SetActive(false);
            Debug.Log("Join room panel hidden");
        }

        if (waitingRoomPanel != null)
        {
            waitingRoomPanel.SetActive(false);
            Debug.Log("Waiting room panel hidden");
        }
    }

    private void OnCreateRoomButtonClicked()
    {
        Debug.Log("OnCreateRoomButtonClicked called");

        // 중복 실행 방지 - 더 강력한 체크
        if (isProcessingRequest)
        {
            Debug.Log("Room creation request already in progress - ignoring");
            return;
        }

        Debug.Log("Create Room button clicked - showing panel");
        ShowCreateRoomPanel();

        // 비동기 작업을 별도 메서드로 시작
        StartCreateRoom();
    }

    private async void StartCreateRoom()
    {
        if (isProcessingRequest)
        {
            Debug.Log("Create room already in progress");
            return;
        }

        isProcessingRequest = true;
        SetLoadingState(true);

        Debug.Log("Calling StartHostAsync...");

        try
        {
            string joinCode = await networkManager.StartHostAsync();

            Debug.Log("StartHostAsync completed with join code: " + (joinCode ?? "null"));

            if (!string.IsNullOrEmpty(joinCode))
            {
                currentJoinCode = joinCode;
                Debug.Log("Room created successfully. Join code: " + joinCode);

                // 룸 코드 UI 업데이트
                if (roomCodeDisplay != null)
                {
                    roomCodeDisplay.text = joinCode;
                }

                if (waitingText != null)
                {
                    waitingText.text = "Room created! Share the code with your friend.";
                }

                // Enter Waiting Room 버튼 활성화
                if (enterWaitingRoomButton != null)
                {
                    enterWaitingRoomButton.interactable = true;
                    Debug.Log("Enter Waiting Room button enabled");
                }
            }
            else
            {
                Debug.LogWarning("Join code is null/empty - staying on create room panel");
                ShowErrorMessage("Failed to create room. Please try again.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error creating room: " + e.Message);
            ShowErrorMessage("Error creating room: " + e.Message);
        }
        finally
        {
            SetLoadingState(false);
            isProcessingRequest = false;
        }
    }

    private void OnJoinRoomButtonClicked()
    {
        Debug.Log("OnJoinRoomButtonClicked called");
        ShowJoinRoomPanel();
    }

    private void OnLogoutButtonClicked()
    {
        if (networkManager != null && networkManager.IsConnected)
        {
            networkManager.DisconnectClient();
        }

        if (authManager != null)
        {
            authManager.Logout();
        }
    }

    private void OnCopyCodeButtonClicked()
    {
        if (!string.IsNullOrEmpty(currentJoinCode))
        {
            GUIUtility.systemCopyBuffer = currentJoinCode;
            ShowSuccessMessage("Room code copied to clipboard!");
        }
        else if (networkManager != null && !string.IsNullOrEmpty(networkManager.CurrentJoinCode))
        {
            GUIUtility.systemCopyBuffer = networkManager.CurrentJoinCode;
            ShowSuccessMessage("Room code copied to clipboard!");
        }
    }

    private void OnCancelCreateButtonClicked()
    {
        Debug.Log("Cancel Create button clicked");
        if (networkManager != null)
        {
            networkManager.StopHost();
        }
        currentJoinCode = "";
        isProcessingRequest = false;
        ShowMainMenu();
    }

    private void OnEnterWaitingRoomClicked()
    {
        Debug.Log("Enter Waiting Room button clicked");
        ShowWaitingRoom();
    }

    private void OnConfirmJoinButtonClicked()
    {
        Debug.Log("OnConfirmJoinButtonClicked called");

        if (isProcessingRequest)
        {
            Debug.Log("Join request already in progress");
            return;
        }

        string roomCode = roomCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(roomCode))
        {
            ShowErrorMessage("Please enter a room code.");
            return;
        }

        StartJoinRoom(roomCode);
    }

    private async void StartJoinRoom(string roomCode)
    {
        isProcessingRequest = true;
        SetLoadingState(true);

        try
        {
            bool success = await networkManager.JoinGameAsync(roomCode);

            if (success)
            {
                ShowWaitingRoom();
            }
            else
            {
                ShowErrorMessage("Failed to join room. Please check the room code.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error joining room: " + e.Message);
            ShowErrorMessage("Error joining room: " + e.Message);
        }
        finally
        {
            SetLoadingState(false);
            isProcessingRequest = false;
        }
    }

    private void OnCancelJoinButtonClicked()
    {
        Debug.Log("Cancel Join button clicked");
        ShowMainMenu();
    }

    private void OnLeaveRoomButtonClicked()
    {
        Debug.Log("Leave Room button clicked");
        if (networkManager != null)
        {
            if (networkManager.IsHost)
            {
                networkManager.StopHost();
            }
            else
            {
                networkManager.DisconnectClient();
            }
        }

        currentJoinCode = "";
        ShowMainMenu();
    }

    private void OnRoomCodeInputChanged(string input)
    {
        string cleanInput = input.ToUpper();
        cleanInput = System.Text.RegularExpressions.Regex.Replace(cleanInput, @"[^A-Z0-9]", "");

        if (cleanInput != input)
        {
            roomCodeInput.text = cleanInput;
        }

        if (confirmJoinButton != null) confirmJoinButton.interactable = cleanInput.Length >= 4;
    }

    private void OnJoinCodeGenerated(string joinCode)
    {
        Debug.Log("OnJoinCodeGenerated called with code: " + joinCode);

        currentJoinCode = joinCode;

        if (roomCodeDisplay != null)
        {
            roomCodeDisplay.text = joinCode;
            Debug.Log("Room code display updated");
        }

        if (waitingText != null)
        {
            waitingText.text = "Room created! Share the code with your friend.";
            Debug.Log("Waiting text updated");
        }

        if (currentRoomCodeText != null)
        {
            currentRoomCodeText.text = "Room Code: " + joinCode;
        }

        // Enter Waiting Room 버튼 활성화
        if (enterWaitingRoomButton != null)
        {
            enterWaitingRoomButton.interactable = true;
            Debug.Log("Enter Waiting Room button enabled via event");
        }
    }

    private void OnConnectionStatusChanged(bool isConnected)
    {
        Debug.Log("Connection status changed: " + isConnected);
        if (isConnected)
        {
            UpdateWaitingRoomInfo();
        }
        else
        {
            // 연결이 끊어졌을 때만 메인 메뉴로 이동
            if (!isProcessingRequest) // 처리 중이 아닐 때만
            {
                ShowMainMenu();
            }
        }
    }

    private void OnNetworkError(string errorMessage)
    {
        Debug.LogError("Network error: " + errorMessage);
        ShowErrorMessage(errorMessage);

        // 에러 발생 시에만 메인 메뉴로 이동
        if (!isProcessingRequest)
        {
            ShowMainMenu();
        }
    }

    private void OnGameStarted()
    {
        ShowSuccessMessage("Game starting!");
    }

    private void UpdateWelcomeText()
    {
        if (welcomeText != null)
        {
            if (authManager != null && authManager.IsLoggedIn())
            {
                welcomeText.text = "Welcome, " + authManager.GetCurrentUserName() + "!";
            }
            else
            {
                welcomeText.text = "Welcome!";
            }
        }
    }

    private void UpdateWaitingRoomInfo()
    {
        if (networkManager != null && networkManager.IsConnected)
        {
            int connectedPlayers = Unity.Netcode.NetworkManager.Singleton?.ConnectedClients?.Count ?? 0;
            if (connectedPlayersText != null) connectedPlayersText.text = "Connected Players: " + connectedPlayers + "/2";

            string displayCode = !string.IsNullOrEmpty(currentJoinCode) ? currentJoinCode : networkManager.CurrentJoinCode;
            if (!string.IsNullOrEmpty(displayCode))
            {
                if (currentRoomCodeText != null) currentRoomCodeText.text = "Room Code: " + displayCode;
            }

            if (statusText != null)
            {
                if (networkManager.IsHost)
                {
                    statusText.text = "Host - Waiting for other players...";
                }
                else
                {
                    statusText.text = "Waiting for game to start...";
                }
            }
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(isLoading);

        // 처리 중일 때 버튼 비활성화
        bool buttonsEnabled = !isLoading && !isProcessingRequest;

        if (createRoomButton != null) createRoomButton.interactable = buttonsEnabled;
        if (joinRoomButton != null) joinRoomButton.interactable = buttonsEnabled;
        if (logoutButton != null) logoutButton.interactable = buttonsEnabled;
        if (confirmJoinButton != null) confirmJoinButton.interactable = buttonsEnabled && (roomCodeInput != null && roomCodeInput.text.Length >= 4);
        if (cancelJoinButton != null) cancelJoinButton.interactable = buttonsEnabled;
        if (cancelCreateButton != null) cancelCreateButton.interactable = buttonsEnabled;
        if (leaveRoomButton != null) leaveRoomButton.interactable = buttonsEnabled;
    }

    private void ShowErrorMessage(string message)
    {
        Debug.LogWarning("Error message: " + message);
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
            errorText.gameObject.SetActive(true);

            Invoke(nameof(ClearErrorText), 4f);
        }
    }

    private void ShowSuccessMessage(string message)
    {
        Debug.Log("Success message: " + message);
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.green;
            errorText.gameObject.SetActive(true);

            Invoke(nameof(ClearErrorText), 3f);
        }
    }

    private void ClearErrorText()
    {
        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
            errorText.text = "";
        }
    }

    private void OnButtonClick(System.Action buttonAction)
    {
        if (enableSounds && buttonClickSound != null)
        {
            buttonClickSound.Play();
        }

        buttonAction?.Invoke();
    }

    private void Update()
    {
        if (waitingRoomPanel != null && waitingRoomPanel.activeInHierarchy)
        {
            UpdateWaitingRoomInfo();
        }
    }
}