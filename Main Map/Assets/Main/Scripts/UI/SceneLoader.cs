using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // 메인 메뉴 UI 전체를 참조 (예: Canvas 등)
    public GameObject uiRoot;

    public void LoadWaitingRoom()
    {
        // 1. UI 즉시 비활성화
        if (uiRoot != null)
        {
            uiRoot.SetActive(false);
        }

        // 2. WaitingRoom 씬을 Additive 로드 (비동기)
        SceneManager.LoadSceneAsync("WaitingRoom", LoadSceneMode.Additive).completed += (op) =>
        {
            // 3. WaitingRoom 씬을 활성화 씬으로 설정
            Scene loadedScene = SceneManager.GetSceneByName("WaitingRoom");
            SceneManager.SetActiveScene(loadedScene);

            // 4. MainMenu 씬 언로드
            SceneManager.UnloadSceneAsync("MainMenu");
        };
    }
}
