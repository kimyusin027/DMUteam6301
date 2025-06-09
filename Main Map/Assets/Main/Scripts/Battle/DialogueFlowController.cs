using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueFlowController : MonoBehaviour
{
    public TextMeshProUGUI dialogueText;       // 질문 텍스트
    public TextMeshProUGUI dialogueFlowText;   // 선택에 따른 대사 출력
    public TextMeshProUGUI timerText;
    public Button threatenButton;
    public Button persuadeButton;
    public Button ignoreButton;

    public float timeLimit = 15f;
    private float remainingTime;
    private Coroutine timerCoroutine;
    private bool isInChoiceStage = false;

    void Start()
    {
        ShowMainChoice();
    }

    void ShowMainChoice()
    {
        dialogueText.text = "당신은 어떤 행동을 할까요?";
        dialogueFlowText.text = "";
        isInChoiceStage = true;

        ShowButtons(true, "대화한다", "공격한다", "도망을 시도한다");

        threatenButton.onClick.RemoveAllListeners();
        persuadeButton.onClick.RemoveAllListeners();
        ignoreButton.onClick.RemoveAllListeners();

        threatenButton.onClick.AddListener(OnTalkSelected);
        persuadeButton.onClick.AddListener(OnAttackSelected);
        ignoreButton.onClick.AddListener(OnRunSelected);

        ResetTimer();
    }

    void OnTalkSelected()
    {
        dialogueText.text = "어떤 방식으로 대화를 시도할까요?";
        dialogueFlowText.text = "";
        isInChoiceStage = true;

        ShowButtons(true, "협박", "회유", "무시");

        threatenButton.onClick.RemoveAllListeners();
        persuadeButton.onClick.RemoveAllListeners();
        ignoreButton.onClick.RemoveAllListeners();

        threatenButton.onClick.AddListener(() => OnSubDialogue("협박"));
        persuadeButton.onClick.AddListener(() => OnSubDialogue("회유"));
        ignoreButton.onClick.AddListener(() => OnSubDialogue("무시"));

        ResetTimer();
    }

    void OnSubDialogue(string choice)
    {
        isInChoiceStage = true;

        switch (choice)
        {
            case "협박":
                dialogueFlowText.text = "너 따위는 상대가 안 돼. 무릎 꿇지 않으면 끝장을 보게 될 거야!";
                break;
            case "회유":
                dialogueFlowText.text = "싸우지 말고 서로 살아남을 길을 찾아보자...";
                break;
            case "무시":
                dialogueFlowText.text = "흥, 너 따위는 신경도 안 써.";
                break;
        }

        dialogueText.text = "어떻게 하시겠습니까?";
        ShowButtons(true, "계속한다", "말을 바꾼다", "");

        threatenButton.onClick.RemoveAllListeners();
        persuadeButton.onClick.RemoveAllListeners();
        ignoreButton.onClick.RemoveAllListeners();

        threatenButton.onClick.AddListener(() => OnDialogueResult(true));
        persuadeButton.onClick.AddListener(() => ShowMainChoice()); // 말 바꿔 다시 선택

        ignoreButton.gameObject.SetActive(false); // 세 번째 버튼 숨김

        ResetTimer();
    }

    void OnDialogueResult(bool successTry)
    {
        isInChoiceStage = false;
        StopTimer();

        if (successTry)
        {
            bool success = Random.value < 0.6f;

            dialogueText.text = success ? "성공!" : "실패!";
            dialogueFlowText.text = success
                ? "<color=green>적이 겁을 먹고 약해졌습니다! (공격력 다운)</color>"
                : "<color=red>적이 분노했습니다! (공격력 증가)</color>";

            // 플레이어 2 미니게임 성공 시뮬레이션
            bool player2Success = Random.value < 0.8f;

            EnemyInfo info = EnemyInfoProvider.Instance.GenerateInfo();
            string formatted = EnemyInfoProvider.Instance.FormatInfoText(info, player2Success);

            dialogueFlowText.text += "\n\n" + formatted;

            // ✅  GameManager를 통해 ClientRpc로 정보 전달
            GameManager.Instance.SendEnemyInfoToClientRpc(
                JsonUtility.ToJson(info),
                player2Success
            );
        }

        ShowButtons(false);
        Invoke(nameof(EnterBattle), 2f);
    }

    void OnAttackSelected()
    {
        StopTimer();
        dialogueText.text = "당신은 적을 공격하려 합니다!";
        dialogueFlowText.text = "<color=green>선공 기회를 얻었습니다!</color>";
        ShowButtons(false);
        Invoke(nameof(EnterBattle), 2f);
    }

    void OnRunSelected()
    {
        StopTimer();
        dialogueText.text = "당신은 도망치려 시도합니다...";
        dialogueFlowText.text = "";
        ShowButtons(false);
        Invoke(nameof(EnterBattle), 2f);
    }

    void ShowButtons(bool show, string text1 = "", string text2 = "", string text3 = "")
    {
        threatenButton.gameObject.SetActive(show);
        persuadeButton.gameObject.SetActive(show);
        ignoreButton.gameObject.SetActive(show && !string.IsNullOrEmpty(text3));

        threatenButton.GetComponentInChildren<TextMeshProUGUI>().text = text1;
        persuadeButton.GetComponentInChildren<TextMeshProUGUI>().text = text2;

        if (!string.IsNullOrEmpty(text3))
            ignoreButton.GetComponentInChildren<TextMeshProUGUI>().text = text3;
    }

    void ResetTimer()
    {
        StopTimer();
        remainingTime = timeLimit;
        timerCoroutine = StartCoroutine(TimerCountdown());
    }

    void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
    }

    IEnumerator TimerCountdown()
    {
        while (remainingTime > 0 && isInChoiceStage)
        {
            remainingTime -= Time.deltaTime;
            timerText.text = $"남은 시간: <b>{Mathf.CeilToInt(remainingTime)}초</b>";
            yield return null;
        }

        if (isInChoiceStage)
        {
            dialogueText.text = "시간 초과!";
            dialogueFlowText.text = "<color=red>적이 선공 기회를 얻었습니다!</color>";
            ShowButtons(false);
            Invoke(nameof(EnterBattle), 2f);
        }
    }

    void EnterBattle()
    {
        Debug.Log("→ 전투 씬 진입 또는 턴 전환 시작");
        // 추후 BattleManager.StartBattlePhase(); 연결 가능
    }
}
