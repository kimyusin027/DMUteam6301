using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Player2UIController : MonoBehaviour
{
    [Header("체력 관련")]
    public Image myHpBar;
    public Image onePHpBar;

    //[Header("상태 아이콘")]
    //public GameObject icon_Downed;
    //public GameObject icon_Fighting;

    [Header("상호작용 및 미션")]
    public TextMeshProUGUI interactPromptText;
    public TextMeshProUGUI missionText;

    [Header("미니맵")]
    public GameObject miniMapRoot;

    [Header("핑 시스템")]
    public GameObject pingPrefab;
    public Transform pingParent;

    [Header("협동 알림")]
    public TextMeshProUGUI cooperationAlertText;

    // 체력 UI 갱신
    public void UpdateMyHP(float ratio)
    {
        myHpBar.fillAmount = Mathf.Clamp01(ratio);
    }

    public void Update1PHP(float ratio)
    {
        onePHpBar.fillAmount = Mathf.Clamp01(ratio);
    }

    // 상태 아이콘
    //public void Set1PStatus(string status)
    //{
    //    icon_Downed.SetActive(status == "Downed");
    //    icon_Fighting.SetActive(status == "Fighting");
    //}

    // 상호작용 안내
    public void ShowInteractPrompt(string prompt)
    {
        interactPromptText.text = prompt;
        interactPromptText.gameObject.SetActive(true);
    }

    public void HideInteractPrompt()
    {
        interactPromptText.gameObject.SetActive(false);
    }

    // 미션 텍스트 갱신
    public void SetMissionText(string msg)
    {
        missionText.text = msg;
    }

    // 핑 찍기
    public void CreatePing(Vector3 worldPosition)
    {
        GameObject ping = Instantiate(pingPrefab, pingParent);
        ping.transform.position = worldPosition;
        Destroy(ping, 3f); // 3초 후 자동 제거
    }

    // 협동 알림
    public void ShowCooperationAlert(string msg, float duration = 2f)
    {
        StartCoroutine(ShowAlertCoroutine(msg, duration));
    }

    private System.Collections.IEnumerator ShowAlertCoroutine(string msg, float t)
    {
        cooperationAlertText.text = msg;
        cooperationAlertText.gameObject.SetActive(true);
        yield return new WaitForSeconds(t);
        cooperationAlertText.gameObject.SetActive(false);
    }
}