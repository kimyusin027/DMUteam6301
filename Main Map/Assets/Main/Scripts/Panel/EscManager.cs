using UnityEngine;

public class EscManager : MonoBehaviour
{
    public GameObject Player;
    public GameObject CancelPanel;
    bool activeCancel = false;

    private void Start()
    {
        CancelPanel.SetActive(activeCancel);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            activeCancel = !activeCancel;
            CancelPanel.SetActive(activeCancel);
        }
    }

    public void GameSave()
    {
        PlayerPrefs.SetFloat("PlayerX", Player.transform.position.x);
        PlayerPrefs.SetFloat("PlayerY", Player.transform.position.y);
        PlayerPrefs.SetFloat("PlayerZ", Player.transform.position.z);
        PlayerPrefs.Save();

        CancelPanel.SetActive(false);
    }

    public void GameLoad()
    {
        if (!PlayerPrefs.HasKey("PlayerX"))
            return;
        float x = PlayerPrefs.GetFloat("PlayerX");
        float y = PlayerPrefs.GetFloat("PlayerY");
        float z = PlayerPrefs.GetFloat("PlayerZ");

        Player.transform.position = new Vector3(x, y, z);

        CancelPanel.SetActive(false);
    }

    public void GameExit()
    {
        Application.Quit();
    }
}
