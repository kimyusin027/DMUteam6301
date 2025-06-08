using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class NetworkEscManager : NetworkBehaviour
{
    public GameObject CancelPanel;
    private bool activeCancel = false;

    // 이 ESC Manager가 어떤 플레이어용인지 식별
    [Header("Player Assignment")]
    public bool isForPlayer1 = true; // Player (1)용이면 true, Player (2)용이면 false

    private void Start()
    {
        // 초기 상태: Canvas 비활성화
        if (CancelPanel != null)
        {
            CancelPanel.SetActive(false);
            activeCancel = false;
        }

        Debug.Log($"NetworkEscManager Start - {gameObject.name}, IsForPlayer1: {isForPlayer1}, Panel: {CancelPanel?.name}");
    }

    public override void OnNetworkSpawn()
    {
        // 각 플레이어가 자신의 ESC Manager만 조작할 수 있도록 설정
        bool shouldControlThisManager = false;

        if (isForPlayer1 && IsHost) // Player (1)용 Manager는 호스트가 조작
        {
            shouldControlThisManager = true;
        }
        else if (!isForPlayer1 && IsClient && !IsHost) // Player (2)용 Manager는 클라이언트가 조작
        {
            shouldControlThisManager = true;
        }

        if (!shouldControlThisManager)
        {
            // 다른 플레이어의 ESC Manager는 비활성화
            enabled = false;
        }

        Debug.Log($"NetworkEscManager OnNetworkSpawn - {gameObject.name}, ShouldControl: {shouldControlThisManager}, IsHost: {IsHost}");
    }

    private void Update()
    {
        // enabled가 false면 실행되지 않음 (다른 플레이어의 Manager)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleEscMenu();
        }
    }

    private void ToggleEscMenu()
    {
        activeCancel = !activeCancel;

        if (CancelPanel != null)
        {
            CancelPanel.SetActive(activeCancel);
        }

        // 커서 상태 변경
        Cursor.lockState = activeCancel ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = activeCancel;

        Debug.Log($"ESC Menu toggled - {gameObject.name}, Active: {activeCancel}");
    }

    public void GameSave()
    {
        // 멀티플레이어에서는 저장 기능 비활성화
        Debug.Log("Save functionality disabled in multiplayer");
        CloseMenu();
    }

    public void GameLoad()
    {
        // 멀티플레이어에서는 로드 기능 비활성화
        Debug.Log("Load functionality disabled in multiplayer");
        CloseMenu();
    }

    public void GameExit()
    {
        // 네트워크 게임 종료
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // 로비로 돌아가기
        SceneManager.LoadScene("LobbyScene");
    }

    public void CloseMenu()
    {
        activeCancel = false;
        if (CancelPanel != null)
            CancelPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log($"ESC Menu closed - {gameObject.name}");
    }
}