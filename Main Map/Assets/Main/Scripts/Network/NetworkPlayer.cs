using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Collections;

public class NetworkPlayer : NetworkBehaviour
{
    [Header("Player Components")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject playerModel;
    [SerializeField] private Material[] playerMaterials;
    [SerializeField] private Camera playerCamera; // 프리펩에 포함된 카메라
    [SerializeField] private AudioListener audioListener; // 프리펩에 포함된 오디오 리스너

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();
    private NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>();

    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isGrounded;

    private CharacterController characterController;
    private Renderer playerRenderer;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (playerModel != null)
        {
            playerRenderer = playerModel.GetComponent<Renderer>();
        }

        // CharacterController 설정 최적화
        if (characterController != null)
        {
            characterController.stepOffset = 0.3f;
            characterController.slopeLimit = 45f;
            characterController.skinWidth = 0.08f;
        }

        // 프리펩에서 카메라와 오디오 리스너 자동 찾기 (할당되지 않은 경우)
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        if (audioListener == null)
            audioListener = GetComponentInChildren<AudioListener>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"=== OnNetworkSpawn called for ClientID: {OwnerClientId}, IsOwner: {IsOwner} ===");

        networkPosition.OnValueChanged += OnPositionChanged;
        networkRotation.OnValueChanged += OnRotationChanged;
        playerName.OnValueChanged += OnPlayerNameChanged;

        if (IsOwner)
        {
            Debug.Log($"Setting up LOCAL player for ClientID: {OwnerClientId}");
            SetupLocalPlayer();

            string currentPlayerName = AuthenticationManager.Instance?.GetCurrentUserName() ?? $"Player{OwnerClientId}";
            SetPlayerNameServerRpc(currentPlayerName);

            // 스폰 위치 설정
            StartCoroutine(SetSpawnPositionCoroutine());
        }
        else
        {
            Debug.Log($"Setting up REMOTE player for ClientID: {OwnerClientId}");
            SetupRemotePlayer();
        }

        SetPlayerColor();

