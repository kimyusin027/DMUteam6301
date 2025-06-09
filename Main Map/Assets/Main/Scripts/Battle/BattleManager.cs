using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    public static string selectedEnemyName;

    public static void StartBattle(string enemyName)
    {
        selectedEnemyName = enemyName;
        SceneManager.LoadScene("2DBattle");
    }
}
