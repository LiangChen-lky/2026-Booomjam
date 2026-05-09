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
        AudioManager.Instance.Play(SFX.UIClick);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