        Debug.Log($"NetworkPlayer spawned - ClientID: {OwnerClientId}, IsOwner: {IsOwner}, Position: {transform.position}");
    }

    public override void OnNetworkDespawn()
    {
        networkPosition.OnValueChanged -= OnPositionChanged;
        networkRotation.OnValueChanged -= OnRotationChanged;
        playerName.OnValueChanged -= OnPlayerNameChanged;
    }

    private void SetupLocalPlayer()
    {
        Debug.Log($"SetupLocalPlayer called for ClientID: {OwnerClientId}");

        // 모든 다른 카메라들을 비활성화
        DisableAllOtherCameras();

        // 자신의 카메라와 오디오 리스너 활성화
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            playerCamera.tag = "MainCamera";
            Debug.Log($"Local player camera activated for ClientID: {OwnerClientId}");
        }
        else
        {
            Debug.LogError($"Player camera not found for ClientID: {OwnerClientId}");
        }

        if (audioListener != null)
        {
            // 다른 모든 AudioListener 비활성화
            DisableAllOtherAudioListeners();
            audioListener.enabled = true;
            Debug.Log($"Local player audio listener activated for ClientID: {OwnerClientId}");
        }
        else
        {
            Debug.LogError($"Audio listener not found for ClientID: {OwnerClientId}");
        }
    }

    private void SetupRemotePlayer()
    {
        Debug.Log($"SetupRemotePlayer called for ClientID: {OwnerClientId}");

        // 원격 플레이어의 카메라와 오디오 리스너 비활성화
        if (playerCamera != null)
        {
            playerCamera.enabled = false;
            Debug.Log($"Remote player camera disabled for ClientID: {OwnerClientId}");
        }

        if (audioListener != null)
        {
            audioListener.enabled = false;
            Debug.Log($"Remote player audio listener disabled for ClientID: {OwnerClientId}");
        }

        // 플레이어 모델은 보이도록 유지
        if (playerModel != null)
        {
            playerModel.SetActive(true);
        }
    }

    private void DisableAllOtherCameras()
    {
        // 씬의 모든 카메라를 찾아서 자신의 카메라가 아닌 경우 비활성화
        Camera[] allCameras = FindObjectsOfType<Camera>(true);
        foreach (Camera cam in allCameras)
        {
            if (cam != playerCamera && cam.gameObject.scene.isLoaded)
            {
                cam.enabled = false;
                Debug.Log($"Disabled other camera: {cam.name}");
            }
        }
    }

    private void DisableAllOtherAudioListeners()
    {
        // 씬의 모든 AudioListener를 찾아서 자신의 것이 아닌 경우 비활성화
        AudioListener[] allListeners = FindObjectsOfType<AudioListener>(true);
        foreach (AudioListener listener in allListeners)
        {
            if (listener != audioListener && listener.gameObject.scene.isLoaded)
            {
                listener.enabled = false;
                Debug.Log($"Disabled other audio listener: {listener.name}");
            }
        }
    }

    private System.Collections.IEnumerator SetSpawnPositionCoroutine()
    {
        // 네트워크 동기화를 위해 잠시 대기
        yield return new WaitForSeconds(0.1f);

        // GameManager가 이미 올바른 위치에 스폰했으므로 현재 위치 그대로 사용
        Vector3 currentPosition = transform.position;

        // 서로를 마주보도록 회전 설정
        Quaternion spawnRotation = GetFacingRotation();
        transform.rotation = spawnRotation;

        // 속도 완전 초기화
        velocity = Vector3.zero;

        // CharacterController 리셋
        if (characterController != null)
        {
            characterController.enabled = false;
            yield return null; // 한 프레임 대기
            characterController.enabled = true;
        }

        // 서버에 위치와 회전 업데이트 (현재 위치 유지)
        if (IsServer)
        {
            networkPosition.Value = currentPosition;
            networkRotation.Value = spawnRotation;
        }
        else
        {
            UpdatePositionServerRpc(currentPosition, spawnRotation);
        }

        Debug.Log($"Player {OwnerClientId} spawned at GameManager position: {currentPosition}, rotation: {spawnRotation.eulerAngles}");
    }

    private Quaternion GetFacingRotation()
    {
        // Z축 기준으로 서로를 마주보도록 회전
        if (OwnerClientId == 0)
        {
            // 호스트(z:-300): +Z 방향(북쪽)을 바라봄 (클라이언트 쪽으로)
            return Quaternion.LookRotation(Vector3.forward);
        }
        else
        {
            // 클라이언트(z:150): -Z 방향(남쪽)을 바라봄 (호스트 쪽으로)
            return Quaternion.LookRotation(Vector3.back);
        }
    }

    private void SetPlayerColor()
    {
        if (playerRenderer != null && playerMaterials.Length > 0)
        {
            int materialIndex = (int)(OwnerClientId % (ulong)playerMaterials.Length);
            playerRenderer.material = playerMaterials[materialIndex];
            Debug.Log($"Player {OwnerClientId} color set to material index: {materialIndex}");
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        CheckGrounded();
        HandleInput();
        HandleMovement();

        if (HasMoved())
        {
            UpdatePositionServerRpc(transform.position, transform.rotation);
        }
    }

    private void CheckGrounded()
    {
        // 단순화: CharacterController의 isGrounded만 사용
        isGrounded = characterController != null && characterController.isGrounded;
    }

    private void HandleInput()
    {
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");
    }

    private void HandleMovement()
    {
        if (characterController == null || !characterController.enabled) return;

        // 수평 이동만 처리 (단순화)
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;

        // 이동 벡터 계산
        Vector3 moveVector = moveDirection * moveSpeed * Time.deltaTime;

        // 회전 처리
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 단순한 중력 처리
        if (characterController.isGrounded)
        {
            // 바닥에 있을 때는 중력을 거의 0으로
            velocity.y = -0.5f;
        }
        else
        {
            // 공중에 있을 때만 중력 적용
            velocity.y += -9.81f * Time.deltaTime;
        }

        // 최종 이동: 수평 이동 + 수직 이동(중력)
        Vector3 finalMove = moveVector + new Vector3(0, velocity.y * Time.deltaTime, 0);

        // 이동 적용
        characterController.Move(finalMove);

        // 비정상적인 상황 체크 및 수정
        if (transform.position.y < -10f) // 너무 아래로 떨어진 경우
        {
            Debug.LogWarning($"Player {OwnerClientId} fell too low, resetting position");
            Vector3 resetPos = new Vector3(transform.position.x, 110f, transform.position.z); // Y=110으로 리셋 (바닥 높이)
            transform.position = resetPos;
            velocity = Vector3.zero;
            UpdatePositionServerRpc(resetPos, transform.rotation);
        }
    }

    private bool HasMoved()
    {
        return Vector3.Distance(transform.position, networkPosition.Value) > 0.01f ||
               Quaternion.Angle(transform.rotation, networkRotation.Value) > 1f;
    }

    [ServerRpc]
    private void UpdatePositionServerRpc(Vector3 position, Quaternion rotation)
    {
        networkPosition.Value = position;
        networkRotation.Value = rotation;
    }

    [ServerRpc]
    private void SetPlayerNameServerRpc(string name)
    {
        playerName.Value = name;
    }

    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        if (!IsOwner)
        {
            StartCoroutine(InterpolatePosition(newValue));
        }
    }

    private void OnRotationChanged(Quaternion oldValue, Quaternion newValue)
    {
        if (!IsOwner)
        {
            StartCoroutine(InterpolateRotation(newValue));
        }
    }

    private void OnPlayerNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        if (playerNameText != null)
        {
            playerNameText.text = newValue.ToString();
        }
        Debug.Log($"Player name changed: {newValue} (ClientID: {OwnerClientId})");
    }

    private System.Collections.IEnumerator InterpolatePosition(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;
        float interpolationTime = 0.1f;

        while (elapsedTime < interpolationTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / interpolationTime;

            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
    }

    private System.Collections.IEnumerator InterpolateRotation(Quaternion targetRotation)
    {
        Quaternion startRotation = transform.rotation;
        float elapsedTime = 0f;
        float interpolationTime = 0.1f;

        while (elapsedTime < interpolationTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / interpolationTime;

            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.rotation = targetRotation;
    }

    public string GetPlayerName()
    {
        return playerName.Value.ToString();
    }

    public new bool IsLocalPlayer()
    {
        return IsOwner;
    }

    // 디버그용 메서드
    [ContextMenu("Teleport to Safe Position")]
    private void TeleportToSafePosition()
    {
        if (IsOwner)
        {
            // 현재 위치에서 Y만 바닥 높이로 조정
            Vector3 safePosition = new Vector3(transform.position.x, 110f, transform.position.z);
            transform.position = safePosition;
            velocity = Vector3.zero;
            UpdatePositionServerRpc(safePosition, transform.rotation);
            Debug.Log($"Player {OwnerClientId} teleported to safe position: {safePosition}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (characterController != null)
        {
            // Ground check 시각화 (단순화)
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 spherePosition = transform.position - Vector3.up * (characterController.height * 0.5f);
            Gizmos.DrawWireSphere(spherePosition, characterController.radius);
        }
    }
}