using UnityEngine;

public class EnemyInfoProvider : MonoBehaviour
{
    public static EnemyInfoProvider Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public EnemyInfo GenerateInfo()
    {
        return new EnemyInfo
        {
            enemyName = "Valkyrie-SX",
            type = "로봇",
            weakness = "좌측 회로",
            mapHint = "북쪽 방향에 탈출구 존재"
        };
    }

    public string FormatInfoText(EnemyInfo info, bool detailed)
    {
        if (!detailed)
            return "<color=yellow>일부 정보만 수신되었습니다 (해킹 실패)</color>";

        return $"<b>적 해킹 정보</b>\n" +
               $"- 이름: <color=cyan>{info.enemyName}</color>\n" +
               $"- 유형: <color=orange>{info.type}</color>\n" +
               $"- 약점: <color=red>{info.weakness}</color>\n" +
               $"- 맵 정보: <color=yellow>{info.mapHint}</color>";
    }
}
