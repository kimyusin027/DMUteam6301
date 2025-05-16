using UnityEngine;

public class FPS : MonoBehaviour
{
    private float deltaTime = 0f;
    
    static bool Result = true;

    [SerializeField, Range(1, 100)]
    private int size = 25;

    [SerializeField]
    private Color color = Color.gray;

    public bool isShow = false;

    void Start()
    {
        Application.targetFrameRate = 480;
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            isShow = !isShow;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Application.targetFrameRate = 60;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Application.targetFrameRate = 144;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Application.targetFrameRate = 240;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Application.targetFrameRate = 480;
        }
    }

    private void OnGUI()
    {
        if (isShow)
        {
            GUIStyle style = new GUIStyle();

            Rect rect = new Rect(30, 30, Screen.width, Screen.height);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = size;
            style.normal.textColor = color;

            float ms = deltaTime * 1000f;
            float fps = 1.0f / deltaTime;
            string text = string.Format("{0:0.} FPS ({1:0.0} ms)", fps, ms);

            GUI.Label(rect, text, style);
        }
    }
}
