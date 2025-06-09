using TMPro;
using UnityEngine;

public class EnemyInfoUIController : MonoBehaviour
{
    public static EnemyInfoUIController Instance;

    [SerializeField] private TextMeshProUGUI enemyInfoText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ShowEnemyInfo(string infoText)
    {
        if (enemyInfoText != null)
        {
            enemyInfoText.text = infoText;
            enemyInfoText.gameObject.SetActive(true);
        }
    }

    public void HideEnemyInfo()
    {
        if (enemyInfoText != null)
        {
            enemyInfoText.gameObject.SetActive(false);
        }
    }
}
