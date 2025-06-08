using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyProximityDetector : MonoBehaviour
{
    public float detectionRadius = 5f;         // 감지 반경
    public string targetSceneName = "Test"; // 전환할 씬 이름
    public string enemyTag = "Enemy";          // 적 태그

    private bool triggered = false;

    void Update()
    {
        if (triggered) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance <= detectionRadius)
            {
                Debug.Log("적 근처 감지됨. 씬 전환!");
                triggered = true;
                SceneManager.LoadScene(targetSceneName);
                break;
            }
        }
    }
}
