using UnityEngine;

public class BattleSceneLoader : MonoBehaviour
{
    void Start()
    {
        // 1. 배경 생성
        GameObject background = Resources.Load<GameObject>("Backgrounds/Battle_Background");
        if (background != null)
            Instantiate(background, Vector3.zero, Quaternion.identity);

        // 2. 플레이어 생성
        GameObject player = Resources.Load<GameObject>("Player/Player_Unit");
        if (player != null)
            Instantiate(player, new Vector3(-3, 0, 0), Quaternion.identity);

        // 3. 적 생성
        string enemyName = BattleManager.selectedEnemyName;
        if (!string.IsNullOrEmpty(enemyName))
        {
            GameObject enemyPrefab = Resources.Load<GameObject>("Enemies/" + enemyName);
            if (enemyPrefab != null)
            {
                Instantiate(enemyPrefab, new Vector3(3, 0, 0), Quaternion.identity);
            }
            else
            {
                Debug.LogError("프리팹을 찾을 수 없습니다: " + enemyName);
            }
        }
    }
}
