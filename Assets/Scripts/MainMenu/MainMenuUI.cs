using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;

    void Awake()
    {
        startButton.onClick.AddListener(OnStartGame);
        exitButton.onClick.AddListener(OnExitGame);

        // 按钮悬停音效
        var startTrigger = startButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var startHoverEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        startHoverEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        startHoverEntry.callback.AddListener((_) => AudioManager.Instance.Play(SFX.UIHover));
        startTrigger.triggers.Add(startHoverEntry);

        var exitTrigger = exitButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var exitHoverEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        exitHoverEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        exitHoverEntry.callback.AddListener((_) => AudioManager.Instance.Play(SFX.UIHover));
        exitTrigger.triggers.Add(exitHoverEntry);

        // 播放大厅 BGM
        AudioManager.Instance.PlayBGM(BGM.MainMenu);
    }

    void OnStartGame()
    {
        AudioManager.Instance.Play(SFX.UIClick);
        AudioManager.Instance.PlayBGM(BGM.Exploration);
        SceneManager.LoadScene("SampleScene");
    }

    void OnExitGame()
    {
        AudioManager.Instance.Play(SFX.UIConfirmExit);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
