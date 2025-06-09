using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueTimerController : MonoBehaviour
{
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI timerText;
    public Button threatenButton;
    public Button persuadeButton;
    public Button ignoreButton;

    public float timeLimit = 15f; // 제한 시간 (초)

    private float remainingTime;
    private bool isChoiceMade = false;

    void Start()
    {
        // 초기화
        dialogueText.text = "당신은 어떤 말을 건넬까요?";
        remainingTime = timeLimit;

        // 버튼에 함수 연결
        threatenButton.onClick.AddListener(() => OnChoiceSelected("협박"));
        persuadeButton.onClick.AddListener(() => OnChoiceSelected("회유"));
        ignoreButton.onClick.AddListener(() => OnChoiceSelected("무시"));

        StartCoroutine(StartTimer());
    }

    IEnumerator StartTimer()
    {
        while (remainingTime > 0 && !isChoiceMade)
        {
            remainingTime -= Time.deltaTime;
            timerText.text = $"남은 시간: {Mathf.CeilToInt(remainingTime)}초";
            yield return null;
        }

        if (!isChoiceMade)
        {
            OnTimeout();
        }
    }

    void OnChoiceSelected(string choice)
    {
        isChoiceMade = true;
        Debug.Log($"플레이어가 '{choice}' 선택함");

        // 이곳에서 선택에 따라 적에게 디버프 적용
        ApplyDebuff(choice);

        // 추후 네트워크 전송도 여기서
        // NetworkManager.SendChoiceToOtherPlayer(choice);
    }

    void OnTimeout()
    {
        Debug.Log("시간 초과! 선택하지 않음. 디폴트 처리.");
        ApplyDefaultOutcome();
    }

    void ApplyDebuff(string choice)
    {
        // 예시: 선택지에 따라 적에게 다른 디버프 적용
        switch (choice)
        {
            case "협박":
                Debug.Log("→ 적 공격력 강화!");
                break;
            case "회유":
                Debug.Log("→ 대화문으로 전환");
                break;
            case "무시":
                Debug.Log("→ 아무일도 벌어지지 않았다");
                break;
        }

        // 이후 UI 닫기, 턴 넘기기 등 처리
    }

    void ApplyDefaultOutcome()
    {
        Debug.Log("→ 적이 오히려 버프를 받음!");
        // 예: 적의 방어력 +10%
    }
}
